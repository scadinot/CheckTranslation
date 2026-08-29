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

        var verdicts = new List<LayoutVerdict>();

        foreach (var fileRows in rows.GroupBy(row => BuildIdentity(row.Project, row.File), StringComparer.OrdinalIgnoreCase))
        {
            if (!neutralPathByIdentity.TryGetValue(fileRows.Key, out var neutralPath))
                continue;

            var geometry = FormGeometryReader.Read(neutralPath);
            if (geometry.Count == 0)
                continue;   // formulaire non localisable : rien à confronter, on ne conclut pas

            AnalyzeForm(geometry, fileRows, languageCode, measure, verdicts);
        }

        return verdicts;
    }

    private static void AnalyzeForm(
        FormGeometry geometry,
        IEnumerable<TranslationRow> fileRows,
        string languageCode,
        TextWidthMeasurer measure,
        List<LayoutVerdict> verdicts)
    {
        var rowByControl = new Dictionary<string, TranslationRow>(StringComparer.Ordinal);
        var sourceTexts = new Dictionary<string, string>(StringComparer.Ordinal);
        var translatedTexts = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var row in fileRows)
        {
            if (!FormGeometryReader.TrySplitKey(row.Key, out var controlName, out var property)
                || property != DisplayedTextProperty)
                continue;

            var translation = row.Translations.GetValueOrDefault(languageCode, string.Empty);

            // Sans traduction, il n'y a rien à confronter : la ligne ne reçoit pas de verdict
            // plutôt que d'être déclarée conforme à tort.
            if (string.IsNullOrEmpty(translation))
                continue;

            rowByControl[controlName] = row;
            sourceTexts[controlName] = row.French;
            translatedTexts[controlName] = translation;
        }

        if (rowByControl.Count == 0)
            return;

        var analysis = LayoutAnalyzer.AnalyzeRegression(geometry, sourceTexts, translatedTexts, measure);

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

    private static string BuildIdentity(string project, string file)
        => project.Trim() + "|" + file.Trim();
}
