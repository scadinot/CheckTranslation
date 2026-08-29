using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CheckTranslation;

/// <summary>
/// Lecture de la liste des projets d'une solution, aux deux formats : le format historique
/// « .sln » (texte) et le format « .slnx » (XML).
/// </summary>
internal static partial class SolutionReader
{
    private static readonly string[] ProjectExtensions = [".csproj", ".vbproj", ".fsproj"];

    /// <summary>
    /// Retourne les projets référencés par la solution : nom (fichier projet sans extension)
    /// et répertoire absolu. Les projets absents du disque sont ignorés silencieusement — une
    /// solution peut référencer un projet non récupéré (branche, sous-module absent).
    /// </summary>
    public static List<SolutionProject> ReadProjects(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionPath)) ?? ".";

        var relativePaths = string.Equals(Path.GetExtension(solutionPath), ".slnx", StringComparison.OrdinalIgnoreCase)
            ? ReadSlnxProjectPaths(solutionPath)
            : ReadSlnProjectPaths(solutionPath);

        var projects = new List<SolutionProject>();
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in relativePaths)
        {
            if (!ProjectExtensions.Contains(Path.GetExtension(relativePath), StringComparer.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, NormalizeSeparators(relativePath)));
            var directory = Path.GetDirectoryName(fullPath);

            if (directory is null || !Directory.Exists(directory))
            {
                System.Diagnostics.Debug.WriteLine($"[SolutionReader] Projet ignoré (répertoire absent) : {fullPath}");
                continue;
            }

            // Deux projets d'un même répertoire scanneraient les mêmes .resx : on ne garde que le premier.
            if (!seenDirectories.Add(directory))
                continue;

            projects.Add(new SolutionProject(Path.GetFileNameWithoutExtension(fullPath), directory));
        }

        return projects;
    }

    private static List<string> ReadSlnxProjectPaths(string solutionPath)
    {
        var document = XDocument.Load(solutionPath);

        // Les <Project> peuvent être imbriqués dans des <Folder> : on les prend à tous les niveaux.
        return document.Descendants("Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToList();
    }

    private static List<string> ReadSlnProjectPaths(string solutionPath)
    {
        var paths = new List<string>();

        foreach (var line in File.ReadLines(solutionPath))
        {
            var match = SlnProjectLineRegex().Match(line);
            if (match.Success)
                paths.Add(match.Groups["path"].Value);
        }

        return paths;
    }

    private static string NormalizeSeparators(string path)
        => path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

    // Project("{type-guid}") = "Nom", "chemin\projet.csproj", "{guid}"
    // Les dossiers de solution utilisent la même syntaxe, mais leur « chemin » n'a pas
    // d'extension de projet : ils sont écartés par le filtre sur l'extension.
    [GeneratedRegex("""^Project\("\{[^}]*\}"\)\s*=\s*"[^"]*",\s*"(?<path>[^"]*)"\s*,""", RegexOptions.CultureInvariant)]
    private static partial Regex SlnProjectLineRegex();
}

internal sealed record SolutionProject(string Name, string Directory);
