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
        => Scan(solutionPath).Projects;

    /// <summary>
    /// Même lecture que <see cref="ReadProjects"/>, en conservant de quoi expliquer un résultat
    /// vide : combien d'entrées la solution déclare, combien sont des projets d'un langage géré,
    /// et lesquelles pointent vers un fichier absent du disque. Sans ces compteurs, une solution
    /// qui ne ramène aucun projet est indiscernable d'une solution sans traductions.
    /// </summary>
    public static SolutionScan Scan(string solutionPath)
    {
        var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(solutionPath)) ?? ".";

        var relativePaths = string.Equals(Path.GetExtension(solutionPath), ".slnx", StringComparison.OrdinalIgnoreCase)
            ? ReadSlnxProjectPaths(solutionPath)
            : ReadSlnProjectPaths(solutionPath);

        var projects = new List<SolutionProject>();
        var missing = new List<string>();
        var unsupported = new List<string>();
        var seenDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var relativePath in relativePaths)
        {
            if (!ProjectExtensions.Contains(Path.GetExtension(relativePath), StringComparer.OrdinalIgnoreCase))
            {
                // Dossiers de solution et projets d'un langage non géré (.vcxproj, .sqlproj,
                // .shproj…). Ils sont écartés à dessein, mais il faut pouvoir le dire.
                if (!string.IsNullOrEmpty(Path.GetExtension(relativePath)))
                    unsupported.Add(relativePath);
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, NormalizeSeparators(relativePath)));
            var directory = Path.GetDirectoryName(fullPath);

            // On teste le fichier projet lui-même, pas seulement son répertoire : une solution
            // peut pointer vers un .csproj supprimé dont le dossier existe encore, et scanner ce
            // dossier ramènerait des .resx qui ne font plus partie de la solution.
            if (directory is null || !File.Exists(fullPath))
            {
                System.Diagnostics.Debug.WriteLine($"[SolutionReader] Projet ignoré (fichier projet absent) : {fullPath}");
                missing.Add(relativePath);
                continue;
            }

            // Deux projets d'un même répertoire scanneraient les mêmes .resx : on ne garde que le premier.
            if (!seenDirectories.Add(directory))
                continue;

            projects.Add(new SolutionProject(Path.GetFileNameWithoutExtension(fullPath), directory));
        }

        return new SolutionScan(relativePaths.Count, projects, missing, unsupported);
    }

    private static List<string> ReadSlnxProjectPaths(string solutionPath)
    {
        var document = XDocument.Load(solutionPath);

        // Les <Project> peuvent être imbriqués dans des <Folder> : on les prend à tous les niveaux.
        // La comparaison porte sur le nom local : un jour où le format déclarerait un espace de
        // noms, un filtre sur le nom qualifié ne trouverait plus rien — et silencieusement.
        return document.Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => element.Attributes().FirstOrDefault(a => a.Name.LocalName == "Path")?.Value)
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

/// <summary>
/// Résultat détaillé de la lecture d'une solution. <paramref name="DeclaredEntries"/> compte
/// toutes les entrées déclarées, projets et dossiers de solution confondus.
/// </summary>
internal sealed record SolutionScan(
    int DeclaredEntries,
    List<SolutionProject> Projects,
    List<string> MissingProjectFiles,
    List<string> UnsupportedProjects);
