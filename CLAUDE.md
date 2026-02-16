# CLAUDE.md - Guide pour assistants IA sur CheckTransation

## Apercu du projet

CheckTransation est une application de bureau Windows Forms en .NET 10.0 destinee au controle des traductions d'un logiciel. Elle lit un fichier Excel exporte par ResX Resource Manager (via Visual Studio), filtre les entrees marquees `@Invariant` et affiche les traductions dans un tableau pour verification.

## Stack technique

- **Langage :** C# (version recente avec references nullables et usings implicites)
- **Framework :** .NET 10.0 (`net10.0-windows`)
- **Interface :** Windows Forms (WinForms)
- **Systeme de build :** MSBuild via la CLI `dotnet`
- **Support IDE :** Visual Studio (fichier solution : `CheckTransation.slnx`)
- **Dependances :**
  - `ClosedXML` 0.104.2 — lecture des fichiers Excel (.xlsx)

## Structure du projet

```
CheckTransation/
├── CheckTransation.slnx       # Fichier solution
├── CheckTransation.csproj      # Fichier projet (WinExe, net10.0-windows)
├── Program.cs                  # Point d'entree de l'application (STAThread, lance Form1)
├── Form1.cs                    # Logique du formulaire principal (chargement et affichage)
├── Form1.Designer.cs           # Code genere par le designer (DataGridView + StatusStrip)
├── Form1.resx                  # Definitions des ressources du formulaire
├── ExcelReader.cs              # Lecture du fichier Excel ResX Manager avec filtrage @Invariant
├── TranslationRow.cs           # Modele de donnees pour une ligne de traduction
├── Input.xlsx                  # Fichier Excel exporte par ResX Manager (~3 Mo)
├── README.md                   # Readme minimal
└── CLAUDE.md                   # Ce fichier
```

Il s'agit d'une solution a projet unique, a plat, sans sous-repertoires pour les sources, les tests ou la configuration.

## Format du fichier Excel (Input.xlsx)

Le fichier est un export ResX Resource Manager contenant une feuille `ResXResourceManager` avec ~22 000 lignes :

| Colonne | Contenu                  |
|---------|--------------------------|
| A       | Project                  |
| B       | File                     |
| C       | Key                      |
| D       | Comment (contient `@Invariant` pour les lignes a ignorer) |
| E       | Texte francais (langue par defaut, sans en-tete) |
| F       | Comment.de-DE            |
| G       | .de-DE (allemand)        |
| H-S     | Autres langues (en-US, es-ES, it-IT, nl-NL, pl-PL, zh-CN) |

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
- **Formulaire principal :** `Form1.cs` / `Form1.Designer.cs` — DataGridView en lecture seule avec colonnes Projet, Fichier, Cle, Francais, Allemand + barre de statut
- **Lecture Excel :** `ExcelReader.cs` — charge le fichier via ClosedXML, ignore les lignes `@Invariant` (colonne D)
- **Modele :** `TranslationRow.cs` — POCO avec propriétés Project, File, Key, French, German
- **Pas d'injection de dependances, pas de MVVM, pas d'architecture en couches** — application simple basee sur des formulaires
- **Donnees d'entree :** `Input.xlsx` est copie dans le repertoire de sortie via `CopyToOutputDirectory`

## Conventions de code

- **Namespace :** `CheckTransation` (note : le nom du projet contient une faute de frappe — "Transation" au lieu de "Transaction" ; conserver cette orthographe pour la coherence)
- **References nullables :** Activees — utiliser les annotations `?` lorsque les valeurs nulles sont possibles
- **Usings implicites :** Actives — les espaces de noms courants `System.*` et `System.Windows.Forms.*` sont importes automatiquement
- **Namespaces a portee de fichier :** Utilises dans `Program.cs`, `ExcelReader.cs`, `TranslationRow.cs` — a portee de bloc dans `Form1.cs` — les deux styles sont acceptables mais privilegier la portee de fichier pour les nouveaux fichiers
- **Modificateurs d'acces :** Utiliser `internal` pour les classes non exposees hors de l'assembly (voir `Program.cs`), `public` pour les classes de formulaires

## Avertissements importants

- **Ne PAS modifier `Form1.Designer.cs` a la main** — ce fichier est genere automatiquement par le designer Windows Forms. Les modifications de l'interface doivent etre faites via le designer ou en modifiant soigneusement la methode `InitializeComponent()`
- **Ne PAS modifier `Form1.resx` manuellement** — gere par le designer
- **`Input.xlsx` est un fichier binaire** — ne pas tenter de le lire ou l'analyser comme du texte ; la lecture se fait via ClosedXML dans `ExcelReader.cs`
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
