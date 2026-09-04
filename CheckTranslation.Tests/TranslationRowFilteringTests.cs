namespace CheckTranslation.Tests;

public class TranslationRowFilteringTests
{
    private static TranslationRow Row(string key = "K", string french = "", string translation = "", string comment = "")
        => new() { Project = "Proj", File = @"Properties\Msg", Key = key, French = french, Translation = translation, Comment = comment };

    private static List<TranslationRow> Filter(IReadOnlyList<TranslationRow> rows, string column, string filter)
        => TranslationRowFiltering.Filter(rows, new Dictionary<string, string> { [column] = filter });

    [Fact]
    public void TextFilter_IsCaseInsensitiveContains()
    {
        var rows = new[] { Row("BtnOk"), Row("lblTitle"), Row("btnCancel") };

        var filtered = Filter(rows, "Key", "btn");

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, r => r.Key == "lblTitle");
    }

    [Fact]
    public void TextFilter_EqualsPrefixRequiresExactMatch()
    {
        // La forme « = » existe pour le tableau de bord : Properties\Msg ne doit pas ramener Msg2.
        var rows = new[] { Row(french: "Msg"), Row(french: "Msg2") };

        var filtered = Filter(rows, "French", "=Msg");

        Assert.Single(filtered);
        Assert.Equal("Msg", filtered[0].French);
    }

    [Theory]
    [InlineData("translation:none", "sans")]
    [InlineData("translation:done", "avec")]
    public void TranslationPseudoFilters_SplitOnPresence(string filter, string expectedKey)
    {
        var rows = new[]
        {
            Row(key: "sans", french: "Texte", translation: ""),
            Row(key: "avec", french: "Texte", translation: "Text"),
        };

        var filtered = Filter(rows, "Translation", filter);

        Assert.Single(filtered);
        Assert.Equal(expectedKey, filtered[0].Key);
    }

    [Fact]
    public void TranslationSame_MatchesIdenticalIgnoringCaseAndEdges()
    {
        var rows = new[]
        {
            Row(key: "same", french: "Total", translation: "  total "),
            Row(key: "differs", french: "Fichier", translation: "File"),
            Row(key: "empty", french: "Texte", translation: ""),
        };

        var filtered = Filter(rows, "Translation", "translation:same");

        Assert.Single(filtered);
        Assert.Equal("same", filtered[0].Key);
    }

    [Fact]
    public void ScoreNone_MatchesUnparsableComments()
    {
        var rows = new[]
        {
            Row(key: "scored", comment: "095 - ok"),
            Row(key: "unscored", comment: "à relire"),
            Row(key: "blank", comment: ""),
        };

        var filtered = Filter(rows, "Comment", "score:none");

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, r => r.Key == "scored");
    }

    [Fact]
    public void ScoreBelow_IncludesUnscoredAndIsInclusive()
    {
        var rows = new[]
        {
            Row(key: "low", comment: "060 - limite incluse"),
            Row(key: "high", comment: "061 - au-dessus"),
            Row(key: "none", comment: "pas de score"),
        };

        var filtered = Filter(rows, "Comment", "score<60");

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, r => r.Key == "high");
    }

    [Fact]
    public void ScoreAtLeast_ExcludesUnscored()
    {
        var rows = new[]
        {
            Row(key: "keep", comment: "090 - bon"),
            Row(key: "drop", comment: "089 - juste sous"),
            Row(key: "none", comment: "sans score"),
        };

        var filtered = Filter(rows, "Comment", "score>=90");

        Assert.Single(filtered);
        Assert.Equal("keep", filtered[0].Key);
    }

    [Fact]
    public void ScoreBucket_IsClosedRange()
    {
        var rows = new[]
        {
            Row(key: "in-low", comment: "060 - borne basse"),
            Row(key: "in-high", comment: "069 - borne haute"),
            Row(key: "out", comment: "070 - au-dessus"),
        };

        var filtered = Filter(rows, "Comment", "score:60-69");

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, r => r.Key == "out");
    }

    [Fact]
    public void LayoutFilters_MatchActiveVerdict()
    {
        var truncated = Row(key: "trunc");
        truncated.SetLayoutVerdict("de-DE", LayoutStatus.Truncated, "déborde");
        truncated.SelectLayoutVerdict("de-DE");

        var ok = Row(key: "ok");
        ok.SetLayoutVerdict("de-DE", LayoutStatus.Ok, string.Empty);
        ok.SelectLayoutVerdict("de-DE");

        var rows = new[] { truncated, ok };

        Assert.Equal("trunc", Assert.Single(Filter(rows, "LayoutIssue", "layout:issues")).Key);
        Assert.Equal("trunc", Assert.Single(Filter(rows, "LayoutIssue", "layout:truncation")).Key);
        Assert.Equal("ok", Assert.Single(Filter(rows, "LayoutIssue", "layout:ok")).Key);
        Assert.Empty(Filter(rows, "LayoutIssue", "layout:collision"));
    }

    [Fact]
    public void Filters_Combine()
    {
        var rows = new[]
        {
            Row(key: "btnOk", french: "Valider", translation: "OK"),
            Row(key: "btnCancel", french: "Annuler", translation: ""),
            Row(key: "lblTitle", french: "Valider", translation: "Validate"),
        };

        var filtered = TranslationRowFiltering.Filter(rows, new Dictionary<string, string>
        {
            ["French"] = "valider",
            ["Translation"] = "translation:done",
        });

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, r => r.Key == "btnCancel");
    }

    [Fact]
    public void UnknownColumn_IsIgnored()
    {
        var rows = new[] { Row("a"), Row("b") };

        var filtered = Filter(rows, "Inconnue", "xyz");

        Assert.Equal(2, filtered.Count);
    }
}
