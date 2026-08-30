namespace CheckTranslation;

internal interface ILayoutCheckService
{
    /// <summary>
    /// Analyse la mise en page des lignes pour <b>toutes</b> les langues indiquées, en une passe,
    /// et retourne les verdicts.
    ///
    /// Une passe par langue serait un gâchis : le coût dominant — découvrir les <c>.resx</c>, lire
    /// la géométrie de chaque formulaire, en déduire l'échelle — ne dépend pas de la langue. Seules
    /// les mesures de texte et la confrontation se répètent.
    ///
    /// Ne modifie <b>aucune</b> ligne : les <see cref="TranslationRow"/> sont liés au
    /// <c>DataGridView</c>, les écrire depuis le thread de fond où tourne l'analyse les exposerait
    /// à une lecture concurrente par le rendu de la grille. L'appelant applique les verdicts sur
    /// le thread d'interface.
    /// </summary>
    IReadOnlyList<LayoutVerdict> Analyze(
        string solutionPath,
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<string> languageCodes,
        TextWidthMeasurer measure);
}

/// <summary>Verdict calculé pour une ligne dans une langue, à appliquer par l'appelant.</summary>
internal sealed record LayoutVerdict(TranslationRow Row, string LanguageCode, LayoutStatus Status, string Issue)
{
    public bool IsIssue => Status is LayoutStatus.Truncated or LayoutStatus.Collision;
}
