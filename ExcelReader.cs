using ClosedXML.Excel;

namespace CheckTransation;

/// <summary>
/// Lit le fichier Excel exporté par ResX Manager et retourne les lignes de traduction.
/// </summary>
internal static class ExcelReader
{
    // Colonnes du fichier ResX Manager (1-indexees)
    private const int ColProject = 1;  // A
    private const int ColFile = 2;     // B
    private const int ColKey = 3;      // C
    private const int ColComment = 4;  // D
    private const int ColFrench = 5;   // E (langue par defaut, sans en-tete)
    private const int ColGerman = 7;   // G (.de-DE)

    public static List<TranslationRow> Load(string filePath, IProgress<int>? progress = null)
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

            rows.Add(new TranslationRow
            {
                RowNumber = r,
                Project = worksheet.Cell(r, ColProject).GetString(),
                File = worksheet.Cell(r, ColFile).GetString(),
                Key = worksheet.Cell(r, ColKey).GetString(),
                French = worksheet.Cell(r, ColFrench).GetString(),
                German = worksheet.Cell(r, ColGerman).GetString(),
            });

            int percent = (r - 1) * 100 / totalRows;
            if (percent > lastPercent)
            {
                lastPercent = percent;
                progress?.Report(percent);
            }
        }

        return rows;
    }

    public static void Save(string filePath, IReadOnlyList<TranslationRow> rows)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        foreach (var row in rows)
        {
            worksheet.Cell(row.RowNumber, ColGerman).Value = row.German;
        }

        workbook.Save();
    }
}
