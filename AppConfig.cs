using System.Security.Cryptography;

namespace CheckTranslation;

internal sealed class AppConfig
{
    private static readonly string FilePath = Path.Combine(AppContext.BaseDirectory, "CheckTransation.cfg");

    public static AppConfig Current { get; private set; } = new();

    public string Prompt { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public void Save()
    {
        var lines = new[]
        {
            $"Prompt={Prompt.Replace("\r", "").Replace("\n", "\\n")}",
            $"Key={EncryptKey(Key)}",
            $"Url={Url}",
            $"ModelName={ModelName}",
        };
        File.WriteAllLines(FilePath, lines);
        Current = this;
    }

    public static AppConfig Load()
    {
        var config = new AppConfig();
        if (!File.Exists(FilePath))
            return config;

        foreach (var line in File.ReadAllLines(FilePath))
        {
            var sep = line.IndexOf('=');
            if (sep < 0) continue;

            var key = line[..sep];
            var value = line[(sep + 1)..];

            switch (key)
            {
                case "Prompt": config.Prompt = value.Replace("\\n", "\n"); break;
                case "Key": config.Key = DecryptKey(value); break;
                case "Url": config.Url = value; break;
                case "ModelName": config.ModelName = value; break;
            }
        }

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
}
