using ClosedXML.Excel;

namespace CheckTranslation.Tests;

public sealed class GlossaryExcelTests : IDisposable
{
    private static readonly LanguageInfo[] Languages =
    [
        new("de-DE", "Allemand", 7),
        new("en-US", "Anglais", 9),
    ];

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CheckTranslation.Tests", Guid.NewGuid().ToString("N"));

    public GlossaryExcelTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string NewPath() => Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".xlsx");

    private static GlossaryTerm Term(string source, string context = "", string reviewerComment = "",
        params (string Code, string Value)[] translations)
    {
        var term = new GlossaryTerm { Source = source, Context = context, ReviewerComment = reviewerComment };
        foreach (var (code, value) in translations)
            term.Translations[code] = value;
        return term;
    }

    [Fact]
    public void ExportThenImport_RoundTripsTermsStampAndLanguages()
    {
        var path = NewPath();
        var terms = new[]
        {
            Term("borne", "point de raccordement", "vu", ("de-DE", "Klemme"), ("en-US", "terminal")),
            Term("disjoncteur", translations: ("de-DE", "Leistungsschalter")),
        };

        GlossaryExcel.Export(path, terms, "STAMP-123", Languages);
        var file = GlossaryExcel.Import(path, Languages);

        Assert.Equal("STAMP-123", file.Stamp);
        Assert.Equal(new[] { "de-DE", "en-US" }, file.LanguageCodes.OrderBy(c => c, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(2, file.Terms.Count);

        var borne = Assert.Single(file.Terms, t => t.Source == "borne");
        Assert.Equal("point de raccordement", borne.Context);
        Assert.Equal("vu", borne.ReviewerComment);
        Assert.Equal("Klemme", borne.Translations["de-DE"]);
        Assert.Equal("terminal", borne.Translations["en-US"]);

        // Le diff après aller-retour doit être vide : c'est le contrat du cycle de contrôle.
        Assert.Empty(GlossaryDiff.Compute(terms, file.Terms, Languages));
    }

    [Fact]
    public void Import_FindsLanguageColumnsByCode_NotByPosition()
    {
        // Un réviseur qui réordonne les colonnes ne casse pas l'import : le code entre
        // parenthèses fait foi.
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Glossaire");
            sheet.Cell(1, 1).Value = "Anglais (en-US)";
            sheet.Cell(1, 2).Value = "Source (FR)";
            sheet.Cell(1, 3).Value = "Commentaire réviseur";
            sheet.Cell(1, 4).Value = "Contexte";
            sheet.Cell(2, 1).Value = "terminal";
            sheet.Cell(2, 2).Value = "borne";
            workbook.SaveAs(path);
        }

        var file = GlossaryExcel.Import(path, Languages);

        var term = Assert.Single(file.Terms);
        Assert.Equal("borne", term.Source);
        Assert.Equal("terminal", term.Translations["en-US"]);
        Assert.Equal("en-US", Assert.Single(file.LanguageCodes));
    }

    [Fact]
    public void Import_RefusesWorkbookWithoutGlossarySheet()
    {
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Feuille imprévue");
            sheet.Cell(1, 1).Value = "Source (FR)";
            workbook.SaveAs(path);
        }

        Assert.Throws<InvalidDataException>(() => GlossaryExcel.Import(path, Languages));
    }

    [Theory]
    [InlineData("Contexte")]
    [InlineData("Commentaire réviseur")]
    [InlineData("Source (FR)")]
    public void Import_RefusesWorkbookMissingUniversalColumn(string removedHeader)
    {
        // Une colonne universelle absente serait lue comme vide et le diff proposerait son
        // effacement en masse : le classeur est refusé.
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Glossaire");
            int column = 1;
            foreach (var header in new[] { "Source (FR)", "Contexte", "Allemand (de-DE)", "Commentaire réviseur" })
            {
                if (header != removedHeader)
                    sheet.Cell(1, column++).Value = header;
            }
            workbook.SaveAs(path);
        }

        Assert.Throws<InvalidDataException>(() => GlossaryExcel.Import(path, Languages));
    }

    [Fact]
    public void Import_RefusesDuplicateLanguageColumn()
    {
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Glossaire");
            sheet.Cell(1, 1).Value = "Source (FR)";
            sheet.Cell(1, 2).Value = "Contexte";
            sheet.Cell(1, 3).Value = "Allemand (de-DE)";
            sheet.Cell(1, 4).Value = "Copie (de-DE)";
            sheet.Cell(1, 5).Value = "Commentaire réviseur";
            workbook.SaveAs(path);
        }

        Assert.Throws<InvalidDataException>(() => GlossaryExcel.Import(path, Languages));
    }

    [Fact]
    public void Import_RefusesDuplicateTermSource()
    {
        // Un doublon serait départagé en silence par le diff : des corrections seraient perdues.
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Glossaire");
            sheet.Cell(1, 1).Value = "Source (FR)";
            sheet.Cell(1, 2).Value = "Contexte";
            sheet.Cell(1, 3).Value = "Commentaire réviseur";
            sheet.Cell(2, 1).Value = "borne";
            sheet.Cell(3, 1).Value = "BORNE";
            workbook.SaveAs(path);
        }

        Assert.Throws<InvalidDataException>(() => GlossaryExcel.Import(path, Languages));
    }

    [Fact]
    public void Import_IgnoresExtraCommentColumn()
    {
        // « Commentaire interne » ajouté par un réviseur : ni confusion avec la colonne
        // officielle, ni refus pour doublon.
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Glossaire");
            sheet.Cell(1, 1).Value = "Source (FR)";
            sheet.Cell(1, 2).Value = "Contexte";
            sheet.Cell(1, 3).Value = "Commentaire interne";
            sheet.Cell(1, 4).Value = "Commentaire réviseur";
            sheet.Cell(2, 1).Value = "borne";
            sheet.Cell(2, 3).Value = "note privée";
            sheet.Cell(2, 4).Value = "note officielle";
            workbook.SaveAs(path);
        }

        var file = GlossaryExcel.Import(path, Languages);

        Assert.Equal("note officielle", Assert.Single(file.Terms).ReviewerComment);
    }

    [Fact]
    public void Import_SkipsRowsWithoutSource()
    {
        var path = NewPath();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.Worksheets.Add("Glossaire");
            sheet.Cell(1, 1).Value = "Source (FR)";
            sheet.Cell(1, 2).Value = "Contexte";
            sheet.Cell(1, 3).Value = "Commentaire réviseur";
            sheet.Cell(2, 1).Value = "borne";
            sheet.Cell(3, 1).Value = "   ";
            sheet.Cell(4, 1).Value = "tension";
            workbook.SaveAs(path);
        }

        var file = GlossaryExcel.Import(path, Languages);

        Assert.Equal(2, file.Terms.Count);
    }
}
