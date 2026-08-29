namespace CheckTranslation;

/// <summary>
/// Progression de chargement d'une source. L'unité dépend de la source : lignes lues pour
/// l'Excel, fichiers .resx neutres traités pour la source .resx. L'UI n'affiche pas d'unité.
/// </summary>
internal readonly record struct SourceLoadProgress(int Done, int Total) { }
