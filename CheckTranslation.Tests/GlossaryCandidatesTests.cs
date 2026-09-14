namespace CheckTranslation.Tests;

public class GlossaryCandidatesTests
{
    private static readonly LanguageInfo English = new("en-US", "Anglais");
    private static readonly LanguageInfo German = new("de-DE", "Allemand");
    private static readonly LanguageInfo Spanish = new("es-ES", "Espagnol");

    private static GlossaryEntry Entry(string source, string destination, string context = "")
        => new() { Source = source, Destination = destination, Context = context };

    private static TranslationRow Row(params (string Code, string Value)[] translations)
    {
        var row = new TranslationRow { French = "texte" };
        foreach (var (code, value) in translations)
            row.Translations[code] = value;
        return row;
    }

    [Fact]
    public void LanguagesWithContent_KeepsOrder_AndOnlyLanguagesWithATranslation()
    {
        var rows = new[]
        {
            Row(("de-DE", "Text")),
            Row(("de-DE", ""), ("es-ES", "   ")),
        };

        var languages = GlossaryCandidates.LanguagesWithContent(rows, new[] { English, German, Spanish });

        // Une chaîne vide ou blanche n'est pas du contenu ; l'anglais n'a rien du tout.
        Assert.Equal("de-DE", Assert.Single(languages).Code);
    }

    [Fact]
    public void LanguagesWithContent_OneRowSuffices()
    {
        var rows = new[] { Row(), Row(), Row(("es-ES", "texto")) };

        var languages = GlossaryCandidates.LanguagesWithContent(rows, new[] { English, German, Spanish });

        Assert.Equal("es-ES", Assert.Single(languages).Code);
    }

    [Fact]
    public void Merge_GroupsBySourceAcrossLanguages_CaseInsensitive()
    {
        var merged = GlossaryCandidates.Merge(new (string, IReadOnlyList<GlossaryEntry>)[]
        {
            ("de-DE", new[] { Entry("Disjoncteur", "Leistungsschalter") }),
            ("en-US", new[] { Entry("disjoncteur", "circuit breaker") }),
        });

        var term = Assert.Single(merged);
        Assert.Equal("Disjoncteur", term.Source);
        Assert.Equal(GlossaryTermStatus.Proposed, term.Status);
        Assert.Equal("Leistungsschalter", term.Translations["de-DE"]);
        Assert.Equal("circuit breaker", term.Translations["en-US"]);
    }

    [Fact]
    public void Merge_FirstNonEmptyContextWins()
    {
        var merged = GlossaryCandidates.Merge(new (string, IReadOnlyList<GlossaryEntry>)[]
        {
            ("de-DE", new[] { Entry("borne", "Klemme", "") }),
            ("en-US", new[] { Entry("borne", "terminal", "point de raccordement") }),
            ("es-ES", new[] { Entry("borne", "borne", "autre définition") }),
        });

        Assert.Equal("point de raccordement", Assert.Single(merged).Context);
    }

    [Fact]
    public void Merge_SkipsEmptyDestinations_KeepsFirstAppearanceOrder_AndFirstProposalPerLanguage()
    {
        var merged = GlossaryCandidates.Merge(new (string, IReadOnlyList<GlossaryEntry>)[]
        {
            ("de-DE", new[] { Entry("tension", "Spannung"), Entry("câble", "Kabel"), Entry("vide", "") }),
            ("en-US", new[] { Entry("câble", "cable"), Entry("tension", "voltage") }),
            ("de-DE", new[] { Entry("tension", "Netzspannung") }),
        });

        Assert.Equal(new[] { "tension", "câble" }, merged.Select(t => t.Source));
        Assert.DoesNotContain(merged, t => t.Source == "vide");
        // Deux lots d'une même langue : la première proposition reste.
        Assert.Equal("Spannung", merged[0].Translations["de-DE"]);
        Assert.Equal("voltage", merged[0].Translations["en-US"]);
    }

    [Fact]
    public void Merge_EmptyInput_GivesNoTerm()
    {
        Assert.Empty(GlossaryCandidates.Merge(Array.Empty<(string, IReadOnlyList<GlossaryEntry>)>()));
    }

    private static GlossaryTerm Term(string source, string context = "", params (string Code, string Value)[] cells)
    {
        var term = new GlossaryTerm { Source = source, Context = context };
        foreach (var (code, value) in cells)
            term.Translations[code] = value;
        return term;
    }

    [Fact]
    public void FindExisting_MatchesNormalizedSource_IgnoringCase()
    {
        var terms = new[] { Term("Disjoncteur différentiel", "", ("de-DE", "FI-Schalter")) };

        Assert.NotNull(GlossaryCandidates.FindExisting(terms, "  disjoncteur différentiel\n"));
        Assert.Null(GlossaryCandidates.FindExisting(terms, "disjoncteur"));
        Assert.Null(GlossaryCandidates.FindExisting(terms, ""));
    }

    [Fact]
    public void Classify_NewTerm_EveryFilledCellIsAdded()
    {
        var diff = GlossaryCandidates.Classify(Term("tension", "grandeur", ("de-DE", "Spannung"), ("en-US", "")), existing: null);

        Assert.True(diff.IsNewTerm);
        Assert.Equal(CandidateCellStatus.Added, diff.Cells["de-DE"]);
        Assert.Equal(CandidateCellStatus.Empty, diff.Cells["en-US"]);
        Assert.Equal(CandidateCellStatus.Added, diff.Context);
        // Le contexte d'un terme nouveau part avec lui : il ne « remplit » pas un vide existant.
        Assert.False(diff.FillsContext);
        Assert.True(diff.AddsAnything);
    }

    [Fact]
    public void Classify_NewTermWithoutAnyCell_AddsNothing()
    {
        var diff = GlossaryCandidates.Classify(Term("vide", "un contexte", ("de-DE", "  ")), existing: null);

        Assert.False(diff.AddsAnything);
    }

    [Fact]
    public void Classify_ExistingTerm_DistinguishesAddedExistingAndConflict()
    {
        var existing = Term("borne", "", ("de-DE", "Klemme"), ("it-IT", "morsetto"));
        var candidate = Term("Borne", "raccordement", ("de-DE", "Anschluss"), ("en-US", "terminal"), ("it-IT", " morsetto "));

        var diff = GlossaryCandidates.Classify(candidate, existing);

        Assert.False(diff.IsNewTerm);
        Assert.Equal(CandidateCellStatus.Conflict, diff.Cells["de-DE"]);  // tranché autrement : ignoré
        Assert.Equal(CandidateCellStatus.Added, diff.Cells["en-US"]);     // case vide : remplie
        Assert.Equal(CandidateCellStatus.Existing, diff.Cells["it-IT"]);  // identique après normalisation : rien
        Assert.Equal(CandidateCellStatus.Added, diff.Context);            // contexte vide au glossaire : rempli
        Assert.True(diff.FillsContext);
        Assert.Equal(new[] { "en-US" }, diff.AddedCells);
    }

    [Fact]
    public void Classify_ExistingCellWithoutProposal_IsExisting_AndCaseDifferenceIsAConflict()
    {
        var existing = Term("borne", "ctx", ("de-DE", "Klemme"));

        var diff = GlossaryCandidates.Classify(Term("borne", "autre", ("en-US", "terminal")), existing);
        // La cellule du glossaire apparaît même sans proposition : le dialog doit la montrer.
        Assert.Equal(CandidateCellStatus.Existing, diff.Cells["de-DE"]);
        Assert.Equal(CandidateCellStatus.Conflict, diff.Context);
        Assert.False(diff.FillsContext);

        // « klemme » n'est pas « Klemme » : la casse d'un terme imposé compte, c'est un conflit.
        var casing = GlossaryCandidates.Classify(Term("borne", "", ("de-DE", "klemme")), existing);
        Assert.Equal(CandidateCellStatus.Conflict, casing.Cells["de-DE"]);
    }

    [Fact]
    public void Classify_NothingToAdd_WhenEverythingIsAlreadyThere()
    {
        var existing = Term("borne", "ctx", ("de-DE", "Klemme"));

        var diff = GlossaryCandidates.Classify(Term("borne", "ctx", ("de-DE", " Klemme ")), existing);

        Assert.False(diff.AddsAnything);
        Assert.Empty(diff.AddedCells);
    }
}
