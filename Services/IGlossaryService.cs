namespace CheckTranslation;

internal interface IGlossaryService
{
    IReadOnlyList<GlossaryEntry> GetEntries(string languageCode);
    void ReplaceEntries(string languageCode, IReadOnlyList<GlossaryEntry> entries);

    /// <summary>
    /// Projection injectée dans les prompts pour une langue : les seuls termes Validé portant une
    /// traduction non vide pour ce code. C'est l'état que la retraduction ciblée photographie
    /// avant et après l'éditeur de glossaire pour détecter les contraintes qui ont changé.
    /// </summary>
    IReadOnlyList<GlossaryEntry> GetPromptEntries(string languageCode);

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
    /// <see cref="ReplaceTerms"/> puis <see cref="Save"/>, transactionnellement : si la
    /// persistance échoue, l'état mémoire est restauré tel qu'avant l'appel — le service ne
    /// reste jamais muté en mémoire pendant que l'UI signale un échec.
    /// </summary>
    void ReplaceTermsAndSave(IReadOnlyList<GlossaryTerm> terms);

    /// <summary>
    /// Verse des candidats d'extraction dans le glossaire ET persiste : un terme nouveau naît
    /// <see cref="GlossaryTermStatus.Proposed"/> (le contrôle le validera — voir GLOSSAIRE.md) ;
    /// un terme existant reçoit la traduction proposée seulement si sa case pour cette langue est
    /// vide, et garde son statut. Si la persistance échoue, l'état mémoire est restauré et
    /// l'exception remonte. Retourne le nombre de termes créés ou complétés — un compte de
    /// termes, pas d'entrées : des candidats en doublon sur la même source ne comptent qu'une
    /// fois, la garde de non-écrasement écartant les suivants.
    /// </summary>
    int AddProposedTerms(string languageCode, IReadOnlyList<GlossaryEntry> entries);

    /// <summary>
    /// Empreinte de la totalité du glossaire (tous champs, toutes langues), écrite dans le
    /// classeur d'export et comparée à l'import pour détecter que le glossaire a changé côté
    /// application pendant le contrôle externe.
    /// </summary>
    string GetExportStamp();

    /// <summary>
    /// Exporte le glossaire vers un classeur de contrôle, transactionnellement : les termes
    /// Proposé passent En contrôle (les Validé restent injectés pendant le contrôle), le
    /// classeur est écrit avec l'empreinte de cet état, puis la bascule est persistée. Si une
    /// étape échoue, les statuts sont restaurés : le glossaire ne reste jamais « En contrôle »
    /// sans classeur produit. Retourne le nombre de termes basculés.
    /// </summary>
    int ExportForReview(string filePath, IReadOnlyList<LanguageInfo> languages);

    /// <summary>
    /// Écrit une copie datée du glossaire courant à côté de glossary.json, avant qu'un import
    /// n'applique ses changements. Retourne le chemin du fichier créé.
    /// </summary>
    string CreateBackup();

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
