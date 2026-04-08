namespace CheckTranslation;

internal interface IGlossaryService
{
    IReadOnlyList<GlossaryEntry> GetEntries(string languageCode);
    void ReplaceEntries(string languageCode, IReadOnlyList<GlossaryEntry> entries);
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
