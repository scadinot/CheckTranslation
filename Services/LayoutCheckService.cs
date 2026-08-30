namespace CheckTranslation;

/// <summary>
/// Confronte les traductions à la place disponible dans les formulaires d'une solution.
///
/// Ne concerne que les libellés réellement affichés dans un contrôle, c'est-à-dire les clés
/// <c>contrôle.Text</c> d'un fichier de formulaire. Une chaîne de message, un
/// <c>ToolTipText</c> ou un <c>AccessibleName</c> n'occupent aucune surface fixe : les analyser
/// n'aurait pas de sens, et les lignes correspondantes ne reçoivent aucun verdict.
///
/// Le service ne modifie aucune ligne : voir <see cref="ILayoutCheckService.Analyze"/>.
/// </summary>
internal sealed class LayoutCheckService : ILayoutCheckService
{
    private const string DisplayedTextProperty = "Text";

    public IReadOnlyList<LayoutVerdict> Analyze(
        string solutionPath,
        IReadOnlyList<TranslationRow> rows,
        string languageCode,
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

            var input = BuildInput(geometry, fileRows, languageCode);
            if (input is not null)
                forms.Add(input);
        }

        // La géométrie n'est lue qu'une fois : l'échelle de chaque formulaire est calculée sur ce
        // qui est déjà en mémoire, puis leur médiane sert de repli aux formulaires qui n'ont aucun
        // contrôle AutoSize — la résolution de conception est une propriété du poste qui a dessiné
        // les formulaires, pas de chacun d'eux.
        var fallbackScale = MedianScale(forms, measure);

        var verdicts = new List<LayoutVerdict>();
        foreach (var form in forms.Where(form => form.RowByControl.Count > 0))
            AnalyzeForm(form, measure, fallbackScale, verdicts);

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
    /// Rassemble ce qu'il faut pour analyser un formulaire. Les textes source sont collectés pour
    /// <b>tous</b> les libellés, y compris ceux sans traduction : ils ne reçoivent aucun verdict,
    /// mais ils étoffent l'étalonnage.
    ///
    /// Un formulaire entièrement non traduit est retourné malgré tout, sans aucune ligne à juger :
    /// il ne produira pas de verdict, mais son échelle compte dans le repli de la solution. Une
    /// langue dont les traductions ne couvrent encore que des formulaires sans contrôle
    /// <c>AutoSize</c> resterait sinon sans étalon, alors que le reste de la solution en fournit un.
    /// </summary>
    private static FormAnalysisInput? BuildInput(
        FormGeometry geometry,
        IEnumerable<TranslationRow> fileRows,
        string languageCode)
    {
        var rowByControl = new Dictionary<string, TranslationRow>(StringComparer.Ordinal);
        var sourceTexts = new Dictionary<string, string>(StringComparer.Ordinal);
        var translatedTexts = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in fileRows)
        {
            if (!FormGeometryReader.TrySplitKey(row.Key, out var controlName, out var property)
                || property != DisplayedTextProperty)
                continue;

            sourceTexts[controlName] = row.French;

            var translation = row.Translations.GetValueOrDefault(languageCode, string.Empty);

            // Sans traduction, il n'y a rien à confronter : la ligne ne reçoit pas de verdict
            // plutôt que d'être déclarée conforme à tort.
            if (string.IsNullOrEmpty(translation))
                continue;

            rowByControl[controlName] = row;
            translatedTexts[controlName] = translation;
        }

        return sourceTexts.Count == 0
            ? null
            : new FormAnalysisInput(geometry, rowByControl, sourceTexts, translatedTexts);
    }

    private static void AnalyzeForm(
        FormAnalysisInput form,
        TextWidthMeasurer measure,
        double? fallbackScale,
        List<LayoutVerdict> verdicts)
    {
        var rowByControl = form.RowByControl;

        var analysis = LayoutAnalyzer.AnalyzeRegression(
            form.Geometry, form.SourceTexts, form.TranslatedTexts, measure, fallbackScale);

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
            verdicts.Add(new LayoutVerdict(rowByControl[name], verdict.Status, verdict.Issue));
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

    /// <summary>Un formulaire prêt à analyser : sa géométrie et ses textes, lus une seule fois.</summary>
    private sealed record FormAnalysisInput(
        FormGeometry Geometry,
        Dictionary<string, TranslationRow> RowByControl,
        Dictionary<string, string> SourceTexts,
        Dictionary<string, string> TranslatedTexts);

    private static string BuildIdentity(string project, string file)
        => project.Trim() + "|" + file.Trim();
}
