using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckTranslation;

internal sealed class GlossaryService : IGlossaryService
{
    private static readonly string FilePath = Path.Combine(AppConfig.ConfigDirectory, "glossary.json");
    private const int ExtractionBatchSize = 20;

    // Statuts en toutes lettres dans le JSON (lisible par un humain, robuste aux réordonnancements
    // de l'enum) ; EntriesByLanguage nul non réécrit : le fichier migre en v2 à la première sauvegarde.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly object _lock = new();
    private Glossary _glossary;
    private bool _loaded;
    // Vrai si glossary.json existe mais n'a pas pu être lu : Save refuse alors d'écrire, sans quoi
    // un glossaire vide de repli écraserait des données existantes.
    private bool _loadFailed;

    public GlossaryService()
    {
        _glossary = new Glossary();
    }

    /// <summary>
    /// Projection du glossaire transversal sur une langue : les termes qui portent une traduction
    /// non vide pour ce code, sous la forme historique (Source / Destination / Contexte). C'est la
    /// seule lecture par langue — l'éditeur actuel et l'extraction continuent de fonctionner sans
    /// rien savoir du schéma transversal.
    /// </summary>
    public IReadOnlyList<GlossaryEntry> GetEntries(string languageCode)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return ProjectLocked(languageCode, validatedOnly: false);
        }
    }

    /// <summary>
    /// La liste devient exactement l'ensemble des entrées de cette langue : les traductions
    /// citées sont posées sur leur terme (créé au besoin), celles qui ne le sont plus sont
    /// retirées. Un terme qui ne porte plus aucune traduction disparaît. Une écriture par
    /// l'éditeur est une décision humaine : le terme passe <see cref="GlossaryTermStatus.Validated"/>.
    /// </summary>
    public void ReplaceEntries(string languageCode, IReadOnlyList<GlossaryEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        EnsureLoaded();
        lock (_lock)
        {
            var cleaned = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Source) && !string.IsNullOrWhiteSpace(e.Destination))
                .ToList();

            // Clés de conservation sur la même normalisation que le stockage : avec un simple
            // Trim, une Source contenant un retour à la ligne serait ajoutée normalisée puis
            // jugée absente par la boucle de suppression, et sa traduction retirée aussitôt.
            var keptSources = new HashSet<string>(cleaned.Select(e => NormalizeCell(e.Source)), StringComparer.OrdinalIgnoreCase);

            foreach (var entry in cleaned)
            {
                var term = FindTermLocked(entry.Source);
                if (term is null)
                {
                    term = new GlossaryTerm { Source = NormalizeCell(entry.Source) };
                    _glossary.Terms.Add(term);
                }

                // Normalisé à l'écriture (espaces de bord, retours à la ligne) : le stockage,
                // l'empreinte et le rendu du prompt restent alignés. La migration v1, elle, copie
                // verbatim pour préserver les empreintes existantes.
                term.Translations[languageCode] = NormalizeCell(entry.Destination);
                // Le contexte est désormais commun à toutes les langues : dernier éditeur gagnant,
                // comme pour n'importe quel champ partagé.
                term.Context = NormalizeCell(entry.Context);
                term.Status = GlossaryTermStatus.Validated;
            }

            foreach (var term in _glossary.Terms)
            {
                if (!keptSources.Contains(NormalizeCell(term.Source)))
                    term.Translations.Remove(languageCode);
            }

            _glossary.Terms.RemoveAll(term => term.Translations.Count == 0);
        }
    }

    public IReadOnlyList<GlossaryTerm> GetTerms()
    {
        EnsureLoaded();
        lock (_lock)
        {
            return _glossary.Terms.Select(CloneTerm).ToList();
        }
    }

    public void ReplaceTerms(IReadOnlyList<GlossaryTerm> terms)
    {
        EnsureLoaded();
        lock (_lock)
        {
            var cleaned = new List<GlossaryTerm>();
            var seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var term in terms)
            {
                if (term is null || string.IsNullOrWhiteSpace(term.Source))
                    continue;

                var copy = CloneTerm(term);
                // Source normalisée comme les autres champs (retours à la ligne compris) : c'est
                // la clé d'identité du terme, la déduplication et les prompts en dépendent. Un
                // doublon après normalisation est écarté (premier gagnant) — l'éditeur les refuse
                // déjà, ceci protège les autres appelants.
                copy.Source = NormalizeCell(copy.Source);
                if (!seenSources.Add(copy.Source))
                    continue;

                copy.Context = NormalizeCell(copy.Context);
                copy.ReviewerComment = NormalizeCell(copy.ReviewerComment);

                foreach (var (code, destination) in copy.Translations.ToList())
                {
                    var normalized = NormalizeCell(destination);
                    if (normalized.Length == 0)
                        copy.Translations.Remove(code);
                    else
                        copy.Translations[code] = normalized;
                }

                cleaned.Add(copy);
            }

            _glossary.Terms = cleaned;
        }
    }

    public int AddProposedTerms(string languageCode, IReadOnlyList<GlossaryEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return 0;

        EnsureLoaded();
        lock (_lock)
        {
            int touched = 0;

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.Destination))
                    continue;

                var term = FindTermLocked(entry.Source);
                if (term is null)
                {
                    // Un candidat d'extraction naît Proposé : il n'entre dans les prompts qu'une
                    // fois validé (gouvernance de GLOSSAIRE.md).
                    term = new GlossaryTerm
                    {
                        Source = NormalizeCell(entry.Source),
                        Context = NormalizeCell(entry.Context),
                        Status = GlossaryTermStatus.Proposed,
                    };
                    _glossary.Terms.Add(term);
                }
                else if (term.Translations.TryGetValue(languageCode, out var existing)
                    && !string.IsNullOrWhiteSpace(existing))
                {
                    // Ne jamais écraser une traduction déjà tranchée par une proposition.
                    continue;
                }

                term.Translations[languageCode] = NormalizeCell(entry.Destination);
                touched++;
            }

            return touched;
        }
    }

    public void Save()
    {
        EnsureLoaded();
        lock (_lock)
        {
            if (_loadFailed)
                throw new InvalidOperationException(
                    "Le glossaire existant n'a pas pu être lu : enregistrer maintenant écraserait son contenu. Corrigez ou supprimez glossary.json puis relancez l'application.");

            try
            {
                Directory.CreateDirectory(AppConfig.ConfigDirectory);
                var json = JsonSerializer.Serialize(_glossary, JsonOptions);
                AtomicFile.WriteAllText(FilePath, json);
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlossaryService] Échec de sauvegarde : {ex.Message}");
                throw;
            }
        }
    }

    public string BuildGlossarySection(string languageCode, string languageName)
    {
        EnsureLoaded();
        IReadOnlyList<GlossaryEntry> entries;
        lock (_lock)
        {
            // Seuls les termes validés sont injectés : une proposition en attente de contrôle
            // ne doit pas contaminer les traductions (gouvernance décrite dans GLOSSAIRE.md).
            entries = ProjectLocked(languageCode, validatedOnly: true);
        }

        if (entries.Count == 0)
            return string.Empty;

        // Même tri que l'empreinte : le prompt doit être une fonction du contenu, pas de l'ordre
        // de stockage — sinon réordonner les termes changerait le prompt sans invalider le cache.
        var ordered = entries
            .OrderBy(entry => entry.Source, StringComparer.Ordinal)
            .ThenBy(entry => entry.Destination, StringComparer.Ordinal)
            .ThenBy(entry => entry.Context, StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.Append("## Glossaire métier ").Append(languageName).AppendLine();
        sb.AppendLine();
        sb.AppendLine("Tu DOIS respecter les traductions suivantes pour les termes ci-dessous. Respecte la casse et adapte le contexte (genre, nombre, conjugaison) si applicable.");
        sb.AppendLine();
        sb.AppendLine("| Source | Destination | Contexte |");
        sb.AppendLine("|---|---|---|");

        foreach (var entry in ordered)
        {
            sb.Append("| ")
              .Append(EscapeMarkdownCell(entry.Source)).Append(" | ")
              .Append(EscapeMarkdownCell(entry.Destination)).Append(" | ")
              .Append(EscapeMarkdownCell(entry.Context)).AppendLine(" |");
        }

        return sb.ToString();
    }

    public string GetGlossaryFingerprint(string languageCode)
    {
        EnsureLoaded();
        IReadOnlyList<GlossaryEntry> entries;
        lock (_lock)
        {
            // Même périmètre que l'injection (termes validés). L'empreinte hache les valeurs
            // stockées ; le rendu du prompt n'y ajoute que l'échappement markdown, déterministe et
            // injectif : à contenu stocké égal, prompt égal. Sur des données migrées telles
            // quelles, elle est identique à celle du schéma v1 : les caches survivent.
            entries = ProjectLocked(languageCode, validatedOnly: true);
        }

        if (entries.Count == 0)
            return "empty";

        // Entrées triées avant hachage : réordonner le glossaire sans en changer le contenu
        // ne modifie pas le fingerprint, donc n'invalide pas le cache.
        var ordered = entries
            .OrderBy(entry => entry.Source, StringComparer.Ordinal)
            .ThenBy(entry => entry.Destination, StringComparer.Ordinal)
            .ThenBy(entry => entry.Context, StringComparer.Ordinal);

        var sb = new StringBuilder();
        foreach (var entry in ordered)
        {
            sb.Append(entry.Source).Append('\u001F')
              .Append(entry.Destination).Append('\u001F')
              .Append(entry.Context).Append('\u001E');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes, 0, 8);
    }

    public async Task<IReadOnlyList<GlossaryEntry>> ExtractCandidatesAsync(
        IReadOnlyList<string> frenchTexts,
        AppConfig config,
        string languageCode,
        string languageName,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (frenchTexts.Count == 0)
            return Array.Empty<GlossaryEntry>();

        EnsureLoaded();
        var existing = GetEntries(languageCode);
        var existingTerms = new HashSet<string>(
            existing.Select(e => e.Source.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var existingListBlock = BuildExistingTermsBlock(existing);
        var systemPrompt = AppConfig.DefaultExtractionPrompt
            .Replace("{language}", languageName)
            .Replace("{existingTerms}", existingListBlock);

        var aggregated = new List<GlossaryEntry>();
        var seenInBatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int processed = 0;

        for (int offset = 0; offset < frenchTexts.Count; offset += ExtractionBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var slice = frenchTexts.Skip(offset).Take(ExtractionBatchSize).ToList();
            var userMessage = BuildNumberedUserMessage(slice);

            string raw;
            try
            {
                raw = await Translator.CallApiAsync(systemPrompt, userMessage, config);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlossaryService] Échec extraction (batch {offset}) : {ex.Message}");
                processed += slice.Count;
                progress?.Report(processed);
                continue;
            }

            var parsed = ParseExtractionResponse(raw);
            foreach (var candidate in parsed)
            {
                var key = candidate.Source.Trim();
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(candidate.Destination))
                    continue;
                if (existingTerms.Contains(key))
                    continue;
                if (!seenInBatches.Add(key))
                    continue;

                aggregated.Add(candidate);
            }

            processed += slice.Count;
            progress?.Report(processed);
        }

        return aggregated;
    }

    private static string BuildExistingTermsBlock(IReadOnlyList<GlossaryEntry> existing)
    {
        if (existing.Count == 0)
            return "Aucun terme n'est encore défini dans le glossaire pour cette langue.";

        var sb = new StringBuilder();
        sb.AppendLine("## Termes déjà présents dans le glossaire (NE PAS les ré-extraire)");
        sb.AppendLine();
        foreach (var entry in existing.Take(200))
        {
            sb.Append("- ").Append(entry.Source);
            if (!string.IsNullOrWhiteSpace(entry.Destination))
                sb.Append(" → ").Append(entry.Destination);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildNumberedUserMessage(IReadOnlyList<string> texts)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Textes sources français :");
        sb.AppendLine();
        for (int i = 0; i < texts.Count; i++)
            sb.Append(i + 1).Append(". ").AppendLine(texts[i]);
        return sb.ToString();
    }

    private static List<GlossaryEntry> ParseExtractionResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<GlossaryEntry>();

        var jsonText = ExtractJsonArray(raw);
        if (string.IsNullOrWhiteSpace(jsonText))
            return new List<GlossaryEntry>();

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new List<GlossaryEntry>();

            var result = new List<GlossaryEntry>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    continue;

                var entry = new GlossaryEntry
                {
                    Source = ReadString(element, "term"),
                    Destination = ReadString(element, "translation"),
                    Context = ReadString(element, "context"),
                };
                result.Add(entry);
            }
            return result;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GlossaryService] JSON invalide : {ex.Message}");
            return new List<GlossaryEntry>();
        }
    }

    private static string ExtractJsonArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end <= start)
            return string.Empty;
        return raw.Substring(start, end - start + 1);
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString() ?? string.Empty;
        return string.Empty;
    }

    private void EnsureLoaded()
    {
        lock (_lock)
        {
            if (_loaded)
                return;
            _loaded = true;

            if (!File.Exists(FilePath))
            {
                _glossary = new Glossary();
                return;
            }

            try
            {
                var json = File.ReadAllText(FilePath);
                _glossary = JsonSerializer.Deserialize<Glossary>(json, JsonOptions) ?? new Glossary();

                // Un fichier édité à la main peut porter des null explicites : les neutraliser
                // plutôt que de basculer tout le glossaire en mode vide sur une exception. La
                // reconstruction des dictionnaires rétablit aussi le comparateur, que la
                // désérialisation perd (codes de langue insensibles à la casse, comme en v1).
                _glossary.Terms ??= new List<GlossaryTerm>();
                _glossary.Terms.RemoveAll(term => term is null);
                foreach (var term in _glossary.Terms)
                {
                    term.Source ??= string.Empty;
                    term.Context ??= string.Empty;
                    term.ReviewerComment ??= string.Empty;
                    term.Translations = new Dictionary<string, string>(
                        term.Translations ?? new Dictionary<string, string>(),
                        StringComparer.OrdinalIgnoreCase);
                }

                MigrateFromV1Locked();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlossaryService] Chargement impossible : {ex.Message}");
                _glossary = new Glossary();
                _loadFailed = true;
            }
        }
    }

    /// <summary>
    /// Normalisation des valeurs saisies : espaces de bord retirés, retours à la ligne aplatis.
    /// Interne : l'éditeur s'en sert pour que sa détection de doublons juge sur la même forme
    /// que le stockage.
    /// </summary>
    internal static string NormalizeCell(string? value)
        => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();

    /// <summary>
    /// Migration du schéma v1 (entrées par langue) vers le schéma transversal : les entrées de
    /// même Source fusionnent en un terme, le premier contexte non vide l'emporte. Les entrées v1
    /// étaient déjà injectées dans les prompts : les termes migrés naissent donc
    /// <see cref="GlossaryTermStatus.Validated"/>, sans quoi la migration changerait le
    /// comportement des traductions. Idempotente : rejouée à chaque chargement tant que le fichier
    /// n'a pas été réécrit en v2, plus jamais ensuite (EntriesByLanguage nul n'est pas réécrit).
    /// </summary>
    private void MigrateFromV1Locked()
    {
        if (_glossary.EntriesByLanguage is not { Count: > 0 } legacy)
        {
            _glossary.EntriesByLanguage = null;
            return;
        }

        foreach (var (languageCode, entries) in legacy)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.Destination))
                    continue;

                var term = FindTermLocked(entry.Source);
                if (term is null)
                {
                    term = new GlossaryTerm { Source = entry.Source.Trim(), Status = GlossaryTermStatus.Validated };
                    _glossary.Terms.Add(term);
                }

                term.Translations[languageCode] = entry.Destination;
                if (term.Context.Length == 0 && !string.IsNullOrWhiteSpace(entry.Context))
                    term.Context = entry.Context;
            }
        }

        _glossary.EntriesByLanguage = null;
        _glossary.Version = 2;
    }

    private static GlossaryTerm CloneTerm(GlossaryTerm term) => new()
    {
        Source = term.Source,
        Context = term.Context,
        Status = term.Status,
        ReviewerComment = term.ReviewerComment,
        Translations = new Dictionary<string, string>(term.Translations, StringComparer.OrdinalIgnoreCase),
    };

    private GlossaryTerm? FindTermLocked(string source)
    {
        // Comparaison sur la forme normalisée : l'identité d'un terme ne doit dépendre ni des
        // espaces de bord ni d'un retour à la ligne collé par mégarde.
        var normalized = NormalizeCell(source);
        return _glossary.Terms.Find(term => string.Equals(NormalizeCell(term.Source), normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Projette les termes sur une langue, sous la forme historique Source / Destination /
    /// Contexte. <paramref name="validatedOnly"/> distingue les deux usages : l'édition voit tout,
    /// les prompts et l'empreinte ne voient que le validé. À appeler sous <see cref="_lock"/>.
    /// </summary>
    private List<GlossaryEntry> ProjectLocked(string languageCode, bool validatedOnly)
    {
        var entries = new List<GlossaryEntry>();

        foreach (var term in _glossary.Terms)
        {
            if (validatedOnly && term.Status != GlossaryTermStatus.Validated)
                continue;

            if (!term.Translations.TryGetValue(languageCode, out var destination)
                || string.IsNullOrWhiteSpace(destination))
                continue;

            entries.Add(new GlossaryEntry
            {
                Source = term.Source,
                Destination = destination,
                Context = term.Context,
            });
        }

        return entries;
    }

    private static string EscapeMarkdownCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        return value
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("|", "\\|")
            .Trim();
    }
}
