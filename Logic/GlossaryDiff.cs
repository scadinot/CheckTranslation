namespace CheckTranslation;

/// <summary>
/// Calcule puis applique les différences entre le glossaire courant et un classeur importé.
/// Aucune dépendance à l'interface ni au disque : c'est ce qui rend le cœur du cycle d'import
/// éprouvable hors WinForms. Les comparaisons se font sur la forme normalisée
/// (<see cref="GlossaryService.NormalizeCell"/>), celle du stockage.
/// </summary>
internal static class GlossaryDiff
{
    /// <summary>
    /// Confronte le glossaire courant au contenu importé. Chaque différence devient une ligne à
    /// accepter ou refuser individuellement. Les suppressions de terme naissent refusées : une
    /// ligne perdue par un réviseur dans Excel ne doit pas effacer un terme sans un choix
    /// explicite — les autres changements naissent acceptés.
    /// </summary>
    public static List<GlossaryChange> Compute(
        IReadOnlyList<GlossaryTerm> current,
        IReadOnlyList<GlossaryTerm> imported,
        IReadOnlyList<LanguageInfo> languages)
    {
        var changes = new List<GlossaryChange>();
        var currentBySource = IndexBySource(current);
        var importedBySource = IndexBySource(imported);

        foreach (var (source, incoming) in importedBySource)
        {
            if (!currentBySource.TryGetValue(source, out var existing))
            {
                changes.Add(new GlossaryChange(GlossaryChangeKind.TermAdded, incoming.Source, null,
                    "Nouveau terme", string.Empty, DescribeTerm(incoming, languages)) { Accepted = true });
                continue;
            }

            foreach (var language in languages)
            {
                var oldValue = Normalize(existing.Translations.GetValueOrDefault(language.Code));
                var newValue = Normalize(incoming.Translations.GetValueOrDefault(language.Code));
                if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    changes.Add(new GlossaryChange(GlossaryChangeKind.TranslationChanged, existing.Source,
                        language.Code, language.Name, oldValue, newValue) { Accepted = true });
            }

            var oldContext = Normalize(existing.Context);
            var newContext = Normalize(incoming.Context);
            if (!string.Equals(oldContext, newContext, StringComparison.Ordinal))
                changes.Add(new GlossaryChange(GlossaryChangeKind.ContextChanged, existing.Source, null,
                    "Contexte", oldContext, newContext) { Accepted = true });

            var oldComment = Normalize(existing.ReviewerComment);
            var newComment = Normalize(incoming.ReviewerComment);
            if (!string.Equals(oldComment, newComment, StringComparison.Ordinal))
                changes.Add(new GlossaryChange(GlossaryChangeKind.ReviewerCommentChanged, existing.Source, null,
                    "Commentaire réviseur", oldComment, newComment) { Accepted = true });
        }

        foreach (var (source, existing) in currentBySource)
        {
            if (!importedBySource.ContainsKey(source))
                changes.Add(new GlossaryChange(GlossaryChangeKind.TermRemoved, existing.Source, null,
                    "Terme supprimé du classeur", DescribeTerm(existing, languages), string.Empty) { Accepted = false });
        }

        return changes
            .OrderBy(change => change.Source, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.FieldLabel, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Applique les changements acceptés au glossaire courant et retourne la nouvelle liste de
    /// termes. Un terme touché par un changement de fond accepté (traduction, contexte, ajout)
    /// passe <see cref="GlossaryTermStatus.Validated"/> : c'est le sens du retour de contrôle.
    /// Un commentaire réviseur seul n'est qu'une annotation, il ne valide rien. Enfin, un terme
    /// En contrôle revenu inchangé dans le classeur repasse lui aussi Validé : les réviseurs
    /// l'ont vu et laissé tel quel, le contrôle est terminé pour lui — sans cette règle il
    /// resterait exclu des prompts indéfiniment.
    /// </summary>
    public static List<GlossaryTerm> Apply(
        IReadOnlyList<GlossaryTerm> current,
        IReadOnlyList<GlossaryTerm> imported,
        IReadOnlyList<GlossaryChange> changes)
    {
        var result = current.Select(Clone).ToList();
        var resultBySource = IndexBySource(result);
        var importedBySource = IndexBySource(imported);

        foreach (var change in changes)
        {
            if (!change.Accepted)
                continue;

            var key = Normalize(change.Source);

            switch (change.Kind)
            {
                case GlossaryChangeKind.TermAdded:
                    if (!resultBySource.ContainsKey(key) && importedBySource.TryGetValue(key, out var incoming))
                    {
                        var added = Clone(incoming);
                        added.Status = GlossaryTermStatus.Validated;
                        result.Add(added);
                        resultBySource[key] = added;
                    }
                    break;

                case GlossaryChangeKind.TermRemoved:
                    if (resultBySource.TryGetValue(key, out var removed))
                    {
                        result.Remove(removed);
                        resultBySource.Remove(key);
                    }
                    break;

                case GlossaryChangeKind.TranslationChanged:
                    if (resultBySource.TryGetValue(key, out var term) && change.LanguageCode is { } code)
                    {
                        if (string.IsNullOrEmpty(change.NewValue))
                            term.Translations.Remove(code);
                        else
                            term.Translations[code] = change.NewValue;
                        term.Status = GlossaryTermStatus.Validated;
                    }
                    break;

                case GlossaryChangeKind.ContextChanged:
                    if (resultBySource.TryGetValue(key, out var contextTerm))
                    {
                        contextTerm.Context = change.NewValue;
                        contextTerm.Status = GlossaryTermStatus.Validated;
                    }
                    break;

                case GlossaryChangeKind.ReviewerCommentChanged:
                    if (resultBySource.TryGetValue(key, out var commentTerm))
                        commentTerm.ReviewerComment = change.NewValue;
                    break;
            }
        }

        foreach (var term in result)
        {
            if (term.Status == GlossaryTermStatus.InReview && importedBySource.ContainsKey(Normalize(term.Source)))
                term.Status = GlossaryTermStatus.Validated;
        }

        return result;
    }

    private static Dictionary<string, GlossaryTerm> IndexBySource(IReadOnlyList<GlossaryTerm> terms)
    {
        var index = new Dictionary<string, GlossaryTerm>(StringComparer.OrdinalIgnoreCase);
        foreach (var term in terms)
        {
            var key = Normalize(term.Source);
            if (key.Length > 0 && !index.ContainsKey(key))
                index[key] = term;
        }
        return index;
    }

    /// <summary>Résumé d'un terme pour la colonne Avant/Après d'un ajout ou d'une suppression.</summary>
    private static string DescribeTerm(GlossaryTerm term, IReadOnlyList<LanguageInfo> languages)
    {
        var parts = languages
            .Select(language => (language.Code, Value: Normalize(term.Translations.GetValueOrDefault(language.Code))))
            .Where(pair => pair.Value.Length > 0)
            .Select(pair => $"{pair.Code}: {pair.Value}");

        return string.Join(" · ", parts);
    }

    private static string Normalize(string? value) => GlossaryService.NormalizeCell(value);

    private static GlossaryTerm Clone(GlossaryTerm term) => new()
    {
        Source = term.Source,
        Context = term.Context,
        Status = term.Status,
        ReviewerComment = term.ReviewerComment,
        Translations = new Dictionary<string, string>(term.Translations, StringComparer.OrdinalIgnoreCase),
    };
}

internal enum GlossaryChangeKind
{
    TermAdded,
    TermRemoved,
    TranslationChanged,
    ContextChanged,
    ReviewerCommentChanged,
}

/// <summary>
/// Une différence entre le glossaire courant et le classeur importé, à accepter ou refuser.
/// <paramref name="LanguageCode"/> n'est renseigné que pour un changement de traduction.
/// </summary>
internal sealed record GlossaryChange(
    GlossaryChangeKind Kind,
    string Source,
    string? LanguageCode,
    string FieldLabel,
    string OldValue,
    string NewValue)
{
    public bool Accepted { get; set; }
}
