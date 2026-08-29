namespace CheckTranslation;

/// <summary>
/// Source de traductions : un export Excel ResX Resource Manager (.xlsx) ou une arborescence
/// de fichiers .resx désignée par une solution (.sln / .slnx). Une instance est liée à un
/// chemin donné et sert de point d'entrée unique au chargement et à la sauvegarde.
/// </summary>
internal interface ITranslationSource
{
    /// <summary>Chemin désigné par l'utilisateur (classeur Excel ou fichier solution).</summary>
    string Path { get; }

    /// <summary>Libellé court du type de source, affiché dans la status bar (« Excel », « resx »).</summary>
    string Kind { get; }

    /// <summary>
    /// Indique si la fusion source → destination est disponible. Seule la source Excel la
    /// supporte pour l'instant ; le bouton Fusion est désactivé pour les autres.
    /// </summary>
    bool SupportsMerge { get; }

    /// <summary>
    /// Indique si la vérification de mise en page est possible. Elle exige la géométrie des
    /// contrôles, qui n'existe que dans les fichiers <c>.resx</c> : l'export Excel n'en contient
    /// aucune trace.
    /// </summary>
    bool SupportsLayoutCheck { get; }

    /// <summary>
    /// Charge toutes les lignes traduisibles. Les lignes marquées <c>@Invariant</c> dans le
    /// commentaire source sont exclues.
    /// </summary>
    List<TranslationRow> Load(IReadOnlyList<LanguageInfo> languages, IProgress<SourceLoadProgress>? progress = null);

    /// <summary>
    /// Réécrit les traductions et les commentaires de vérification. Le texte source (français)
    /// n'est jamais modifié : il est en lecture seule dans l'application.
    /// </summary>
    void Save(IReadOnlyList<TranslationRow> rows, IReadOnlyList<LanguageInfo> languages);
}
