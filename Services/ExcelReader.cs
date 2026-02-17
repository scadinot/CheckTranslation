using ClosedXML.Excel;

namespace CheckTranslation;

/// <summary>
/// Lit le fichier Excel exporté par ResX Manager et retourne les lignes de traduction.
/// </summary>
internal static class ExcelReader
{
    // Colonnes du fichier ResX Manager (1-indexees)
    private const int ColProject = 1;  // A
    private const int ColFile    = 2;  // B
    private const int ColKey     = 3;  // C
    private const int ColComment = 4;  // D
    private const int ColFrench  = 5;  // E (langue par defaut, sans en-tete)

    public static List<TranslationRow> Load(string filePath, int[] translationColumns, int activeColumn, IProgress<int>? progress = null)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<TranslationRow>();
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        int totalRows = lastRow - 1;
        int lastPercent = 0;

        for (int r = 2; r <= lastRow; r++)
        {
            var comment = worksheet.Cell(r, ColComment).GetString();
            if (comment.Contains("@Invariant", StringComparison.OrdinalIgnoreCase))
                continue;

            var row = new TranslationRow
            {
                RowNumber = r,
                Project = worksheet.Cell(r, ColProject).GetString(),
                File    = worksheet.Cell(r, ColFile).GetString(),
                Key     = worksheet.Cell(r, ColKey).GetString(),
                French  = worksheet.Cell(r, ColFrench).GetString(),
            };

            foreach (var col in translationColumns)
            {
                row.Translations[col] = worksheet.Cell(r, col).GetString();
                row.Comments[col] = worksheet.Cell(r, col - 1).GetString();
            }

            row.Translation = row.Translations.GetValueOrDefault(activeColumn, string.Empty);
            row.Comment = row.Comments.GetValueOrDefault(activeColumn, string.Empty);
            rows.Add(row);

            int percent = (r - 1) * 100 / totalRows;
            if (percent > lastPercent)
            {
                lastPercent = percent;
                progress?.Report(percent);
            }
        }

        return rows;
    }

    public static void Save(string filePath, int activeColumn, IReadOnlyList<TranslationRow> rows)
    {
        // Synchroniser la langue active dans le dictionnaire avant de sauvegarder
        foreach (var row in rows)
            row.Translations[activeColumn] = row.Translation;

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        foreach (var row in rows)
        {
            foreach (var (col, value) in row.Translations)
                worksheet.Cell(row.RowNumber, col).Value = value;
        }

        workbook.Save();
    }
}
