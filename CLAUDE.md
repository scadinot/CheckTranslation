# CLAUDE.md — Guide technique

Guide technique du projet **CheckTranslation**, destiné aux humains comme aux assistants IA. Couvre l'architecture, les conventions et les avertissements — ce qu'une session de travail doit toujours avoir en tête.

> Documentation utilisateur et installation : voir [README.md](README.md).
> Feuille de route et historique des changements : voir [ROADMAP.md](ROADMAP.md).
> Process et gouvernance du glossaire : voir [GLOSSAIRE.md](GLOSSAIRE.md).

---

## Table des matières

1. [Aperçu](#1-aperçu)
2. [Stack technique](#2-stack-technique)
3. [Structure du projet](#3-structure-du-projet)
4. [Format Excel (ResX Resource Manager)](#4-format-excel-resx-resource-manager)
5. [Build & exécution](#5-build--exécution)
6. [Architecture](#6-architecture)
7. [Flux fonctionnels](#7-flux-fonctionnels)
8. [Détails IA (Translator + TranslationService + Glossaire)](#8-détails-ia-translator--translationservice--glossaire)
9. [Conventions de code](#9-conventions-de-code)
10. [Avertissements importants](#10-avertissements-importants)
11. [Tests](#11-tests)
12. [Suivi et roadmap](#12-suivi-et-roadmap)

---

## 1. Aperçu

CheckTranslation est une application de bureau **Windows Forms** en **C# / .NET 8** (`net8.0-windows`) destinée à **contrôler, traduire et vérifier** des chaînes de ressources `.resx`. Elle sait travailler sur **deux sources** :

- un **export Excel** généré par ResX Resource Manager (`.xlsx`) ;
- les **fichiers `.resx` directement**, désignés par une solution (`.sln` / `.slnx`).

Dans les deux cas, l'application affiche les entrées dans un `DataGridView`, permet l'édition de la traduction dans une langue cible, puis réécrit la source d'origine.

Elle peut appeler les API **OpenAI** et **Anthropic**, en direct ou au travers de la passerelle **Bifrost**, pour traduire et vérifier des traductions en lot, avec cache mémoire et glossaire métier injecté dans les prompts. Elle peut aussi fusionner des traductions d'un fichier source vers un fichier destination avec résolution des conflits ligne-par-ligne.

---

## 2. Stack technique

- **Langage :** C# (références nullables activées, implicit usings activés)
- **Framework :** .NET 8.0 (`net8.0-windows`)
- **Interface :** Windows Forms
- **Build :** MSBuild via la CLI `dotnet`
- **IDE :** Visual Studio (fichier solution : `CheckTranslation.slnx`)

### Dépendances NuGet
- `ClosedXML` `0.104.2` — lecture/écriture `.xlsx`
- `OpenAI` `2.8.0` — client chat completions + listing modèles
- `Anthropic` `12.8.0` — client messages + listing modèles
- `Markdig` `0.38.0` — rendu Markdown (aperçu des prompts)
- `Polly` `8.5.2` — retry + backoff exponentiel + jitter sur les appels IA
- `Microsoft.Extensions.DependencyInjection` `9.0.0` — conteneur DI

---

## 3. Structure du projet

```
CheckTranslation/
├── CheckTranslation.slnx        # Solution
├── CheckTranslation.csproj      # Projet (WinExe, net8.0-windows)
├── Program.cs                   # Point d'entrée (STAThread, bootstrap DI)
├── Forms/
│   ├── MainForm.cs                           # Logique principale (~2500 lignes, organisée en sections)
│   ├── MainForm.Designer.cs                  # Code généré par le designer
│   ├── MainForm.resx                         # Ressources du form
│   ├── ConfigForm.cs                         # Config (prompts, IA, vidage cache)
│   ├── ConfigForm.Designer.cs
│   ├── ConfigForm.resx
│   ├── MergeDifferenceForm.cs                # Dialog résolution conflits de fusion
│   ├── MergeDifferenceForm.Designer.cs
│   ├── MergeDifferenceForm.resx
│   ├── GlossaryForm.cs                       # Éditeur du glossaire transversal (terme × langue, statuts)
│   ├── GlossaryForm.Designer.cs
│   ├── GlossaryForm.resx
│   ├── GlossaryExtractionDialog.cs           # Extraction IA assistée (validation terme par terme)
│   ├── GlossaryImportDiffForm.cs             # Résolution des différences d'un import de glossaire
│   ├── GlossaryImportDiffForm.Designer.cs   # (pas de .resx : formulaire sans ressources)
│   ├── GlossaryExtractionDialog.Designer.cs
│   ├── GlossaryExtractionDialog.resx
│   ├── DashboardForm.cs                      # Tableau de bord de l'état des traductions
│   ├── DashboardForm.Designer.cs
│   └── DashboardForm.resx
├── Models/
│   ├── TranslationRow.cs                # Ligne de traduction + dictionnaires par colonne
│   ├── AiProvider.cs                    # Enum OpenAI / Anthropic
│   ├── AppConfig.cs                     # Config persistante (JSON + DPAPI)
│   ├── MergeDifference.cs               # Différence source vs destination
│   ├── MergeDifferenceResolution.cs     # Décision utilisateur par ligne (fusion)
│   ├── MergeRowSnapshot.cs              # Snapshot lecture seule pour l'UI de fusion
│   ├── Glossary.cs                      # Glossaire transversal (GlossaryTerm, statuts) + legacy v1
│   ├── GlossaryEntry.cs                 # Projection d'un terme sur une langue (Source / Destination / Context)
│   └── LayoutStatus.cs                  # Enum verdict de mise en page (NotChecked / Ok / Truncated / Collision / Unverifiable)
├── Services/
│   ├── ITranslationSource.cs                 # Abstraction de source (Load / Save / SupportsMerge)
│   ├── ITranslationSourceFactory.cs / TranslationSourceFactory.cs  # .xlsx → Excel, .sln/.slnx → resx
│   ├── ExcelTranslationSource.cs             # Source Excel (seule à supporter la fusion)
│   ├── ResxTranslationSource.cs              # Source .resx
│   ├── IExcelService.cs / ExcelService.cs    # Façade DI pour la fusion Excel
│   ├── ExcelReader.cs                        # Lecture / écriture / fusion via ClosedXML
│   ├── ResxReader.cs                         # Lecture / écriture directe des .resx (XDocument)
│   ├── AtomicFile.cs                         # Écriture en deux temps : temporaire + File.Replace
│   ├── FormGeometryReader.cs                 # Géométrie des contrôles WinForms (socle anti-débordement)
│   ├── GdiTextWidthMeasurer.cs               # Mesure GDI de la largeur rendue (Windows)
│   ├── ILayoutCheckService.cs / LayoutCheckService.cs  # Verdict de mise en page par ligne
│   ├── SolutionReader.cs                     # Liste des projets d'un .sln ou .slnx
│   ├── SourceLoadProgress.cs                 # record struct(Done, Total)
│   ├── ITranslationService.cs / TranslationService.cs  # Cache mémoire + batch + dédup
│   ├── Translator.cs                         # Appels bruts OpenAI/Anthropic + Polly
│   ├── GlossaryExcel.cs                      # Export / import Excel du glossaire (feuilles Glossaire + Infos)
│   └── IGlossaryService.cs / GlossaryService.cs        # Persistance JSON transversale + fingerprint SHA256
├── Logic/
│   ├── QualityScore.cs                       # Parsing score "XXX - ..." + dégradé couleur
│   ├── LayoutAnalyzer.cs                     # Troncatures + collisions entre contrôles frères
│   ├── TranslationRowFiltering.cs            # Filtrage côté client (texte + pseudo-filtres score / traduction / layout)
│   ├── TranslationStatistics.cs              # Calcul de l'état d'avancement (tableau de bord)
│   ├── GlossaryDiff.cs                       # Diff / application d'un import de glossaire (pure)
│   └── GlossaryImpact.cs                     # Lignes impactées par un changement de glossaire (pure)
├── Controls/
│   └── SortableBindingList.cs                # BindingList<T> triable
├── CheckTranslation.Tests/                   # Tests xUnit de la logique pure (§11) — exclu du glob de l'exe
├── .github/workflows/ci.yml                  # CI GitHub Actions : build + tests sur windows-latest
├── FormTest/                                 # Fixture : formulaire localisé en 7 langues, banc d'essai manuel de la vérification de mise en page (§11)
├── Resources/                                # Icônes PNG (drapeaux, toolbar, tabs)
├── Input.xlsx                                # Fichier Excel exemple (~3 Mo)
├── README.md                                 # Documentation utilisateur
└── CLAUDE.md                                 # Ce fichier (guide technique + suivi)
```

---

## 4. Format Excel (ResX Resource Manager)

Export d'une feuille `ResXResourceManager` (~22 000 lignes). Les colonnes sont 1-indexées :

| Colonne | Contenu |
|---------|---------|
| A (1) | Project |
| B (2) | File |
| C (3) | Key |
| D (4) | Comment source (contient `@Invariant` pour ignorer la ligne) |
| E (5) | Texte français (langue par défaut, pas de colonne de commentaire) |
| F | Comment.de-DE |
| G | .de-DE (allemand) |
| H…S | Autres langues : en-US, es-ES, it-IT, nl-NL, pl-PL, zh-CN (chaque langue = une colonne de commentaire précédée d'une colonne de traduction) |

Convention : pour une langue dont la colonne de traduction est `col`, la colonne de commentaire associée est `col - 1`. Cette correspondance code de langue → colonne est portée par `LanguageInfo.Column` et **ne sort pas de `ExcelReader`** : partout ailleurs, une langue est identifiée par son code.

### 4.bis Format .resx (lecture directe)

L'utilisateur ouvre une solution (`.sln` ou `.slnx`) ; `SolutionReader` en extrait les projets, puis `ResxReader` scanne chaque répertoire de projet à la recherche des `.resx`.

| Notion | Origine |
|---|---|
| `Project` | Nom du fichier projet sans extension (`ElectricalBusiness.csproj` → `ElectricalBusiness`) |
| `File` | Chemin du `.resx` **neutre** relatif au projet, sans extension, séparateurs `\` (`Properties\Msg`) |
| `Key` | Attribut `name` de l'élément `<data>` |
| Français + commentaire source | `<value>` et `<comment>` du fichier neutre (`Msg.resx`) |
| Traduction + commentaire par langue | `<value>` et `<comment>` de la variante (`Msg.de-DE.resx`) |

Ces conventions reproduisent **exactement** les colonnes `Project` / `File` / `Key` de l'export Excel : les deux sources produisent donc la même clé de corrélation `Project\|File\|Key`.

Entrées exclues au chargement (mêmes règles que ResX Resource Manager) :
- commentaire neutre contenant `@Invariant` (~30 % des lignes sur le corpus de référence) ;
- entrées non textuelles, repérées par un attribut `type` ou `mimetype` (images, icônes, blobs sérialisés) ;
- métadonnées du designer WinForms, dont la clé commence par `>>` (les clés `$this.Text`, `btnOk.Text`… sont conservées) ;
- répertoires `bin`, `obj`, `.git`, `.vs`.

Une variante de culture n'est jamais confondue avec un fichier neutre : le suffixe est validé contre la liste des cultures connues du système, y compris pour les cultures non gérées par l'application (`Msg.pt-BR.resx` n'est ni chargé, ni traité comme neutre). En second recours, un suffixe de forme BCP-47 valide mais absent de cette liste est accepté (`ca-ES-valencia`).

⚠ La validation ne peut **pas** reposer sur l'exception de `CultureInfo.GetCultureInfo` : sous ICU (le défaut depuis .NET 5, Windows compris), elle ne lève pas pour un nom quelconque bien formé — `Designer`, `Backup`, `resources` renvoient une culture « custom ». S'y fier ferait passer n'importe quel suffixe pour une culture, et le fichier neutre correspondant (`Notes.Backup.resx`) disparaîtrait silencieusement de la découverte.

---

## 5. Build & exécution

```bash
# Build debug
dotnet build

# Build release
dotnet build -c Release

# Lancer
dotnet run

# Tests (chemin explicite : voir §11)
dotnet test CheckTranslation.Tests/CheckTranslation.Tests.csproj -c Release

# Nettoyer
dotnet clean

# Publier
dotnet publish -c Release

```

Le projet cible `net8.0-windows` → SDK .NET 8 requis, Windows uniquement.

### 5.1 Premier chargement plus lent : compilation étagée

Le premier chargement d'une solution après le lancement de l'application est nettement plus long
que les suivants, **à cache disque identique**. Mesuré sur une solution synthétique de 474
formulaires (11 850 lignes), cinq chargements des mêmes fichiers dans le même processus :

| Chargement | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|
| Par défaut | 627 ms | 623 | 426 | 328 | **271** |
| `DOTNET_TieredCompilation=0` | 451 ms | — | — | — | **398** |

C'est la **compilation étagée** de .NET : au premier appel, chaque méthode est compilée par un JIT
rapide qui produit du code médiocre ; après une trentaine d'appels, elle est recompilée optimisée
en arrière-plan avec profilage dynamique. La descente progressive est la signature du passage au
palier 1, méthode après méthode — pas la marche d'escalier qu'on verrait avec un cache. La ligne
`TieredCompilation=0` le confirme par la négative : premier chargement bien meilleur, et plus
aucune amélioration ensuite.

Ce qui n'y est **pas** pour grand-chose, contrairement à l'intuition :
- le `static readonly KnownCultureNames` de `ResxReader`, qui énumère 852 cultures via ICU au
  premier accès : **3,7 ms** ;
- le cache disque, écarté par construction dans la mesure ci-dessus — il joue en revanche au tout
  premier chargement après démarrage de la machine, et lui seul ne repart pas à zéro quand on
  relance l'application.

### 5.2 Contre-mesures évaluées, aucune retenue

Trois pistes ont été mises en œuvre et mesurées, puis écartées. Le tableau est conservé pour que
la question ne se rouvre pas à l'aveugle.

Médiane de 5 exécutions, premier chargement :

| Configuration | 1ᵉʳ chargement | 5ᵉ |
|---|---|---|
| Référence | 627 ms | 271 ms |
| ReadyToRun | 590 ms *(−6 %)* | 259 ms |
| Préchauffage d'une solution miniature | 576 ms *(−8 %)* | 259 ms |
| `TieredCompilation=0` | 451 ms *(−28 %)* | 398 ms *(+47 %)* |

**ReadyToRun rend 6 %, pas davantage**, et l'assembly double de taille (482 Ko → 1016 Ko) pour
cela. La raison : le coût observé n'est pas surtout du *temps de compilation*, que ReadyToRun
supprime, mais de la *qualité du code* au palier 0, et le code ReadyToRun se situe entre le
palier 0 et le palier 1. Un préchauffage à vide au démarrage n'apporte pas plus, et les deux ne se
cumulent pas.

Seul `TieredCompilation=0` déplace vraiment le premier chargement — au prix de 47 % sur tous les
suivants, et sur tout le reste de l'application. Mauvais échange.

**Aucune n'est activée** : 6 % ne paient pas un binaire doublé et une publication liée à
l'architecture. Si `PublishReadyToRun` est réactivé un jour, il doit l'être **sous condition qu'un
RID soit fourni** (`Condition="'$(RuntimeIdentifier)' != ''"`) : sans elle, un
`dotnet publish -c Release` sans `-r` déduit le RID de la machine hôte et échoue dès qu'elle n'est
pas Windows (`NETSDK1082`, pas de runtime pack WindowsForms pour `linux-x64`). Vérifié.

---

## 6. Architecture

### 6.1 Bootstrap (Program.cs + DI)

`Program.Main` :
1. `AppConfig.Load()` → alimente `AppConfig.Current`.
2. `ApplicationConfiguration.Initialize()` (DPI, police).
3. Configure un `ServiceCollection` :
   - `IExcelService → ExcelService` (singleton)
   - `ITranslationService → TranslationService` (singleton)
   - `IGlossaryService → GlossaryService` (singleton)
   - `ConfigForm`, `GlossaryForm`, `GlossaryExtractionDialog` (transient + factory `Func<T>`)
   - `MainForm` (transient, reçoit les services et factories par ctor)
4. `Application.Run(MainForm)` résolu via le conteneur.

Chaque formulaire principal a un **ctor par défaut** qui instancie manuellement les services (compat Designer) et un **ctor DI** utilisé en runtime.

### 6.2 Formulaires

**`Forms/MainForm.cs`** (~2500 lignes) — `partial class` (avec `MainForm.Designer.cs`). Toute la logique applicative est dans ce fichier, organisée en **sections commentées** :
- Constructeur, InitComponent, init des colonnes / boutons / langues
- `// --- Indicateur de qualité (couleur) ---` : `DataGridView_CellFormatting` + `QualityScore.GetBackColor`
- `// --- Filtres (style ResX Resource Manager) ---` : loupe dans l'en-tête, TextBox superposé, debounce 300 ms. Les métriques de layout sont calculées au runtime selon le DPI (`_filterControlHeight`, `_columnHeaderTitleHeight`, `FilterIconWidth`, `FilterBottomMargin`) — plus aucune constante en pixels.
- `// --- Arborescence de la solution (style ResX Resource Manager) ---` : panneau gauche (`SplitContainer` créé par code, grille du Designer réparentée dans `Panel2`), arbre Projet → Fichier à cases à cocher. Décocher restreint l'affichage de la grille (`_uncheckedFiles`, identité projet + fichier), bouton toolbar pour replier le panneau, largeur et visibilité persistées.
- Tri, menu contextuel, chargement/sauvegarde, traduire/vérifier IA, glossaire, auto-traduire
- **Verrou d'écriture disque** : `SetWritingState(bool)` positionne `_isWriting`, désactive `toolStrip` + `dataGridView` et fait annuler la fermeture par `MainForm_FormClosing`. Le refus de fermeture est signalé sans modale par `FlashCloseBlocked()` (message temporaire de 3 s dans la status bar, texte précédent restauré).
- `// --- Icônes ---`
- `// --- Persistance de disposition ---` : taille fenêtre + largeur colonnes (`ColumnFillWeights`, jeu unique)
- `// --- Bouton Rafraîchir + F5 ---` : rechargement fichier + détection changements source
- `// --- Filtre par score de vérification ---` : ComboBox dans l'en-tête Commentaire

**`Forms/ConfigForm.cs`** — dialog modal pour `AppConfig`. Tous les contrôles (prompts, fournisseur IA, clés, URLs, modèles, boutons "Vider cache trad./vérif.") sont dans le Designer. `ConfigForm.cs` contient uniquement les handlers et la logique métier.

**`Forms/MergeDifferenceForm.cs`** — dialog affichant côte à côte ligne source et ligne destination ; deux checkboxes ("reporter français+commentaire", "reporter traduction+commentaire") + bouton Continuer/Annuler. Colonnes DataGridView générées dynamiquement selon le contexte.

**`Forms/GlossaryForm.cs`** — éditeur du glossaire transversal : une ligne par terme, une colonne par langue (créées par code depuis `MainForm.Languages`), plus Statut (Proposé / En contrôle / Validé) et Commentaire réviseur (lecture seule, rempli par l'import). Grille non liée : les dictionnaires de `GlossaryTerm` ne se prêtent pas au binding. Doublons de Source refusés à l'enregistrement, terme saisi à la main Validé par défaut, confirmation si modifications non enregistrées.

**`Forms/GlossaryExtractionDialog.cs`** — dialog d'extraction assistée : l'IA propose une liste de termes candidats (source + destination + contexte), l'utilisateur coche ceux à ajouter (édition possible inline) puis valide. Retourne `AcceptedEntries` (liste filtrée).

### 6.3 Services

**`ExcelReader` (static)** — cœur Excel :
- `Load(filePath, languages, progress)` : parse la feuille, ignore les `@Invariant`, renseigne `TranslationRow.RowNumber/Project/File/Key/FrenchComment/French` + dictionnaires `Translations[code]` et `Comments[code]` (la colonne de chaque langue vient de `LanguageInfo.Column`). Rapporte la progression via `IProgress<SourceLoadProgress>` (toutes les 10 lignes + bornes). Ne positionne pas la vue active : c'est `MainForm` qui appelle `SelectLanguage`.
- `Save(filePath, rows, languages)` : réécrit cellule par cellule les langues connues, la correspondance code → colonne étant reconstruite localement. `WriteCellValue` double une apostrophe de tête pour la préserver. La synchronisation de la vue active est faite en amont par `MainForm` (`CommitActiveLanguage`).
- `Merge(...)` / `GetMergeSourceDifferences(...)` prennent la `LanguageInfo` active : la colonne sert à écrire dans le classeur destination, le code à lire la traduction dans la ligne source.
- Corrélation des lignes via `SyncKey = Project|File|Key` (séparateur `\u001F`), détection des divergences français/commentaire, écriture sélective selon `MergeDifferenceResolution`.

**`ExcelService`** (implémente `IExcelService`) : façade DI **réduite à la fusion** (`Merge`, `GetMergeSourceDifferences`). Le chargement et la sauvegarde passent désormais par `ITranslationSource`.

**`ITranslationSource`** — abstraction des sources. Une instance est liée à un chemin et expose `Path`, `Kind` (« Excel » / « resx », affiché dans la status bar), `SupportsMerge`, `Load(languages, progress)` et `Save(rows, languages)`. Deux implémentations : `ExcelTranslationSource` (`SupportsMerge = true`) et `ResxTranslationSource` (`SupportsMerge = false`). `TranslationSourceFactory.Create(path)` choisit selon l'extension et expose `OpenFileFilter` pour l'`OpenFileDialog`.

**`SolutionReader` (static)** — projets d'une solution, aux deux formats : `.slnx` (XML, `<Project Path>` à tous les niveaux, y compris dans les `<Folder>`) et `.sln` (texte, regex sur les lignes `Project(...)`, les dossiers de solution étant écartés par le filtre d'extension). Les projets absents du disque sont ignorés ; deux projets d'un même répertoire ne sont scannés qu'une fois.

**`ResxReader` (static)** — lecture / écriture directe des `.resx` :
- `DiscoverFiles(solutionPath)` : énumère les `.resx` neutres et leur identité (Projet, Fichier). Ne lit aucun XML — partagé par le chargement et la sauvegarde pour garantir la même correspondance identité → chemin disque.
- `Load(...)` : construit les `TranslationRow` depuis le neutre + les variantes de culture, en appliquant les exclusions du §4.bis. Un `.resx` au XML invalide est ignoré (trace `Debug`) sans interrompre le chargement de la solution.
- **Asymétrie assumée sur les erreurs d'E/S** : au *chargement*, un `.resx` invalide ou inaccessible (verrouillé par Visual Studio, droits, chemin trop long) est ignoré avec une trace `Debug` — le reste de la solution se charge. À la *sauvegarde*, un fichier en échec n'est **jamais** ignoré en silence : `Save` écrit tout ce qu'il peut, collecte les échecs et lève une `IOException` les listant, que `MainForm` affiche. Ignorer silencieusement à l'écriture ferait croire à l'utilisateur que ses traductions sont enregistrées.
- `Save(...)` : **n'écrit que les variantes de culture, jamais le fichier neutre** (le français est en lecture seule dans l'UI). Écriture chirurgicale : chargement en `LoadOptions.PreserveWhitespace`, mise à jour ou insertion de l'entrée ciblée, fichier réécrit uniquement s'il a réellement changé (sauvegarde idempotente). Le BOM d'origine est préservé (lu sur le fichier d'origine, encore intact puisque l'écriture passe par un temporaire). Un fichier de langue absent est créé à partir de l'en-tête du neutre (déclaration, schéma XSD, `resheader`) vidé de ses données — jamais pour n'y écrire que du vide. L'écriture physique passe par `AtomicFile` : un crash en pleine écriture ne corrompt jamais le `.resx` existant.

**`AtomicFile` (static)** — écriture en deux temps, partagée par toutes les écritures disque de l'application (`ResxReader.Save`, `AppConfig.Save`, `GlossaryService.Save`) : le contenu est écrit dans un temporaire `<cible>.tmp` du même répertoire (même volume — condition pour que le basculement soit atomique), puis substitué à la cible par `File.Replace` (ACL et attributs de la cible préservés) ou `File.Move` quand la cible n'existe pas encore (création d'un fichier de langue). En cas d'échec, le temporaire est supprimé au mieux et l'exception d'origine remonte telle quelle — pour les `.resx`, `IOException` / `UnauthorizedAccessException` continuent donc d'être collectées fichier par fichier par `ResxReader.Save`. Pas de `.bak` systématique : « Backup auto » reste une entrée distincte de [ROADMAP.md](ROADMAP.md) ; le jour où elle sera traitée, `File.Replace` accepte un nom de backup, le point d'insertion est tout trouvé.

**`FormGeometryReader` (static)** — lit la géométrie des contrôles d'un formulaire WinForms depuis son `.resx` neutre (`X.Size`, `X.Location`, `X.Font`, `X.AutoSize`, `$this.ClientSize`), pour pouvoir confronter une traduction à la place réellement disponible. Socle de la vérification de débordement ; **aucune mesure ni UI à ce stade**.
- Ces entrées portent un attribut `type` : elles sont donc déjà exclues des lignes traduisibles par `ResxReader`. Les deux lectures sont complémentaires.
- **Elles n'existent que dans les `.resx`** : l'export Excel n'expose aucune clé de géométrie (vérifié sur le corpus de référence : 0 sur 22 374 lignes). La vérification de débordement est donc réservée au mode `.resx`.
- **Et seulement si le formulaire est en `Localizable = true`** — c'est ce mode qui fait sérialiser la géométrie par contrôle. Sinon `Read` renvoie une géométrie vide. *Sur le corpus de référence, tous les formulaires le sont* (confirmé par l'auteur) : la signature s'observe déjà dans l'export, où chaque contrôle porte `.AccessibleDescription`, `.AccessibleName` et `.ImageKey`.
- `FormGeometry.TryGetForKey("btnOk.Text")` retrouve le contrôle porteur d'une clé ; `GetEffectiveFont` applique l'héritage de la police du formulaire.
- Lit aussi la **filiation** via les métadonnées `>>X.Parent` : les coordonnées étant relatives au conteneur, `EnumerateSiblingGroups()` ne regroupe que des contrôles comparables.

**`Translator` (static)** — appels bruts aux providers :
- OpenAI via `OpenAI.Chat.ChatClient`.
- Anthropic via `AnthropicClient` (endpoint normalisé : retrait de `/v1` ou `/v1/messages`), `MaxTokens = 2048`.
- **Bifrost** (passerelle auto-hébergée) : aucun client dédié. La passerelle expose un chemin par dialecte, donc `BifrostOpenAI` réutilise le client OpenAI et `BifrostAnthropic` le client Anthropic — seule l'URL de base change. `ResolveApiKey` substitue un jeton neutre quand aucune clé n'est saisie : les SDK refusent une chaîne vide, alors qu'une instance Bifrost locale n'exige pas de clé.
- `TranslateBatchAsync(texts, config, lang, glossarySection)` et `VerifyBatchAsync(pairs, ...)` : construisent une liste numérotée, remplacent `{language}` et `{glossary}` dans le system prompt, demandent une réponse strictement numérotée.
- `TranslateInBatchesAsync` / `VerifyInBatchesAsync` : découpent en lots de `BatchSize = 20` (constante `internal`, réutilisée par `TranslationService.ChunkResults`), parallélisent avec `Task.WhenAll` + `SemaphoreSlim(FixedParallelBatchRequests = 4)`.
- Retry Polly (3 tentatives, backoff exponentiel + jitter), pipeline `static readonly RetryPipeline` construit une seule fois (stateless, partagé par tous les appels), sur : `HttpRequestException` transitoire (408/429/5xx + statut nul), `TimeoutException`, `TaskCanceledException`, `ClientResultException` (408/429/5xx).
- **Callback `onBatchCompleted(batchItems, batchResults)`** invoqué dans chaque tâche parallèle juste après réception des résultats du batch → permet à `TranslationService` de reporter la progression au fil de l'eau.
- `ParseNumberedList` (regex `^\s*(\d+)[.)]\s*(.+)$`) supporte les entrées multi-lignes.

**`TranslationService`** (implémente `ITranslationService`) :
- Deux caches distincts (`Dictionary<string,string>` + `lock`) : traduction, vérification.
- Clé traduction : `Provider|Url|Model|Language|GlossaryFingerprint|Texte`.
- Clé vérification : `Provider|Url|Model|Language|GlossaryFingerprint|French|Translation`.
- Déduplication intra-lot (plusieurs lignes avec le même français → une seule requête API).
- Utilise `Translator.*InBatchesAsync` avec le callback `onBatchCompleted` pour remplir le cache et reporter la progression au fil des batches (`Interlocked.Add` sur le compteur `completed`).
- API : `UpdateTranslationCache`, `UpdateVerificationCache`, `GetTranslationCacheCount`, `GetVerificationCacheCount`, `ClearTranslationCache`, `ClearVerificationCache`.
- **Deux niveaux de correspondance de clé** : les `Get*Count` matchent `Provider|Url|Model|Language|GlossaryFingerprint` (le fingerprint est un paramètre obligatoire) → les entrées devenues inatteignables après modification du glossaire ne sont plus comptées ; les `Clear*` matchent uniquement `Provider|Url|Model` → la purge emporte aussi les entrées périmées.

**`GlossaryService`** (implémente `IGlossaryService`) :
- Stockage **transversal** (`GlossaryTerm` : un terme français, ses traductions par langue, un statut Proposé / En contrôle / Validé, un commentaire réviseur — voir [GLOSSAIRE.md](GLOSSAIRE.md)) dans `%LocalAppData%\CheckTranslation\glossary.json`. L'ancien schéma par langue (v1) est migré au chargement (idempotent, fichier réécrit en v2 à la première sauvegarde) ; les termes migrés naissent Validé pour ne pas changer le comportement des prompts.
- `GetTerms()` / `ReplaceTerms(terms)` : la surface de l'éditeur multi-langues, terme entier (copies isolées, normalisation à l'écriture). `AddProposedTerms(code, entries)` : versement des candidats d'extraction — terme nouveau Proposé, case déjà tranchée jamais écrasée.
- `GetEntries(code)` / `ReplaceEntries(code, entries)` / `Save()` : la surface par langue est conservée par **projection** — l'éditeur actuel et l'extraction fonctionnent sans connaître le schéma transversal. Une écriture par l'éditeur vaut décision humaine : le terme passe Validé.
- `BuildGlossarySection(langueCode, langueName)` : produit la section texte injectée à la place du placeholder `{glossary}`. Chaîne vide si aucune entrée → aucun impact sur le prompt.
- `GetGlossaryFingerprint(langueCode)` : SHA256 des entrées triées, inclus dans les clés de cache → **invalidation automatique** dès qu'une entrée change.
- L'extraction IA réutilise `Translator.CallApiAsync` (pipeline Polly multi-providers) avec un prompt JSON strict ; filtrage des termes déjà connus avant proposition à l'utilisateur.

### 6.4 Logic

**`QualityScore`** (static) — format attendu `XXX - commentaire` (3 chiffres, 0..100). Regex `^\s*(\d{3})\s*[-–]\s*`. `GetBackColor(score)` interpole linéairement une palette pastel (rouge < 60 → orange 70 → jaune 80 → vert 90–100).

**`LayoutAnalyzer`** (static) — confronte les textes affichés à la place disponible. **Deux défaillances distinctes, qui s'excluent** :
- **Troncature** — contrôle à largeur fixe dont le texte dépasse : le texte est coupé, le contrôle ne bouge pas.
- **Collision** — contrôle en `AutoSize` : il n'est *jamais* tronqué, il s'élargit et recouvre un voisin. Ce sont précisément les contrôles sans largeur sérialisée, d'où l'insuffisance d'un simple test « le texte tient-il ».

Seules les paires de **frères** sont comparées, et une collision n'est retenue que si au moins un des deux contrôles s'élargit : deux contrôles fixes qui se chevauchent sont un défaut de maquette, pas de traduction.

`AnalyzeRegression(source, traduction)` ne retient que les défauts **introduits** par la traduction — un défaut déjà présent en français n'est pas imputable au traducteur et serait signalé pour chaque langue.

**Tout est raisonné en rapport, jamais en valeur absolue.** Les coordonnées d'un `.resx` sont figées à la résolution du poste qui a dessiné le formulaire — sur le corpus de référence un `Label` fait 32 px de haut là où 96 ppp en donnerait 15. Confronter une mesure prise à l'exécution à ces coordonnées mélangerait deux repères et sous-détecterait d'un facteur voisin de deux. Il n'existe pas non plus de marge interne universelle : elle dépend du type de contrôle.

L'étalon est donc pris dans le fichier lui-même :

| Contrôle | Étalon | Largeur attendue |
|---|---|---|
| `AutoSize` | sa `Size` sérialisée **est** la largeur rendue du texte français | `Size.Width × mesure(traduction) / mesure(français)` |
| taille fixe | échelle du formulaire = médiane des `Size.Width / mesure(français)` de ses `AutoSize` | `échelle × mesure(traduction)`, comparée à `Size.Width` |

L'échelle et la marge interne s'annulent dans le rapport : **aucune constante à calibrer, aucune hypothèse sur le DPI**.

Un garde-fou protège l'étalon lui-même : la `Size` d'un contrôle `AutoSize` ne vaut que si elle correspond encore à son texte français. Un libellé modifié directement dans le `.resx`, sans rouvrir le concepteur, laisse une taille périmée. Un contrôle dont l'échelle s'écarte de plus d'un facteur 2 de celle de son formulaire est donc déclaré non vérifiable. La tolérance est volontairement large : la marge interne varie d'un type de contrôle à l'autre (la case d'un `CheckBox` compte dans sa largeur, pas dans son texte). Un formulaire à contrôle unique n'a aucun repère : son étalon est accepté. Un formulaire sans contrôle `AutoSize` exploitable se rabat sur la médiane des échelles des autres formulaires de la solution (`LayoutCheckService`) — la résolution de conception est une propriété du poste, pas du formulaire — et reste non vérifiable si la solution entière n'en fournit aucune.

La dimension verticale suit le même raisonnement, par **retours à la ligne explicites** : la hauteur sérialisée d'un contrôle `AutoSize` correspond aux lignes de son texte source et varie dans le rapport des nombres de lignes (ce qui rend visibles les collisions verticales) ; un contrôle fixe est confronté à la hauteur d'étalon de sa police, médiane des hauteurs par ligne des `AutoSize` du formulaire, police par police (`ComputeLineHeightsByFont`). Une police sans `AutoSize` pour l'étalonner fait passer le test vertical plutôt que le fausser avec l'étalon d'une autre. Le repli automatique (wrap) n'est **pas** prédit : une ligne trop longue qui se replie à l'exécution ne compte que pour une.

La mesure de largeur est **injectée** (`TextWidthMeasurer`) : en production c'est `GdiTextWidthMeasurer`, donc Windows ; l'injection rend toute l'analyse éprouvable hors WinForms avec une mesure déterministe. Son unité n'a pas d'importance, seule compte sa cohérence d'un appel à l'autre.

**`GdiTextWidthMeasurer`** (`IDisposable`) — implémentation de production, via `TextRenderer.MeasureText`. Trois pièges traités, chacun capable de produire des faux positifs silencieux :
- **DPI** : la mesure passe par une surface mémoire à 96 ppp, résolution des coordonnées de conception. Sur le contexte de l'écran, un affichage à 150 % gonflerait toutes les largeurs d'un tiers et ferait paraître tout débordé.
- **Handles GDI** : les `Font` sont mises en cache par (famille, taille, gras) et libérées avec l'instance — en créer une par appel épuiserait les handles sur des dizaines de milliers de lignes × 7 langues.
- **Multi-lignes** : une valeur de ressource peut contenir des retours à la ligne ; la largeur retenue est celle de la ligne la plus large.

Le repère de mesure n'a pas à correspondre à celui du fichier : l'analyse ne compare que des rapports, l'échelle s'y annule. C'est ce qui a permis de supprimer le paramètre `extraPadding`, qui aurait dû être calibré à la main.

**`LayoutCheckService`** (implémente `ILayoutCheckService`) — orchestre la vérification sur une solution : regroupe les lignes par fichier, lit la géométrie de chaque formulaire, compare la traduction au français et **retourne** des `LayoutVerdict`.
- **Toutes les langues en une seule passe** (`Analyze(..., IReadOnlyList<string> languageCodes, ...)`). Le coût dominant — `ResxReader.DiscoverFiles`, la lecture XML de chaque formulaire, le calcul de l'échelle à partir du français — ne dépend pas de la langue : le refaire par langue serait du gâchis pur. Mesuré sur une solution synthétique de 474 formulaires × 25 contrôles : ×1,8 pour passer d'une langue à sept en une passe, contre ×4,1 en relançant une passe par langue.
- Il ne modifie **aucune** ligne : les `TranslationRow` sont liés au `DataGridView`, les écrire depuis le thread de fond où tourne l'analyse les exposerait à une lecture concurrente par le rendu. `MainForm` applique les verdicts sur le thread d'interface.
- Ne traite que les clés `contrôle.Text` : un message, un `ToolTipText` ou un `AccessibleName` n'occupent aucune surface fixe.
- Une ligne **sans traduction** reste `NotChecked` — la déclarer conforme serait faux.
- Un formulaire **non localisable** (aucune géométrie) laisse toutes ses lignes `NotChecked` : on ne conclut pas faute de données.
- Une collision marque **les deux** lignes en cause, corriger l'une ou l'autre résolvant le problème ; une troncature déjà signalée n'est pas masquée par une collision.

**`TranslationStatistics`** (static) — calcule l'état d'avancement, sans aucune dépendance à l'interface : c'est ce qui le rend éprouvable hors WinForms.
- `Compute(rows, languages)` → `TranslationOverview` : lignes, projets, fichiers, un `LanguageStatistics` par langue, et un `LayoutStatistics` **par langue analysée** (`Layouts`, vide si rien n'a été analysé). `TotalLayoutIssues` est nul — et non zéro — dans ce cas : zéro se lirait « aucun défaut ».
- Par langue : traduites, non traduites, **identiques au français**, vérifiées, score moyen, distribution par tranche. Vérifications, moyennes et tranches ne se comptent qu'**à l'intérieur des lignes traduites** : un score resté sur une ligne vidée est périmé.
- `ComputeGroups(rows, langue, GroupBy.Project | GroupBy.File)` : mêmes indicateurs par projet ou par fichier, **triés du moins avancé au plus avancé**.
- `ScoreBuckets()` est l'**unique source de vérité** des tranches : le tableau de bord en fait ses colonnes, la liste déroulante de la colonne Commentaire en fait ses entrées.
- Deux choix de lecture : la moyenne est nulle et non zéro quand rien n'est vérifié (zéro se lirait « toutes mauvaises ») ; la part vérifiée est rapportée aux lignes **traduites** et non au total, sinon l'indicateur plafonnerait sans que personne n'y puisse rien.

**`TranslationRowFiltering`** (static) — `Filter(allRows, filters)` sur toutes les colonnes. Un filtre texte est un « contient », sauf s'il commence par `=` : égalité exacte (utilisé par le tableau de bord). Pour `Translation` : `translation:none`, `translation:done`, `translation:same` (identique au français — un signal, pas une erreur). Pour `Comment`, supporte les pseudo-filtres :
- `score:none` → lignes sans score parsable
- `score:60-69` → tranche fermée (partagée avec le tableau de bord)
- `score<N` → lignes sans score OU score ≤ N (inclusif)
- `score>=N` → lignes avec score ≥ N

**`GlossaryDiff`** (static) — cœur pur de l'import de glossaire : `Compute(current, imported, languages)` produit les différences terme par terme et champ par champ (suppressions refusées par défaut), `Apply(current, imported, changes, reviewedAllLanguages)` applique les changements acceptés et gère les statuts — un changement de fond accepté valide le terme, un terme En contrôle revenu strictement inchangé n'est validé que si le classeur couvrait toutes les langues. Un doublon de source dans le glossaire courant est refusé (état ambigu).

**`GlossaryImpact`** (static) — cœur pur de la retraduction ciblée : `ComputeChangedTerms(before, after)` compare deux photographies de la projection prompts (par langue) et retient les termes apparus ou modifiés — jamais les supprimés, décision assumée de GLOSSAIRE.md ; `SelectImpactedRows(rows, terms)` sélectionne les lignes dont le français contient un terme, en inclusion insensible à la casse (les formes fléchies éloignées peuvent échapper, limite documentée).

### 6.5 Modèles

**`TranslationRow`** (POCO) :
- Identité : `RowNumber` (ligne Excel), `Project`, `File`, `Key`.
- Source : `French`, `FrenchComment`.
- Vue langue active : `Translation`, `Comment` (modifiées directement par l'UI).
- Multi-langues : `Translations`, `Comments` et les **verdicts de mise en page**, tous indexés par code de langue. Un verdict décrit une traduction précise, donc une langue précise : le stocker par langue est ce qui permet d'analyser les sept en une passe et de basculer sans rien recalculer. `SelectLayoutVerdict(code)` charge la vue active (`LayoutStatus` / `LayoutIssue`), `GetLayoutStatus(code)` lit celle d'une autre langue, `InvalidateLayoutVerdict(code)` périme la seule langue éditée.
- `SwitchLanguage(oldCol, newCol)` : pousse la vue active dans le dictionnaire sous `oldCol`, recharge depuis `newCol`. Évite le rechargement Excel à chaque switch de langue.
- `GetSyncKey()` : `Project|File|Key` avec séparateur `\u001F`.

**Fusion** :
- `MergeRowSnapshot` : record immuable `(Project, File, Key, French, FrenchComment, Translation, TranslationComment)`.
- `MergeDifference(SyncKey, Source, Destination)` : paire affichée dans le dialog.
- `MergeDifferenceResolution(UpdateFrenchAndComment, UpdateTranslationAndComment)` : décision utilisateur (bool + bool). Propriété dérivée `HasAnyChange`.

**Glossaire** :
- `Glossary` : conteneur v2 — liste de `GlossaryTerm` + champ legacy `EntriesByLanguage` relu pour migration, jamais réécrit.
- `GlossaryTerm` : terme transversal (Source FR canonique, Contexte commun, Statut, Commentaire réviseur, `Translations` par code de langue).
- `GlossaryTermStatus` : Proposé / En contrôle / Validé — seuls les Validé sont injectés dans les prompts.
- `GlossaryEntry(Source, Destination, Context)` : triplet de projection d'un terme sur une langue — la forme sous laquelle l'éditeur, l'extraction, les prompts et l'empreinte voient le glossaire. La source reste implicitement le français.

### 6.6 Configuration persistante (`AppConfig`)

Fichier : `%LocalAppData%\CheckTranslation\CheckTranslation.config.json` (lecture de compatibilité sur `AppContext.BaseDirectory` si le nouveau emplacement est absent).

Contenu persisté :
- Prompts (`TranslatePrompt`, `VerifyPrompt`)
- Fournisseur sélectionné (`Provider`) et paramètres par provider (`OpenAiKey/Url/ModelName`, `AnthropicKey/Url/ModelName`, `BifrostOpenAiKey/Url/ModelName`, `BifrostAnthropicKey/Url/ModelName`)
- `SelectedLanguageCode`, `WindowWidth/Height`
- `ColumnFillWeights` : largeurs de colonnes (compat : l'ancien jeu `ColumnFillWeightsWithDetails` est lu en repli ; `ShowDetails` et le jeu « sans détails » ne sont plus persistés)
- `ShowSolutionTree` / `SolutionTreeWidth` : visibilité et largeur du panneau d'arborescence

**Sécurité** : clés API chiffrées via DPAPI (`ProtectedData.Protect`, `DataProtectionScope.CurrentUser`). En cas d'échec de déchiffrement (autre utilisateur, format invalide), retourne la valeur telle quelle plutôt que de planter.

**Compat legacy** : les champs historiques `Key`/`Url`/`ModelName` (OpenAI uniquement) sont toujours reconnus au chargement. Une configuration antérieure à l'ajout de Bifrost se charge sans perte : les champs absents prennent les valeurs par défaut de la passerelle, et un nom de fournisseur inconnu retombe sur `OpenAI`.

### 6.6.bis Fournisseurs IA

Quatre entrées, chacune avec sa clé, son URL et son modèle persistés séparément — basculer de l'une à l'autre ne demande aucune ressaisie :

| `AiProvider` | Dialecte / client | URL par défaut | Modèle par défaut |
|---|---|---|---|
| `OpenAI` | OpenAI, direct | `https://api.openai.com/v1` | `gpt-5.2` |
| `Anthropic` | Anthropic, direct | `https://api.anthropic.com` | `claude-sonnet-4-6` |
| `BifrostOpenAI` | OpenAI, via passerelle | `http://localhost:8080/openai/v1` | `openai/gpt-5.2` |
| `BifrostAnthropic` | Anthropic, via passerelle | `http://localhost:8080/anthropic` | `anthropic/claude-sonnet-4-6` |

Deux prédicats portent cette classification et évitent d'éparpiller les `switch` : `AppConfig.IsBifrost(provider)` (clé facultative) et `AppConfig.UsesAnthropicDialect(provider)` (choix du SDK). En mode Bifrost, le nom de modèle est **préfixé par le fournisseur amont** (`openai/…`, `anthropic/…`), convention propre à la passerelle.

---

## 7. Flux fonctionnels

### 7.1 Démarrage
1. `AppConfig.Load()` alimente `AppConfig.Current`.
2. `Program.Main` configure la DI et lance `MainForm`.
3. `MainForm` restaure la taille de la fenêtre et les largeurs de colonnes depuis `AppConfig`.

### 7.2 Ouverture d'une source
1. Dialog `OpenFileDialog` (filtre fourni par `TranslationSourceFactory.OpenFileFilter` : `.xlsx`, `.sln`, `.slnx`) → `_currentFilePath`.
2. `TranslationSourceFactory.Create(path)` construit `_currentSource` selon l'extension.
3. `source.Load(Languages, progress)` charge les lignes (progress bar alimentée par `SourceLoadProgress(Done, Total)` — en lignes lues pour l'Excel, en fichiers neutres traités pour le .resx).
4. Les entrées `@Invariant` sont ignorées (les deux sources).
5. `row.SelectLanguage(code)` positionne la vue active : les sources remplissent les dictionnaires par code de langue mais ignorent la langue affichée.
6. Les filtres de l'UI sont réinitialisés, `DataSource` assigné, et `btnMerge` activé **seulement si** `source.SupportsMerge`.

### 7.3 Affichage / édition
- `DataGridView` avec colonnes :
  - Clé (insérée par code, toujours visible) — Projet et Fichier n'ont plus de colonne, le panneau d'arborescence porte cette information
  - Français (Designer, lecture seule)
  - Traduction (Designer, éditable)
  - Commentaire (inséré par code, lecture seule)
- `CellFormatting` colorie Traduction + Commentaire selon `QualityScore.TryParse(row.Comment)`.
- `CellEndEdit` sur Traduction → `ITranslationService.UpdateTranslationCache(...)`.

### 7.4 Changement de langue
- Clic sur un drapeau de la toolbar.
- Chaque `TranslationRow.SwitchLanguage(oldCol, newCol)` bascule la vue active.
- Filtres réinitialisés, status bar mise à jour, `AppConfig.SelectedLanguageCode` persisté.

### 7.5 Filtrage
- TextBox superposé à l'en-tête (toutes colonnes sauf Commentaire) + debounce 300 ms.
- ComboBox superposé à l'en-tête Commentaire (filtres par score).
- **Arborescence de la solution** (panneau gauche, style ResX Resource Manager) : cases à cocher Projet → Fichier. Décocher restreint l'affichage ; cocher/décocher un projet propage à ses fichiers, l'état du projet reflète celui de ses fichiers. Même debounce que les filtres texte.
- **Synchronisation de sélection grille ↔ arbre** : sélectionner des lignes dans la grille sélectionne et fait défiler le fichier de la ligne courante dans l'arbre (mono-sélection : l'ancre décide) ; sélectionner un nœud de l'arbre sélectionne dans la grille les lignes affichées du fichier, ou toutes celles du projet, et défile jusqu'à la première. Verrou anti-boucle `_isSyncingSelection` ; un fichier masqué par le bandeau n'est pas synchronisé (le filtre reste maître).
- **Bandeau de l'arborescence** : case maîtresse « tout cocher / décocher » à trois états et zone de filtre du panneau. La case est asymétrique, à dessein : cocher fait de la sélection exactement les éléments visibles (ce que le filtre masque est décoché, même s'il l'était) ; décocher vide tout, visible ou non. Filtrer « Catalog » puis cocher réduit donc la grille aux fichiers Catalog, rien d'autre. Le filtre du bandeau ne restreint que l'arbre, jamais la grille. Les états cochés vivent dans `_uncheckedFiles`, pas dans les nœuds : filtrer ne perd pas les décochages faits case par case.
- `TranslationRowFiltering.Filter(lignes visibles selon l'arbre, _filters)` recalcule le snapshot affiché.
- `ApplyFiltersPreservingSelection()` conserve la sélection et le scroll lors des ré-applications.

### 7.6 Tri
- Clic sur l'en-tête d'une colonne → tri asc/desc (`dataGridView.Sort`).
- Indicateur (triangle) dessiné à droite du titre dans `CellPainting`.

### 7.7 Traduire / vérifier (IA)
Menu contextuel sur `colTranslation` :
- **Auto-traduire (copie existante)** : recopie des traductions connues pour le même français — sans appel IA.
- **Traduire** : via IA (batch).
- **Vérifier la traduction** : via IA (batch).
- **Extraire les termes métier…** : déclenche l'extraction IA et ouvre `GlossaryExtractionDialog`.

En multi-sélection : mêmes actions sur toute la sélection. Progress bar alimentée via `IProgress<int>`; `UseWaitCursor` pendant l'appel. Avertissement si des réponses IA n'ont pas pu être extraites.

Pendant tout l'appel, **toolbar et grille sont gelées** (même mécanisme que l'analyse de mise en page) : les résultats du batch s'écrivent dans la vue active des lignes sélectionnées, et changer de langue, fusionner ou rafraîchir pendant l'attente corromprait les données (voir §10). F5 est bloqué par `CanRefreshView`, qui vérifie `toolStrip.Enabled`.

### 7.8 Rafraîchir (bouton ou F5)
- **Fichier chargé** : `ExcelService.LoadWithRowProgress(...)` recharge depuis le disque. Détection des lignes dont le français/commentaire source a changé → confirmation utilisateur avant d'écraser les valeurs en mémoire. Les traductions des lignes inchangées sont conservées.
- **Pas de fichier / après édition** : `ApplyFiltersPreservingSelection()` réapplique filtres + tri.
- L'indicateur `Rafraîchir *` (étoile) s'affiche après une édition/traduction/vérification pour signaler qu'une ré-application filtres/tri est utile.

### 7.9 Sauvegarde
- `MainForm` pousse d'abord la vue active dans les dictionnaires (`row.CommitActiveLanguage(code)`), puis appelle `_currentSource.Save(rows, Languages)`. Sans ce commit, la langue affichée ne serait pas sauvegardée : elle ne vit que dans `Translation` / `Comment`.
- Source Excel : réécriture des cellules de toutes les langues connues. Les commentaires de vérification (format score) sont aussi persistés dans les colonnes Excel correspondantes.
- Source .resx : réécriture des seules variantes de culture réellement modifiées ; le fichier neutre n'est jamais touché.
- Toute la durée de l'opération est encadrée par `SetWritingState(true/false)` (bloc `finally`) : toolbar et grille désactivées, fermeture de la fenêtre refusée, status bar « Sauvegarde en cours… ».

### 7.10 Fusion (source Excel uniquement)
0. `SupportsMerge` vaut `false` pour la source .resx : le bouton est désactivé et `BtnMerge_Click` sort immédiatement.
1. Dialog → `_destinationFilePath`, puis `SetWritingState(true)` — le verrou couvre **toute** la fusion (analyse des différences, dialogs de résolution, écriture disque) et est levé dans le `finally`. Les `MergeDifferenceForm` restent utilisables : ce sont des dialogs modaux, la désactivation du parent ne les affecte pas.
2. `ExcelService.GetMergeSourceDifferences(...)` détecte toutes les lignes où le français ou le commentaire source diffère entre source et destination.
3. Pour chaque différence, `MergeDifferenceForm` s'ouvre. L'utilisateur choisit les champs à reporter (ou annule la fusion globalement).
4. `ExcelService.Merge(...)` applique les résolutions. Les lignes sans divergence source → report systématique de la traduction. Retour : nombre de lignes mises à jour.

### 7.10.bis Vérification de mise en page (source .resx uniquement)
1. `ITranslationSource.SupportsLayoutCheck` vaut `false` pour l'Excel : l'export ne contient aucune géométrie, l'analyse est simplement sautée.
2. **L'analyse est lancée au chargement de la solution, pour toutes les langues à la fois.** `MainForm.RunLayoutCheckAsync()` pousse d'abord la vue active dans les dictionnaires, puis instancie un `GdiTextWidthMeasurer` (dans un `using`) et appelle `LayoutCheckService.Analyze(source.Path, rows, tous les codes, …)` dans un `Task.Run`. Les verdicts retournés ne sont appliqués qu'au retour, sur le thread d'interface, et seulement si la source n'a pas changé entre-temps.
3. Chaque ligne reçoit un `LayoutStatus` **par langue** et un libellé ; la colonne « Mise en page » affiche celui de la langue courante : rouge pour une troncature, orange pour une collision, gris pour un cas non vérifiable.
4. Le ComboBox de l'en-tête filtre sur ces états (`layout:issues`, `layout:truncation`, `layout:collision`, `layout:unverifiable`, `layout:ok`).
5. **Changer de langue ne relance rien** : `SelectLanguage` rebascule sur les verdicts déjà calculés de la nouvelle langue. Éditer une traduction périme le verdict de cette ligne pour cette seule langue (`InvalidateLayoutVerdict`) ; rafraîchir (F5) reconstruit les lignes depuis le disque et rejoue donc l'analyse.
6. Un échec est tracé **et signalé dans la status bar** : l'analyse reste un confort et ne doit pas faire échouer le chargement, mais une colonne vide sans explication ne dit rien.

### 7.11.bis Tableau de bord
1. Bouton toolbar → `MainForm` pousse d'abord la vue active dans les dictionnaires (`CommitActiveLanguage`), sans quoi les éditions en cours manqueraient au décompte.
2. `DashboardForm` calcule via `TranslationStatistics` et affiche : bandeau de cartes, onglets *Par langue* / *Par projet* / *Par fichier* / *Mise en page*.
3. Un clic sur un chiffre souligné (ou un double-clic sur une ligne de projet / fichier) ferme la fenêtre en renvoyant un `DashboardDrillDown` (langue, colonne, valeur de filtre).
4. `MainForm.ApplyDrillDown` bascule sur la langue, **remet tous les filtres à zéro**, puis pose le filtre demandé : dans la zone de saisie ou la liste déroulante de la colonne concernée, sauf pour Projet et Fichier qui n'ont plus de colonne — leur filtre passe par l'arborescence, en sélection exacte (`SelectTreeExactly`), avec la même garantie que l'ancien filtre « = » : le chiffre cliqué et la grille comptent la même chose.

### 7.11 Glossaire
- **Édition manuelle** : bouton toolbar `btnGlossary` → `GlossaryForm`, grille transversale terme × langue avec statuts. Seuls les termes Validé sont injectés dans les prompts.
- **Extraction assistée** : menu contextuel "Extraire les termes métier…" sur une sélection → IA propose des candidats → `GlossaryExtractionDialog` → acceptation → versement en termes **Proposé** (`AddProposedTerms`), à valider dans l'éditeur ou par le contrôle externe avant d'entrer dans les prompts.
- **Injection dans les prompts** : à chaque appel de `TranslateInBatchesAsync` / `VerifyInBatchesAsync`, `MainForm` construit `glossarySection` (texte) et `glossaryFingerprint` (SHA256) pour la langue active. Le fingerprint invalide automatiquement les entrées de cache périmées.
- **Export / import Excel (contrôle externe)** : boutons de `GlossaryForm` → `GlossaryExcel` (classeur Glossaire + Infos avec empreinte d'export), diff par terme et par champ (`GlossaryDiff.Compute`) présenté dans `GlossaryImportDiffForm` (acceptation individuelle, suppressions décochées), backup daté (`CreateBackup`) puis application transactionnelle (`ReplaceTermsAndSave`). Les statuts suivent GLOSSAIRE.md : Proposé → En contrôle à l'export, changements acceptés → Validé, terme revenu strictement inchangé → Validé seulement si le classeur couvre toutes les langues.
- **Retraduction ciblée (phase 4)** : `BtnGlossary_Click` photographie la projection prompts (`GetPromptEntries`, termes Validé par langue) avant et après l'éditeur ; `GlossaryImpact.ComputeChangedTerms` en déduit les termes apparus ou modifiés (les supprimés n'impactent pas), `SelectImpactedRows` sélectionne les lignes par inclusion insensible à la casse du français. Après confirmation (compte par langue), `RetranslateImpactedAsync` retraduit puis re-vérifie langue par langue en écrivant dans les dictionnaires par code, invalide les verdicts de mise en page des traductions changées, et recharge la vue active à la fin.

---

## 8. Détails IA (Translator + TranslationService + Glossaire)

### 8.1 Paramètres globaux
- `BatchSize = 20`
- `Temperature = 0.1f` — envoyée seulement aux modèles qui l'acceptent : au premier refus (« temperature is deprecated », Claude Sonnet 5 et au-delà), le couple fournisseur-modèle est mémorisé (`TemperatureRejectedByModel`) et l'appel rejoué sans température
- `FixedParallelBatchRequests = 4`
- `RetryCount = 3`
- `AnthropicMaxTokens = 2048`

### 8.2 Progression temps réel
Historiquement, `TranslationService` passait `null` en progress à `Translator` et ne reportait qu'après `Task.WhenAll` — la progress bar sautait de 0 à 100 % à la fin. Depuis la PR #7 :
- `Translator.ProcessBatchesAsync` accepte un callback `Action<IReadOnlyList<T>, string[]>? onBatchCompleted` invoqué dans chaque tâche parallèle juste après réception des résultats d'un batch.
- `TranslationService` passe un callback qui : remplit le cache, mappe les résultats aux indices originaux (via `pendingByText` / `pendingByPair`), incrémente `completed` via `Interlocked.Add`, reporte la progression.
- Résultat UX : la status bar s'incrémente par paliers de ~20 au rythme des retours API (toutes les 3–10 s).

### 8.3 Glossaire dans les prompts
Les prompts par défaut contiennent un placeholder `{glossary}`. Juste avant l'appel API :
```csharp
systemPrompt = systemPrompt
    .Replace("{language}", targetLanguage)
    .Replace("{glossary}", glossarySection ?? string.Empty);
```
Le `glossarySection` est construit par `GlossaryService.BuildGlossarySection(...)` sous forme :
```
Glossaire à respecter (Source → Destination) :
- "ABC" → "XYZ" (contexte : …)
- …
```
(chaîne vide si aucune entrée → le placeholder disparaît du prompt).

### 8.4 Invalidation de cache via fingerprint
La clé de cache inclut `GlossaryFingerprint` = SHA256 hex des entrées triées `(Source|Destination|Context)`. Dès qu'une entrée est ajoutée, modifiée ou supprimée, le fingerprint change → les anciennes clés ne matchent plus → les lignes concernées seront retraduites à la prochaine demande.

---

## 9. Conventions de code

- **Namespace unique** : `CheckTranslation` pour tout le projet, indépendamment des sous-dossiers.
- **Nullable enable** + annotations `?` explicites.
- **Implicit usings** enable.
- **File-scoped namespaces** (`namespace CheckTranslation;`).
- **Modificateurs d'accès** : `internal` pour les classes d'assembly (`TranslationRow`, `AppConfig`, services, modèles de fusion…). `public` uniquement pour les classes héritant de `Form`.
- **Records** pour les modèles immuables : `MergeDifference`, `MergeRowSnapshot`, `MergeDifferenceResolution`, `LanguageInfo`, `ExcelLoadProgress`.
- **Sections commentées** : `// --- Nom de section ---` pour structurer les gros fichiers (ex. `MainForm.cs`).
- **Nommage** :
  - UI (champs de contrôles) : **sans** underscore, ex. `btnOk`, `grid`, `languageCombo`.
  - État non-UI (champs privés) : **avec** underscore, ex. `_glossaryService`, `_dirty`, `_allRows`.
- **PascalCase** pour membres publics, **camelCase** pour variables locales et paramètres.

---

## 10. Avertissements importants

- **Ne PAS modifier `*.Designer.cs` à la main** — générés par le designer WinForms. Toute modif manuelle doit être reproductible par le designer, sinon elle sera écrasée.
- **Ne PAS modifier les `*.resx` manuellement** — générés par le designer.
- **Les `.resx` ont des `LogicalName` explicites dans le `.csproj`** — nécessaire car ils sont dans un sous-dossier `Forms/`. Ne pas supprimer ces entrées (`MainForm`, `ConfigForm`, `GlossaryForm`, `GlossaryExtractionDialog`).
- **Les colonnes Clé, Commentaire et Mise en page sont créées programmatiquement** dans `MainForm` (`InitKeyColumn`, `InitCommentColumn`, `InitLayoutColumn`) et insérées autour des colonnes du Designer. Ne pas les ajouter dans `MainForm.Designer.cs`. Projet et Fichier n'ont plus de colonne : leur filtrage passe par l'arborescence.
- **Le bouton `Rafraîchir` est ajouté programmatiquement** (`InitRefreshButton` dans la section « Bouton Rafraîchir + F5 ») puis disposé par `ArrangeToolStripItems`.
- **Les ComboBox de filtre par score sont créés par code** (`TryCreateSpecialFilterControl` dans la section « Filtre par score de vérification ») et superposés aux en-têtes du DataGridView.
- **Fusion Excel** : résolution ligne-par-ligne via `MergeDifferenceForm` pour CHAQUE ligne divergente. L'utilisateur peut annuler la fusion globalement.
- **Écritures disque protégées par `SetWritingState`** — ne PAS revenir à un `btnXxx.Enabled = false` ponctuel dans `BtnSave_Click` / `BtnMerge_Click` : le verrou global est ce qui bloque aussi la fermeture (`_isWriting` lu par `MainForm_FormClosing`). Toujours appeler `SetWritingState(false)` dans un `finally`.
- **Les batchs IA gèlent toolbar + grille** — `TranslateRowsAsync` / `VerifyRowsAsync` / `ExtractTermsRowsAsync` désactivent `toolStrip` et `dataGridView`, pas seulement Ouvrir/Sauver. Les résultats du batch atterrissent dans la vue active des lignes : changer de langue committerait le placeholder « Traduction en cours... » dans la langue quittée et ferait écrire les résultats dans la nouvelle ; fusionner reporterait les placeholders dans l'Excel destination ; rafraîchir remplacerait `_allRows` et les résultats iraient dans des objets orphelins. Le `finally` ne rouvre pas l'UI si `_isWriting` est posé (même précaution que `RunLayoutCheckAsync`).
- **F5 se bloque dans `CanRefreshView`, pas dans la toolbar** — le raccourci passe par `ProcessCmdKey`, au niveau de la fenêtre : désactiver `toolStrip` ne l'arrête pas. `CanRefreshView` vérifie `toolStrip.Enabled` et couvre ainsi tous les gels (batch IA, écriture disque, analyse de mise en page, chargement, rafraîchissement). Ne pas retirer cette condition : le bouton Rafraîchir grisé ne protégerait plus rien au clavier, et F5 ré-entrerait dans `BtnRefresh_Click` pendant un rechargement.
- **Le chargement et le rafraîchissement gèlent aussi toolbar + grille** — `LoadFileAsync` et `BtnRefresh_Click` désactivent `toolStrip` et `dataGridView` comme les batchs IA : pendant un rechargement, changer de langue mélangerait les vues actives entre anciennes et nouvelles lignes, et fusionner lirait des données en cours de remplacement. Les états individuels de Sauver / Fusionner restent gérés séparément : ils décrivent la source et survivent au dégel du container.
- **Métriques des filtres calculées au runtime** — pas de constantes en pixels : `InitFilterPanel` mesure police et `TextBox.PreferredHeight`, `UpdateFilterPanelLayout` / `DataGridView_CellPainting` / `TryLayoutSpecialFilterControl` doivent rester synchronisés sur ces champs, sinon les filtres se désalignent en DPI 125/150/200 %.
- **L'aperçu Markdown des prompts retire l'extension *generic attributes* de Markdig** — `UseAdvancedExtensions()` l'embarque, et elle lit `{...}` comme un bloc d'attributs HTML : `{language}` et `{glossary}` disparaissaient de l'aperçu sans erreur, à l'endroit précis où l'utilisateur vérifie qu'ils sont posés. Ne pas revenir à un `UseAdvancedExtensions()` nu.
- **Les champs de fournisseur sont réétirés au chargement** (`StretchProviderFields`) — ancrés gauche+droite dans un `SplitContainer`, ils figent leur distance d'ancrage avant que `EndInit` n'applique le `SplitterDistance`, et s'affichent bien plus étroits que ce que le Designer indique. Élargir les champs dans le Designer ne corrigerait rien.
- **Un panneau docké reçoit sa `Size` complète dans le Designer, jamais `Height` seul** — un contrôle ancré à droite fige sa distance d'ancrage sur la largeur *courante* du panneau ; avec `Height` seul, cette largeur est celle par défaut (200 px) et les boutons Enregistrer / Annuler partent hors champ dès que le dock étire le panneau (cas vécu : `GlossaryForm`, `GlossaryImportDiffForm`, `GlossaryExtractionDialog` — boutons mesurés à x=1995 dans un panneau de 1138 px). Écrire `bottomPanel.Size = new Size(<largeur du ClientSize de conception>, 48)`, comme le fait le vrai Designer (`DashboardForm` est le modèle sain).
- **Les dialogs se bornent à l'écran via `ScreenFit.Apply(this)` dans `OnLoad`** — helper partagé (`Forms/ScreenFit.cs`), utilisé par `ConfigForm`, `GlossaryForm`, `GlossaryImportDiffForm` et `GlossaryExtractionDialog`. `ConfigForm` dépasse un écran 1080p dès 125 % de mise à l'échelle : sans la borne, OK / Annuler sortent du champ. La `MinimumSize` est bornée **avant** la fenêtre (sinon elle interdit le rétrécissement) et l'écran de référence est celui du `Owner`. Tout nouveau dialog plus grand que ~1000 px doit l'appeler.
- **`ConfigForm.BuildCurrentConfig()` reconstruit un `AppConfig` complet** — tout nouveau champ persistant non édité dans le dialog doit y être recopié depuis `AppConfig.Current`, faute de quoi un clic sur OK le réinitialise (cas vécu avec `WindowWidth/Height` et les `ColumnFillWeights`).
- **La géométrie se lit dans `<data>` *et* dans `<metadata>`** — le designer écrit dans l'un ou l'autre selon les cas. N'en lire qu'un ferait perdre la filiation `>>X.Parent` sans erreur, et un contrôle sans parent est écarté des groupes de frères : il ne collisionnerait plus jamais, en silence.
- **Les `.resx` et les `.slnx` sont lus par nom local, jamais par nom qualifié** — un fichier déclarant un espace de noms ne ramènerait alors plus rien, *sans erreur* : il paraîtrait simplement vide. `ResxReader.Children` / `Attribute` et `SolutionReader.ReadSlnxProjectPaths` portent cette règle.
- **Un chargement .resx qui ne ramène rien doit se dire** — `ResxLoadReport.Describe()` nomme l'étape où la chaîne s'est arrêtée (aucun projet reconnu, aucun `.resx`, toutes les entrées exclues) et `MainForm` l'affiche. Une grille vide seule est indiscernable d'une solution sans traductions.
- **Écriture .resx : ne jamais toucher le fichier neutre** — le français est en lecture seule dans l'UI ; `ResxReader.Save` n'écrit que les variantes `<stem>.<code>.resx`.
- **Écriture .resx chirurgicale** — chargement en `PreserveWhitespace` et réécriture seulement si le contenu a changé : c'est ce qui garantit un diff minimal dans le gestionnaire de sources et une sauvegarde idempotente. Ne pas remplacer par une regénération complète du document.
- **Toute écriture disque passe par `AtomicFile`** — `ResxReader.Save`, `AppConfig.Save` et `GlossaryService.Save` écrivent dans un temporaire du même répertoire puis basculent (`File.Replace` / `File.Move`). Ne pas revenir à une écriture directe dans la cible : un crash en pleine écriture la tronquerait — et pour les `.resx`, c'est le code source de l'utilisateur. Le temporaire doit rester dans le même répertoire que la cible (même volume), sans quoi le basculement cesse d'être atomique.
- **Une langue est identifiée par son code, pas par une colonne** — `TranslationRow.Translations` / `Comments` sont indexés par code (`« de-DE »`). Le numéro de colonne Excel (`LanguageInfo.Column`) ne doit rester connu que de `ExcelReader`.
- **`Input.xlsx` est binaire** — ne pas tenter de le lire en texte. La lecture se fait via ClosedXML.
- **Le projet cible `net8.0-windows`** — Windows uniquement, SDK .NET 8 requis.
- **Le premier chargement plus lent n'est pas un défaut à corriger** — c'est la compilation étagée
  de .NET (§5.1). Les trois contre-mesures ont été mises en œuvre et mesurées (§5.2) : ReadyToRun
  rend 6 % pour un binaire doublé, un préchauffage 8 %, et désactiver la compilation étagée coûte
  47 % sur tout le reste. **Aucune n'est activée.** Ne pas y revenir sans une mesure nouvelle — et
  si `PublishReadyToRun` revient, avec la condition sur le RID que documente le §5.2.
- **Le glossaire est transversal, sa surface par langue est une projection** — `GetEntries` / `ReplaceEntries` / `BuildGlossarySection` / `GetGlossaryFingerprint` projettent les `GlossaryTerm` sur une langue. Seuls les termes **Validé** alimentent prompts et empreinte ; l'éditeur et l'extraction valident ce qu'ils écrivent. Ne pas réintroduire un stockage par langue : le process de contrôle externe (GLOSSAIRE.md) repose sur le terme comme unité.
- **La détection d'impact compare la projection prompts, pas les changements d'import** — `BtnGlossary_Click` photographie `GetPromptEntries` (même `ProjectLocked(validatedOnly: true)` que prompts et empreinte) avant et après l'éditeur. Ne pas la rebrancher sur les `GlossaryChange` de l'import : les promotions En contrôle → Validé et les éditions manuelles changent les prompts sans passer par un import, et seraient perdues.
- **La retraduction ciblée écrit dans les dictionnaires par code, jamais dans la vue active** — `RetranslateImpactedAsync` committe d'abord la vue active des lignes impactées de la langue affichée, écrit `Translations[code]` / `Comments[code]` pour chaque langue, invalide le verdict de mise en page des seules traductions réellement changées, et recharge la vue active à la fin. Écrire via `Translation` / `Comment` ne fonctionnerait que pour la langue affichée. Le score d'une ligne retraduite est effacé avant re-vérification : un score de l'ancien glossaire est périmé ; une réponse inexploitable laisse la ligne entière telle quelle (ancienne traduction + ancien score, cohérents entre eux).
- **Un terme supprimé du glossaire ne déclenche pas de retraduction** — décision assumée (GLOSSAIRE.md) : une suppression lève une contrainte, elle n'invalide pas les traductions existantes. Ne pas « compléter » `ComputeChangedTerms` avec les termes disparus sans rouvrir cette décision.
- **La migration v1 du glossaire est idempotente et non destructive** — rejouée en mémoire à chaque chargement tant que `glossary.json` n'a pas été réécrit ; les termes migrés naissent Validé (ils étaient déjà injectés) et l'empreinte projetée reste identique à celle du v1 : les caches survivent. Le champ legacy `EntriesByLanguage` ne doit jamais être réécrit (nul à la sauvegarde).
- **Icône du bouton Glossaire** : `LoadGlossaryIcon()` teste l'existence de `Resources/glossary.png` et retombe sur `Resources/config.png` si absent.
- **La température s'auto-désactive par modèle, ne pas la refixer inconditionnellement** — les modèles récents (Claude Sonnet 5+) refusent le paramètre `temperature` (erreur de validation Bedrock). `Translator` apprend le refus au premier appel (`IsTemperatureRejection` + `MarkTemperatureRejected`, garde anti-boucle) et rejoue sans. La mémoire est en session : le premier batch sur un tel modèle coûte un aller-retour de plus, c'est voulu — pas de liste de modèles à maintenir.
- **Clé API facultative en mode Bifrost uniquement** — `HasApiConfig` et `Translator.ResolveApiKey` s'appuient sur `AppConfig.IsBifrost`. Ne pas rendre la clé facultative pour les accès directs : l'appel partirait et échouerait côté serveur au lieu d'être bloqué en amont.
- **Les quatre `RadioButton` de fournisseur vivent dans des containers différents** (les panneaux des deux `SplitContainer` de `grpAuth`) : WinForms ne gère donc pas l'exclusivité, elle est faite à la main dans `ProviderChanged` sous le garde-fou `_isUpdatingProvider`. Tout nouveau fournisseur doit être ajouté à `ProviderButtons`, sinon il ne sera ni sélectionnable ni relu.
- **Le tableau de bord commence lui aussi par `CommitActiveLanguage`** — il lit les dictionnaires par code de langue, jamais la vue active. Sans ce commit, les éditions en cours seraient comptées comme non traduites.
- **Un score sans traduction est périmé, pas une vérification** — `TranslationStatistics` ne compte les vérifications, les moyennes et les tranches qu'à l'intérieur des lignes traduites. Sinon « vérifiées » pouvait dépasser « traduites », le taux sortir de [0,1] et « non vérifiées » devenir négatif. Les filtres des chiffres concernés portent donc aussi `translation:done`.
- **Les tranches de score ont une seule définition** — `TranslationStatistics.ScoreBuckets()`. Le tableau de bord en fait ses colonnes et `MainForm` en fait les entrées du filtre Commentaire : les faire diverger ferait qu'un clic sur « 42 » ne ramènerait pas 42 lignes.
- **Un filtre texte préfixé de `=` est une égalité exacte** — la saisie manuelle reste un « contient ». Depuis que Projet et Fichier n'ont plus de colonne, le drill-down du tableau de bord ne pose plus ces filtres en texte : il passe par l'arborescence (`SelectTreeExactly`), qui porte la même exigence — un fichier n'est identifié que par son projet *et* son chemin, et `Properties\Msg` ne doit pas ramener `Properties\Msg2`. Le support du `=` reste dans `TranslationRowFiltering` pour les autres colonnes.
- **`ApplyDrillDown` remet tous les filtres à zéro avant de poser le sien** — un filtre resté en place afficherait moins de lignes que le chiffre cliqué, et le tableau de bord passerait pour faux. L'arborescence de la solution en fait partie : le drill-down recoche tout (`ResetSolutionTreeChecks`).
- **L'arborescence de la solution est un filtre d'affichage, pas une exclusion** — décocher un fichier le retire de la grille, mais la sauvegarde, la fusion, le tableau de bord et l'analyse de mise en page portent toujours sur toutes les lignes (`_allRows`). Ne pas brancher ces opérations sur `GetTreeVisibleRows` sans repenser les invariants du tableau de bord.
- **Le panneau d'arborescence suit `dataGridView.Enabled`** — un seul hook (`EnabledChanged`) gèle `Panel1` entier (arbre, case maîtresse, filtre du bandeau) avec la grille pour tous les gels de l'application (batch IA, écriture disque, analyse de mise en page, chargement, rafraîchissement) : ne pas ajouter de désactivation site par site. Le double-clic sur une case est neutralisé (`CheckBoxTreeView`) : WinForms bascule sinon l'état visuel sans lever `AfterCheck`, et l'affichage se désynchronise du filtre.
- **Les états cochés de l'arbre vivent dans `_uncheckedFiles`, jamais dans les nœuds** — le filtre du bandeau reconstruit les nœuds visibles, et la synchronisation (`SyncUncheckedFromVisibleNodes`) ne reporte que l'état des nœuds affichés. Repartir de zéro à chaque coche perdrait les décochages des éléments masqués par le filtre.
- **`RunLayoutCheckAsync` commence par `CommitActiveLanguage`** — la vue active ne vit que dans `Translation` tant qu'elle n'est pas poussée. Sans ce commit, l'analyse porterait sur la valeur d'avant l'édition et afficherait un verdict en désaccord avec la grille.
- **La vérification de mise en page couvre toutes les langues en une passe** — `LayoutCheckService.Analyze` prend une liste de codes, pas un code. Ne pas revenir à une passe par langue : la lecture de la géométrie, qui domine le coût, serait refaite à l'identique sept fois (×4,1 mesuré, contre ×1,8 pour la passe unique).
- **Elle est lancée au chargement et au rafraîchissement, jamais au changement de langue** — les sept langues sont déjà calculées, `SwitchToLanguage` n'a qu'à rebasculer la vue active. Rebrancher l'analyse sur ce chemin ne ferait que recalculer ce qui existe déjà.
- **Un verdict de mise en page vit sous son code de langue** — comme `Translations` et `Comments`. Ne pas le ramener à un champ unique : c'est ce qui obligeait à tout effacer à chaque bascule et à relancer l'analyse.
- **L'analyse de mise en page ne compare que des rapports** — la `Size` sérialisée d'un contrôle `AutoSize` sert d'étalon, l'échelle du formulaire s'en déduit pour les contrôles fixes. Ne pas réintroduire de comparaison directe entre une mesure et une coordonnée du `.resx` : les deux ne sont pas dans le même repère, et l'écart observé sur le corpus est d'un facteur voisin de deux.
- **Le multi-lignes se compte en retours à la ligne explicites** — la hauteur d'un `AutoSize` suit le rapport des nombres de lignes, un contrôle fixe est confronté à la hauteur d'étalon de sa police (médiane des `AutoSize` du formulaire, police par police). Le repli automatique (wrap) n'est pas prédit : ne pas conclure d'un « conforme » qu'une ligne longue ne se replie pas à l'exécution — c'est l'angle mort documenté dans [ROADMAP.md](ROADMAP.md) (vérification de débordement). L'orientation d'une troncature (`LayoutIssue.Vertical`) ne participe pas à la `Signature` : un contrôle déjà défectueux en français ne devient pas imputable à la traduction parce que l'axe change.
- **`GdiTextWidthMeasurer` détient des handles GDI** — une instance par passe d'analyse, dans un `using`. Ne pas en faire un singleton : le cache de polices n'est jamais libéré autrement.
- **`CheckTranslation.Tests` est exclu du glob de l'exécutable** — le projet principal vit à la racine du dépôt : sans les `Compile/EmbeddedResource/None/Content Remove="CheckTranslation.Tests\**"` de `CheckTranslation.csproj`, ses `.cs` seraient compilés dans l'exe et le build casserait (références xUnit absentes). L'accès des tests aux types `internal` passe par `InternalsVisibleTo` dans le même fichier. Ne retirer ni l'un ni l'autre.
- **Annulation IA non disponible** : pas de `CancellationToken` exposé côté UI. Fermer l'app pendant un gros batch est tolérable mais brutal.
- **Caches mémoire non persistés** : perdus à la fermeture.

---

## 11. Tests

**Projet de tests** : `CheckTranslation.Tests/` (xUnit, `net8.0-windows`), câblé dans la solution.

```bash
# Lancer les tests (chemin explicite : voir la note .slnx ci-dessous)
dotnet test CheckTranslation.Tests/CheckTranslation.Tests.csproj -c Release
```

Deux points de câblage à connaître :
- **`InternalsVisibleTo("CheckTranslation.Tests")`** dans `CheckTranslation.csproj` — les classes du projet sont `internal`, c'est ce qui les rend visibles des tests.
- **Le sous-dossier est exclu du glob de l'exécutable** (`Compile Remove="CheckTranslation.Tests\**"` etc. dans `CheckTranslation.csproj`) — le projet principal vit à la racine, sans cette exclusion ses globs par défaut compileraient les `.cs` des tests dans l'exe (références xUnit introuvables). Ne pas retirer ces lignes.

**Couverture actuelle** (logique pure uniquement, aucun test d'UI) : `QualityScore` (parsing + palette), `TranslationRowFiltering` (contient / `=` exact / pseudo-filtres score, traduction, layout), `TranslationStatistics` (comptages dans les lignes traduites, moyennes nulles, cohérence tranche comptée / tranche filtrée), `GlossaryDiff` (types de changements, statuts, promotion sous couverture complète, doublon refusé), `GlossaryImpact` (projection avant/après, suppressions ignorées, sélection insensible à la casse), `GlossaryExcel` (aller-retour sur fichiers temporaires, refus des classeurs ambigus), `Translator.ParseNumberedList` (comportements caractérisés, dont : un numéro hors bornes est rattaché à l'entrée courante), caches de `TranslationService` (clés par langue + fingerprint, purge par modèle), `GlossaryService.NormalizeCell`.

**Prochaines extensions naturelles** : `LayoutAnalyzer` (la mesure injectée `TextWidthMeasurer` existe pour ça), `ExcelReader.Merge` (fusion + conflits), `AppConfig` (round-trip DPAPI + compat legacy — attention : ne jamais écrire dans le vrai `%LocalAppData%`). De même, ne pas instancier `GlossaryService` dans un test : son chemin de stockage n'est pas injectable et `EnsureLoaded` lirait le glossaire réel de l'utilisateur.

**Banc d'essai manuel** : le dossier `FormTest/` contient un formulaire localisé en 7 langues, jamais instancié par l'application — c'est une fixture, pas une fonctionnalité. Ouvrir `CheckTranslation.slnx` dans l'application elle-même le fait apparaître comme n'importe quel formulaire du corpus : ses contrôles calibrés (labels `AutoSize`, label à largeur fixe, bouton, case à cocher) et ses variantes de culture, dont les commentaires portent des scores connus, permettent de vérifier à la main la chaîne de mise en page (troncatures, collisions) et la colorisation par score. Faute de projet séparé, il est compilé dans l'exécutable ; l'en exclure est un choix ouvert.

**CI** : workflow GitHub Actions `.github/workflows/ci.yml` — build de l'application puis build + exécution des tests, sur `windows-latest` (obligatoire : `net8.0-windows`), déclenché sur push `master` et sur toute PR. Les étapes visent les `.csproj` explicitement, pas la solution : la `.slnx` n'est reconnue par la CLI `dotnet` qu'à partir du SDK 9, et la CI ne doit pas dépendre du SDK du runner. Pas d'`.editorconfig` ni d'analyseurs Roslyn / StyleCop à ce jour.

---

## 12. Suivi et roadmap

La feuille de route et le suivi chronologique des changements vivent dans [ROADMAP.md](ROADMAP.md).

**Convention inchangée** : chaque changement ajoute sa ligne à l'historique de ROADMAP.md et met
la feuille de route à jour, comme il met à jour les sections de ce guide qu'il touche (§10 en
particulier).

---

*Document vivant — à maintenir à jour au fil des PR.*
