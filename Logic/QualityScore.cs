using System.Text.RegularExpressions;

namespace CheckTranslation;

internal static partial class QualityScore
{
    private static readonly Regex ScoreRegex = new(@"^\s*(\d{3})\s*[-–]\s*", RegexOptions.CultureInvariant);

    public static bool TryParse(string? comment, out int score)
    {
        score = 0;
        if (string.IsNullOrWhiteSpace(comment))
            return false;

        // Attendu : "XXX - ...". On tolère les espaces et le tiret long.
        var m = ScoreRegex.Match(comment);
        if (!m.Success)
            return false;

        return int.TryParse(m.Groups[1].Value, out score) && score is >= 0 and <= 100;
    }

    public static Color GetBackColor(int score)
    {
        // Dégradé (interpolation linéaire) entre les seuils.
        // Palette volontairement "pastel" pour conserver la lisibilité dans un DataGridView.
        // Seuils utilisés (ancrages) : 0 -> 60 -> 70 -> 80 -> 90 -> 100.
        score = Math.Clamp(score, 0, 100);

        var c0 = Color.FromArgb(255, 199, 206); // rouge
        var c60 = Color.FromArgb(255, 217, 179); // orange clair
        var c70 = Color.FromArgb(255, 242, 204); // jaune
        var c80 = Color.FromArgb(226, 239, 218);
        var c90 = Color.FromArgb(198, 239, 206); // vert
        var c100 = Color.FromArgb(180, 230, 190); // vert un peu plus soutenu

        return score switch
        {
            < 60 => LerpColor(c0, c60, score / 60f),
            < 70 => LerpColor(c60, c70, (score - 60) / 10f),
            < 80 => LerpColor(c70, c80, (score - 70) / 10f),
            < 90 => LerpColor(c80, c90, (score - 80) / 10f),
            _ => LerpColor(c90, c100, (score - 90) / 10f),
        };
    }

    private static Color LerpColor(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        int r = (int)MathF.Round(a.R + (b.R - a.R) * t);
        int g = (int)MathF.Round(a.G + (b.G - a.G) * t);
        int bl = (int)MathF.Round(a.B + (b.B - a.B) * t);
        return Color.FromArgb(255, r, g, bl);
    }

}
