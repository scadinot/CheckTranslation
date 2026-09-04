namespace CheckTranslation;

/// <summary>
/// Bilan d'une extraction de termes métier : les candidats retenus et, à côté, ce qui n'a pas
/// pu l'être — lots en échec d'appel API, lots dont la réponse était illisible, candidats écartés
/// parce que déjà au glossaire. Une liste vide seule est indiscernable d'un échec : c'est ce
/// bilan qui permet à l'interface de dire lequel des deux s'est produit.
/// </summary>
/// <param name="Candidates">Termes proposés, dédupliqués, absents du glossaire.</param>
/// <param name="Batches">Nombre de lots envoyés à l'IA.</param>
/// <param name="FailedBatches">Lots dont l'appel API a échoué.</param>
/// <param name="UnreadableBatches">Lots dont la réponse n'a pas pu être lue (JSON invalide ou tronqué).</param>
/// <param name="AlreadyKnown">Candidats proposés par l'IA mais déjà présents au glossaire, donc écartés.</param>
/// <param name="Truncated">Au moins une réponse illisible porte la signature d'une troncature (plafond de tokens).</param>
/// <param name="FirstError">Message du premier échec d'appel API, s'il y en a eu.</param>
internal sealed record GlossaryExtractionResult(
    IReadOnlyList<GlossaryEntry> Candidates,
    int Batches,
    int FailedBatches,
    int UnreadableBatches,
    int AlreadyKnown,
    bool Truncated,
    string? FirstError)
{
    public int ProblemBatches => FailedBatches + UnreadableBatches;
}

/// <summary>
/// Lecture d'une réponse d'extraction. <see cref="Success"/> faux signifie « illisible », jamais
/// « rien trouvé » : un tableau vide est un succès avec zéro entrée.
/// </summary>
internal sealed record ExtractionParse(IReadOnlyList<GlossaryEntry> Entries, bool Success, bool Truncated)
{
    public static ExtractionParse Unreadable(bool truncated)
        => new(Array.Empty<GlossaryEntry>(), Success: false, Truncated: truncated);
}
