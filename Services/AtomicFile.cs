namespace CheckTranslation;

/// <summary>
/// Écriture de fichier en deux temps : le contenu est d'abord écrit dans un fichier temporaire
/// du même répertoire (même volume — condition pour que le basculement soit atomique), puis
/// substitué au fichier cible. Un crash ou une coupure en pleine écriture laisse au pire un
/// temporaire orphelin : la cible, elle, reste intacte — soit l'ancienne version, soit la
/// nouvelle, jamais un fichier tronqué.
/// </summary>
internal static class AtomicFile
{
    /// <summary>
    /// Écrit via <paramref name="writeToTemp"/> dans un temporaire puis bascule sur la cible.
    /// Le callback reçoit le chemin du temporaire et doit avoir refermé le fichier en sortant.
    /// En cas d'échec (écriture ou basculement), le temporaire est supprimé au mieux et
    /// l'exception d'origine remonte telle quelle.
    /// </summary>
    public static void Write(string path, Action<string> writeToTemp)
    {
        var tempPath = path + ".tmp";
        try
        {
            writeToTemp(tempPath);

            // File.Replace préserve les ACL et attributs de la cible ; il exige qu'elle existe,
            // d'où le repli sur un simple renommage pour un fichier créé (nouvelle langue).
            // Pas de .bak systématique : la roadmap (ROADMAP.md) traite le backup comme un sujet à part.
            if (File.Exists(path))
                File.Replace(tempPath, path, destinationBackupFileName: null);
            else
                File.Move(tempPath, path);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    /// <summary>Équivalent atomique de <see cref="File.WriteAllText(string, string?)"/>.</summary>
    public static void WriteAllText(string path, string contents)
        => Write(path, tempPath => File.WriteAllText(tempPath, contents));

    private static void TryDelete(string tempPath)
    {
        try
        {
            File.Delete(tempPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nettoyage au mieux : c'est l'échec d'écriture d'origine qui doit remonter,
            // pas celui de la suppression du temporaire.
        }
    }
}
