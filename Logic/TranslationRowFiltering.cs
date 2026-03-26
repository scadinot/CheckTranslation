namespace CheckTranslation;

internal static class TranslationRowFiltering
{
    public static List<TranslationRow> Filter(IReadOnlyList<TranslationRow> allRows, IReadOnlyDictionary<string, string> filters)
    {
        IEnumerable<TranslationRow> filtered = allRows;

        foreach (var (prop, filter) in filters)
        {
            filtered = prop switch
            {
                "Project" => filtered.Where(r => r.Project.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "File" => filtered.Where(r => r.File.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "Key" => filtered.Where(r => r.Key.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "French" => filtered.Where(r => r.French.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "Translation" => filtered.Where(r => r.Translation.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "Comment" => ApplyCommentFilter(filtered, filter),
                _ => filtered,
            };
        }

        return filtered.ToList();
    }

    private static IEnumerable<TranslationRow> ApplyCommentFilter(IEnumerable<TranslationRow> rows, string filter)
    {
        const string scorePrefix = "score<";
        const string scoreMinPrefix = "score>=";
        const string scoreNone = "score:none";

        if (string.Equals(filter, scoreNone, StringComparison.OrdinalIgnoreCase))
            return rows.Where(r => !QualityScore.TryParse(r.Comment, out _));

        if (filter.StartsWith(scorePrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(filter[scorePrefix.Length..], out var threshold))
        {
            return rows.Where(r => !QualityScore.TryParse(r.Comment, out var score) || score <= threshold);
        }

        if (filter.StartsWith(scoreMinPrefix, StringComparison.OrdinalIgnoreCase)
            && int.TryParse(filter[scoreMinPrefix.Length..], out var minScore))
        {
            return rows.Where(r => QualityScore.TryParse(r.Comment, out var score) && score >= minScore);
        }

        return rows.Where(r => r.Comment.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
