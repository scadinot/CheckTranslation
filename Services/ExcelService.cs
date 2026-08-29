namespace CheckTranslation;

internal sealed class ExcelService : IExcelService
{
    public List<MergeDifference> GetMergeSourceDifferences(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows)
        => ExcelReader.GetMergeSourceDifferences(destinationFilePath, activeLanguage, rows);

    public int Merge(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows)
        => ExcelReader.Merge(destinationFilePath, activeLanguage, rows);

    public int Merge(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows, IReadOnlyDictionary<string, MergeDifferenceResolution> sourceDifferenceResolutions)
        => ExcelReader.Merge(destinationFilePath, activeLanguage, rows, sourceDifferenceResolutions);
}
