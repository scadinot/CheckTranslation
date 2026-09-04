using ClosedXML.Excel;

namespace CheckTranslation;

/// <summary>
/// Export et import du glossaire transversal au format Excel, le support d'échange avec les
/// équipes de contrôle externes (GLOSSAIRE.md, phases 2 et 3). Une feuille « Glossaire » porte
/// une ligne par terme ; une feuille « Infos » porte la date d'export et l'empreinte du glossaire
/// à cet instant, relue à l'import pour détecter que le glossaire a bougé entre-temps.
/// </summary>
internal static class GlossaryExcel
{
    private const string TermsSheetName = "Glossaire";
    private const string InfoSheetName = "Infos";
    private const string StampLabel = "Empreinte";

    public static void Export(
        string filePath,
        IReadOnlyList<GlossaryTerm> terms,
        string stamp,
        IReadOnlyList<LanguageInfo> languages)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(TermsSheetName);

        int column = 1;
        sheet.Cell(1, column++).Value = "Source (FR)";
        sheet.Cell(1, column++).Value = "Contexte";
        foreach (var language in languages)
            sheet.Cell(1, column++).Value = $"{language.Name} ({language.Code})";
        sheet.Cell(1, column++).Value = "Statut";
        sheet.Cell(1, column).Value = "Commentaire réviseur";

        int row = 2;
        foreach (var term in terms.OrderBy(t => t.Source, StringComparer.OrdinalIgnoreCase))
        {
            column = 1;
            sheet.Cell(row, column++).Value = term.Source;
            sheet.Cell(row, column++).Value = term.Context;
            foreach (var language in languages)
                sheet.Cell(row, column++).Value = term.Translations.GetValueOrDefault(language.Code, string.Empty);
            sheet.Cell(row, column++).Value = StatusLabel(term.Status);
            sheet.Cell(row, column).Value = term.ReviewerComment;
            row++;
        }

        var header = sheet.Range(1, 1, 1, column);
        header.Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, Math.Min(row, 50));

        var info = workbook.Worksheets.Add(InfoSheetName);
        info.Cell(1, 1).Value = "Exporté le";
        info.Cell(1, 2).Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        info.Cell(2, 1).Value = StampLabel;
        info.Cell(2, 2).Value = stamp;
        info.Cell(3, 1).Value = "Ne pas modifier cette feuille : elle permet de rapprocher le retour de son export.";
        info.Columns().AdjustToContents();

        workbook.SaveAs(filePath);
    }

    /// <summary>
    /// Relit un classeur de contrôle. Les colonnes de langue sont retrouvées par le code entre
    /// parenthèses de leur en-tête, pas par leur position : un réviseur qui réordonne ou masque
    /// des colonnes ne casse pas l'import. La colonne Statut du classeur est ignorée : les
    /// statuts sont gouvernés par l'application (un changement accepté vaut validation).
    /// </summary>
    public static GlossaryImportFile Import(string filePath, IReadOnlyList<LanguageInfo> languages)
    {
        using var workbook = new XLWorkbook(filePath);

        var sheet = workbook.Worksheets.FirstOrDefault(ws => string.Equals(ws.Name, TermsSheetName, StringComparison.OrdinalIgnoreCase))
            ?? workbook.Worksheets.First();

        int lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        int sourceColumn = 0, contextColumn = 0, reviewerColumn = 0;
        var languageColumns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int c = 1; c <= lastColumn; c++)
        {
            var headerText = sheet.Cell(1, c).GetString().Trim();
            if (headerText.Length == 0)
                continue;

            var code = ExtractLanguageCode(headerText, languages);
            if (code is not null)
            {
                languageColumns[code] = c;
            }
            else if (headerText.StartsWith("Source", StringComparison.OrdinalIgnoreCase))
            {
                sourceColumn = c;
            }
            else if (headerText.StartsWith("Contexte", StringComparison.OrdinalIgnoreCase))
            {
                contextColumn = c;
            }
            else if (headerText.StartsWith("Commentaire", StringComparison.OrdinalIgnoreCase))
            {
                reviewerColumn = c;
            }
        }

        if (sourceColumn == 0)
            throw new InvalidDataException("Colonne « Source (FR) » introuvable dans la première ligne du classeur.");

        var terms = new List<GlossaryTerm>();
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int r = 2; r <= lastRow; r++)
        {
            var source = sheet.Cell(r, sourceColumn).GetString();
            if (string.IsNullOrWhiteSpace(source))
                continue;

            var term = new GlossaryTerm
            {
                Source = source,
                Context = contextColumn > 0 ? sheet.Cell(r, contextColumn).GetString() : string.Empty,
                ReviewerComment = reviewerColumn > 0 ? sheet.Cell(r, reviewerColumn).GetString() : string.Empty,
            };

            foreach (var (code, columnIndex) in languageColumns)
            {
                var value = sheet.Cell(r, columnIndex).GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    term.Translations[code] = value;
            }

            terms.Add(term);
        }

        string? stamp = null;
        var infoSheet = workbook.Worksheets.FirstOrDefault(ws => string.Equals(ws.Name, InfoSheetName, StringComparison.OrdinalIgnoreCase));
        if (infoSheet is not null)
        {
            int infoLastRow = infoSheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int r = 1; r <= infoLastRow; r++)
            {
                if (string.Equals(infoSheet.Cell(r, 1).GetString().Trim(), StampLabel, StringComparison.OrdinalIgnoreCase))
                {
                    stamp = infoSheet.Cell(r, 2).GetString().Trim();
                    break;
                }
            }
        }

        return new GlossaryImportFile(terms, stamp);
    }

    private static string? ExtractLanguageCode(string headerText, IReadOnlyList<LanguageInfo> languages)
    {
        foreach (var language in languages)
        {
            if (headerText.Contains($"({language.Code})", StringComparison.OrdinalIgnoreCase))
                return language.Code;
        }

        return null;
    }

    private static string StatusLabel(GlossaryTermStatus status) => status switch
    {
        GlossaryTermStatus.Proposed => "Proposé",
        GlossaryTermStatus.InReview => "En contrôle",
        _ => "Validé",
    };
}

/// <summary>Contenu relu d'un classeur de contrôle : les termes, et l'empreinte de l'export d'origine si la feuille Infos a survécu.</summary>
internal sealed record GlossaryImportFile(List<GlossaryTerm> Terms, string? Stamp);
