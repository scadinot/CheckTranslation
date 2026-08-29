using ClosedXML.Excel;

namespace CheckTranslation;

/// <summary>
/// Lit et écrit le fichier Excel exporté par ResX Resource Manager.
///
/// Les lignes produites sont indexées par code de langue (comme la source .resx) ; la
/// correspondance code → colonne Excel est portée par <see cref="LanguageInfo.Column"/> et ne
/// sort pas de cette classe. Pour une langue en colonne <c>col</c>, son commentaire est en
/// <c>col - 1</c>.
/// </summary>
internal static class ExcelReader
{
    // Colonnes du fichier ResX Manager (1-indexees)
    private const int ColProject = 1;  // A
    private const int ColFile    = 2;  // B
    private const int ColKey     = 3;  // C
    private const int ColComment = 4;  // D
    private const int ColFrench  = 5;  // E (langue par defaut, sans en-tete)

    public static List<TranslationRow> Load(string filePath, IReadOnlyList<LanguageInfo> languages, IProgress<SourceLoadProgress>? progress = null)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        var rows = new List<TranslationRow>();
        int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        int totalRows = lastRow - 1;
        if (totalRows > 0)
            progress?.Report(new SourceLoadProgress(0, totalRows));

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

            foreach (var language in languages)
            {
                row.Translations[language.Code] = worksheet.Cell(r, language.Column).GetString();
                row.Comments[language.Code] = worksheet.Cell(r, language.Column - 1).GetString();
            }

            rows.Add(row);

            // Progression en "lignes lues" (pas en %). On limite un peu la fréquence
            // des updates UI pour éviter de saturer le thread UI sur les gros fichiers.
            int done = r - 1;
            if (done == 1 || done == totalRows || done % 10 == 0)
                progress?.Report(new SourceLoadProgress(done, totalRows));
        }

        return rows;
    }

    public static void Save(string filePath, IReadOnlyList<TranslationRow> rows, IReadOnlyList<LanguageInfo> languages)
    {
        var columnByCode = BuildColumnByCode(languages);

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();

        foreach (var row in rows)
        {
            foreach (var (code, value) in row.Translations)
            {
                if (columnByCode.TryGetValue(code, out var column))
                    WriteCellValue(worksheet.Cell(row.RowNumber, column), value);
            }

            foreach (var (code, value) in row.Comments)
            {
                if (columnByCode.TryGetValue(code, out var column))
                    WriteCellValue(worksheet.Cell(row.RowNumber, column - 1), value);
            }
        }

        workbook.Save();
    }

    public static int Merge(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows)
        => Merge(destinationFilePath, activeLanguage, rows, new Dictionary<string, MergeDifferenceResolution>(StringComparer.OrdinalIgnoreCase));

    public static int Merge(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows, IReadOnlyDictionary<string, MergeDifferenceResolution> sourceDifferenceResolutions)
    {
        int activeColumn = activeLanguage.Column;

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

            var resolution = GetMergeResolution(sourceDiffers, syncKey, sourceDifferenceResolutions);
            if (resolution is null || !resolution.HasAnyChange)
                continue;

            if (resolution.UpdateFrenchAndComment)
            {
                WriteCellValue(worksheet.Cell(r, ColFrench), sourceRow.French);
                WriteCellValue(worksheet.Cell(r, ColComment), sourceRow.FrenchComment);
            }

            if (resolution.UpdateTranslationAndComment)
            {
                WriteCellValue(worksheet.Cell(r, activeColumn), sourceRow.Translations.GetValueOrDefault(activeLanguage.Code, sourceRow.Translation));
                WriteCellValue(worksheet.Cell(r, activeColumn - 1), sourceRow.Comments.GetValueOrDefault(activeLanguage.Code, sourceRow.Comment));
            }

            mergedCount++;
        }

        workbook.Save();
        return mergedCount;
    }

    public static List<MergeDifference> GetMergeSourceDifferences(string destinationFilePath, LanguageInfo activeLanguage, IReadOnlyList<TranslationRow> rows)
    {
        int activeColumn = activeLanguage.Column;

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
            if (!string.Equals(destinationFrench, sourceRow.French, StringComparison.Ordinal)
                || !string.Equals(comment, sourceRow.FrenchComment, StringComparison.Ordinal))
            {
                differences.Add(new MergeDifference(
                    syncKey,
                    CreateSnapshot(sourceRow),
                    CreateSnapshot(worksheet, r, activeColumn)));
            }
        }

        return differences;
    }

    private static MergeDifferenceResolution? GetMergeResolution(bool sourceDiffers, string syncKey, IReadOnlyDictionary<string, MergeDifferenceResolution> sourceDifferenceResolutions)
    {
        if (!sourceDiffers)
            return new MergeDifferenceResolution(UpdateFrenchAndComment: false, UpdateTranslationAndComment: true);

        return sourceDifferenceResolutions.TryGetValue(syncKey, out var resolution)
            ? resolution
            : null;
    }

    private static MergeRowSnapshot CreateSnapshot(TranslationRow row)
        => new(
            row.Project,
            row.File,
            row.Key,
            row.French,
            row.FrenchComment,
            row.Translation,
            row.Comment);

    private static MergeRowSnapshot CreateSnapshot(IXLWorksheet worksheet, int rowNumber, int activeColumn)
        => new(
            worksheet.Cell(rowNumber, ColProject).GetString(),
            worksheet.Cell(rowNumber, ColFile).GetString(),
            worksheet.Cell(rowNumber, ColKey).GetString(),
            worksheet.Cell(rowNumber, ColFrench).GetString(),
            worksheet.Cell(rowNumber, ColComment).GetString(),
            worksheet.Cell(rowNumber, activeColumn).GetString(),
            worksheet.Cell(rowNumber, activeColumn - 1).GetString());

    private static Dictionary<string, int> BuildColumnByCode(IReadOnlyList<LanguageInfo> languages)
        => languages.ToDictionary(language => language.Code, language => language.Column, StringComparer.OrdinalIgnoreCase);

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
