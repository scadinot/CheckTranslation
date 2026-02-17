using System.Security.Cryptography;
using System.Text.Json;

namespace CheckTranslation;

internal sealed class AppConfig
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "CheckTranslation.config.json");

    public static AppConfig Current { get; private set; } = new();

    public string TranslatePrompt { get; set; } = string.Empty;
    public string VerifyPrompt { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public bool ShowDetails { get; set; } = false;

    public void Save()
    {
        var dto = new ConfigDto(TranslatePrompt, VerifyPrompt, EncryptKey(Key), Url, ModelName, ShowDetails);
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
        Current = this;
    }

    public static AppConfig Load()
    {
        if (!File.Exists(FilePath))
            return new AppConfig();

        var json = File.ReadAllText(FilePath);
        var dto = JsonSerializer.Deserialize<ConfigDto>(json);
        if (dto is null)
            return new AppConfig();

        var config = new AppConfig
        {
            TranslatePrompt = dto.TranslatePrompt,
            VerifyPrompt = dto.VerifyPrompt,
            Key = DecryptKey(dto.Key),
            Url = dto.Url,
            ModelName = dto.ModelName,
            ShowDetails = dto.ShowDetails,
        };

        Current = config;
        return config;
    }

    private static string EncryptKey(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string DecryptKey(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var encrypted = Convert.FromBase64String(encryptedText);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return encryptedText;
        }
        catch (FormatException)
        {
            return encryptedText;
        }
    }

    private record ConfigDto(string TranslatePrompt, string VerifyPrompt, string Key, string Url, string ModelName, bool ShowDetails = false);
}
