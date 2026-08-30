namespace CheckTranslation;

/// <summary>
/// Calcule l'état d'avancement d'une source de traduction. Aucune dépendance à l'interface : les
/// chiffres sont produits ici, l'affichage les met en forme. C'est ce qui les rend éprouvables
/// hors WinForms, et réutilisables si un export vient plus tard.
///
/// <b>Les compteurs se lisent dans les dictionnaires par code de langue</b>, pas dans la vue
/// active (<see cref="TranslationRow.Translation"/>). L'appelant doit donc avoir poussé la langue
/// affichée avec <see cref="TranslationRow.CommitActiveLanguage"/>, sinon les éditions en cours
/// manqueraient au décompte — même précaution que la vérification de mise en page.
/// </summary>
internal static class TranslationStatistics
{
    /// <summary>
    /// Bornes basses des tranches de score, alignées sur la palette de <see cref="QualityScore"/>
    /// pour que le tableau de bord et la coloration de la grille racontent la même histoire.
    ///
    /// Privé à dessein : un tableau exposé reste modifiable même déclaré <c>readonly</c>, et la
    /// « source unique de vérité » des tranches se ferait casser en silence. Il se lit par
    /// <see cref="ScoreBuckets"/>.
    /// </summary>
    private static readonly int[] ScoreBucketFloors = [0, 60, 70, 80, 90];

    /// <summary>
    /// Tranches de score, avec le libellé affiché et le filtre correspondant. <b>Unique source de
    /// vérité</b> : le tableau de bord en fait ses colonnes, la liste déroulante de la grille en
    /// fait ses entrées. Les faire diverger ferait qu'un clic sur « 42 » ne ramènerait pas
    /// 42 lignes — le genre d'incohérence qui ruine la confiance dans un tableau de bord.
    /// </summary>
    public static IEnumerable<(string Label, string Filter)> ScoreBuckets()
    {
        for (int i = 0; i < ScoreBucketFloors.Length; i++)
        {
            int low = ScoreBucketFloors[i];
            int high = i == ScoreBucketFloors.Length - 1 ? 100 : ScoreBucketFloors[i + 1] - 1;
            yield return ($"{low} – {high}", $"score:{low}-{high}");
        }
    }

    public static bool TryGetScoreBucketFilter(string label, out string filter)
    {
        foreach (var (bucketLabel, bucketFilter) in ScoreBuckets())
        {
            if (string.Equals(label, bucketLabel, StringComparison.Ordinal))
            {
                filter = bucketFilter;
                return true;
            }
        }

        filter = string.Empty;
        return false;
    }

    public static TranslationOverview Compute(
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<LanguageInfo> languages)
    {
        var languageStats = languages
            .Select(language => ComputeLanguage(rows, language))
            .ToList();

        return new TranslationOverview(
            Rows: rows.Count,
            Projects: rows.Select(row => row.Project).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            Files: rows.Select(row => row.Project + "|" + row.File).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            Languages: languageStats,
            Layouts: languages
                .Select(language => ComputeLayout(rows, language))
                .OfType<LayoutStatistics>()
                .ToList());
    }

    private static LanguageStatistics ComputeLanguage(IReadOnlyList<TranslationRow> rows, LanguageInfo language)
    {
        int translated = 0, sameAsSource = 0, verified = 0;
        long scoreSum = 0;
        var buckets = new int[ScoreBucketFloors.Length];

        foreach (var row in rows)
        {
            var translation = row.Translations.GetValueOrDefault(language.Code, string.Empty);

            // Un score décrit une traduction. S'il n'y en a plus — libellé vidé, commentaire resté
            // en place — le score est périmé : le compter gonflerait l'avancement de la
            // vérification, ferait passer « vérifiées » devant « traduites », et rendrait
            // « non vérifiées » négatif. Tout se compte donc à l'intérieur des lignes traduites.
            if (string.IsNullOrWhiteSpace(translation))
                continue;

            translated++;

            // Une traduction identique à la source n'est pas fautive en soi — « Total »,
            // « Configuration », un nombre — mais c'est la signature d'une recopie faite pour
            // combler un vide. Le tableau de bord la montre, il ne la condamne pas.
            if (string.Equals(translation.Trim(), row.French.Trim(), StringComparison.OrdinalIgnoreCase))
                sameAsSource++;

            if (QualityScore.TryParse(row.Comments.GetValueOrDefault(language.Code, string.Empty), out var score))
            {
                verified++;
                scoreSum += score;
                buckets[BucketIndex(score)]++;
            }
        }

        return new LanguageStatistics(
            language.Code,
            language.Name,
            Total: rows.Count,
            Translated: translated,
            SameAsSource: sameAsSource,
            Verified: verified,
            // Une moyenne sur zéro ligne vérifiée n'existe pas : nul plutôt que zéro, qui se
            // lirait comme « toutes mauvaises ».
            AverageScore: verified == 0 ? null : (double)scoreSum / verified,
            ScoreBuckets: buckets);
    }

    private static int BucketIndex(int score)
    {
        for (int i = ScoreBucketFloors.Length - 1; i > 0; i--)
            if (score >= ScoreBucketFloors[i])
                return i;

        return 0;
    }

    /// <summary>
    /// État de la mise en page d'une langue. L'analyse les couvre toutes en une passe, chaque
    /// verdict étant stocké sous son code de langue : le tableau de bord peut donc les comparer,
    /// et c'est précisément là qu'on veut voir laquelle déborde le plus.
    ///
    /// Retourne <c>null</c> si aucune ligne de cette langue n'a été analysée — source Excel,
    /// langue pas encore traduite — plutôt que des zéros qui se liraient comme « aucun défaut ».
    /// </summary>
    private static LayoutStatistics? ComputeLayout(IReadOnlyList<TranslationRow> rows, LanguageInfo language)
    {
        int ok = 0, truncated = 0, collision = 0, unverifiable = 0;

        foreach (var row in rows)
        {
            switch (row.GetLayoutStatus(language.Code))
            {
                case LayoutStatus.Ok: ok++; break;
                case LayoutStatus.Truncated: truncated++; break;
                case LayoutStatus.Collision: collision++; break;
                case LayoutStatus.Unverifiable: unverifiable++; break;
            }
        }

        return ok + truncated + collision + unverifiable == 0
            ? null
            : new LayoutStatistics(language.Code, language.Name, ok, truncated, collision, unverifiable);
    }

    /// <summary>
    /// Avancement par projet ou par fichier, pour une langue donnée. Trié du moins avancé au plus
    /// avancé : ce qu'on cherche dans un tableau de bord, c'est ce qui reste à faire.
    /// </summary>
    public static List<GroupStatistics> ComputeGroups(
        IReadOnlyList<TranslationRow> rows,
        string languageCode,
        GroupBy groupBy)
    {
        return rows
            .GroupBy(row => groupBy == GroupBy.Project ? row.Project : row.Project + " › " + row.File,
                     StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                int total = 0, translated = 0, verified = 0;
                long scoreSum = 0;

                foreach (var row in group)
                {
                    total++;

                    // Même règle que par langue : un score sans traduction est périmé.
                    if (string.IsNullOrWhiteSpace(row.Translations.GetValueOrDefault(languageCode, string.Empty)))
                        continue;

                    translated++;

                    if (QualityScore.TryParse(row.Comments.GetValueOrDefault(languageCode, string.Empty), out var score))
                    {
                        verified++;
                        scoreSum += score;
                    }
                }

                return new GroupStatistics(
                    group.Key,
                    Total: total,
                    Translated: translated,
                    Verified: verified,
                    AverageScore: verified == 0 ? null : (double)scoreSum / verified);
            })
            .OrderBy(group => group.TranslatedRatio)
            .ThenByDescending(group => group.Total)
            .ToList();
    }
}

internal enum GroupBy
{
    Project,
    File,
}

/// <summary>Chiffres d'une langue. <paramref name="Total"/> est le nombre de lignes de la source.</summary>
internal sealed record LanguageStatistics(
    string Code,
    string Name,
    int Total,
    int Translated,
    int SameAsSource,
    int Verified,
    double? AverageScore,
    IReadOnlyList<int> ScoreBuckets)
{
    public int Untranslated => Total - Translated;

    /// <summary>Part traduite, entre 0 et 1. Une source vide vaut 0 et non « tout est fait ».</summary>
    public double TranslatedRatio => Total == 0 ? 0 : (double)Translated / Total;

    /// <summary>
    /// Part vérifiée, rapportée aux lignes <b>traduites</b> et non au total : vérifier une ligne
    /// qui n'est pas traduite n'a pas de sens, et le rapporter au total ferait plafonner
    /// l'indicateur bien en dessous de 100 % sans que personne n'y puisse rien.
    /// </summary>
    public double VerifiedRatio => Translated == 0 ? 0 : (double)Verified / Translated;
}

/// <summary>Avancement d'un projet ou d'un fichier, pour une langue.</summary>
internal sealed record GroupStatistics(
    string Name,
    int Total,
    int Translated,
    int Verified,
    double? AverageScore)
{
    public int Untranslated => Total - Translated;

    public double TranslatedRatio => Total == 0 ? 0 : (double)Translated / Total;

    public double VerifiedRatio => Translated == 0 ? 0 : (double)Verified / Translated;
}

/// <summary>
/// Verdicts de mise en page d'<b>une</b> langue. L'analyse les couvre toutes en une passe : le
/// tableau de bord en aligne une par langue et les compare.
/// </summary>
internal sealed record LayoutStatistics(
    string LanguageCode,
    string LanguageName,
    int Ok,
    int Truncated,
    int Collision,
    int Unverifiable)
{
    public int Issues => Truncated + Collision;

    public int Analyzed => Ok + Truncated + Collision + Unverifiable;
}

internal sealed record TranslationOverview(
    int Rows,
    int Projects,
    int Files,
    IReadOnlyList<LanguageStatistics> Languages,
    IReadOnlyList<LayoutStatistics> Layouts)
{
    /// <summary>
    /// Total des défauts, toutes langues analysées confondues. Nul — et non zéro — si rien n'a
    /// été analysé : zéro se lirait « aucun défaut », ce qui n'est pas la même information.
    /// </summary>
    public int? TotalLayoutIssues => Layouts.Count == 0 ? null : Layouts.Sum(layout => layout.Issues);

    public int TotalLayoutChecked => Layouts.Sum(layout => layout.Analyzed);

    /// <summary>Langue qui déborde le plus, en nombre de défauts. Nulle si rien n'a été analysé.</summary>
    public LayoutStatistics? WorstLayout => Layouts.Count == 0 ? null : Layouts.MaxBy(layout => layout.Issues);

    /// <summary>
    /// Part traduite toutes langues confondues : le total des cases remplies sur le total des
    /// cases à remplir. Un chiffre unique pour dire « où en est le chantier ».
    /// </summary>
    public double OverallTranslatedRatio
    {
        get
        {
            long total = Languages.Sum(language => (long)language.Total);
            return total == 0 ? 0 : (double)Languages.Sum(language => (long)language.Translated) / total;
        }
    }

    public double OverallVerifiedRatio
    {
        get
        {
            long translated = Languages.Sum(language => (long)language.Translated);
            return translated == 0 ? 0 : (double)Languages.Sum(language => (long)language.Verified) / translated;
        }
    }

    public LanguageStatistics? LeastAdvanced => Languages.MinBy(language => language.TranslatedRatio);

    public LanguageStatistics? MostAdvanced => Languages.MaxBy(language => language.TranslatedRatio);
}
