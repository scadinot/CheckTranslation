namespace CheckTranslation;

internal sealed class ExcelService : IExcelService
{
    public List<TranslationRow> Load(string filePath, int[] translationColumns, int activeColumn, IProgress<int>? progress = null)
    {
        IProgress<ExcelLoadProgress>? adapted = progress is null
            ? null
            : new Progress<ExcelLoadProgress>(p =>
            {
                int percent = p.Total > 0 ? (p.Done * 100 / p.Total) : 0;
                progress.Report(percent);
            });

        return ExcelReader.Load(filePath, translationColumns, activeColumn, adapted);
    }

    public void Save(string filePath, int activeColumn, IReadOnlyList<TranslationRow> rows)
        => ExcelReader.Save(filePath, activeColumn, rows);
}
