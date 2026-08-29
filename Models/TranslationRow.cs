namespace CheckTranslation;

/// <summary>
/// Représente une ligne de traduction, indépendamment de la source (export Excel ResX Manager
/// ou fichiers .resx lus directement). L'identité fonctionnelle d'une ligne est
/// <see cref="Project"/> | <see cref="File"/> | <see cref="Key"/>.
/// </summary>
internal sealed class TranslationRow
{
    /// <summary>
    /// Numéro de ligne dans la feuille Excel. Uniquement renseigné par la source Excel
    /// (localisation d'écriture) ; vaut 0 pour la source .resx, qui se repère par Project/File/Key.
    /// </summary>
    public int RowNumber { get; set; }
    public string Project { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string FrenchComment { get; set; } = string.Empty;
    public string French { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    /// <summary>Traductions par code de langue (ex. « de-DE »).</summary>
    public Dictionary<string, string> Translations { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Commentaires (score de vérification) par code de langue.</summary>
    public Dictionary<string, string> Comments { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void SwitchLanguage(string oldLanguageCode, string newLanguageCode)
    {
        CommitActiveLanguage(oldLanguageCode);
        SelectLanguage(newLanguageCode);
    }

    /// <summary>
    /// Charge la vue active depuis les dictionnaires, sans rien y pousser. Utilisé juste après
    /// le chargement d'une source, qui remplit les dictionnaires mais ignore la langue affichée.
    /// </summary>
    public void SelectLanguage(string languageCode)
    {
        Translation = Translations.GetValueOrDefault(languageCode, string.Empty);
        Comment = Comments.GetValueOrDefault(languageCode, string.Empty);
        ClearLayoutVerdict();
    }

    /// <summary>
    /// Pousse la vue active (Translation / Comment, éditée par l'UI) dans les dictionnaires
    /// sous le code de langue courant. À appeler avant toute écriture disque.
    /// </summary>
    public void CommitActiveLanguage(string languageCode)
    {
        Translations[languageCode] = Translation;
        Comments[languageCode] = Comment;
    }

    /// <summary>Verdict de la vérification de mise en page, pour la langue affichée.</summary>
    public LayoutStatus LayoutStatus { get; private set; } = LayoutStatus.NotChecked;

    /// <summary>Libellé du défaut, affiché dans la grille. Vide si aucun défaut.</summary>
    public string LayoutIssue { get; private set; } = string.Empty;

    internal void SetLayoutVerdict(LayoutStatus status, string issue)
    {
        LayoutStatus = status;
        LayoutIssue = issue;
    }

    /// <summary>
    /// Remet le verdict à « non analysé ». Appelé avant toute analyse et à chaque changement de
    /// langue : un verdict porte sur une traduction précise, le conserver induirait en erreur.
    /// </summary>
    internal void ClearLayoutVerdict() => SetLayoutVerdict(LayoutStatus.NotChecked, string.Empty);

    public string GetSyncKey()
        => string.Join("\u001F", Project.Trim(), File.Trim(), Key.Trim());
}
