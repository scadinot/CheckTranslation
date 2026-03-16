namespace CheckTranslation;

internal sealed class TranslationService : ITranslationService
{
    private readonly Dictionary<string, string> _translationCache = new(StringComparer.Ordinal);
    private readonly object _cacheLock = new();

    public void UpdateTranslationCache(string frenchText, string translation, AppConfig config, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(frenchText))
            return;

        var cacheKey = BuildCacheKey(frenchText, config, targetLanguage);

        lock (_cacheLock)
        {
            if (string.IsNullOrWhiteSpace(translation))
                _translationCache.Remove(cacheKey);
            else
                _translationCache[cacheKey] = translation;
        }
    }

    public async Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
    {
        var results = new string[texts.Count];
        var pendingByText = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        int completed = 0;

        for (int i = 0; i < texts.Count; i++)
        {
            var text = texts[i];
            var cacheKey = BuildCacheKey(text, config, targetLanguage);

            string? cachedTranslation;
            lock (_cacheLock)
                _translationCache.TryGetValue(cacheKey, out cachedTranslation);

            if (!string.IsNullOrEmpty(cachedTranslation))
            {
                results[i] = cachedTranslation;
                completed++;
                continue;
            }

            if (!pendingByText.TryGetValue(text, out var indexes))
            {
                indexes = [];
                pendingByText[text] = indexes;
            }

            indexes.Add(i);
        }

        progress?.Report(completed);

        if (pendingByText.Count > 0)
        {
            var uniqueTexts = pendingByText.Keys.ToList();
            var translatedBatches = await Translator.TranslateInBatchesAsync(uniqueTexts, config, targetLanguage, null);

            int translatedCount = 0;
            foreach (var batch in translatedBatches)
            {
                for (int i = 0; i < batch.Length && translatedCount < uniqueTexts.Count; i++, translatedCount++)
                {
                    var sourceText = uniqueTexts[translatedCount];
                    var translation = batch[i];

                    if (!string.IsNullOrEmpty(translation))
                    {
                        var cacheKey = BuildCacheKey(sourceText, config, targetLanguage);
                        lock (_cacheLock)
                            _translationCache[cacheKey] = translation;
                    }

                    foreach (var index in pendingByText[sourceText])
                        results[index] = translation;

                    completed += pendingByText[sourceText].Count;
                    progress?.Report(completed);
                }
            }
        }

        return ChunkResults(results);
    }

    public Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
        => Translator.VerifyInBatchesAsync(pairs, config, targetLanguage, progress);

    private static string BuildCacheKey(string text, AppConfig config, string targetLanguage)
        => string.Join("\u001F", config.Provider, config.Url, config.ModelName, targetLanguage, text);

    private static IReadOnlyList<string[]> ChunkResults(IReadOnlyList<string> results)
    {
        var batches = new List<string[]>();

        for (int i = 0; i < results.Count; i += 20)
            batches.Add(results.Skip(i).Take(20).ToArray());

        return batches;
    }
}
