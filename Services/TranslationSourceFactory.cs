namespace CheckTranslation;

internal sealed class TranslationSourceFactory : ITranslationSourceFactory
{
    public string OpenFileFilter => "Solution Visual Studio (*.sln;*.slnx)|*.sln;*.slnx";

    public ITranslationSource Create(string path)
    {
        var extension = System.IO.Path.GetExtension(path);

        if (string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
            return new ResxTranslationSource(path);

        throw new NotSupportedException(
            $"Extension non prise en charge : « {extension} ». Attendu : .sln ou .slnx.");
    }
}
