namespace CheckTranslation;

/// <summary>
/// Source de traductions : une arborescence de fichiers .resx désignée par une solution
/// (.sln / .slnx). Une instance est liée à un chemin donné et sert de point d'entrée unique au
/// chargement et à la sauvegarde. L'abstraction survit à la disparition de la source Excel : elle
/// isole l'interface de la lecture disque et reste le point d'accroche d'une source future.
/// </summary>
internal interface ITranslationSource
{
    /// <summary>Chemin désigné par l'utilisateur (fichier solution).</summary>
    string Path { get; }

    /// <summary>Libellé court du type de source, affiché dans la status bar (« resx »).</summary>
    string Kind { get; }

    /// <summary>
    /// Indique si la vérification de mise en page est possible. Elle exige la géométrie des
    /// contrôles, sérialisée dans les <c>.resx</c> des formulaires <c>Localizable</c>.
    /// </summary>
    bool SupportsLayoutCheck { get; }

    /// <summary>
    /// Charge toutes les lignes traduisibles. Les lignes marquées <c>@Invariant</c> dans le
    /// commentaire source sont exclues.
    /// </summary>
    List<TranslationRow> Load(IReadOnlyList<LanguageInfo> languages, IProgress<SourceLoadProgress>? progress = null);

    /// <summary>
    /// Compte rendu du dernier <see cref="Load"/>, ou <c>null</c> si la source n'en produit pas.
    /// Sert à expliquer un chargement qui ne ramène aucune ligne : sans lui, l'utilisateur voit
    /// une grille vide sans savoir si la source est vide ou si rien n'a été trouvé.
    /// </summary>
    string? LastLoadReport { get; }

    /// <summary>
    /// Réécrit les traductions et les commentaires de vérification. Le texte source (français)
    /// n'est jamais modifié : il est en lecture seule dans l'application.
    /// </summary>
    void Save(IReadOnlyList<TranslationRow> rows, IReadOnlyList<LanguageInfo> languages);
}
