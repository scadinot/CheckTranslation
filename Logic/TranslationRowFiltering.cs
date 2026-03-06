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
                "Comment" => filtered.Where(r => r.Comment.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                _ => filtered,
            };
        }

        return filtered.ToList();
    }
}
