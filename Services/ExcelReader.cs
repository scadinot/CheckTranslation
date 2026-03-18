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

    public static List<TranslationRow> Load(string filePath, int[] translationColumns, int activeColumn, IProgress<ExcelLoadProgress>? progress = null)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<TranslationRow>();
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        int totalRows = lastRow - 1;
        if (totalRows > 0)
            progress?.Report(new ExcelLoadProgress(0, totalRows));

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

            // Progression en "lignes lues" (pas en %). On limite un peu la fréquence
            // des updates UI pour éviter de saturer le thread UI sur les gros fichiers.
            int done = r - 1;
            if (done == 1 || done == totalRows || done % 10 == 0)
                progress?.Report(new ExcelLoadProgress(done, totalRows));
        }

        return rows;
    }

    public static void Save(string filePath, int activeColumn, IReadOnlyList<TranslationRow> rows)
    {
        // Synchroniser la langue active dans le dictionnaire avant de sauvegarder
        foreach (var row in rows)
        {
            row.Translations[activeColumn] = row.Translation;
            row.Comments[activeColumn] = row.Comment;
        }

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        foreach (var row in rows)
        {
            foreach (var (col, value) in row.Translations)
                worksheet.Cell(row.RowNumber, col).Value = value;

            foreach (var (col, value) in row.Comments)
                worksheet.Cell(row.RowNumber, col - 1).Value = value;
        }

        workbook.Save();
    }
}
