namespace CheckTranslation.Tests;

public class TranslationStatisticsTests
{
    private static readonly LanguageInfo German = new("de-DE", "Allemand", 7);
    private static readonly LanguageInfo English = new("en-US", "Anglais", 9);

    private static TranslationRow Row(string project, string file, string key, string french,
        string? deTranslation = null, string? deComment = null)
    {
        var row = new TranslationRow { Project = project, File = file, Key = key, French = french };
        if (deTranslation is not null)
            row.Translations["de-DE"] = deTranslation;
        if (deComment is not null)
            row.Comments["de-DE"] = deComment;
        return row;
    }

    [Fact]
    public void Compute_CountsWithinTranslatedOnly()
    {
        var rows = new[]
        {
            Row("P", "F", "k1", "Bonjour", "Hallo", "095 - bon"),
            Row("P", "F", "k2", "Fichier", "Datei"),
            Row("P", "F", "k3", "Total", "Total"),
            // Score resté sur une ligne non traduite : périmé, ne compte ni comme vérifiée ni
            // dans la moyenne — sinon « vérifiées » pourrait dépasser « traduites ».
            Row("P", "F", "k4", "Annuler", deComment: "080 - périmé"),
        };

        var overview = TranslationStatistics.Compute(rows, new[] { German });
        var de = overview.Languages.Single();

        Assert.Equal(4, de.Total);
        Assert.Equal(3, de.Translated);
        Assert.Equal(1, de.Untranslated);
        Assert.Equal(1, de.SameAsSource);
        Assert.Equal(1, de.Verified);
        Assert.Equal(95, de.AverageScore);
    }

    [Fact]
    public void Compute_AverageIsNullWhenNothingVerified()
    {
        var rows = new[] { Row("P", "F", "k1", "Bonjour", "Hallo") };

        var de = TranslationStatistics.Compute(rows, new[] { German }).Languages.Single();

        // Nul et non zéro : zéro se lirait « toutes mauvaises ».
        Assert.Null(de.AverageScore);
        Assert.Equal(0, de.VerifiedRatio);
    }

    [Fact]
    public void Compute_CountsDistinctProjectsAndFiles()
    {
        var rows = new[]
        {
            Row("P1", "F1", "k1", "a"),
            Row("P1", "F1", "k2", "b"),
            Row("P1", "F2", "k1", "c"),
            // Même chemin de fichier dans un autre projet : c'est un fichier distinct.
            Row("P2", "F1", "k1", "d"),
        };

        var overview = TranslationStatistics.Compute(rows, new[] { German });

        Assert.Equal(4, overview.Rows);
        Assert.Equal(2, overview.Projects);
        Assert.Equal(3, overview.Files);
    }

    [Fact]
    public void Compute_LayoutsEmptyAndTotalIssuesNullWhenNothingAnalyzed()
    {
        var rows = new[] { Row("P", "F", "k1", "a", "b") };

        var overview = TranslationStatistics.Compute(rows, new[] { German });

        Assert.Empty(overview.Layouts);
        // Nul et non zéro : zéro se lirait « aucun défaut ».
        Assert.Null(overview.TotalLayoutIssues);
    }

    [Fact]
    public void Compute_LayoutCountsPerLanguage()
    {
        var analyzed = Row("P", "F", "k1", "a", "b");
        analyzed.SetLayoutVerdict("de-DE", LayoutStatus.Truncated, "déborde");
        analyzed.SetLayoutVerdict("en-US", LayoutStatus.Ok, string.Empty);

        var overview = TranslationStatistics.Compute(new[] { analyzed }, new[] { German, English });

        Assert.Equal(2, overview.Layouts.Count);
        Assert.Equal(1, overview.Layouts.Single(l => l.LanguageCode == "de-DE").Truncated);
        Assert.Equal(1, overview.Layouts.Single(l => l.LanguageCode == "en-US").Ok);
        Assert.Equal(1, overview.TotalLayoutIssues);
        Assert.Equal("de-DE", overview.WorstLayout?.LanguageCode);
    }

    [Fact]
    public void ScoreBuckets_AreTheSingleSourceOfTruth()
    {
        var buckets = TranslationStatistics.ScoreBuckets().ToList();

        Assert.Equal(5, buckets.Count);
        Assert.Equal("score:0-59", buckets[0].Filter);
        Assert.Equal("score:90-100", buckets[^1].Filter);

        // Chaque libellé doit retrouver son filtre : la liste déroulante de la grille en dépend.
        foreach (var (label, filter) in buckets)
        {
            Assert.True(TranslationStatistics.TryGetScoreBucketFilter(label, out var found));
            Assert.Equal(filter, found);
        }
    }

    [Fact]
    public void BucketCounts_MatchBucketFilters()
    {
        // Un score compté dans une tranche doit être ramené par le filtre de la même tranche,
        // sinon un clic sur « 42 » ne ramène pas 42 lignes.
        var rows = new[]
        {
            Row("P", "F", "k1", "a", "t", "059 - haut de la première tranche"),
            Row("P", "F", "k2", "b", "t", "060 - bas de la deuxième"),
            Row("P", "F", "k3", "c", "t", "100 - plafond"),
        };

        var de = TranslationStatistics.Compute(rows, new[] { German }).Languages.Single();
        Assert.Equal(1, de.ScoreBuckets[0]);
        Assert.Equal(1, de.ScoreBuckets[1]);
        Assert.Equal(1, de.ScoreBuckets[^1]);

        var filters = TranslationStatistics.ScoreBuckets().Select(b => b.Filter).ToList();
        for (int i = 0; i < filters.Count; i++)
        {
            var filtered = TranslationRowFiltering.Filter(
                rows.Select(r => { r.SelectLanguage("de-DE"); return r; }).ToList(),
                new Dictionary<string, string> { ["Comment"] = filters[i] });
            Assert.Equal(de.ScoreBuckets[i], filtered.Count);
        }
    }

    [Fact]
    public void ComputeGroups_SortsLeastAdvancedFirst()
    {
        var rows = new[]
        {
            Row("Avancé", "F", "k1", "a", "done"),
            Row("Avancé", "F", "k2", "b", "done"),
            Row("EnRetard", "F", "k1", "c", "done"),
            Row("EnRetard", "F", "k2", "d"),
        };

        var groups = TranslationStatistics.ComputeGroups(rows, "de-DE", GroupBy.Project);

        Assert.Equal(2, groups.Count);
        Assert.Equal("EnRetard", groups[0].Name);
        Assert.Equal(0.5, groups[0].TranslatedRatio);
        Assert.Equal("Avancé", groups[1].Name);
    }
}
