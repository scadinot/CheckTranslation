namespace CheckTranslation;

internal interface ITranslationService
{
    Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, IProgress<int>? progress = null);
    Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, IProgress<int>? progress = null);
    void UpdateTranslationCache(string frenchText, string translation, AppConfig config, string targetLanguage);
    int GetTranslationCacheCount();
}
