namespace CheckTranslation.Tests;

public class QualityScoreTests
{
    [Theory]
    [InlineData("095 - bonne traduction", 95)]
    [InlineData("100 - parfait", 100)]
    [InlineData("000 - vide", 0)]
    [InlineData("  080 - espaces de tête tolérés", 80)]
    [InlineData("075 – tiret long toléré", 75)]
    public void TryParse_AcceptsExpectedFormat(string comment, int expected)
    {
        Assert.True(QualityScore.TryParse(comment, out var score));
        Assert.Equal(expected, score);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("95 - deux chiffres seulement")]
    [InlineData("150 - hors bornes")]
    [InlineData("101 - hors bornes")]
    [InlineData("abc - pas de score")]
    [InlineData("095 sans tiret")]
    [InlineData("bonne traduction, 095 - score pas en tête")]
    public void TryParse_RejectsInvalidFormats(string? comment)
    {
        Assert.False(QualityScore.TryParse(comment, out _));
    }

    [Fact]
    public void GetBackColor_ReturnsPaletteAnchors()
    {
        // Les ancrages documentés de la palette pastel : rouge à 0, vert à 90, vert soutenu à 100.
        Assert.Equal(Color.FromArgb(255, 199, 206), QualityScore.GetBackColor(0));
        Assert.Equal(Color.FromArgb(255, 217, 179), QualityScore.GetBackColor(60));
        Assert.Equal(Color.FromArgb(255, 242, 204), QualityScore.GetBackColor(70));
        Assert.Equal(Color.FromArgb(198, 239, 206), QualityScore.GetBackColor(90));
        Assert.Equal(Color.FromArgb(180, 230, 190), QualityScore.GetBackColor(100));
    }

    [Fact]
    public void GetBackColor_ClampsOutOfRangeScores()
    {
        Assert.Equal(QualityScore.GetBackColor(0), QualityScore.GetBackColor(-10));
        Assert.Equal(QualityScore.GetBackColor(100), QualityScore.GetBackColor(250));
    }

    [Fact]
    public void GetBackColor_InterpolatesBetweenAnchors()
    {
        // À mi-chemin de deux ancrages, chaque composante est entre les deux bornes.
        var low = QualityScore.GetBackColor(60);
        var high = QualityScore.GetBackColor(70);
        var mid = QualityScore.GetBackColor(65);

        Assert.InRange(mid.G, Math.Min(low.G, high.G), Math.Max(low.G, high.G));
        Assert.InRange(mid.B, Math.Min(low.B, high.B), Math.Max(low.B, high.B));
    }
}
