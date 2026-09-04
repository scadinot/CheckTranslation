namespace CheckTranslation;

/// <summary>
/// Statut de validation d'un terme, porteur de la gouvernance du process de contrôle externe
/// (voir <c>GLOSSAIRE.md</c>). Seuls les termes <see cref="Validated"/> sont injectés dans les
/// prompts : une proposition non contrôlée ne doit pas contaminer les traductions.
/// </summary>
internal enum GlossaryTermStatus
{
    /// <summary>Candidat (extraction IA ou saisie) en attente de contrôle.</summary>
    Proposed,

    /// <summary>Exporté vers les équipes externes, retour attendu.</summary>
    InReview,

    /// <summary>Tranché : injecté dans les prompts de traduction et de vérification.</summary>
    Validated,
}

/// <summary>
/// Un terme métier transversal : un terme source français canonique et ses traductions imposées dans
/// chaque langue cible. Le terme est l'unité de gouvernance (statut, contexte, commentaire de
/// révision) ; les langues n'en sont que les colonnes. Remplace le stockage par langue du schéma
/// v1, où chaque langue ignorait les autres.
/// </summary>
internal sealed class GlossaryTerm
{
    /// <summary>Terme français canonique (singulier, non conjugué). Identité du terme.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Définition métier courte, commune à toutes les langues.</summary>
    public string Context { get; set; } = string.Empty;

    public GlossaryTermStatus Status { get; set; } = GlossaryTermStatus.Proposed;

    /// <summary>Commentaire des équipes de contrôle externe, ramené par l'import.</summary>
    public string ReviewerComment { get; set; } = string.Empty;

    /// <summary>Traductions par code de langue (« de-DE »). Absente ou vide = non tranchée, non injectée.</summary>
    public Dictionary<string, string> Translations { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Contenu du fichier <c>glossary.json</c>. <see cref="Terms"/> est le schéma courant (v2,
/// transversal) ; <see cref="EntriesByLanguage"/> est l'ancien schéma par langue, relu pour la
/// migration au chargement et plus jamais réécrit (nul à la sauvegarde, donc absent du JSON).
/// </summary>
internal sealed class Glossary
{
    public int Version { get; set; } = 2;

    public List<GlossaryTerm> Terms { get; set; } = new();

    // Legacy v1 : lecture seule, pour migration.
    public Dictionary<string, List<GlossaryEntry>>? EntriesByLanguage { get; set; }
}
