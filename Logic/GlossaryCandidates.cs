namespace CheckTranslation;

/// <summary>
/// Candidats d'extraction multi-langues. L'extraction IA travaille langue par langue — un prompt
/// par langue cible, sans filtrage des termes déjà connus — ; ce module décide quelles langues
/// valent un appel, recolle les résultats en termes transversaux, et classe chaque proposition
/// face au glossaire (à écrire, déjà là, en conflit). Pur, éprouvable hors WinForms.
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

    /// <summary>Terme du glossaire de même source que <paramref name="source"/> (normalisée, insensible à la casse), ou null.</summary>
    public static GlossaryTerm? FindExisting(IReadOnlyList<GlossaryTerm> terms, string? source)
    {
        var key = GlossaryService.NormalizeCell(source);
        if (key.Length == 0)
            return null;
        return terms.FirstOrDefault(t => string.Equals(GlossaryService.NormalizeCell(t.Source), key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Confronte un candidat au terme que le glossaire porte déjà sous la même source. C'est la
    /// définition unique de ce que le versement écrira : le dialog de validation en fait ses
    /// couleurs (vert = sera écrit, noir = déjà là, rouge = diffère d'une valeur tranchée et ne
    /// sera pas appliqué) et <c>GlossaryService.AddProposedTerms</c> en fait ses écritures — les
    /// deux ne peuvent donc pas se contredire.
    /// </summary>
    public static CandidateDiff Classify(GlossaryTerm candidate, GlossaryTerm? existing)
    {
        var codes = candidate.Translations.Keys
            .Concat(existing?.Translations.Keys ?? Enumerable.Empty<string>())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var cells = new Dictionary<string, CandidateCellStatus>(StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
            cells[code] = ClassifyValue(candidate.Translations.GetValueOrDefault(code), existing?.Translations.GetValueOrDefault(code));

        return new CandidateDiff(existing is null, ClassifyValue(candidate.Context, existing?.Context), cells);
    }

    private static CandidateCellStatus ClassifyValue(string? proposed, string? current)
    {
        var p = GlossaryService.NormalizeCell(proposed);
        var c = GlossaryService.NormalizeCell(current);
        if (c.Length == 0)
            return p.Length == 0 ? CandidateCellStatus.Empty : CandidateCellStatus.Added;
        if (p.Length == 0)
            return CandidateCellStatus.Existing;
        return string.Equals(p, c, StringComparison.Ordinal) ? CandidateCellStatus.Existing : CandidateCellStatus.Conflict;
    }
}

/// <summary>Sort d'une cellule (ou du contexte) d'un candidat, confronté au glossaire.</summary>
internal enum CandidateCellStatus
{
    /// <summary>Ni proposition, ni valeur au glossaire.</summary>
    Empty,

    /// <summary>Sera écrit : terme nouveau, ou case vide du glossaire que la proposition remplit.</summary>
    Added,

    /// <summary>Déjà au glossaire — identique à la proposition, ou sans proposition : rien à écrire.</summary>
    Existing,

    /// <summary>Le glossaire porte déjà une autre valeur, tranchée : la proposition ne l'écrasera pas.</summary>
    Conflict,
}

/// <summary>
/// Résultat de <see cref="GlossaryCandidates.Classify"/>. Un terme nouveau n'est versé que s'il a
/// au moins une cellule ; un terme existant l'est dès qu'une cellule ou son contexte vide se
/// remplit.
/// </summary>
internal sealed record CandidateDiff(
    bool IsNewTerm,
    CandidateCellStatus Context,
    IReadOnlyDictionary<string, CandidateCellStatus> Cells)
{
    public IEnumerable<string> AddedCells => Cells.Where(cell => cell.Value == CandidateCellStatus.Added).Select(cell => cell.Key);

    public bool FillsContext => !IsNewTerm && Context == CandidateCellStatus.Added;

    public bool AddsAnything => AddedCells.Any() || FillsContext;
}
