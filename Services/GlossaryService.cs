using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CheckTranslation;

internal sealed class GlossaryService : IGlossaryService
{
    private static readonly string FilePath = Path.Combine(AppConfig.ConfigDirectory, "glossary.json");
    private const int ExtractionBatchSize = 20;

    private readonly object _lock = new();
    private Glossary _glossary;
    private bool _loaded;

    public GlossaryService()
    {
        _glossary = new Glossary();
    }

    public IReadOnlyList<GlossaryEntry> GetEntries(string languageCode)
    {
        EnsureLoaded();
        lock (_lock)
        {
            if (_glossary.EntriesByLanguage.TryGetValue(languageCode, out var entries))
                return entries.Select(Clone).ToList();
            return Array.Empty<GlossaryEntry>();
        }
    }

    public void ReplaceEntries(string languageCode, IReadOnlyList<GlossaryEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return;

        EnsureLoaded();
        lock (_lock)
        {
            var cleaned = entries
                .Where(e => !string.IsNullOrWhiteSpace(e.Source) && !string.IsNullOrWhiteSpace(e.Destination))
                .Select(Clone)
                .ToList();

            if (cleaned.Count == 0)
                _glossary.EntriesByLanguage.Remove(languageCode);
            else
                _glossary.EntriesByLanguage[languageCode] = cleaned;
        }
    }

    public void Save()
    {
        EnsureLoaded();
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(AppConfig.ConfigDirectory);
                var json = JsonSerializer.Serialize(_glossary, new JsonSerializerOptions { WriteIndented = true });
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
        List<GlossaryEntry> entries;
        lock (_lock)
        {
            if (!_glossary.EntriesByLanguage.TryGetValue(languageCode, out var stored) || stored.Count == 0)
                return string.Empty;
            entries = stored.Select(Clone).ToList();
        }

        var sb = new StringBuilder();
        sb.Append("## Glossaire métier ").Append(languageName).AppendLine();
        sb.AppendLine();
        sb.AppendLine("Tu DOIS respecter les traductions suivantes pour les termes ci-dessous. Respecte la casse et adapte le contexte (genre, nombre, conjugaison) si applicable.");
        sb.AppendLine();
        sb.AppendLine("| Source | Destination | Contexte |");
        sb.AppendLine("|---|---|---|");

        foreach (var entry in entries)
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
        List<GlossaryEntry> entries;
        lock (_lock)
        {
            if (!_glossary.EntriesByLanguage.TryGetValue(languageCode, out var stored) || stored.Count == 0)
                return "empty";
            entries = stored.Select(Clone).ToList();
        }

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
                var loaded = JsonSerializer.Deserialize<Glossary>(json);
                _glossary = loaded ?? new Glossary();
                if (_glossary.EntriesByLanguage.Comparer is not StringComparer comparer || comparer != StringComparer.OrdinalIgnoreCase)
                {
                    _glossary.EntriesByLanguage = new Dictionary<string, List<GlossaryEntry>>(
                        _glossary.EntriesByLanguage,
                        StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlossaryService] Chargement impossible : {ex.Message}");
                _glossary = new Glossary();
            }
        }
    }

    private static GlossaryEntry Clone(GlossaryEntry source) => new()
    {
        Source = source.Source,
        Destination = source.Destination,
        Context = source.Context,
    };

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
