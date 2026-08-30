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
                "Project" => ApplyTextFilter(filtered, filter, row => row.Project),
                "File" => ApplyTextFilter(filtered, filter, row => row.File),
                "Key" => ApplyTextFilter(filtered, filter, row => row.Key),
                "French" => ApplyTextFilter(filtered, filter, row => row.French),
                "Translation" => ApplyTranslationFilter(filtered, filter),
                "Comment" => ApplyCommentFilter(filtered, filter),
                "LayoutIssue" => ApplyLayoutFilter(filtered, filter),
                _ => filtered,
            };
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Filtre texte d'une colonne. Un filtre saisi à la main est un « contient » — c'est ce qu'on
    /// attend d'une zone de recherche. Un filtre préfixé de <c>=</c> exige l'égalité exacte.
    ///
    /// Cette forme exacte existe pour le tableau de bord : il annonce un nombre de lignes et doit
    /// en ramener exactement autant. Avec un « contient », un double-clic sur le fichier
    /// <c>Properties\Msg</c> ramènerait aussi <c>Properties\Msg2</c>, et le même chemin présent
    /// dans deux projets ramènerait les deux — le compteur et la grille ne diraient plus la même
    /// chose.
    /// </summary>
    private static IEnumerable<TranslationRow> ApplyTextFilter(
        IEnumerable<TranslationRow> rows,
        string filter,
        Func<TranslationRow, string> selector)
        => filter.StartsWith('=')
            ? rows.Where(row => string.Equals(selector(row), filter[1..], StringComparison.OrdinalIgnoreCase))
            : rows.Where(row => selector(row).Contains(filter, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Pseudo-filtres de la colonne de traduction : « translation:none » (non traduite),
    /// « translation:done » (traduite), « translation:same » (traduction identique au français).
    ///
    /// « Identique au français » est un signal, pas une erreur : un libellé comme « Total » ou
    /// « Configuration » peut légitimement ne pas changer. C'est en revanche la signature d'une
    /// recopie faite pour combler un vide.
    /// </summary>
    private static IEnumerable<TranslationRow> ApplyTranslationFilter(IEnumerable<TranslationRow> rows, string filter)
        => filter.ToLowerInvariant() switch
        {
            "translation:none" => rows.Where(r => string.IsNullOrWhiteSpace(r.Translation)),
            "translation:done" => rows.Where(r => !string.IsNullOrWhiteSpace(r.Translation)),
            "translation:same" => rows.Where(IsSameAsFrench),
            _ => ApplyTextFilter(rows, filter, row => row.Translation),
        };

    /// <summary>Traduction non vide et identique au texte source, aux espaces et à la casse près.</summary>
    public static bool IsSameAsFrench(TranslationRow row)
        => !string.IsNullOrWhiteSpace(row.Translation)
            && string.Equals(row.Translation.Trim(), row.French.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Pseudo-filtres de la colonne de mise en page : « layout:issues » (tout défaut),
    /// « layout:truncation », « layout:collision », « layout:unverifiable », « layout:ok ».
    /// </summary>
    private static IEnumerable<TranslationRow> ApplyLayoutFilter(IEnumerable<TranslationRow> rows, string filter)
        => filter switch
        {
            "layout:issues" => rows.Where(r => r.LayoutStatus is LayoutStatus.Truncated or LayoutStatus.Collision),
            "layout:truncation" => rows.Where(r => r.LayoutStatus == LayoutStatus.Truncated),
            "layout:collision" => rows.Where(r => r.LayoutStatus == LayoutStatus.Collision),
            "layout:unverifiable" => rows.Where(r => r.LayoutStatus == LayoutStatus.Unverifiable),
            "layout:ok" => rows.Where(r => r.LayoutStatus == LayoutStatus.Ok),
            _ => rows.Where(r => r.LayoutIssue.Contains(filter, StringComparison.OrdinalIgnoreCase)),
        };

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

        // Tranche fermée « score:60-69 », pour rendre cliquable la distribution du tableau de bord.
        if (filter.StartsWith("score:", StringComparison.OrdinalIgnoreCase))
        {
            var bounds = filter["score:".Length..].Split('-');
            if (bounds.Length == 2 && int.TryParse(bounds[0], out var low) && int.TryParse(bounds[1], out var high))
                return rows.Where(r => QualityScore.TryParse(r.Comment, out var score) && score >= low && score <= high);
        }

        return rows.Where(r => r.Comment.Contains(filter, StringComparison.OrdinalIgnoreCase));
    }
}
