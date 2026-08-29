namespace CheckTranslation;

/// <summary>
/// Mesure la largeur rendue d'un texte, en pixels, pour une police donnée. En production c'est
/// GDI (<c>TextRenderer.MeasureText</c>), donc Windows ; l'injecter permet d'éprouver toute
/// l'analyse hors WinForms avec une mesure déterministe.
/// </summary>
internal delegate int TextWidthMeasurer(string text, FontDescriptor? font);

/// <summary>
/// Confronte les textes affichés à la place disponible dans un formulaire.
///
/// Deux défaillances distinctes, qui s'excluent :
/// <list type="bullet">
/// <item><b>Troncature</b> — un contrôle à largeur fixe dont le texte dépasse : le texte est
/// coupé, le contrôle ne bouge pas.</item>
/// <item><b>Collision</b> — un contrôle en <c>AutoSize</c> n'est jamais tronqué : il s'élargit
/// pour contenir son texte et vient recouvrir un voisin. C'est le cas des contrôles qui n'ont
/// justement pas de largeur sérialisée.</item>
/// </list>
///
/// Les coordonnées d'un contrôle sont relatives à son parent : seules les paires de frères sont
/// comparées.
/// </summary>
internal static class LayoutAnalyzer
{
    /// <summary>
    /// Analyse un jeu de textes (contrôle → texte affiché) pour un formulaire donné.
    /// <paramref name="measure"/> doit inclure la marge interne du contrôle si elle compte.
    /// </summary>
    public static LayoutAnalysis Analyze(
        FormGeometry geometry,
        IReadOnlyDictionary<string, string> textByControl,
        TextWidthMeasurer measure)
    {
        var truncations = new List<LayoutIssue>();
        var boxes = new Dictionary<string, ControlBox>(StringComparer.Ordinal);
        var unverifiable = new List<string>();

        foreach (var (name, text) in textByControl)
        {
            if (!geometry.ControlsByName.TryGetValue(name, out var control))
            {
                unverifiable.Add(name);
                continue;
            }

            var width = measure(text, geometry.GetEffectiveFont(control));

            if (control.GrowsWithText)
            {
                // Le contrôle s'adapte : sa largeur devient celle du texte. Il faut sa position
                // ET sa hauteur : une hauteur supposée nulle ne croiserait jamais personne, et
                // le contrôle sortirait silencieusement du champ de la détection.
                if (control.Location is not { } location || control.Size is not { } grown)
                {
                    unverifiable.Add(name);
                    continue;
                }

                boxes[name] = new ControlBox(location.Width, location.Height, width, grown.Height);
                continue;
            }

            if (control.Size is not { } size)
            {
                unverifiable.Add(name);
                continue;
            }

            if (width > size.Width)
                truncations.Add(new LayoutIssue(name, null, width - size.Width));

            if (control.Location is { } fixedLocation)
                boxes[name] = new ControlBox(fixedLocation.Width, fixedLocation.Height, size.Width, size.Height);
        }

        return new LayoutAnalysis(truncations, DetectCollisions(geometry, boxes), unverifiable);
    }

    /// <summary>
    /// Ne retient que les défauts <b>introduits</b> par la traduction : ceux déjà présents avec le
    /// texte source ne sont pas imputables au traducteur, et les signaler pour chaque langue
    /// noierait les vrais cas.
    /// </summary>
    public static LayoutAnalysis AnalyzeRegression(
        FormGeometry geometry,
        IReadOnlyDictionary<string, string> sourceTexts,
        IReadOnlyDictionary<string, string> translatedTexts,
        TextWidthMeasurer measure)
    {
        var baseline = Analyze(geometry, sourceTexts, measure);
        var candidate = Analyze(geometry, translatedTexts, measure);

        var knownTruncations = baseline.Truncations.Select(issue => issue.Signature).ToHashSet(StringComparer.Ordinal);
        var knownCollisions = baseline.Collisions.Select(issue => issue.Signature).ToHashSet(StringComparer.Ordinal);

        return new LayoutAnalysis(
            candidate.Truncations.Where(issue => !knownTruncations.Contains(issue.Signature)).ToList(),
            candidate.Collisions.Where(issue => !knownCollisions.Contains(issue.Signature)).ToList(),
            candidate.Unverifiable);
    }

    private static List<LayoutIssue> DetectCollisions(FormGeometry geometry, Dictionary<string, ControlBox> boxes)
    {
        var collisions = new List<LayoutIssue>();

        foreach (var siblings in geometry.EnumerateSiblingGroups())
        {
            var positioned = siblings
                .Where(control => boxes.ContainsKey(control.Name))
                .OrderBy(control => control.Name, StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < positioned.Count; i++)
            {
                for (int j = i + 1; j < positioned.Count; j++)
                {
                    var first = positioned[i];
                    var second = positioned[j];

                    // Deux contrôles à taille fixe qui se chevauchent sont un défaut de maquette,
                    // pas de traduction : au moins l'un des deux doit s'être élargi.
                    if (!first.GrowsWithText && !second.GrowsWithText)
                        continue;

                    var box = boxes[first.Name];
                    var other = boxes[second.Name];
                    if (!box.IntersectsWith(other))
                        continue;

                    collisions.Add(new LayoutIssue(first.Name, second.Name, box.OverlapWidth(other)));
                }
            }
        }

        return collisions;
    }
}

/// <summary>Rectangle d'un contrôle dans le repère de son parent.</summary>
internal readonly record struct ControlBox(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;
    public int Bottom => Top + Height;

    /// <summary>
    /// Chevauchement strict : deux contrôles jointifs (le bord de l'un sur celui de l'autre) ne
    /// se recouvrent pas.
    /// </summary>
    public bool IntersectsWith(ControlBox other)
        => Left < other.Right && Right > other.Left && Top < other.Bottom && Bottom > other.Top;

    public int OverlapWidth(ControlBox other)
        => Math.Max(0, Math.Min(Right, other.Right) - Math.Max(Left, other.Left));
}

/// <summary>
/// Un défaut de mise en page. <paramref name="OtherControl"/> est renseigné pour une collision,
/// nul pour une troncature. <paramref name="OverflowPixels"/> mesure le dépassement.
/// </summary>
internal sealed record LayoutIssue(string Control, string? OtherControl, int OverflowPixels)
{
    /// <summary>Identité stable d'un défaut, indépendante de l'ampleur : sert à comparer deux analyses.</summary>
    public string Signature => OtherControl is null ? Control : $"{Control}|{OtherControl}";
}

internal sealed record LayoutAnalysis(
    IReadOnlyList<LayoutIssue> Truncations,
    IReadOnlyList<LayoutIssue> Collisions,
    IReadOnlyList<string> Unverifiable)
{
    public bool IsClean => Truncations.Count == 0 && Collisions.Count == 0;
}
