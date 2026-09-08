using System.Text.RegularExpressions;

namespace CheckTranslation;

/// <summary>
/// Écarts au glossaire : les lignes dont la traduction n'emploie pas le terme imposé alors que
/// le français contient le terme source. C'est le contrôle que fait <c>glossary.py check</c> dans
/// l'outillage resx-tools d'elec calc, porté ici à l'identique pour que l'application et les
/// skills comptent la même chose sur le même <c>glossary.json</c> :
/// - côté français, des mots entiers (« terre » ne se déclenche pas sur « atterrissage ») ;
/// - côté cible, une inclusion insensible à la casse, chaque forme d'une cellule à variantes
///   (« kabel / kabl ») valant, avec tolérance sur la dernière lettre d'un mot d'au moins cinq
///   caractères (« curva » couvre « curve ») — le contrôle propose une relecture, il ne
///   prononce pas une faute ;
/// - pour le chinois, comparaison sur les seuls caractères CJK, sauf pour une forme latine
///   (« RCD », « MPPT ») cherchée dans le texte brut ;
/// - quand deux termes se recouvrent, seul le plus long est contrôlé : « transformateur de
///   courant » n'est pas contrôlé sur « transformateur ».
/// Aucune dépendance à l'interface ni au service : éprouvable hors WinForms. Toute évolution ici
/// doit être reportée dans <c>glossary.py</c>, et réciproquement.
/// </summary>
internal static partial class GlossaryDeviation
{
    [GeneratedRegex(@"[A-Za-zÀ-ÿĀ-ſ][A-Za-zÀ-ÿĀ-ſ'’-]*")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"[一-鿿]")]
    private static partial Regex CjkRegex();

    /// <summary>Formes acceptées d'une cellule : « kabel / kabl » → « kabel », « kabl ».</summary>
    public static IReadOnlyList<string> Variants(string? cell)
        => (cell ?? string.Empty)
            .Split('/')
            .Select(v => v.Trim())
            .Where(v => v.Length > 0)
            .ToList();

    /// <summary>Le français contient le terme, en mots entiers et dans l'ordre.</summary>
    public static bool FrenchContains(string? french, string? term)
    {
        var words = Words(french);
        var needle = Words(term);
        if (needle.Count == 0 || words.Count < needle.Count)
            return false;

        for (int i = 0; i <= words.Count - needle.Count; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Count && match; j++)
                match = string.Equals(words[i + j], needle[j], StringComparison.Ordinal);
            if (match)
                return true;
        }

        return false;
    }

    /// <summary>La traduction emploie l'une des formes acceptées de la cellule.</summary>
    public static bool TargetContains(string? value, string? cell, string languageCode)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var variant in Variants(cell))
        {
            if (string.Equals(languageCode, "zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                // Ponctuation et espaces sont erratiques en chinois : on compare sur les seuls
                // caractères CJK. Un terme rendu par un sigle latin n'y survivrait pas : il est
                // alors cherché dans le texte brut.
                var haystack = CjkRegex().IsMatch(variant)
                    ? string.Concat(CjkRegex().Matches(value).Select(m => m.Value))
                    : value;
                if (haystack.Contains(variant, StringComparison.Ordinal))
                    return true;
                continue;
            }

            var low = value.ToLowerInvariant();
            var wanted = variant.ToLowerInvariant();
            if (low.Contains(wanted, StringComparison.Ordinal))
                return true;

            // Flexion des langues romanes sur la finale : la dernière lettre du dernier mot est
            // retirée avant comparaison, quand ce mot compte au moins cinq caractères.
            int space = wanted.LastIndexOf(' ');
            var head = space >= 0 ? wanted[..space] : string.Empty;
            var last = space >= 0 ? wanted[(space + 1)..] : wanted;
            if (last.Length >= 5)
            {
                var stem = head.Length > 0 ? head + " " + last[..^1] : last[..^1];
                if (low.Contains(stem, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Projection des termes Validé sur une langue pour le contrôle : <b>tous</b> les termes, y
    /// compris ceux sans cellule pour cette langue (Destination vide). Ils ne seront pas
    /// contrôlés, mais ils participent à la règle du plus long terme — « régime de neutre » sans
    /// cellule allemande doit continuer de masquer « neutre » dans « régime de neutre TT »,
    /// exactement comme dans <c>glossary.py</c>. La projection prompts (<c>GetPromptEntries</c>) ne
    /// conviendrait pas : elle ne porte que les termes traduits.
    /// </summary>
    public static List<GlossaryEntry> ControlledEntries(IReadOnlyList<GlossaryTerm> terms, string languageCode)
        => terms
            .Where(t => t.Status == GlossaryTermStatus.Validated && !string.IsNullOrWhiteSpace(t.Source))
            .Select(t => new GlossaryEntry
            {
                Source = t.Source,
                Destination = t.Translations.GetValueOrDefault(languageCode, string.Empty),
                Context = t.Context,
            })
            .ToList();

    /// <summary>
    /// Termes présents dans le français et contrôlables : le plus long l'emporte sur ceux qu'il
    /// contient, <i>avant</i> d'écarter les entrées sans traduction (cellule vide). L'ordre compte :
    /// un terme long sans cellule masque quand même le terme court qu'il contient.
    /// </summary>
    public static List<GlossaryEntry> MatchingTerms(string? french, IReadOnlyList<GlossaryEntry> entries)
    {
        var matched = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Source) && FrenchContains(french, e.Source))
            .ToList();

        return matched
            .Where(e => !matched.Any(other => !ReferenceEquals(other, e)
                && other.Source.Contains(e.Source, StringComparison.OrdinalIgnoreCase)
                && other.Source.Length > e.Source.Length))
            .Where(e => !string.IsNullOrWhiteSpace(e.Destination))
            .ToList();
    }

    /// <summary>
    /// Vrai si la ligne est traduite dans cette langue et qu'au moins un terme attendu manque à
    /// la traduction. Une ligne non traduite n'est pas un écart : il n'y a rien à retraduire.
    /// </summary>
    public static bool IsDeviation(TranslationRow row, string languageCode, IReadOnlyList<GlossaryEntry> entries)
    {
        var translation = row.Translations.GetValueOrDefault(languageCode);
        if (string.IsNullOrWhiteSpace(translation))
            return false;

        return MatchingTerms(row.French, entries)
            .Any(entry => !TargetContains(translation, entry.Destination, languageCode));
    }

    /// <summary>Lignes en écart pour une langue, dans l'ordre d'origine.</summary>
    public static List<TranslationRow> SelectDeviations(
        IReadOnlyList<TranslationRow> rows,
        string languageCode,
        IReadOnlyList<GlossaryEntry> entries)
    {
        if (entries.Count == 0)
            return new List<TranslationRow>();

        return rows.Where(row => IsDeviation(row, languageCode, entries)).ToList();
    }

    private static List<string> Words(string? text)
        => WordRegex().Matches(text ?? string.Empty).Select(m => m.Value.ToLowerInvariant()).ToList();
}
