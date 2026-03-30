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
                FrenchComment = comment,
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
                WriteCellValue(worksheet.Cell(row.RowNumber, col), value);

            foreach (var (col, value) in row.Comments)
                WriteCellValue(worksheet.Cell(row.RowNumber, col - 1), value);
        }

        workbook.Save();
    }

    public static int Merge(string destinationFilePath, int activeColumn, IReadOnlyList<TranslationRow> rows)
        => Merge(destinationFilePath, activeColumn, rows, new Dictionary<string, MergeDifferenceResolution>(StringComparer.OrdinalIgnoreCase));

    public static int Merge(string destinationFilePath, int activeColumn, IReadOnlyList<TranslationRow> rows, IReadOnlyDictionary<string, MergeDifferenceResolution> sourceDifferenceResolutions)
    {
        foreach (var row in rows)
        {
            row.Translations[activeColumn] = row.Translation;
            row.Comments[activeColumn] = row.Comment;
        }

        using var workbook = new XLWorkbook(destinationFilePath);
        var worksheet = workbook.Worksheets.First();

        var rowsByKey = rows
            .GroupBy(row => BuildSyncKey(row.Project, row.File, row.Key), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        int mergedCount = 0;

        for (int r = 2; r <= lastRow; r++)
        {
            var comment = worksheet.Cell(r, ColComment).GetString();
            if (comment.Contains("@Invariant", StringComparison.OrdinalIgnoreCase))
                continue;

            var syncKey = BuildSyncKey(
                worksheet.Cell(r, ColProject).GetString(),
                worksheet.Cell(r, ColFile).GetString(),
                worksheet.Cell(r, ColKey).GetString());

            if (!rowsByKey.TryGetValue(syncKey, out var sourceRow))
                continue;

            var destinationFrench = worksheet.Cell(r, ColFrench).GetString();
            bool sourceDiffers = !string.Equals(destinationFrench, sourceRow.French, StringComparison.Ordinal)
                || !string.Equals(comment, sourceRow.FrenchComment, StringComparison.Ordinal);

            if (sourceDiffers)
            {
                if (!sourceDifferenceResolutions.TryGetValue(syncKey, out var resolution))
                    continue;

                bool hasAnyUpdate = resolution.UpdateFrenchAndComment || resolution.UpdateTranslationAndComment;
                if (!hasAnyUpdate)
                    continue;

                if (resolution.UpdateFrenchAndComment)
                {
                    WriteCellValue(worksheet.Cell(r, ColFrench), sourceRow.French);
                    WriteCellValue(worksheet.Cell(r, ColComment), sourceRow.FrenchComment);
                }

                if (resolution.UpdateTranslationAndComment)
                {
                    WriteCellValue(worksheet.Cell(r, activeColumn), sourceRow.Translations.GetValueOrDefault(activeColumn, sourceRow.Translation));
                    WriteCellValue(worksheet.Cell(r, activeColumn - 1), sourceRow.Comments.GetValueOrDefault(activeColumn, sourceRow.Comment));
                    mergedCount++;
                    continue;
                }

                mergedCount++;
                continue;
            }

            WriteCellValue(worksheet.Cell(r, activeColumn), sourceRow.Translations.GetValueOrDefault(activeColumn, sourceRow.Translation));
            WriteCellValue(worksheet.Cell(r, activeColumn - 1), sourceRow.Comments.GetValueOrDefault(activeColumn, sourceRow.Comment));
            mergedCount++;
        }

        workbook.Save();
        return mergedCount;
    }

    public static List<MergeDifference> GetMergeSourceDifferences(string destinationFilePath, int activeColumn, IReadOnlyList<TranslationRow> rows)
    {
        using var workbook = new XLWorkbook(destinationFilePath);
        var worksheet = workbook.Worksheets.First();

        var rowsByKey = rows
            .GroupBy(row => BuildSyncKey(row.Project, row.File, row.Key), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var differences = new List<MergeDifference>();

        for (int r = 2; r <= lastRow; r++)
        {
            var comment = worksheet.Cell(r, ColComment).GetString();
            if (comment.Contains("@Invariant", StringComparison.OrdinalIgnoreCase))
                continue;

            var syncKey = BuildSyncKey(
                worksheet.Cell(r, ColProject).GetString(),
                worksheet.Cell(r, ColFile).GetString(),
                worksheet.Cell(r, ColKey).GetString());

            if (!rowsByKey.TryGetValue(syncKey, out var sourceRow))
                continue;

            var destinationFrench = worksheet.Cell(r, ColFrench).GetString();
            var destinationTranslation = worksheet.Cell(r, activeColumn).GetString();
            var destinationTranslationComment = worksheet.Cell(r, activeColumn - 1).GetString();
            if (!string.Equals(destinationFrench, sourceRow.French, StringComparison.Ordinal)
                || !string.Equals(comment, sourceRow.FrenchComment, StringComparison.Ordinal))
            {
                differences.Add(new MergeDifference(
                    syncKey,
                    sourceRow.Project,
                    sourceRow.File,
                    sourceRow.Key,
                    sourceRow.French,
                    destinationFrench,
                    sourceRow.FrenchComment,
                    comment,
                    sourceRow.Translation,
                    destinationTranslation,
                    sourceRow.Comment,
                    destinationTranslationComment));
            }
        }

        return differences;
    }

    private static string BuildSyncKey(string project, string file, string key)
        => string.Join("\u001F", project.Trim(), file.Trim(), key.Trim());

    private static void WriteCellValue(IXLCell cell, string? value)
    {
        var text = value ?? string.Empty;

        if (text.StartsWith('\''))
            text = "'" + text;

        cell.Value = text;
    }
}
