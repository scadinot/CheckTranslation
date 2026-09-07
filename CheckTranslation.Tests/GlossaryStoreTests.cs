namespace CheckTranslation.Tests;

/// <summary>
/// Le magasin du glossaire est injectable : ces tests n'approchent jamais le glossary.json réel
/// de l'utilisateur. Aucun test n'appelle CreateBackup (qui écrit dans le profil utilisateur).
/// </summary>
public sealed class GlossaryStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CheckTranslation.Tests", Guid.NewGuid().ToString("N"));

    public GlossaryStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private string StorePath(string name = "glossary.json") => Path.Combine(_dir, name);

    private static GlossaryTerm Term(string source, GlossaryTermStatus status = GlossaryTermStatus.Validated,
        params (string Code, string Value)[] translations)
    {
        var term = new GlossaryTerm { Source = source, Status = status };
        foreach (var (code, value) in translations)
            term.Translations[code] = value;
        return term;
    }

    [Fact]
    public void Save_WritesSortedTermsAndLanguages_WithStatusAsText()
    {
        var path = StorePath();
        var service = new GlossaryService(path);

        service.ReplaceTermsAndSave(new[]
        {
            Term("tension", translations: [("en-US", "voltage"), ("de-DE", "Spannung")]),
            Term("câble", GlossaryTermStatus.Proposed, ("en-US", "cable")),
            Term("alimentation", translations: ("en-US", "supply")),
        });

        var json = File.ReadAllText(path);
        // Accents écrits tels quels : le fichier est lu par des humains et des skills, et son diff
        // doit rester lisible — pas de « câble ».
        Assert.Contains("\"câble\"", json);
        Assert.DoesNotContain("\\u00", json);
        // Le fichier est versionné avec les sources d'une solution : son ordre doit être stable.
        Assert.True(json.IndexOf("\"alimentation\"") < json.IndexOf("\"câble\""));
        Assert.True(json.IndexOf("\"câble\"") < json.IndexOf("\"tension\""));
        Assert.True(json.IndexOf("\"de-DE\"") < json.IndexOf("\"en-US\": \"voltage\""));
        // Statuts en toutes lettres : lisibles par les skills, robustes au réordonnancement de l'enum.
        Assert.Contains("\"Status\": \"Validated\"", json);
        Assert.Contains("\"Status\": \"Proposed\"", json);
        Assert.DoesNotContain("EntriesByLanguage", json);
    }

    [Fact]
    public void NewStoreInMissingDirectory_LoadsEmpty_AndIsCreatedOnFirstSave()
    {
        // Une solution équipée d'un .claude mais sans glossaire : le fichier naît à la première
        // sauvegarde, dans un répertoire qui peut lui-même ne pas exister encore.
        var path = Path.Combine(_dir, ".claude", "glossary.json");
        var service = new GlossaryService(path);

        Assert.Empty(service.GetTerms());
        Assert.False(File.Exists(path));

        service.ReplaceTermsAndSave(new[] { Term("borne", translations: ("de-DE", "Klemme")) });

        Assert.True(File.Exists(path));
        Assert.Equal("Klemme", Assert.Single(new GlossaryService(path).GetTerms()).Translations["de-DE"]);
    }

    [Fact]
    public void ReadsHandWrittenJson_MigratedFromMarkdownTable()
    {
        // La forme produite par la migration du glossary.md d'elec calc, écrite à la main.
        var path = StorePath();
        File.WriteAllText(path, """
            {
              "Version": 2,
              "Terms": [
                {
                  "Source": "câble",
                  "Context": "",
                  "Status": "Validated",
                  "ReviewerComment": "",
                  "Translations": { "de-DE": "Kabel", "pl-PL": "kabel / kabl" }
                },
                {
                  "Source": "borne",
                  "Status": "Proposed",
                  "Translations": { "it-IT": "morsetto" }
                }
              ]
            }
            """);

        var service = new GlossaryService(path);
        var terms = service.GetTerms();

        Assert.Equal(2, terms.Count);
        Assert.Equal(GlossaryTermStatus.Validated, Assert.Single(terms, t => t.Source == "câble").Status);
        Assert.Equal(GlossaryTermStatus.Proposed, Assert.Single(terms, t => t.Source == "borne").Status);
        Assert.Equal("kabel / kabl", service.GetEntries("pl-PL").Single().Destination);
        // Seuls les Validé atteignent les prompts : « borne » n'y est pas.
        Assert.Empty(service.GetPromptEntries("it-IT"));
    }

    [Fact]
    public void BuildGlossarySection_InjectsOnlyTheCanonicalFormOfMultiFormCells()
    {
        var service = new GlossaryService(StorePath());
        service.ReplaceTermsAndSave(new[] { Term("câble", translations: ("pl-PL", "kabel / kabl")) });

        var section = service.BuildGlossarySection("pl-PL", "Polonais");

        // La première forme est celle à écrire ; les suivantes ne servent qu'au contrôle des
        // formes fléchies côté resx-tools et ne doivent pas être imposées au modèle.
        Assert.Contains("| kabel |", section);
        Assert.DoesNotContain("kabl", section);
    }

    [Theory]
    [InlineData("kabel / kabl", "kabel")]
    [InlineData("surge protective device / SPD", "surge protective device")]
    [InlineData("  Spannung  ", "Spannung")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("/x", "/x")]
    public void CanonicalForm_TakesTheFirstFormBeforeSlash(string? cell, string expected)
    {
        Assert.Equal(expected, GlossaryService.CanonicalForm(cell));
    }

    [Fact]
    public void SwitchStore_ReloadsFromTheNewFile_AndBackAgain()
    {
        var pathA = StorePath("a.json");
        var pathB = StorePath("b.json");
        new GlossaryService(pathA).ReplaceTermsAndSave(new[] { Term("alpha", translations: ("en-US", "alpha")) });
        new GlossaryService(pathB).ReplaceTermsAndSave(new[] { Term("beta", translations: ("en-US", "beta")) });

        var service = new GlossaryService(pathA);
        Assert.Equal("alpha", Assert.Single(service.GetTerms()).Source);
        Assert.True(service.IsSolutionStore);

        service.SwitchStore(pathB);
        Assert.Equal(pathB, service.StorePath);
        Assert.Equal("beta", Assert.Single(service.GetTerms()).Source);

        // Retour au magasin global : le chemin change, rien n'est lu tant qu'on n'y accède pas
        // (le test ne doit pas toucher le glossaire réel de l'utilisateur).
        service.SwitchStore(null);
        Assert.False(service.IsSolutionStore);

        service.SwitchStore(pathA);
        Assert.Equal("alpha", Assert.Single(service.GetTerms()).Source);
    }

    [Fact]
    public void SwitchStore_ChangesTheFingerprint_SoCachesCannotLeak()
    {
        var pathA = StorePath("a.json");
        var pathB = StorePath("b.json");
        new GlossaryService(pathA).ReplaceTermsAndSave(new[] { Term("alpha", translations: ("en-US", "alpha")) });
        new GlossaryService(pathB).ReplaceTermsAndSave(new[] { Term("beta", translations: ("en-US", "beta")) });

        var service = new GlossaryService(pathA);
        var before = service.GetGlossaryFingerprint("en-US");
        service.SwitchStore(pathB);

        Assert.NotEqual(before, service.GetGlossaryFingerprint("en-US"));
    }

    [Fact]
    public void Locator_FindsSolutionGlossary_OnlyWhenClaudeDirectoryExists()
    {
        var solution = Path.Combine(_dir, "Franklin.sln");
        File.WriteAllText(solution, string.Empty);

        Assert.Null(SolutionGlossaryLocator.Locate(solution));

        Directory.CreateDirectory(Path.Combine(_dir, ".claude"));
        var located = SolutionGlossaryLocator.Locate(solution);

        Assert.Equal(Path.Combine(_dir, ".claude", "glossary.json"), located);
        // Le fichier lui-même peut ne pas exister encore : c'est la présence du répertoire qui compte.
        Assert.False(File.Exists(located!));
    }

    [Theory]
    [InlineData("Export.xlsx")]
    [InlineData("Projet.csproj")]
    public void Locator_IgnoresNonSolutionSources(string fileName)
    {
        Directory.CreateDirectory(Path.Combine(_dir, ".claude"));
        var source = Path.Combine(_dir, fileName);
        File.WriteAllText(source, string.Empty);

        Assert.Null(SolutionGlossaryLocator.Locate(source));
    }

    [Fact]
    public void Locator_AcceptsSlnx()
    {
        Directory.CreateDirectory(Path.Combine(_dir, ".claude"));
        var solution = Path.Combine(_dir, "CheckTranslation.slnx");
        File.WriteAllText(solution, string.Empty);

        Assert.NotNull(SolutionGlossaryLocator.Locate(solution));
    }
}
