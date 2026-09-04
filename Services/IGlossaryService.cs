namespace CheckTranslation;

internal interface IGlossaryService
{
    IReadOnlyList<GlossaryEntry> GetEntries(string languageCode);
    void ReplaceEntries(string languageCode, IReadOnlyList<GlossaryEntry> entries);

    /// <summary>Copie de travail des termes transversaux (isolée : modifier le retour est sans effet).</summary>
    IReadOnlyList<GlossaryTerm> GetTerms();

    /// <summary>
    /// Le glossaire devient exactement cette liste de termes : c'est l'écriture de l'éditeur
    /// multi-langues, statuts compris. Les valeurs sont normalisées (espaces de bord, retours à
    /// la ligne), les traductions vides retirées, les termes sans source ignorés. Un terme sans
    /// aucune traduction est conservé : c'est un terme défini dont les colonnes restent à remplir.
    /// </summary>
    void ReplaceTerms(IReadOnlyList<GlossaryTerm> terms);

    /// <summary>
    /// Verse des candidats d'extraction dans le glossaire : un terme nouveau naît
    /// <see cref="GlossaryTermStatus.Proposed"/> (le contrôle le validera — voir GLOSSAIRE.md) ;
    /// un terme existant reçoit la traduction proposée seulement si sa case pour cette langue est
    /// vide, et garde son statut. Retourne le nombre de termes créés ou complétés.
    /// </summary>
    int AddProposedTerms(string languageCode, IReadOnlyList<GlossaryEntry> entries);

    void Save();

    /// <summary>
    /// Construit le fragment markdown à injecter dans le placeholder <c>{glossary}</c> des prompts.
    /// Retourne une chaîne vide si aucun terme n'est défini pour la langue.
    /// </summary>
    string BuildGlossarySection(string languageCode, string languageName);

    /// <summary>
    /// Empreinte stable du contenu glossaire pour une langue. Utilisée par <see cref="ITranslationService"/>
    /// pour invalider le cache quand le glossaire change.
    /// </summary>
    string GetGlossaryFingerprint(string languageCode);

    /// <summary>
    /// Propose des termes candidats à partir des textes français sélectionnés. Les termes déjà présents
    /// dans le glossaire pour la langue cible sont filtrés automatiquement.
    /// </summary>
    Task<IReadOnlyList<GlossaryEntry>> ExtractCandidatesAsync(
        IReadOnlyList<string> frenchTexts,
        AppConfig config,
        string languageCode,
        string languageName,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);
}
