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

    [Fact]
    public void ParseExtractionResponse_ReadsArray_EvenWrappedInProseOrFences()
    {
        const string raw = """
            Voici les termes :
            ```json
            [
              { "term": "disjoncteur", "translation": "Schutzschalter", "context": "protection" },
              { "term": "borne", "translation": "Klemme", "context": "" }
            ]
            ```
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.True(parse.Success);
        Assert.False(parse.Truncated);
        Assert.Equal(2, parse.Entries.Count);
        Assert.Equal("Schutzschalter", parse.Entries[0].Destination);
    }

    [Fact]
    public void ParseExtractionResponse_EmptyArray_IsASuccessWithNoEntries()
    {
        // « Rien trouvé » est un résultat, pas un défaut : l'UI ne doit pas le présenter en erreur.
        var parse = GlossaryService.ParseExtractionResponse("[]");

        Assert.True(parse.Success);
        Assert.Empty(parse.Entries);
    }

    [Fact]
    public void ParseExtractionResponse_TruncatedArray_IsUnreadableAndFlaggedTruncated()
    {
        // Signature d'une réponse coupée par le plafond de tokens : tableau ouvert, jamais fermé.
        const string raw = """
            [
              { "term": "disjoncteur", "translation": "Schutzschalter", "context": "protection" },
              { "term": "borne", "translation": "Klem
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.False(parse.Success);
        Assert.True(parse.Truncated);
        Assert.Empty(parse.Entries);
    }

    [Fact]
    public void ParseExtractionResponse_TruncatedWithSurvivingInnerBracket_IsStillFlaggedTruncated()
    {
        // Un ']' intérieur (dans un contexte) a survécu à la coupure : le JSON reste invalide et
        // la fin de réponse trahit la troncature.
        const string raw = """
            [
              { "term": "cable", "translation": "Kabel", "context": "voir [1]" },
              { "term": "borne", "translation": "Klem
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.False(parse.Success);
        Assert.True(parse.Truncated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Je ne peux pas répondre à cette demande.")]
    [InlineData("{ \"terms\": \"pas un tableau\" }")]
    public void ParseExtractionResponse_NoArray_IsUnreadableButNotTruncated(string raw)
    {
        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.False(parse.Success);
        Assert.False(parse.Truncated);
    }

    [Fact]
    public void ParseExtractionResponse_SkipsNonObjectItemsAndMissingFields()
    {
        var parse = GlossaryService.ParseExtractionResponse("[ 42, { \"term\": \"borne\" }, \"texte\" ]");

        Assert.True(parse.Success);
        var entry = Assert.Single(parse.Entries);
        Assert.Equal("borne", entry.Source);
        Assert.Equal(string.Empty, entry.Destination);
    }

    [Fact]
    public void ParseExtractionResponse_IgnoresBracketsInProseAroundTheArray()
    {
        // « [1] » avant n'est pas un tableau d'objets, « [2] » après ne décale pas la fin :
        // ni l'un ni l'autre ne doit faire passer une réponse valide pour illisible.
        const string raw = """
            Termes extraits selon la norme [1] :
            [ { "term": "borne", "translation": "Klemme", "context": "raccordement" } ]
            Références : voir [2] et [3].
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.True(parse.Success);
        Assert.Equal("Klemme", Assert.Single(parse.Entries).Destination);
    }

    [Fact]
    public void ParseExtractionResponse_UnclosedProseBracketBeforeJson_IsNotATruncation()
    {
        // Un lien Markdown amputé ou un crochet de prose jamais refermé ne peut pas ouvrir le
        // tableau attendu : il est ignoré, le vrai JSON plus loin est lu normalement.
        const string raw = """
            D'après la norme [CEI 60364 (voir la référence en ligne
            [ { "term": "borne", "translation": "Klemme", "context": "raccordement" } ]
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.True(parse.Success);
        Assert.Equal("borne", Assert.Single(parse.Entries).Source);
    }

    [Fact]
    public void ParseExtractionResponse_TruncatedAfterProseBracket_IsStillFlaggedTruncated()
    {
        const string raw = """
            Voir [1].
            [ { "term": "borne", "translation": "Klem
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.False(parse.Success);
        Assert.True(parse.Truncated);
    }

    [Fact]
    public void ParseExtractionResponse_BracketsInsideStrings_DoNotCloseTheArray()
    {
        const string raw = """
            [
              { "term": "câble", "translation": "Kabel", "context": "voir [1] et l'échappement \"[\" ici" },
              { "term": "borne", "translation": "Klemme", "context": "" }
            ]
            """;

        var parse = GlossaryService.ParseExtractionResponse(raw);

        Assert.True(parse.Success);
        Assert.Equal(2, parse.Entries.Count);
    }
}
