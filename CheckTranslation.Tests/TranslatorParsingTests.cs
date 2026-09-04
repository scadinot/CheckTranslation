namespace CheckTranslation.Tests;

public class TranslatorParsingTests
{
    [Fact]
    public void ParseNumberedList_ParsesDotAndParenthesisNumbering()
    {
        var results = Translator.ParseNumberedList("1. Premier\n2) Deuxième", 2);

        Assert.Equal("Premier", results[0]);
        Assert.Equal("Deuxième", results[1]);
    }

    [Fact]
    public void ParseNumberedList_JoinsContinuationLines()
    {
        var results = Translator.ParseNumberedList("1. Début de la phrase\nsuite sur la ligne suivante\n2. Autre", 2);

        Assert.Equal("Début de la phrase suite sur la ligne suivante", results[0]);
        Assert.Equal("Autre", results[1]);
    }

    [Fact]
    public void ParseNumberedList_MissingEntryStaysEmpty()
    {
        var results = Translator.ParseNumberedList("2. Seule la deuxième", 3);

        Assert.True(string.IsNullOrEmpty(results[0]));
        Assert.Equal("Seule la deuxième", results[1]);
        Assert.True(string.IsNullOrEmpty(results[2]));
    }

    [Fact]
    public void ParseNumberedList_OutOfRangeIndexIsTreatedAsContinuation()
    {
        // Comportement caractérisé : un « 7. » hors bornes ne crée pas d'entrée — il est rattaché
        // au texte de l'entrée courante, comme une ligne de continuation. Il ne plante jamais et
        // ne déborde jamais du tableau attendu.
        var results = Translator.ParseNumberedList("1. Bon\n7. Fantôme", 2);

        Assert.Equal("Bon 7. Fantôme", results[0]);
        Assert.True(string.IsNullOrEmpty(results[1]));
    }

    [Fact]
    public void ParseNumberedList_IgnoresPreambleBeforeFirstNumber()
    {
        var results = Translator.ParseNumberedList("Voici les traductions :\n1. Résultat", 1);

        Assert.Equal("Résultat", results[0]);
    }

    [Fact]
    public void ParseNumberedList_LastEntryWinsOnDuplicateNumber()
    {
        var results = Translator.ParseNumberedList("1. Première version\n1. Version corrigée", 1);

        Assert.Equal("Version corrigée", results[0]);
    }
}
