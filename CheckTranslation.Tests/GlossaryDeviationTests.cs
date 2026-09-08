namespace CheckTranslation.Tests;

/// <summary>
/// Comportements portés de glossary.py (elec calc, resx-tools) : les deux outils doivent
/// compter les mêmes écarts sur le même glossary.json.
/// </summary>
public class GlossaryDeviationTests
{
    private static GlossaryEntry Entry(string source, string destination)
        => new() { Source = source, Destination = destination };

    private static TranslationRow Row(string french, string code, string translation)
    {
        var row = new TranslationRow { Key = french, French = french };
        row.Translations[code] = translation;
        return row;
    }

    [Theory]
    [InlineData("La mise à la terre est obligatoire", "terre", true)]
    [InlineData("Piste d'atterrissage", "terre", false)]
    [InlineData("Le Transformateur de courant mesure", "transformateur de courant", true)]
    [InlineData("Un transformateur mesure le courant", "transformateur de courant", false)]
    [InlineData("", "terre", false)]
    public void FrenchContains_MatchesWholeWordsInOrder(string french, string term, bool expected)
    {
        Assert.Equal(expected, GlossaryDeviation.FrenchContains(french, term));
    }

    [Fact]
    public void Variants_SplitOnSlashAndTrim()
    {
        Assert.Equal(new[] { "kabel", "kabl" }, GlossaryDeviation.Variants("kabel / kabl"));
        Assert.Equal(new[] { "SPD" }, GlossaryDeviation.Variants(" SPD "));
        Assert.Empty(GlossaryDeviation.Variants(""));
    }

    [Theory]
    [InlineData("Das Kabel ist lang", "Kabel", "de-DE", true)]
    [InlineData("Ein Leitungsschutzschalter", "Leistungsschalter", "de-DE", false)]
    [InlineData("Dwa kable", "kabel / kabl", "pl-PL", true)]
    [InlineData("Le curve di intervento", "curva", "it-IT", true)]
    [InlineData("I cavi sono lunghi", "cavo", "it-IT", false)]
    [InlineData("Nennspannung des Netzes", "Spannung", "de-DE", true)]
    public void TargetContains_IsInclusionWithVariantsAndLastLetterTolerance(string value, string cell, string code, bool expected)
    {
        // « curva » (5 lettres) couvre « curve » par la tolérance sur la finale ; « cavo » (4
        // lettres) ne couvre pas « cavi » : c'est le seuil de glossary.py, porté tel quel.
        Assert.Equal(expected, GlossaryDeviation.TargetContains(value, cell, code));
    }

    [Theory]
    [InlineData("电压互感器（测量）", "电压", true)]
    [InlineData("电 压 互感器", "电压", true)]
    [InlineData("采用 RCD 保护", "RCD", true)]
    [InlineData("无励磁分接开关", "电压", false)]
    public void TargetContains_ChineseComparesOnCjkCharacters_LatinFormsOnRawText(string value, string cell, bool expected)
    {
        Assert.Equal(expected, GlossaryDeviation.TargetContains(value, cell, "zh-CN"));
    }

    [Fact]
    public void MatchingTerms_KeepsOnlyTheLongestOverlappingTerm()
    {
        var entries = new[]
        {
            Entry("transformateur", "Transformator"),
            Entry("transformateur de courant", "Stromwandler"),
            Entry("courant", "Strom"),
        };

        var matched = GlossaryDeviation.MatchingTerms("Le transformateur de courant mesure", entries);

        // « transformateur » est contenu dans « transformateur de courant » : écarté. « courant »
        // seul aussi : il est un mot de « transformateur de courant ».
        Assert.Equal("transformateur de courant", Assert.Single(matched).Source);
    }

    [Fact]
    public void MatchingTerms_IgnoresEntriesWithoutTranslation()
    {
        var entries = new[] { Entry("neutre", "") };

        Assert.Empty(GlossaryDeviation.MatchingTerms("Le conducteur neutre", entries));
    }

    [Fact]
    public void MatchingTerms_LongerTermWithoutCellStillMasksTheShorterOne()
    {
        // Règle de glossary.py : « régime de neutre » n'a pas de cellule allemande, il n'est pas
        // contrôlé, mais il masque « neutre » — contrôler « neutre » sur « régime de neutre TT »
        // accuserait une traduction juste (Netzform TT ne contient pas Neutral).
        var entries = new[]
        {
            Entry("régime de neutre", ""),
            Entry("neutre", "Neutral"),
        };

        Assert.Empty(GlossaryDeviation.MatchingTerms("Le régime de neutre TT", entries));
        Assert.Equal("neutre", Assert.Single(GlossaryDeviation.MatchingTerms("Le conducteur neutre", entries)).Source);
    }

    [Fact]
    public void ControlledEntries_KeepsValidatedTermsIncludingThoseWithoutCell()
    {
        var terms = new[]
        {
            new GlossaryTerm { Source = "neutre", Status = GlossaryTermStatus.Validated, Translations = { ["de-DE"] = "Neutral" } },
            new GlossaryTerm { Source = "régime de neutre", Status = GlossaryTermStatus.Validated, Translations = { ["en-US"] = "earthing system" } },
            new GlossaryTerm { Source = "borne", Status = GlossaryTermStatus.Proposed, Translations = { ["de-DE"] = "Klemme" } },
        };

        var entries = GlossaryDeviation.ControlledEntries(terms, "de-DE");

        // Les deux Validé sont là, le second avec une destination vide ; le Proposé n'existe pas.
        Assert.Equal(2, entries.Count);
        Assert.Equal("Neutral", Assert.Single(entries, e => e.Source == "neutre").Destination);
        Assert.Equal(string.Empty, Assert.Single(entries, e => e.Source == "régime de neutre").Destination);
    }

    [Fact]
    public void SelectDeviations_FlagsTranslatedLinesMissingTheTerm_SkipsUntranslated()
    {
        var entries = new[] { Entry("disjoncteur", "Leistungsschalter") };
        var rows = new[]
        {
            Row("Le disjoncteur déclenche", "de-DE", "Der Leistungsschalter löst aus"),
            Row("Le disjoncteur différentiel", "de-DE", "FI/LS-Schalter"),
            Row("Le disjoncteur principal", "de-DE", ""),
            Row("La tension est haute", "de-DE", "Die Spannung ist hoch"),
        };

        var deviations = GlossaryDeviation.SelectDeviations(rows, "de-DE", entries);

        Assert.Equal("Le disjoncteur différentiel", Assert.Single(deviations).French);
    }

    [Fact]
    public void SelectDeviations_EmptyGlossary_SelectsNothing()
    {
        var rows = new[] { Row("Le disjoncteur", "de-DE", "irgendwas") };

        Assert.Empty(GlossaryDeviation.SelectDeviations(rows, "de-DE", Array.Empty<GlossaryEntry>()));
    }
}
