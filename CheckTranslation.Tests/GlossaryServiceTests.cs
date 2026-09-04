namespace CheckTranslation.Tests;

public class GlossaryServiceTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  terme  ", "terme")]
    [InlineData("multi\r\nligne", "multi  ligne")]
    [InlineData("fin de ligne\n", "fin de ligne")]
    public void NormalizeCell_TrimsAndFlattensLineBreaks(string? input, string expected)
    {
        Assert.Equal(expected, GlossaryService.NormalizeCell(input));
    }
}
