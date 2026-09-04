using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckTranslation;

internal sealed class GlossaryService : IGlossaryService
{
    private static readonly string FilePath = Path.Combine(AppConfig.ConfigDirectory, "glossary.json");
    // Plus petit que les lots de traduction : la réponse est un JSON verbeux (terme, traduction,
    // contexte par entrée), dix textes tiennent largement sous le plafond de sortie du modèle et
    // la progression est plus fine.
    private const int ExtractionBatchSize = 10;

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

    /// <summary>
    /// Projection prompts (termes Validé uniquement) — même source de vérité que
    /// <see cref="BuildGlossarySection"/> et <see cref="GetGlossaryFingerprint"/> : la détection
    /// d'impact de la retraduction ciblée compare exactement ce que les prompts voient.
    /// </summary>
    public IReadOnlyList<GlossaryEntry> GetPromptEntries(string languageCode)
    {
        EnsureLoaded();
        lock (_lock)
        {
            return ProjectLocked(languageCode, validatedOnly: true);
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

    public void ReplaceTermsAndSave(IReadOnlyList<GlossaryTerm> terms)
    {
        EnsureLoaded();
        lock (_lock)
        {
            // Copie profonde : la restauration doit rendre l'état exact d'avant l'appel, sans
            // repasser par la normalisation de ReplaceTerms.
            var snapshot = _glossary.Terms.Select(CloneTerm).ToList();

            ReplaceTerms(terms);
            try
            {
                Save();
            }
            catch
            {
                _glossary.Terms = snapshot;
                throw;
            }
        }
    }

    public int AddProposedTerms(string languageCode, IReadOnlyList<GlossaryEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(languageCode) || entries.Count == 0)
            return 0;

        EnsureLoaded();
        lock (_lock)
        {
            // Copie profonde avant mutation : la méthode modifie des termes existants en place,
            // la restauration en cas d'échec de persistance doit rendre l'état exact. Paresseuse :
            // prise à la première mutation avérée seulement — une extraction qui ne propose que
            // des doublons ne paie pas le clone du glossaire entier.
            List<GlossaryTerm>? snapshot = null;
            int touched = 0;

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Source) || string.IsNullOrWhiteSpace(entry.Destination))
                    continue;

                var term = FindTermLocked(entry.Source);
                if (term is not null
                    && term.Translations.TryGetValue(languageCode, out var existing)
                    && !string.IsNullOrWhiteSpace(existing))
                {
                    // Ne jamais écraser une traduction déjà tranchée par une proposition.
                    continue;
                }

                snapshot ??= _glossary.Terms.Select(CloneTerm).ToList();

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

                // Un incrément par terme, jamais par entrée : un doublon de source dans les
                // candidats retombe sur la garde de non-écrasement (la case vient d'être
                // remplie, jamais vide ni blanche) et est écarté avant d'arriver ici.
                term.Translations[languageCode] = NormalizeCell(entry.Destination);
                touched++;
            }

            if (touched > 0)
            {
                try
                {
                    Save();
                }
                catch
                {
                    // touched > 0 implique que le snapshot a été pris (première mutation).
                    _glossary.Terms = snapshot!;
                    throw;
                }
            }

            return touched;
        }
    }

    public string GetExportStamp()
    {
        EnsureLoaded();
        lock (_lock)
        {
            var sb = new StringBuilder();

            foreach (var term in _glossary.Terms.OrderBy(t => t.Source, StringComparer.Ordinal))
            {
                sb.Append(term.Source).Append('\u001F')
                  .Append(term.Context).Append('\u001F')
                  .Append(term.Status).Append('\u001F')
                  .Append(term.ReviewerComment).Append('\u001F');

                foreach (var (code, destination) in term.Translations.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
                    sb.Append(code).Append('=').Append(destination).Append('\u001D');

                sb.Append('\u001E');
            }

            // Empreinte complète : la valeur sert précisément à détecter un glossaire modifié
            // pendant le contrôle, et ne coûte qu'une cellule de la feuille Infos — la tronquer
            // n'achèterait rien.
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexString(bytes);
        }
    }

    public int ExportForReview(string filePath, IReadOnlyList<LanguageInfo> languages)
    {
        EnsureLoaded();
        lock (_lock)
        {
            // Refus immédiat : sinon le classeur serait écrit (vide) avant que Save n'échoue,
            // et un fichier d'export existerait malgré l'échec affiché.
            ThrowIfLoadFailed();

            // La bascule Proposé -> En contrôle précède l'écriture pour que l'empreinte du
            // classeur décrive l'état qui restera dans l'application — mais elle n'est
            // persistée qu'après un export réussi, et restaurée si quoi que ce soit échoue :
            // le glossaire ne doit jamais rester « En contrôle » sans classeur produit.
            var flipped = new List<GlossaryTerm>();
            foreach (var term in _glossary.Terms)
            {
                if (term.Status == GlossaryTermStatus.Proposed)
                {
                    term.Status = GlossaryTermStatus.InReview;
                    flipped.Add(term);
                }
            }

            try
            {
                // Les verrous étant réentrants, GetTerms / GetExportStamp / Save s'appellent
                // tels quels depuis la section verrouillée.
                GlossaryExcel.Export(filePath, GetTerms(), GetExportStamp(), languages);
                Save();
                return flipped.Count;
            }
            catch
            {
                foreach (var term in flipped)
                    term.Status = GlossaryTermStatus.Proposed;

                // Si l'export a réussi mais pas la persistance, le classeur existe avec une
                // empreinte décrivant un état restauré : on ne le supprime pas (il a pu écraser
                // un export précédent), l'import signalera simplement l'écart d'empreinte.
                throw;
            }
        }
    }

    public string CreateBackup()
    {
        EnsureLoaded();
        lock (_lock)
        {
            // Un backup pris sur un glossaire illisible serait vide : trompeur pour une
            // fonction de récupération — même refus que les autres écritures.
            ThrowIfLoadFailed();

            // Suffixe numérique si le nom horodaté existe déjà : deux imports dans la même
            // seconde ne doivent pas écraser le même backup, ce qui annulerait son intérêt.
            var baseName = $"glossary-{DateTime.Now:yyyyMMdd-HHmmss}";
            var backupPath = Path.Combine(AppConfig.ConfigDirectory, baseName + ".bak.json");
            for (int n = 1; File.Exists(backupPath); n++)
                backupPath = Path.Combine(AppConfig.ConfigDirectory, $"{baseName}-{n}.bak.json");

            Directory.CreateDirectory(AppConfig.ConfigDirectory);
            var json = JsonSerializer.Serialize(_glossary, JsonOptions);
            AtomicFile.WriteAllText(backupPath, json);
            return backupPath;
        }
    }

    /// <summary>
    /// Refus commun à toutes les écritures disque (enregistrement, export pour contrôle, backup)
    /// quand le glossaire existant n'a pas pu être lu : l'état mémoire est vide, enregistrer
    /// écraserait le fichier, et un classeur d'export ou un backup produits de cet état seraient
    /// trompeurs — un backup vide est pire que pas de backup.
    /// </summary>
    private void ThrowIfLoadFailed()
    {
        if (_loadFailed)
            throw new InvalidOperationException(
                "Le glossaire existant n'a pas pu être lu : toute écriture (enregistrement, export, sauvegarde) partirait d'un état vide et écraserait ou masquerait son contenu. Corrigez ou supprimez glossary.json puis relancez l'application.");
    }

    public void Save()
    {
        EnsureLoaded();
        lock (_lock)
        {
            ThrowIfLoadFailed();

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

    public async Task<GlossaryExtractionResult> ExtractCandidatesAsync(
        IReadOnlyList<string> frenchTexts,
        AppConfig config,
        string languageCode,
        string languageName,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (frenchTexts.Count == 0)
            return new GlossaryExtractionResult(Array.Empty<GlossaryEntry>(), 0, 0, 0, 0, false, null);

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
        int processed = 0, batches = 0, failedBatches = 0, unreadableBatches = 0, alreadyKnown = 0;
        bool truncated = false;
        string? firstError = null;

        for (int offset = 0; offset < frenchTexts.Count; offset += ExtractionBatchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var slice = frenchTexts.Skip(offset).Take(ExtractionBatchSize).ToList();
            var userMessage = BuildNumberedUserMessage(slice);
            batches++;

            string raw;
            try
            {
                raw = await Translator.CallApiAsync(systemPrompt, userMessage, config);
            }
            catch (Exception ex)
            {
                // Un lot en échec ne fait pas tomber les autres, mais il ne disparaît pas non
                // plus : compté et remonté à l'appelant, qui saura dire à l'utilisateur pourquoi
                // il n'a rien (ou moins) reçu. Un échec avalé se lisait « aucun terme trouvé ».
                failedBatches++;
                firstError ??= ex.Message;
                processed += slice.Count;
                progress?.Report(processed);
                continue;
            }

            var parsed = ParseExtractionResponse(raw);
            if (!parsed.Success)
            {
                unreadableBatches++;
                truncated |= parsed.Truncated;
            }

            foreach (var candidate in parsed.Entries)
            {
                var key = candidate.Source.Trim();
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(candidate.Destination))
                    continue;
                if (existingTerms.Contains(key))
                {
                    alreadyKnown++;
                    continue;
                }
                if (!seenInBatches.Add(key))
                    continue;

                aggregated.Add(candidate);
            }

            processed += slice.Count;
            progress?.Report(processed);
        }

        return new GlossaryExtractionResult(aggregated, batches, failedBatches, unreadableBatches, alreadyKnown, truncated, firstError);
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

    /// <summary>
    /// Lit la réponse JSON de l'extraction. Ne confond jamais « le modèle n'a rien trouvé »
    /// (tableau vide, <see cref="ExtractionParse.Success"/> vrai) avec « la réponse est
    /// illisible » (<see cref="ExtractionParse.Success"/> faux) : la première est un résultat,
    /// la seconde un défaut à signaler. Un tableau ouvert et jamais fermé est la signature d'une
    /// réponse tronquée par le plafond de tokens de sortie — cas vécu : à 2048 tokens, un lot de
    /// 20 textes s'arrêtait au milieu d'un objet et l'utilisateur lisait « aucun terme proposé ».
    /// </summary>
    internal static ExtractionParse ParseExtractionResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return ExtractionParse.Unreadable(truncated: false);

        var start = raw.IndexOf('[');
        if (start < 0)
            return ExtractionParse.Unreadable(truncated: false);

        var end = raw.LastIndexOf(']');
        if (end <= start)
            return ExtractionParse.Unreadable(truncated: true);

        try
        {
            using var doc = JsonDocument.Parse(raw.Substring(start, end - start + 1));
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return ExtractionParse.Unreadable(truncated: false);

            var result = new List<GlossaryEntry>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.Object)
                    continue;

                result.Add(new GlossaryEntry
                {
                    Source = ReadString(element, "term"),
                    Destination = ReadString(element, "translation"),
                    Context = ReadString(element, "context"),
                });
            }
            return new ExtractionParse(result, Success: true, Truncated: false);
        }
        catch (JsonException)
        {
            // Un ']' existe mais le document reste invalide : réponse coupée au milieu d'un
            // objet dont un ']' intérieur a survécu, ou JSON malformé — dans les deux cas
            // illisible, et le premier est le plus fréquent.
            return ExtractionParse.Unreadable(truncated: !raw.TrimEnd().EndsWith(']'));
        }
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
