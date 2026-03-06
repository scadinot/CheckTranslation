namespace CheckTranslation;

internal sealed class TranslationService : ITranslationService
{
    public Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(IReadOnlyList<string> texts, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
        => Translator.TranslateInBatchesAsync(texts, config, targetLanguage, progress);

    public Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(IReadOnlyList<(string French, string Translation)> pairs, AppConfig config, string targetLanguage, IProgress<int>? progress = null)
        => Translator.VerifyInBatchesAsync(pairs, config, targetLanguage, progress);
}
