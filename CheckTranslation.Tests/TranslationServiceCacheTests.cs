namespace CheckTranslation.Tests;

public class TranslationServiceCacheTests
{
    private static AppConfig Config(string model) => new() { OpenAiModelName = model };

    [Fact]
    public void TranslationCache_CountsMatchLanguageAndFingerprint()
    {
        var service = new TranslationService();
        var config = Config("model-a");

        service.UpdateTranslationCache("Bonjour", "Hallo", config, "Allemand", "fp1");

        Assert.Equal(1, service.GetTranslationCacheCount(config, "Allemand", "fp1"));
        // Le fingerprint fait partie de la clé : un glossaire modifié rend l'entrée inatteignable.
        Assert.Equal(0, service.GetTranslationCacheCount(config, "Allemand", "fp2"));
        Assert.Equal(0, service.GetTranslationCacheCount(config, "Anglais", "fp1"));
        Assert.Equal(0, service.GetTranslationCacheCount(Config("model-b"), "Allemand", "fp1"));
    }

    [Fact]
    public void ClearTranslationCache_SweepsAllFingerprintsOfTheModel_LeavesOtherModels()
    {
        var service = new TranslationService();
        var config = Config("model-a");
        var other = Config("model-b");

        service.UpdateTranslationCache("Bonjour", "Hallo", config, "Allemand", "fp1");
        service.UpdateTranslationCache("Bonjour", "Hallo bis", config, "Allemand", "fp2");
        service.UpdateTranslationCache("Bonjour", "Hi", other, "Allemand", "fp1");

        // La purge matche Provider|Url|Model : elle emporte aussi les fingerprints périmés.
        Assert.Equal(2, service.ClearTranslationCache(config));
        Assert.Equal(0, service.GetTranslationCacheCount(config, "Allemand", "fp1"));
        Assert.Equal(1, service.GetTranslationCacheCount(other, "Allemand", "fp1"));
    }

    [Fact]
    public void VerificationCache_IsKeyedByPairAndIndependentFromTranslationCache()
    {
        var service = new TranslationService();
        var config = Config("model-a");

        service.UpdateVerificationCache("Bonjour", "Hallo", "095 - bon", config, "Allemand", "fp1");

        Assert.Equal(1, service.GetVerificationCacheCount(config, "Allemand", "fp1"));
        Assert.Equal(0, service.GetVerificationCacheCount(config, "Allemand", "fp2"));
        Assert.Equal(0, service.GetTranslationCacheCount(config, "Allemand", "fp1"));

        Assert.Equal(1, service.ClearVerificationCache(config));
        Assert.Equal(0, service.GetVerificationCacheCount(config, "Allemand", "fp1"));
    }

    [Fact]
    public void UpdateTranslationCache_SameKeyOverwrites()
    {
        var service = new TranslationService();
        var config = Config("model-a");

        service.UpdateTranslationCache("Bonjour", "Hallo", config, "Allemand", "fp1");
        service.UpdateTranslationCache("Bonjour", "Guten Tag", config, "Allemand", "fp1");

        Assert.Equal(1, service.GetTranslationCacheCount(config, "Allemand", "fp1"));
    }
}
