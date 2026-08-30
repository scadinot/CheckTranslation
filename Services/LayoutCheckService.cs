namespace CheckTranslation;

/// <summary>
/// Confronte les traductions à la place disponible dans les formulaires d'une solution.
///
/// Ne concerne que les libellés réellement affichés dans un contrôle, c'est-à-dire les clés
/// <c>contrôle.Text</c> d'un fichier de formulaire. Une chaîne de message, un
/// <c>ToolTipText</c> ou un <c>AccessibleName</c> n'occupent aucune surface fixe : les analyser
/// n'aurait pas de sens, et les lignes correspondantes ne reçoivent aucun verdict.
///
/// <b>La géométrie est lue une seule fois pour toutes les langues.</b> C'est la partie coûteuse —
/// découverte des <c>.resx</c>, lecture XML de chaque formulaire, calcul de l'échelle à partir du
/// français — et elle ne dépend pas de la langue analysée. Ajouter une langue ne coûte donc que
/// ses mesures de texte.
///
/// Le service ne modifie aucune ligne : voir <see cref="ILayoutCheckService.Analyze"/>.
/// </summary>
internal sealed class LayoutCheckService : ILayoutCheckService
{
    private const string DisplayedTextProperty = "Text";

    public IReadOnlyList<LayoutVerdict> Analyze(
        string solutionPath,
        IReadOnlyList<TranslationRow> rows,
        IReadOnlyList<string> languageCodes,
        TextWidthMeasurer measure)
    {
        var neutralPathByIdentity = ResxReader.DiscoverFiles(solutionPath)
            .ToDictionary(group => BuildIdentity(group.Project, group.File), group => group.NeutralPath, StringComparer.OrdinalIgnoreCase);

        var forms = new List<FormAnalysisInput>();

        foreach (var fileRows in rows.GroupBy(row => BuildIdentity(row.Project, row.File), StringComparer.OrdinalIgnoreCase))
        {
            if (!neutralPathByIdentity.TryGetValue(fileRows.Key, out var neutralPath))
                continue;

            var geometry = FormGeometryReader.Read(neutralPath);
            if (geometry.Count == 0)
                continue;   // formulaire non localisable : rien à confronter, on ne conclut pas

            var input = BuildInput(geometry, fileRows);
            if (input is not null)
                forms.Add(input);
        }

        // L'échelle se déduit du français : elle est la même quelle que soit la langue analysée.
        // Elle est donc calculée ici, avant la boucle des langues, et non à chaque passe.
        var fallbackScale = MedianScale(forms, measure);

        var verdicts = new List<LayoutVerdict>();
        foreach (var languageCode in languageCodes)
            foreach (var form in forms)
                AnalyzeForm(form, languageCode, measure, fallbackScale, verdicts);

        return verdicts;
    }

    /// <summary>
    /// Médiane des échelles individuelles. Une médiane, et non une moyenne : un formulaire dessiné
    /// à une autre résolution ne doit pas entraîner tous les autres.
    /// </summary>
    private static double? MedianScale(List<FormAnalysisInput> forms, TextWidthMeasurer measure)
    {
        var scales = forms
            .Select(form => LayoutAnalyzer.ComputeFormScale(form.Geometry, form.SourceTexts, measure))
            .OfType<double>()
            .OrderBy(scale => scale)
            .ToList();

        return scales.Count == 0 ? null : scales[scales.Count / 2];
    }

    /// <summary>
    /// Rassemble ce qu'il faut pour analyser un formulaire, <b>indépendamment de la langue</b> :
    /// sa géométrie, ses textes français et les lignes porteuses. Le tri par langue — qui est
    /// traduit, qui ne l'est pas — se fait ensuite, dans <see cref="AnalyzeForm"/>.
    ///
    /// Les textes source sont collectés pour <b>tous</b> les libellés, y compris ceux qu'aucune
    /// langue ne traduit encore : ils ne produiront aucun verdict, mais ils étoffent l'étalonnage.
    /// Un formulaire entièrement non traduit est donc retourné malgré tout — son échelle compte
    /// dans le repli de la solution.
    /// </summary>
    private static FormAnalysisInput? BuildInput(FormGeometry geometry, IEnumerable<TranslationRow> fileRows)
    {
        var rowByControl = new Dictionary<string, TranslationRow>(StringComparer.Ordinal);
        var sourceTexts = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in fileRows)
        {
            if (!FormGeometryReader.TrySplitKey(row.Key, out var controlName, out var property)
                || property != DisplayedTextProperty)
                continue;

            sourceTexts[controlName] = row.French;
            rowByControl[controlName] = row;
        }

        return sourceTexts.Count == 0
            ? null
            : new FormAnalysisInput(geometry, rowByControl, sourceTexts);
    }

    private static void AnalyzeForm(
        FormAnalysisInput form,
        string languageCode,
        TextWidthMeasurer measure,
        double? fallbackScale,
        List<LayoutVerdict> verdicts)
    {
        var rowByControl = new Dictionary<string, TranslationRow>(StringComparer.Ordinal);
        var translatedTexts = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (controlName, row) in form.RowByControl)
        {
            var translation = row.Translations.GetValueOrDefault(languageCode, string.Empty);

            // Sans traduction, il n'y a rien à confronter : la ligne ne reçoit pas de verdict
            // plutôt que d'être déclarée conforme à tort. « Traduite » se dit ici comme partout
            // ailleurs — filtres et tableau de bord — c'est-à-dire hors blancs : sans quoi une
            // valeur réduite à des espaces serait comptée non traduite d'un côté et jugée de
            // l'autre.
            if (string.IsNullOrWhiteSpace(translation))
                continue;

            rowByControl[controlName] = row;
            translatedTexts[controlName] = translation;
        }

        if (rowByControl.Count == 0)
            return;

        var analysis = LayoutAnalyzer.AnalyzeRegression(
            form.Geometry, form.SourceTexts, translatedTexts, measure, fallbackScale);

        // Tout ce qui a pu être confronté est conforme jusqu'à preuve du contraire.
        var byControl = rowByControl.Keys.ToDictionary(
            name => name,
            _ => (Status: LayoutStatus.Ok, Issue: string.Empty),
            StringComparer.Ordinal);

        foreach (var name in analysis.Unverifiable)
            if (byControl.ContainsKey(name))
                byControl[name] = (LayoutStatus.Unverifiable, "Non vérifiable");

        foreach (var issue in analysis.Truncations)
            if (byControl.ContainsKey(issue.Control))
                byControl[issue.Control] = (LayoutStatus.Truncated, $"Troncature : +{issue.OverflowPixels} px");

        foreach (var issue in analysis.Collisions)
        {
            // Une collision met en cause deux contrôles ; les deux lignes sont marquées, car
            // corriger l'un ou l'autre résout le problème.
            MarkCollision(byControl, issue.Control, issue.OtherControl, issue.OverflowPixels);
            MarkCollision(byControl, issue.OtherControl, issue.Control, issue.OverflowPixels);
        }

        foreach (var (name, verdict) in byControl)
            verdicts.Add(new LayoutVerdict(rowByControl[name], languageCode, verdict.Status, verdict.Issue));
    }

    private static void MarkCollision(
        Dictionary<string, (LayoutStatus Status, string Issue)> byControl,
        string? control,
        string? other,
        int overlap)
    {
        if (control is null || !byControl.TryGetValue(control, out var current))
            return;

        // Une troncature déjà signalée est plus directe à corriger : ne pas la masquer.
        if (current.Status == LayoutStatus.Truncated)
            return;

        byControl[control] = (LayoutStatus.Collision, $"Collision avec « {other} » : {overlap} px");
    }

    /// <summary>
    /// Un formulaire prêt à analyser : sa géométrie et ses textes français, lus une seule fois et
    /// réutilisés pour chaque langue.
    /// </summary>
    private sealed record FormAnalysisInput(
        FormGeometry Geometry,
        Dictionary<string, TranslationRow> RowByControl,
        Dictionary<string, string> SourceTexts);

    private static string BuildIdentity(string project, string file)
        => project.Trim() + "|" + file.Trim();
}
