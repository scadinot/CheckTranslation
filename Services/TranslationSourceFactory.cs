namespace CheckTranslation;

internal sealed class TranslationSourceFactory : ITranslationSourceFactory
{
    public string OpenFileFilter =>
        "Sources prises en charge (*.xlsx;*.sln;*.slnx)|*.xlsx;*.sln;*.slnx"
        + "|Export Excel ResX Resource Manager (*.xlsx)|*.xlsx"
        + "|Solution Visual Studio (*.sln;*.slnx)|*.sln;*.slnx";

    public ITranslationSource Create(string path)
    {
        var extension = System.IO.Path.GetExtension(path);

        if (string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
            return new ExcelTranslationSource(path);

        if (string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
            return new ResxTranslationSource(path);

        throw new NotSupportedException(
            $"Extension non prise en charge : « {extension} ». Attendu : .xlsx, .sln ou .slnx.");
    }
}
