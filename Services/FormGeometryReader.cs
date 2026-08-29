using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace CheckTranslation;

/// <summary>
/// Lit la géométrie des contrôles d'un formulaire WinForms depuis son <c>.resx</c> neutre, afin de
/// pouvoir confronter une traduction à la place réellement disponible dans l'interface.
///
/// Ces entrées n'existent que dans les fichiers <c>.resx</c> : l'export Excel de ResX Resource
/// Manager ne contient aucune clé de géométrie — il ne retient que les entrées traduisibles. La
/// vérification de débordement n'est donc possible qu'en mode <c>.resx</c>.
///
/// Elles n'existent que si le formulaire est en <c>Localizable = true</c> : c'est ce mode qui fait
/// sérialiser <c>Size</c>, <c>Location</c>, <c>Font</c>… par contrôle dans le <c>.resx</c>. Sur un
/// formulaire non localisable, seul le texte est présent et <see cref="Read"/> renvoie une
/// géométrie vide plutôt que null : la distinction « pas de contrôle connu » est portée par
/// <see cref="FormGeometry.TryGetForKey"/>.
///
/// Ces entrées portent un attribut <c>type</c> et sont donc, à dessein, exclues des lignes
/// traduisibles par <see cref="ResxReader"/> : les deux lectures sont complémentaires et ne se
/// marchent pas dessus.
/// </summary>
internal static class FormGeometryReader
{
    /// <summary>Nom conventionnel du formulaire lui-même dans un .resx de designer.</summary>
    internal const string FormControlName = "$this";

    private const string SizeType = "System.Drawing.Size";
    private const string PointType = "System.Drawing.Point";
    private const string FontType = "System.Drawing.Font";
    private const string BooleanType = "System.Boolean";

    /// <summary>
    /// Extrait la géométrie d'un .resx neutre de formulaire. Un fichier illisible ou au XML
    /// invalide donne une géométrie vide : comme au chargement des traductions, un fichier
    /// problématique ne doit pas interrompre l'analyse de la solution.
    /// </summary>
    public static FormGeometry Read(string neutralResxPath)
    {
        var controls = new Dictionary<string, ControlGeometryBuilder>(StringComparer.Ordinal);

        foreach (var (name, property, type, value) in EnumerateTypedEntries(neutralResxPath))
        {
            if (!controls.TryGetValue(name, out var builder))
                controls[name] = builder = new ControlGeometryBuilder(name);

            switch (property)
            {
                case "Size" when type.StartsWith(SizeType, StringComparison.Ordinal):
                    builder.Size = ParseSize(value);
                    break;
                case "ClientSize" when type.StartsWith(SizeType, StringComparison.Ordinal):
                    builder.ClientSize = ParseSize(value);
                    break;
                case "Location" when type.StartsWith(PointType, StringComparison.Ordinal):
                    builder.Location = ParseSize(value);
                    break;
                case "Font" when type.StartsWith(FontType, StringComparison.Ordinal):
                    builder.Font = ParseFont(value);
                    break;
                case "AutoSize" when type.StartsWith(BooleanType, StringComparison.Ordinal):
                    builder.AutoSize = ParseBoolean(value);
                    break;
            }
        }

        var byName = controls.Values
            .Select(builder => builder.Build())
            .ToDictionary(geometry => geometry.Name, StringComparer.Ordinal);

        byName.TryGetValue(FormControlName, out var form);

        return new FormGeometry(form?.Font, byName);
    }

    private static IEnumerable<(string Name, string Property, string Type, string Value)> EnumerateTypedEntries(string path)
    {
        if (!File.Exists(path))
            yield break;

        XDocument document;
        try
        {
            document = XDocument.Load(path);
        }
        catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
        {
            System.Diagnostics.Debug.WriteLine($"[FormGeometryReader] Fichier ignoré : {path} ({ex.GetType().Name} : {ex.Message})");
            yield break;
        }

        foreach (var element in document.Root?.Elements("data") ?? [])
        {
            var name = element.Attribute("name")?.Value;
            var type = element.Attribute("type")?.Value;

            // Seules les entrées typées portent la géométrie ; les entrées de texte n'ont pas
            // d'attribut « type ». Les métadonnées du designer (« >>… ») ne décrivent pas de mise
            // en page : elles donnent le type CLR et le parent de chaque contrôle.
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(type) || name.StartsWith(">>", StringComparison.Ordinal))
                continue;

            if (!TrySplitKey(name, out var controlName, out var property))
                continue;

            yield return (controlName, property, type, element.Element("value")?.Value ?? string.Empty);
        }
    }

    /// <summary>
    /// Sépare une clé de ressource en (contrôle, propriété) : <c>btnOk.Text</c> → (« btnOk »,
    /// « Text »). Le nom d'un contrôle ne peut pas contenir de point, la propriété est donc
    /// toujours le dernier segment.
    /// </summary>
    public static bool TrySplitKey(string resourceKey, out string controlName, out string property)
    {
        controlName = string.Empty;
        property = string.Empty;

        int separator = resourceKey.LastIndexOf('.');
        if (separator <= 0 || separator == resourceKey.Length - 1)
            return false;

        controlName = resourceKey[..separator];
        property = resourceKey[(separator + 1)..];
        return true;
    }

    /// <summary>« 120, 23 » → (120, 23). Les entiers sérialisés par le designer sont invariants.</summary>
    private static ControlSize? ParseSize(string value)
    {
        var parts = value.Split(',');
        if (parts.Length != 2)
            return null;

        return int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
            && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var height)
            ? new ControlSize(width, height)
            : null;
    }

    /// <summary>
    /// « Segoe UI, 9pt » ou « Microsoft Sans Serif, 8.25pt, style=Bold ». La taille peut être
    /// sérialisée avec une virgule décimale selon la culture de la machine qui a généré le
    /// fichier : les deux séparateurs sont acceptés.
    /// </summary>
    private static FontDescriptor? ParseFont(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
            return null;

        var family = parts[0];
        bool bold = value.Contains("Bold", StringComparison.OrdinalIgnoreCase);

        for (int i = 1; i < parts.Length; i++)
        {
            var candidate = parts[i];
            if (!candidate.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
                continue;

            var number = candidate[..^2].Trim();

            // « 8 » suivi de « 25pt » : la taille a été coupée en deux par la virgule décimale.
            if (i > 1 && float.TryParse(parts[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var whole)
                && float.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var fraction)
                && parts[i - 1].All(char.IsDigit) && number.All(char.IsDigit))
            {
                return new FontDescriptor(family, whole + fraction / MathF.Pow(10, number.Length), bold);
            }

            if (float.TryParse(number.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
                return new FontDescriptor(family, size, bold);
        }

        return new FontDescriptor(family, null, bold);
    }

    private static bool? ParseBoolean(string value)
        => bool.TryParse(value.Trim(), out var parsed) ? parsed : null;

    private sealed class ControlGeometryBuilder(string name)
    {
        public ControlSize? Size { get; set; }
        public ControlSize? ClientSize { get; set; }
        public ControlSize? Location { get; set; }
        public FontDescriptor? Font { get; set; }
        public bool? AutoSize { get; set; }

        public ControlGeometry Build() => new(name, Size ?? ClientSize, Location, Font, AutoSize);
    }
}

/// <summary>Taille ou position en pixels, telle que sérialisée par le designer.</summary>
internal readonly record struct ControlSize(int Width, int Height);

/// <summary>Police d'un contrôle. <paramref name="SizeInPoints"/> est absente si non sérialisée.</summary>
internal sealed record FontDescriptor(string Family, float? SizeInPoints, bool Bold);

/// <summary>Géométrie d'un contrôle telle que lue dans le .resx neutre du formulaire.</summary>
internal sealed record ControlGeometry(
    string Name,
    ControlSize? Size,
    ControlSize? Location,
    FontDescriptor? Font,
    bool? AutoSize);

/// <summary>
/// Géométrie de tous les contrôles d'un formulaire. <paramref name="FormFont"/> est la police du
/// formulaire, héritée par les contrôles qui n'en sérialisent pas.
/// </summary>
internal sealed record FormGeometry(
    FontDescriptor? FormFont,
    IReadOnlyDictionary<string, ControlGeometry> ControlsByName)
{
    public static FormGeometry Empty { get; } = new(null, new Dictionary<string, ControlGeometry>(StringComparer.Ordinal));

    public int Count => ControlsByName.Count;

    /// <summary>
    /// Retrouve le contrôle porteur d'une clé de ressource (<c>btnOk.Text</c> → contrôle
    /// « btnOk »). Renvoie false si la clé n'est pas de la forme contrôle.propriété, ou si le
    /// formulaire ne déclare aucune géométrie pour ce contrôle.
    /// </summary>
    public bool TryGetForKey(string resourceKey, out ControlGeometry geometry)
    {
        geometry = null!;

        return FormGeometryReader.TrySplitKey(resourceKey, out var controlName, out _)
            && ControlsByName.TryGetValue(controlName, out geometry!);
    }

    /// <summary>Police effective d'un contrôle : la sienne si sérialisée, sinon celle du formulaire.</summary>
    public FontDescriptor? GetEffectiveFont(ControlGeometry control)
        => control.Font ?? FormFont;
}
