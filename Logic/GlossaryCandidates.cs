namespace CheckTranslation;

/// <summary>
/// Candidats d'extraction multi-langues. L'extraction IA travaille langue par langue — un prompt
/// par langue cible, filtré sur les termes que le glossaire connaît déjà dans cette langue — ; ce
/// module décide quelles langues valent un appel et recolle les résultats en termes
/// transversaux. Pur, éprouvable hors WinForms.
/// </summary>
internal static class GlossaryCandidates
{
    /// <summary>
    /// Langues, dans l'ordre fourni, où au moins une ligne porte une traduction non vide : celles
    /// où la sélection a du contenu. Lit les dictionnaires par code — l'appelant a poussé la
    /// langue affichée avant (<see cref="TranslationRow.CommitActiveLanguage"/>), sans quoi une
    /// traduction saisie et pas encore committée ne compterait pas.
    /// </summary>
    public static List<LanguageInfo> LanguagesWithContent(
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<LanguageInfo> languages)
        => languages
            .Where(language => rows.Any(row => !string.IsNullOrWhiteSpace(row.Translations.GetValueOrDefault(language.Code))))
            .ToList();

    /// <summary>
    /// Fusionne les candidats de plusieurs langues en termes transversaux, par source normalisée
    /// et insensible à la casse : un même terme extrait en allemand et en anglais devient un terme
    /// à deux cellules. Le premier contexte non vide l'emporte, les destinations vides sont
    /// ignorées, l'ordre est celui de première apparition, et pour une langue donnée la première
    /// proposition est conservée. Les termes naissent Proposé : ils n'entrent dans les prompts
    /// qu'une fois validés (GLOSSAIRE.md).
    /// </summary>
    public static List<GlossaryTerm> Merge(
        IReadOnlyList<(string LanguageCode, IReadOnlyList<GlossaryEntry> Candidates)> byLanguage)
    {
        var terms = new List<GlossaryTerm>();
        var bySource = new Dictionary<string, GlossaryTerm>(StringComparer.OrdinalIgnoreCase);

        foreach (var (languageCode, candidates) in byLanguage)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                continue;

            foreach (var candidate in candidates)
            {
                var source = GlossaryService.NormalizeCell(candidate.Source);
                var destination = GlossaryService.NormalizeCell(candidate.Destination);
                if (source.Length == 0 || destination.Length == 0)
                    continue;

                if (!bySource.TryGetValue(source, out var term))
                {
                    term = new GlossaryTerm { Source = source, Status = GlossaryTermStatus.Proposed };
                    bySource[source] = term;
                    terms.Add(term);
                }

                if (term.Context.Length == 0)
                    term.Context = GlossaryService.NormalizeCell(candidate.Context);

                term.Translations.TryAdd(languageCode, destination);
            }
        }

        return terms;
    }
}
