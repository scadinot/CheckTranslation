namespace CheckTranslation;

/// <summary>
/// Progression de chargement d'une source. L'unité est celle que la source choisit — pour la
/// source .resx, le nombre de fichiers neutres traités — et l'UI n'en affiche aucune : elle ne
/// montre qu'un rapport fait / total.
/// </summary>
internal readonly record struct SourceLoadProgress(int Done, int Total) { }
