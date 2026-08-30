namespace CheckTranslation;

/// <summary>
/// Mesure la largeur rendue d'un texte, en pixels, pour une police donnée. En production c'est
/// GDI (<c>TextRenderer.MeasureText</c>), donc Windows ; l'injecter permet d'éprouver toute
/// l'analyse hors WinForms avec une mesure déterministe.
///
/// L'unité n'a pas d'importance : l'analyse ne compare que des rapports de mesures entre elles
/// (voir <see cref="LayoutAnalyzer"/>). Seule compte la cohérence d'un appel à l'autre.
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
/// justement pas de largeur sérialisée choisie par le concepteur.</item>
/// </list>
///
/// <b>Tout est raisonné en rapport, jamais en valeur absolue.</b> Les coordonnées d'un
/// <c>.resx</c> sont figées à la résolution du poste qui a dessiné le formulaire — sur le corpus
/// de référence, un <c>Label</c> fait 32 px de haut là où 96 ppp en donnerait 15. Comparer une
/// mesure prise ici à ces coordonnées reviendrait à mélanger deux repères, et sous-détecterait
/// d'un facteur voisin de deux. Il n'existe pas non plus de marge interne universelle : elle
/// dépend du type de contrôle.
///
/// L'étalon est donc pris dans le fichier lui-même. Pour un contrôle <c>AutoSize</c>, la taille
/// sérialisée <i>est</i> la largeur réellement rendue du texte source, mesurée par WinForms sur
/// le poste de conception. Le rapport entre la mesure de la traduction et celle du texte source,
/// appliqué à cette taille, donne la largeur attendue — l'échelle et la marge interne s'annulent.
/// Pour un contrôle à taille fixe, dont la largeur est un choix de maquette et non le reflet du
/// texte, l'échelle du formulaire est déduite de ses contrôles <c>AutoSize</c>.
///
/// Un formulaire sans aucun contrôle <c>AutoSize</c> exploitable n'est donc pas calibrable : ses
/// contrôles à taille fixe sont déclarés non vérifiables plutôt que jugés au jugé.
///
/// Les coordonnées d'un contrôle sont relatives à son parent : seules les paires de frères sont
/// comparées.
/// </summary>
internal static class LayoutAnalyzer
{
    /// <summary>
    /// Analyse les textes de <paramref name="evaluatedTexts"/> en prenant
    /// <paramref name="referenceTexts"/> comme étalon — c'est-à-dire les textes dans la langue
    /// pour laquelle la géométrie a été sérialisée.
    /// </summary>
    public static LayoutAnalysis Analyze(
        FormGeometry geometry,
        IReadOnlyDictionary<string, string> referenceTexts,
        IReadOnlyDictionary<string, string> evaluatedTexts,
        TextWidthMeasurer measure,
        double? fallbackScale = null)
    {
        var truncations = new List<LayoutIssue>();
        var boxes = new Dictionary<string, ControlBox>(StringComparer.Ordinal);
        var unverifiable = new List<string>();

        var scale = ComputeFormScale(geometry, referenceTexts, measure) ?? fallbackScale;

        foreach (var (name, text) in evaluatedTexts)
        {
            if (!geometry.ControlsByName.TryGetValue(name, out var control)
                || control.Size is not { } size)
            {
                unverifiable.Add(name);
                continue;
            }

            var font = geometry.GetEffectiveFont(control);

            if (control.GrowsWithText)
            {
                // Le contrôle s'adapte : sa largeur suit celle du texte, dans le rapport donné par
                // le texte source. Il faut sa position ET sa hauteur : une hauteur supposée nulle
                // ne croiserait jamais personne, et le contrôle sortirait silencieusement de la
                // détection.
                var reference = referenceTexts.GetValueOrDefault(name, string.Empty);
                var referenceWidth = measure(reference, font);

                if (control.Location is not { } location || referenceWidth <= 0)
                {
                    unverifiable.Add(name);
                    continue;
                }

                var grownWidth = (int)Math.Round(size.Width * (double)measure(text, font) / referenceWidth);
                boxes[name] = new ControlBox(location.Width, location.Height, grownWidth, size.Height);
                continue;
            }

            // Taille fixe : la largeur sérialisée est un choix de maquette, pas la largeur du
            // texte. Il faut donc ramener la mesure dans le repère du fichier, ce que seule
            // l'échelle déduite des contrôles AutoSize permet.
            if (scale is not { } formScale)
            {
                unverifiable.Add(name);
                continue;
            }

            var textWidth = (int)Math.Round(formScale * measure(text, font));
            if (textWidth > size.Width)
                truncations.Add(new LayoutIssue(name, null, textWidth - size.Width));

            if (control.Location is { } fixedLocation)
                boxes[name] = new ControlBox(fixedLocation.Width, fixedLocation.Height, size.Width, size.Height);
        }

        return new LayoutAnalysis(truncations, DetectCollisions(geometry, boxes), unverifiable);
    }

    /// <summary>
    /// Échelle du formulaire : combien d'unités du fichier vaut une unité de mesure. Déduite des
    /// contrôles <c>AutoSize</c>, dont la taille sérialisée est la largeur rendue de leur texte
    /// source. La médiane, et non la moyenne, pour qu'un contrôle atypique — texte vide, police
    /// substituée, contrôle déplacé à la main — ne déplace pas l'étalon.
    ///
    /// Cette échelle absorbe aussi la marge interne des contrôles <c>AutoSize</c>, donc elle
    /// surestime légèrement la largeur du texte seul. Le biais va dans le sens de la détection,
    /// et il est en grande partie annulé par la comparaison au texte source
    /// (<see cref="AnalyzeRegression"/>), qui le subit à l'identique.
    /// </summary>
    public static double? ComputeFormScale(
        FormGeometry geometry,
        IReadOnlyDictionary<string, string> referenceTexts,
        TextWidthMeasurer measure)
    {
        var ratios = new List<double>();

        foreach (var (name, text) in referenceTexts)
        {
            if (!geometry.ControlsByName.TryGetValue(name, out var control)
                || !control.GrowsWithText
                || control.Size is not { } size)
                continue;

            var width = measure(text, geometry.GetEffectiveFont(control));
            if (width > 0)
                ratios.Add((double)size.Width / width);
        }

        if (ratios.Count == 0)
            return null;

        ratios.Sort();
        return ratios[ratios.Count / 2];
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
        TextWidthMeasurer measure,
        double? fallbackScale = null)
    {
        var baseline = Analyze(geometry, sourceTexts, sourceTexts, measure, fallbackScale);
        var candidate = Analyze(geometry, sourceTexts, translatedTexts, measure, fallbackScale);

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
