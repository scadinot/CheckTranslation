namespace CheckTranslation;

/// <summary>
/// Confronte les traductions à la place disponible dans les formulaires d'une solution, et reporte
/// le verdict sur chaque ligne.
///
/// Ne concerne que les libellés réellement affichés dans un contrôle, c'est-à-dire les clés
/// <c>contrôle.Text</c> d'un fichier de formulaire. Une chaîne de message, un
/// <c>ToolTipText</c> ou un <c>AccessibleName</c> n'occupent aucune surface fixe : les analyser
/// n'aurait pas de sens, et les lignes correspondantes restent <see cref="LayoutStatus.NotChecked"/>.
/// </summary>
internal sealed class LayoutCheckService : ILayoutCheckService
{
    private const string DisplayedTextProperty = "Text";

    public int Analyze(string solutionPath, IReadOnlyList<TranslationRow> rows, string languageCode, TextWidthMeasurer measure)
    {
        foreach (var row in rows)
            row.ClearLayoutVerdict();

        var neutralPathByIdentity = ResxReader.DiscoverFiles(solutionPath)
            .ToDictionary(group => BuildIdentity(group.Project, group.File), group => group.NeutralPath, StringComparer.OrdinalIgnoreCase);

        int issues = 0;

        foreach (var fileRows in rows.GroupBy(row => BuildIdentity(row.Project, row.File), StringComparer.OrdinalIgnoreCase))
        {
            if (!neutralPathByIdentity.TryGetValue(fileRows.Key, out var neutralPath))
                continue;

            var geometry = FormGeometryReader.Read(neutralPath);
            if (geometry.Count == 0)
                continue;   // formulaire non localisable : rien à confronter, on ne conclut pas

            issues += AnalyzeForm(geometry, fileRows, languageCode, measure);
        }

        return issues;
    }

    private static int AnalyzeForm(
        FormGeometry geometry,
        IEnumerable<TranslationRow> fileRows,
        string languageCode,
        TextWidthMeasurer measure)
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

            // Sans traduction, il n'y a rien à confronter : la ligne reste « non analysée »
            // plutôt que d'être déclarée conforme à tort.
            if (string.IsNullOrEmpty(translation))
                continue;

            rowByControl[controlName] = row;
            sourceTexts[controlName] = row.French;
            translatedTexts[controlName] = translation;
        }

        if (rowByControl.Count == 0)
            return 0;

        var analysis = LayoutAnalyzer.AnalyzeRegression(geometry, sourceTexts, translatedTexts, measure);

        // Tout ce qui a pu être confronté est conforme jusqu'à preuve du contraire.
        foreach (var row in rowByControl.Values)
            row.SetLayoutVerdict(LayoutStatus.Ok, string.Empty);

        foreach (var name in analysis.Unverifiable)
            if (rowByControl.TryGetValue(name, out var row))
                row.SetLayoutVerdict(LayoutStatus.Unverifiable, "Non vérifiable");

        int issues = 0;

        foreach (var issue in analysis.Truncations)
        {
            if (!rowByControl.TryGetValue(issue.Control, out var row))
                continue;

            row.SetLayoutVerdict(LayoutStatus.Truncated, $"Troncature : +{issue.OverflowPixels} px");
            issues++;
        }

        foreach (var issue in analysis.Collisions)
        {
            // Une collision met en cause deux contrôles ; les deux lignes sont marquées, car
            // corriger l'un ou l'autre résout le problème.
            issues += MarkCollision(rowByControl, issue.Control, issue.OtherControl, issue.OverflowPixels);
            issues += MarkCollision(rowByControl, issue.OtherControl, issue.Control, issue.OverflowPixels);
        }

        return issues;
    }

    private static int MarkCollision(
        IReadOnlyDictionary<string, TranslationRow> rowByControl,
        string? control,
        string? other,
        int overlap)
    {
        if (control is null || !rowByControl.TryGetValue(control, out var row))
            return 0;

        // Une troncature déjà signalée est plus directe à corriger : ne pas la masquer.
        if (row.LayoutStatus == LayoutStatus.Truncated)
            return 0;

        bool alreadyCounted = row.LayoutStatus == LayoutStatus.Collision;
        row.SetLayoutVerdict(LayoutStatus.Collision, $"Collision avec « {other} » : {overlap} px");
        return alreadyCounted ? 0 : 1;
    }

    private static string BuildIdentity(string project, string file)
        => project.Trim() + "|" + file.Trim();
}
