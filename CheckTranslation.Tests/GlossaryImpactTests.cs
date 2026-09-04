namespace CheckTranslation.Tests;

public class GlossaryImpactTests
{
    private static GlossaryEntry Entry(string source, string destination, string context = "")
        => new() { Source = source, Destination = destination, Context = context };

    private static Dictionary<string, IReadOnlyList<GlossaryEntry>> Projection(
        params (string Code, GlossaryEntry[] Entries)[] byLanguage)
    {
        var projection = new Dictionary<string, IReadOnlyList<GlossaryEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, entries) in byLanguage)
            projection[code] = entries;
        return projection;
    }

    [Fact]
    public void ComputeChangedTerms_DetectsAddedAndModified_IgnoresRemoved()
    {
        var before = Projection(
            ("en-US", new[] { Entry("disjoncteur", "circuit breaker"), Entry("borne", "terminal") }),
            ("de-DE", new[] { Entry("disjoncteur", "Leistungsschalter") }));
        var after = Projection(
            ("en-US", new[] { Entry("disjoncteur", "breaker"), Entry("tension", "voltage") }),
            ("de-DE", new[] { Entry("disjoncteur", "Leistungsschalter") }));

        var changed = GlossaryImpact.ComputeChangedTerms(before, after);

        // de-DE strictement identique : absent du résultat. « borne » supprimé : ignoré, à dessein.
        var enUs = Assert.Single(changed).Value;
        Assert.Equal(2, enUs.Count);
        Assert.Contains("disjoncteur", enUs);
        Assert.Contains("tension", enUs);
    }

    [Fact]
    public void ComputeChangedTerms_ContextChangeAloneImpacts()
    {
        // Le contexte est injecté dans les prompts : le changer change la contrainte.
        var before = Projection(("en-US", new[] { Entry("borne", "terminal", "ancien") }));
        var after = Projection(("en-US", new[] { Entry("borne", "terminal", "nouveau") }));

        var changed = GlossaryImpact.ComputeChangedTerms(before, after);

        Assert.Equal("borne", Assert.Single(Assert.Single(changed).Value));
    }

    [Fact]
    public void ComputeChangedTerms_LanguageAbsentBefore_AllTermsImpact()
    {
        // Première validation d'une langue entière (promotion après contrôle) : tout apparaît.
        var before = new Dictionary<string, IReadOnlyList<GlossaryEntry>>(StringComparer.OrdinalIgnoreCase);
        var after = Projection(("en-US", new[] { Entry("borne", "terminal"), Entry("tension", "voltage") }));

        var changed = GlossaryImpact.ComputeChangedTerms(before, after);

        Assert.Equal(2, Assert.Single(changed).Value.Count);
    }

    [Fact]
    public void ComputeChangedTerms_NoChange_EmptyResult()
    {
        var projection = Projection(("en-US", new[] { Entry("borne", "terminal") }));

        Assert.Empty(GlossaryImpact.ComputeChangedTerms(projection, projection));
    }

    [Fact]
    public void SelectImpactedRows_IsCaseInsensitive_KeepsOrder_DeduplicatesRows()
    {
        var rows = new[]
        {
            new TranslationRow { Key = "k1", French = "Le disjoncteur principal protège la TENSION" },
            new TranslationRow { Key = "k2", French = "La tension mesurée est haute" },
            new TranslationRow { Key = "k3", French = "Aucun terme ici" },
            new TranslationRow { Key = "k4", French = "" },
        };

        var impacted = GlossaryImpact.SelectImpactedRows(rows, new[] { "tension", "disjoncteur" });

        // k1 contient les deux termes : une seule fois. L'ordre d'origine est conservé.
        Assert.Equal(2, impacted.Count);
        Assert.Equal("k1", impacted[0].Key);
        Assert.Equal("k2", impacted[1].Key);
    }

    [Fact]
    public void SelectImpactedRows_EmptyTermList_SelectsNothing()
    {
        var rows = new[] { new TranslationRow { Key = "k1", French = "texte" } };

        Assert.Empty(GlossaryImpact.SelectImpactedRows(rows, Array.Empty<string>()));
    }
}
