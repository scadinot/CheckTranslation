namespace CheckTranslation;

/// <summary>
/// Progression de chargement d'une source, en fichiers .resx neutres traités. L'unité reste
/// propre à la source : l'UI n'en affiche aucune.
/// </summary>
internal readonly record struct SourceLoadProgress(int Done, int Total) { }
