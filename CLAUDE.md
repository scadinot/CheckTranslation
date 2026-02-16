# CLAUDE.md - Guide pour assistants IA sur CheckTransation

## Apercu du projet

CheckTransation est une application de bureau Windows Forms en .NET 10.0 destinee au traitement et a la validation de donnees de transactions a partir de fichiers Excel. Le projet est a un stade initial avec une structure de base en place mais peu de fonctionnalites implementees.

## Stack technique

- **Langage :** C# (version recente avec references nullables et usings implicites)
- **Framework :** .NET 10.0 (`net10.0-windows`)
- **Interface :** Windows Forms (WinForms)
- **Systeme de build :** MSBuild via la CLI `dotnet`
- **Support IDE :** Visual Studio (fichier solution : `CheckTransation.slnx`)
- **Dependances :** Aucune en dehors du SDK .NET (pas de packages NuGet)

## Structure du projet

```
CheckTransation/
├── CheckTransation.slnx       # Fichier solution
├── CheckTransation.csproj      # Fichier projet (WinExe, net10.0-windows)
├── Program.cs                  # Point d'entree de l'application (STAThread, lance Form1)
├── Form1.cs                    # Logique du formulaire principal (classe partielle)
├── Form1.Designer.cs           # Code genere automatiquement par le designer
├── Form1.resx                  # Definitions des ressources du formulaire
├── Input.xlsx                  # Fichier de donnees Excel en entree (~3 Mo)
├── README.md                   # Readme minimal
└── CLAUDE.md                   # Ce fichier
```

Il s'agit d'une solution a projet unique, a plat, sans sous-repertoires pour les sources, les tests ou la configuration.

## Commandes de build et d'execution

```bash
# Build (debug)
dotnet build

# Build (release)
dotnet build -c Release

# Lancer l'application
dotnet run

# Nettoyer les artefacts de build
dotnet clean

# Publier
dotnet publish -c Release
```

## Notes d'architecture

- **Point d'entree :** `Program.cs` — bootstrap WinForms standard avec `ApplicationConfiguration.Initialize()` et `Application.Run(new Form1())`
- **Formulaire principal :** `Form1.cs` / `Form1.Designer.cs` — utilise le patron classe partielle (code-behind) standard de WinForms
- **Pas d'injection de dependances, pas de MVVM, pas d'architecture en couches** — application simple basee sur des formulaires
- **Donnees d'entree :** `Input.xlsx` est versionne dans le depot et contient vraisemblablement les donnees de transactions que l'application traite

## Conventions de code

- **Namespace :** `CheckTransation` (note : le nom du projet contient une faute de frappe — "Transation" au lieu de "Transaction" ; conserver cette orthographe pour la coherence)
- **References nullables :** Activees — utiliser les annotations `?` lorsque les valeurs nulles sont possibles
- **Usings implicites :** Actives — les espaces de noms courants `System.*` et `System.Windows.Forms.*` sont importes automatiquement
- **Namespaces a portee de fichier :** Utilises dans `Program.cs` (`namespace CheckTransation;`), a portee de bloc dans `Form1.cs` — les deux styles sont acceptables mais privilegier la portee de fichier pour les nouveaux fichiers
- **Modificateurs d'acces :** Utiliser `internal` pour les classes non exposees hors de l'assembly (voir `Program.cs`), `public` pour les classes de formulaires

## Avertissements importants

- **Ne PAS modifier `Form1.Designer.cs` a la main** — ce fichier est genere automatiquement par le designer Windows Forms. Les modifications de l'interface doivent etre faites via le designer ou en modifiant soigneusement la methode `InitializeComponent()`
- **Ne PAS modifier `Form1.resx` manuellement** — gere par le designer
- **`Input.xlsx` est un fichier binaire** — ne pas tenter de le lire ou l'analyser comme du texte ; utiliser une bibliotheque comme `EPPlus`, `ClosedXML` ou `NPOI` si la lecture Excel est necessaire dans le code
- **Le projet cible `net10.0-windows`** — il necessite le SDK .NET 10 et ne fonctionne que sous Windows

## Tests

- Aucun framework de test n'est actuellement configure
- Aucun projet de test n'existe dans la solution
- Pour ajouter des tests, utiliser un projet de test separe avec xUnit ou NUnit (ex. : `CheckTransation.Tests/`)

## Linting et formatage

- Aucune configuration `.editorconfig`, StyleCop ou analyseur Roslyn n'existe
- Suivre les conventions C# standard (PascalCase pour les membres publics, camelCase pour les variables locales et parametres)
- Aucun pipeline CI/CD n'est configure

## Workflow Git

- **Branche principale :** `master`
- **Patterns d'exclusion :** `.gitignore` standard Visual Studio (artefacts de build, fichiers utilisateur, packages)
- **Les commits sont signes** via cle SSH
- Limiter les modifications de `Input.xlsx` — c'est un fichier binaire volumineux (~3 Mo)
