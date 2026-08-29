namespace CheckTranslation;

/// <summary>
/// Mesure la largeur rendue d'un texte avec GDI, pour confronter une traduction à la place
/// disponible dans un formulaire. Windows uniquement : <see cref="TextRenderer"/> s'appuie sur
/// GDI. C'est l'implémentation de production de <see cref="TextWidthMeasurer"/> ; l'analyse
/// elle-même reste indépendante et éprouvable avec une mesure déterministe.
///
/// Trois points méritent l'attention, chacun capable de produire des faux positifs silencieux :
///
/// <b>DPI</b> — les coordonnées lues dans le <c>.resx</c> sont figées à la résolution du poste qui
/// a dessiné le formulaire, en pratique 96 ppp. Mesurer sur le contexte de l'écran donnerait, sur
/// un affichage à 150 %, des largeurs supérieures d'un tiers aux tailles de contrôle : tout
/// paraîtrait déborder, indépendamment des traductions. La mesure passe donc par une surface de
/// référence à 96 ppp — résolution par défaut d'un <see cref="Bitmap"/> créé en mémoire — pour
/// rester dans le même repère que la géométrie. Un formulaire dessiné à une autre résolution
/// décalerait les deux repères : c'est une hypothèse, pas une garantie.
///
/// <b>Handles GDI</b> — une <see cref="Font"/> détient une ressource système. En créer une par
/// appel, sur des dizaines de milliers de lignes multipliées par sept langues, épuiserait les
/// handles. Les polices sont donc mises en cache et libérées avec l'instance.
///
/// <b>Multi-lignes</b> — une valeur de ressource peut contenir des retours à la ligne. La largeur
/// occupée est celle de la ligne la plus large, pas celle du texte concaténé.
/// </summary>
internal sealed class GdiTextWidthMeasurer : IDisposable
{
    // NoPadding : on veut l'encombrement réel des glyphes, sans la marge que TextRenderer ajoute
    // par défaut. SingleLine : chaque ligne est mesurée séparément, sans retour automatique.
    private const TextFormatFlags MeasureFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

    private static readonly char[] LineSeparators = ['\n', '\r'];

    private readonly Bitmap _referenceSurface;
    private readonly Graphics _referenceContext;
    private readonly Dictionary<FontKey, Font> _fonts = [];
    // System.Threading.Lock est .NET 9+ ; le projet cible net8.0-windows.
    private readonly object _gate = new();
    private readonly int _extraPadding;
    private bool _disposed;

    /// <param name="extraPadding">
    /// Marge ajoutée à chaque mesure, en pixels. Les contrôles réservent une marge interne que
    /// GDI ne connaît pas : un texte large d'exactement la largeur du contrôle est déjà tronqué à
    /// l'écran. La valeur juste se calibre en confrontant l'analyse au rendu réel.
    /// </param>
    public GdiTextWidthMeasurer(int extraPadding = 0)
    {
        _extraPadding = extraPadding;
        _referenceSurface = new Bitmap(1, 1);
        _referenceContext = Graphics.FromImage(_referenceSurface);
    }

    /// <summary>Largeur rendue, en pixels à 96 ppp. Un texte vide occupe zéro.</summary>
    public int Measure(string text, FontDescriptor? descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrEmpty(text))
            return 0;

        lock (_gate)
        {
            var font = GetFont(descriptor);

            int widest = 0;
            foreach (var line in text.Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries))
            {
                var width = TextRenderer.MeasureText(_referenceContext, line, font, MaxProposedSize, MeasureFlags).Width;
                if (width > widest)
                    widest = width;
            }

            return widest + _extraPadding;
        }
    }

    /// <summary>Adapte l'instance au délégué attendu par <see cref="LayoutAnalyzer"/>.</summary>
    public TextWidthMeasurer AsMeasurer() => Measure;

    private static Size MaxProposedSize => new(int.MaxValue, int.MaxValue);

    /// <summary>
    /// Police correspondant au descripteur, mise en cache. Une famille absente de la machine est
    /// silencieusement remplacée par GDI : la mesure reste plausible, mais la trace le signale car
    /// elle ne correspondra pas exactement au poste de développement.
    /// </summary>
    private Font GetFont(FontDescriptor? descriptor)
    {
        var fallback = Control.DefaultFont;
        var key = new FontKey(
            descriptor?.Family ?? fallback.FontFamily.Name,
            descriptor?.SizeInPoints ?? fallback.SizeInPoints,
            descriptor?.Bold ?? false);

        if (_fonts.TryGetValue(key, out var cached))
            return cached;

        Font font;
        try
        {
            font = new Font(key.Family, key.SizeInPoints, key.Bold ? FontStyle.Bold : FontStyle.Regular);
            if (!font.FontFamily.Name.Equals(key.Family, StringComparison.OrdinalIgnoreCase))
                System.Diagnostics.Debug.WriteLine($"[GdiTextWidthMeasurer] Police « {key.Family} » absente : GDI a substitué « {font.FontFamily.Name} ». Les mesures seront approximatives.");
        }
        catch (ArgumentException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GdiTextWidthMeasurer] Police « {key.Family} » inutilisable ({ex.Message}) : repli sur la police par défaut.");
            font = new Font(fallback.FontFamily, key.SizeInPoints, key.Bold ? FontStyle.Bold : FontStyle.Regular);
        }

        _fonts[key] = font;
        return font;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        lock (_gate)
        {
            foreach (var font in _fonts.Values)
                font.Dispose();

            _fonts.Clear();
            _referenceContext.Dispose();
            _referenceSurface.Dispose();
        }
    }

    private readonly record struct FontKey(string Family, float SizeInPoints, bool Bold);
}
