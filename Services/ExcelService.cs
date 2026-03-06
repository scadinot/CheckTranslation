namespace CheckTranslation;

internal sealed class ExcelService : IExcelService
{
    public List<TranslationRow> Load(string filePath, int[] translationColumns, int activeColumn, IProgress<int>? progress = null)
        => ExcelReader.Load(filePath, translationColumns, activeColumn, progress);

    public void Save(string filePath, int activeColumn, IReadOnlyList<TranslationRow> rows)
        => ExcelReader.Save(filePath, activeColumn, rows);
}
