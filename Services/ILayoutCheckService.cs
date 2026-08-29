namespace CheckTranslation;

internal interface ILayoutCheckService
{
    /// <summary>
    /// Analyse la mise en page des lignes pour la langue indiquée et retourne les verdicts.
    ///
    /// Ne modifie <b>aucune</b> ligne : les <see cref="TranslationRow"/> sont liés au
    /// <c>DataGridView</c>, les écrire depuis le thread de fond où tourne l'analyse les exposerait
    /// à une lecture concurrente par le rendu de la grille. L'appelant applique les verdicts sur
    /// le thread d'interface.
    /// </summary>
    IReadOnlyList<LayoutVerdict> Analyze(string solutionPath, IReadOnlyList<TranslationRow> rows, string languageCode, TextWidthMeasurer measure);
}

/// <summary>Verdict calculé pour une ligne, à appliquer par l'appelant.</summary>
internal sealed record LayoutVerdict(TranslationRow Row, LayoutStatus Status, string Issue)
{
    public bool IsIssue => Status is LayoutStatus.Truncated or LayoutStatus.Collision;
}
