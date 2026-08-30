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
        SelectLayoutVerdict(languageCode);
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

    /// <summary>
    /// Verdicts de mise en page <b>par code de langue</b>, sur le même modèle que
    /// <see cref="Translations"/> et <see cref="Comments"/>.
    ///
    /// Une seule case ne suffisait pas : un verdict décrit une traduction précise, donc une langue
    /// précise. Tant qu'il n'y en avait qu'une, changer de langue obligeait à tout effacer — et à
    /// relancer l'analyse. Les stocker par langue permet d'analyser les sept en une passe : la
    /// partie coûteuse (lecture de la géométrie des formulaires) est commune, seules les mesures
    /// se répètent.
    /// </summary>
    private readonly Dictionary<string, LayoutVerdictEntry> _layoutVerdicts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Verdict de la vérification de mise en page, pour la langue affichée.</summary>
    public LayoutStatus LayoutStatus { get; private set; } = LayoutStatus.NotChecked;

    /// <summary>Libellé du défaut, affiché dans la grille. Vide si aucun défaut.</summary>
    public string LayoutIssue { get; private set; } = string.Empty;

    /// <summary>
    /// Charge la vue active depuis les verdicts stockés. Appelé par <see cref="SelectLanguage"/> :
    /// changer de langue n'efface plus rien, il rebascule simplement sur le verdict de la langue
    /// affichée.
    /// </summary>
    public void SelectLayoutVerdict(string languageCode)
    {
        var verdict = _layoutVerdicts.GetValueOrDefault(languageCode);
        LayoutStatus = verdict.Status;
        LayoutIssue = verdict.Issue ?? string.Empty;
    }

    /// <summary>Verdict d'une langue quelconque, sans toucher à la vue active.</summary>
    public LayoutStatus GetLayoutStatus(string languageCode)
        => _layoutVerdicts.GetValueOrDefault(languageCode).Status;

    internal void SetLayoutVerdict(string languageCode, LayoutStatus status, string issue)
        => _layoutVerdicts[languageCode] = new LayoutVerdictEntry(status, issue);

    /// <summary>
    /// Oublie tous les verdicts, toutes langues confondues. Appelé avant chaque passe d'analyse :
    /// une ligne qui n'en ressort plus doit redevenir « non analysée », pas garder l'ancien.
    /// </summary>
    internal void ClearLayoutVerdicts()
    {
        _layoutVerdicts.Clear();
        LayoutStatus = LayoutStatus.NotChecked;
        LayoutIssue = string.Empty;
    }

    /// <summary>
    /// Oublie le verdict d'une seule langue. Appelé quand la traduction change : le verdict
    /// portait sur le texte d'avant, le garder afficherait un jugement sur une valeur qui
    /// n'existe plus.
    /// </summary>
    internal void InvalidateLayoutVerdict(string languageCode)
    {
        _layoutVerdicts.Remove(languageCode);
        SelectLayoutVerdict(languageCode);
    }

    /// <summary>
    /// Verdict stocké. Une langue absente du dictionnaire rend <c>default</c>, dont
    /// <see cref="Status"/> vaut <see cref="LayoutStatus.NotChecked"/> et <see cref="Issue"/>
    /// vaut <c>null</c> — d'où le type nullable, qui dit la vérité plutôt que de promettre une
    /// chaîne que la valeur par défaut ne porte pas.
    /// </summary>
    private readonly record struct LayoutVerdictEntry(LayoutStatus Status, string? Issue);

    public string GetSyncKey()
        => string.Join("\u001F", Project.Trim(), File.Trim(), Key.Trim());
}
