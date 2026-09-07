namespace CheckTranslation;

/// <summary>
/// Où vit le glossaire d'une solution : <c>.claude/glossary.json</c> à côté du fichier <c>.sln</c> /
/// <c>.slnx</c>. C'est le fichier que lisent aussi les skills et l'outillage <c>resx-tools</c> du
/// dépôt (elec calc) : une seule terminologie pour l'application et pour les agents qui
/// traduisent dans les sources. La présence du répertoire <c>.claude</c> suffit — le fichier
/// naît à la première sauvegarde s'il n'existe pas encore. Un export Excel n'a pas de solution :
/// il reste sur le glossaire global du profil utilisateur.
/// </summary>
internal static class SolutionGlossaryLocator
{
    public static readonly string RelativePath = Path.Combine(".claude", "glossary.json");

    public static string? Locate(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        if (!extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase))
            return null;

        var directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
        if (directory is null || !Directory.Exists(Path.Combine(directory, ".claude")))
            return null;

        return Path.Combine(directory, RelativePath);
    }
}
