namespace CheckTranslation;

internal interface ITranslationService
{
    Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, string glossarySection, string glossaryFingerprint, IProgress<int>? progress = null);
    Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, string glossarySection, string glossaryFingerprint, IProgress<int>? progress = null);
    void UpdateTranslationCache(string frenchText, string translation, AppConfig config, string targetLanguage, string glossaryFingerprint);
    int GetTranslationCacheCount(AppConfig config, string targetLanguage, string glossaryFingerprint);
    int ClearTranslationCache(AppConfig config);
    void UpdateVerificationCache(string frenchText, string translation, string verification, AppConfig config, string targetLanguage, string glossaryFingerprint);
    int GetVerificationCacheCount(AppConfig config, string targetLanguage, string glossaryFingerprint);
    int ClearVerificationCache(AppConfig config);
}
