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
}
