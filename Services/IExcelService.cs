namespace CheckTranslation;

internal interface IExcelService
{
    List<TranslationRow> Load(string filePath, int[] translationColumns, int activeColumn, IProgress<int>? progress = null);
    void Save(string filePath, int activeColumn, IReadOnlyList<TranslationRow> rows);
}
