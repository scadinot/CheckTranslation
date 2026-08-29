namespace CheckTranslation;

internal interface ILayoutCheckService
{
    /// <summary>
    /// Analyse la mise en page des lignes pour la langue indiquée et renseigne
    /// <see cref="TranslationRow.LayoutStatus"/> / <see cref="TranslationRow.LayoutIssue"/>.
    /// Retourne le nombre de lignes présentant un défaut.
    /// </summary>
    int Analyze(string solutionPath, IReadOnlyList<TranslationRow> rows, string languageCode, TextWidthMeasurer measure);
}
