namespace CheckTranslation.Tests;

public class GlossaryDiffTests
{
    private static readonly LanguageInfo German = new("de-DE", "Allemand", 7);
    private static readonly LanguageInfo English = new("en-US", "Anglais", 9);

    private static GlossaryTerm Term(string source, GlossaryTermStatus status = GlossaryTermStatus.Validated,
        string context = "", string reviewerComment = "", params (string Code, string Value)[] translations)
    {
        var term = new GlossaryTerm { Source = source, Status = status, Context = context, ReviewerComment = reviewerComment };
        foreach (var (code, value) in translations)
            term.Translations[code] = value;
        return term;
    }

    [Fact]
    public void Compute_DetectsEveryKindOfChange()
    {
        var current = new[]
        {
            Term("disjoncteur", translations: ("de-DE", "Schutzschalter")),
            Term("borne", context: "ancien", translations: ("de-DE", "Klemme")),
            Term("tension", reviewerComment: "", translations: ("de-DE", "Spannung")),
            Term("perdu", translations: ("de-DE", "Verloren")),
        };
        var imported = new[]
        {
            Term("disjoncteur", translations: ("de-DE", "Leistungsschalter")),
            Term("borne", context: "nouveau", translations: ("de-DE", "Klemme")),
            Term("tension", reviewerComment: "à préciser", translations: ("de-DE", "Spannung")),
            Term("nouveau", translations: ("de-DE", "Neu")),
        };

        var changes = GlossaryDiff.Compute(current, imported, new[] { German });

        Assert.Equal(GlossaryChangeKind.TranslationChanged, Assert.Single(changes, c => c.Source == "disjoncteur").Kind);
        Assert.Equal(GlossaryChangeKind.ContextChanged, Assert.Single(changes, c => c.Source == "borne").Kind);
        Assert.Equal(GlossaryChangeKind.ReviewerCommentChanged, Assert.Single(changes, c => c.Source == "tension").Kind);
        Assert.Equal(GlossaryChangeKind.TermAdded, Assert.Single(changes, c => c.Source == "nouveau").Kind);
        Assert.Equal(GlossaryChangeKind.TermRemoved, Assert.Single(changes, c => c.Source == "perdu").Kind);
    }

    [Fact]
    public void Compute_RemovalsAreRefusedByDefault_OthersAccepted()
    {
        var current = new[] { Term("perdu", translations: ("de-DE", "Weg")) };
        var imported = new[] { Term("nouveau", translations: ("de-DE", "Neu")) };

        var changes = GlossaryDiff.Compute(current, imported, new[] { German });

        Assert.False(Assert.Single(changes, c => c.Kind == GlossaryChangeKind.TermRemoved).Accepted);
        Assert.True(Assert.Single(changes, c => c.Kind == GlossaryChangeKind.TermAdded).Accepted);
    }

    [Fact]
    public void Compute_ComparesOnlyRequestedLanguages()
    {
        // Un classeur restreint à une équipe de langue : la colonne absente ne doit pas se lire
        // comme un effacement massif de cette langue.
        var current = new[] { Term("borne", translations: [("de-DE", "Klemme"), ("en-US", "terminal")]) };
        var imported = new[] { Term("borne", translations: ("de-DE", "Klemme")) };

        var changes = GlossaryDiff.Compute(current, imported, new[] { German });

        Assert.Empty(changes);
    }

    [Fact]
    public void Apply_AcceptedTranslationChange_ValidatesTheTerm()
    {
        var current = new[] { Term("borne", GlossaryTermStatus.InReview, translations: ("de-DE", "Klemme")) };
        var imported = new[] { Term("borne", translations: ("de-DE", "Anschlussklemme")) };
        var changes = GlossaryDiff.Compute(current, imported, new[] { German });

        var merged = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);

        var term = Assert.Single(merged);
        Assert.Equal("Anschlussklemme", term.Translations["de-DE"]);
        Assert.Equal(GlossaryTermStatus.Validated, term.Status);
    }

    [Fact]
    public void Apply_RefusedChange_LeavesValueAndStatus()
    {
        var current = new[] { Term("borne", GlossaryTermStatus.InReview, translations: ("de-DE", "Klemme")) };
        var imported = new[] { Term("borne", translations: ("de-DE", "Anschlussklemme")) };
        var changes = GlossaryDiff.Compute(current, imported, new[] { German });
        changes.Single().Accepted = false;

        var merged = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);

        var term = Assert.Single(merged);
        Assert.Equal("Klemme", term.Translations["de-DE"]);
        // Le désaccord n'est pas résolu : le terme reste En contrôle, l'arbitrage se fait dans l'éditeur.
        Assert.Equal(GlossaryTermStatus.InReview, term.Status);
    }

    [Fact]
    public void Apply_ReviewerCommentAlone_DoesNotValidate()
    {
        var current = new[] { Term("borne", GlossaryTermStatus.InReview, translations: ("de-DE", "Klemme")) };
        var imported = new[] { Term("borne", reviewerComment: "vu, à discuter", translations: ("de-DE", "Klemme")) };
        var changes = GlossaryDiff.Compute(current, imported, new[] { German });

        var merged = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);

        var term = Assert.Single(merged);
        Assert.Equal("vu, à discuter", term.ReviewerComment);
        Assert.Equal(GlossaryTermStatus.InReview, term.Status);
    }

    [Fact]
    public void Apply_AcceptedRemoval_DeletesTheTerm()
    {
        var current = new[] { Term("perdu", translations: ("de-DE", "Weg")) };
        var imported = Array.Empty<GlossaryTerm>();
        var changes = GlossaryDiff.Compute(current, imported, new[] { German });
        changes.Single().Accepted = true;

        var merged = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);

        Assert.Empty(merged);
    }

    [Fact]
    public void Apply_UnchangedInReviewTerm_PromotesOnlyWithFullCoverage()
    {
        var current = new[] { Term("borne", GlossaryTermStatus.InReview, translations: ("de-DE", "Klemme")) };
        var imported = new[] { Term("borne", translations: ("de-DE", "Klemme")) };
        var changes = GlossaryDiff.Compute(current, imported, new[] { German });
        Assert.Empty(changes);

        // Classeur complet : le silence des réviseurs clôt le contrôle.
        var full = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);
        Assert.Equal(GlossaryTermStatus.Validated, Assert.Single(full).Status);

        // Classeur partiel : le silence ne couvre pas les autres langues, le terme reste En contrôle.
        var partial = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: false);
        Assert.Equal(GlossaryTermStatus.InReview, Assert.Single(partial).Status);
    }

    [Fact]
    public void Apply_InReviewTermAbsentFromWorkbook_IsNotPromoted()
    {
        var current = new[]
        {
            Term("présent", GlossaryTermStatus.InReview, translations: ("de-DE", "Da")),
            Term("absent", GlossaryTermStatus.InReview, translations: ("de-DE", "Weg")),
        };
        var imported = new[] { Term("présent", translations: ("de-DE", "Da")) };
        var changes = GlossaryDiff.Compute(current, imported, new[] { German });
        // La suppression d'« absent » est refusée par défaut : le terme reste, mais personne ne l'a vu.
        Assert.Single(changes, c => c.Kind == GlossaryChangeKind.TermRemoved);

        var merged = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);

        Assert.Equal(GlossaryTermStatus.Validated, Assert.Single(merged, t => t.Source == "présent").Status);
        Assert.Equal(GlossaryTermStatus.InReview, Assert.Single(merged, t => t.Source == "absent").Status);
    }

    [Fact]
    public void Apply_EmptyNewTranslation_RemovesTheLanguage()
    {
        var current = new[] { Term("borne", translations: [("de-DE", "Klemme"), ("en-US", "terminal")]) };
        var imported = new[] { Term("borne", translations: ("en-US", "terminal")) };
        var changes = GlossaryDiff.Compute(current, imported, new[] { German, English });

        var merged = GlossaryDiff.Apply(current, imported, changes, reviewedAllLanguages: true);

        Assert.False(Assert.Single(merged).Translations.ContainsKey("de-DE"));
    }

    [Fact]
    public void DuplicateSourceInCurrentGlossary_IsRefused()
    {
        // Seul un glossary.json édité à la main peut en contenir : l'état ambigu est refusé
        // plutôt que départagé en silence sur le mauvais terme.
        var current = new[]
        {
            Term("borne", translations: ("de-DE", "Klemme")),
            Term("BORNE", translations: ("de-DE", "Anschluss")),
        };
        var imported = new[] { Term("borne", translations: ("de-DE", "Klemme")) };

        Assert.Throws<InvalidDataException>(() => GlossaryDiff.Compute(current, imported, new[] { German }));
    }
}
