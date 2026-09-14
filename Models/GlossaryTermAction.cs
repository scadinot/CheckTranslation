namespace CheckTranslation;

/// <summary>
/// Ce qu'un terme du glossaire demande à la grille principale, depuis le menu contextuel de
/// l'éditeur : contrôler (vérifier par l'IA) ou retraduire, dans une langue, les lignes dont le
/// français contient le terme. L'éditeur est modal et n'a pas accès aux lignes : il se ferme en
/// portant la demande, <c>MainForm</c> l'exécute — même mécanique que le drill-down du tableau
/// de bord (<see cref="DashboardDrillDown"/>).
/// </summary>
internal sealed record GlossaryTermAction(GlossaryTermActionKind Kind, string Source, string LanguageCode);

internal enum GlossaryTermActionKind
{
    /// <summary>Vérifier par l'IA les traductions existantes des lignes contenant le terme.</summary>
    Verify,

    /// <summary>Retraduire puis re-vérifier les lignes contenant le terme (les écarts seuls, ou toutes).</summary>
    Retranslate,
}
