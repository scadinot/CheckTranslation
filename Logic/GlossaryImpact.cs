namespace CheckTranslation;

/// <summary>
/// Détection des lignes impactées par un changement du glossaire (GLOSSAIRE.md, phase 4 :
/// retraduction ciblée). Compare la projection injectée dans les prompts (termes Validé), langue
/// par langue, entre deux instants — l'ouverture et la fermeture de l'éditeur de glossaire — puis
/// sélectionne les lignes dont le français contient un terme dont la contrainte a changé.
/// Aucune dépendance à l'interface ni au service : éprouvable hors WinForms.
///
/// Deux décisions assumées (GLOSSAIRE.md) : un terme disparu de la projection ne déclenche pas de
/// retraduction — une suppression lève une contrainte, elle n'invalide pas les traductions
/// existantes (un terme erroné se corrige, il ne se supprime pas) ; et la correspondance est une
/// inclusion insensible à la casse — les formes fléchies éloignées du terme canonique peuvent lui
/// échapper, et l'approximation retraduit parfois une ligne de trop plutôt qu'une de moins.
/// </summary>
internal static class GlossaryImpact
{
    /// <summary>
    /// Termes dont la contrainte de prompt a changé entre deux projections, par code de langue :
    /// apparus (terme nouveau, promotion Validé, traduction ajoutée pour la langue) ou modifiés
    /// (destination ou contexte). Les disparus sont ignorés, à dessein. Seules les langues ayant
    /// au moins un terme changé figurent dans le résultat.
    /// </summary>
    public static Dictionary<string, List<string>> ComputeChangedTerms(
        IReadOnlyDictionary<string, IReadOnlyList<GlossaryEntry>> before,
        IReadOnlyDictionary<string, IReadOnlyList<GlossaryEntry>> after)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (code, afterEntries) in after)
        {
            var beforeBySource = IndexBySource(
                before.TryGetValue(code, out var beforeEntries) ? beforeEntries : Array.Empty<GlossaryEntry>());
            var changed = new List<string>();

            foreach (var entry in afterEntries)
            {
                if (string.IsNullOrEmpty(entry.Source))
                    continue;

                if (!beforeBySource.TryGetValue(entry.Source, out var old))
                    changed.Add(entry.Source);
                else if (!string.Equals(old.Destination, entry.Destination, StringComparison.Ordinal)
                    || !string.Equals(old.Context, entry.Context, StringComparison.Ordinal))
                    changed.Add(entry.Source);
            }

            if (changed.Count > 0)
                result[code] = changed;
        }

        return result;
    }

    /// <summary>
    /// Lignes dont le français contient au moins un des termes, en inclusion insensible à la
    /// casse. L'ordre d'origine est conservé et chaque ligne n'apparaît qu'une fois, même si
    /// plusieurs termes la touchent.
    /// </summary>
    public static List<TranslationRow> SelectImpactedRows(
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<string> termSources)
    {
        var impacted = new List<TranslationRow>();

        foreach (var row in rows)
        {
            if (string.IsNullOrEmpty(row.French))
                continue;

            foreach (var source in termSources)
            {
                if (source.Length > 0 && row.French.Contains(source, StringComparison.OrdinalIgnoreCase))
                {
                    impacted.Add(row);
                    break;
                }
            }
        }

        return impacted;
    }

    private static Dictionary<string, GlossaryEntry> IndexBySource(IReadOnlyList<GlossaryEntry> entries)
    {
        // Les projections viennent du service, dont le stockage garantit l'unicité des sources
        // (ReplaceTerms déduplique, l'éditeur et l'import refusent les doublons). Ce dictionnaire
        // ne sert qu'à la comparaison : un doublon résiduel ne coûterait qu'une retraduction de
        // trop, jamais une perte de données.
        var index = new Dictionary<string, GlossaryEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
            index.TryAdd(entry.Source, entry);
        return index;
    }
}
