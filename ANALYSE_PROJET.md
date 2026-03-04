# Analyse détaillée — projet `CheckTranslation`

## 1) Résumé

`CheckTranslation` est une application de bureau **Windows Forms** en **C# 14 / .NET 10** (`net10.0-windows`) destinée à **contrôler, traduire et vérifier** des chaînes de ressources (type `.resx`) à partir d'un **export Excel** généré par **ResX Resource Manager**.

L'application :
- Charge un fichier `.xlsx` exporté par ResX Resource Manager
- Affiche les entrées dans un `DataGridView` triable et filtrable
- Permet l'édition des traductions dans **7 langues** (Anglais, Allemand, Espagnol, Italien, Néerlandais, Polonais, Chinois)
- Appelle les API **OpenAI** ou **Anthropic** pour traduire et vérifier les traductions en lot
- Sauvegarde les modifications directement dans le fichier Excel

---

## 2) Périmètre technique

### Cible et runtime
| Propriété | Valeur |
|-----------|--------|
| Framework cible | `.NET 10` Windows (`net10.0-windows`) |
| Type d'application | `WinExe` |
| UI | Windows Forms |
| Langage | C# 14 |
| Nullable | `enable` |
| ImplicitUsings | `enable` |
| Icône | `Resources/CheckTranslation.ico` |

### Dépendances NuGet
| Package | Version | Usage |
|---------|---------|-------|
| `ClosedXML` | 0.104.2 | Lecture/écriture de fichiers Excel `.xlsx` |
| `OpenAI` | 2.8.0 | Client OpenAI (chat completions + listing de modèles) |
| `Anthropic` | 12.8.0 | Client Anthropic (messages + listing de modèles) |
| `Markdig` | 0.38.0 | Rendu Markdown pour l'aperçu des prompts |

---

## 3) Structure du projet

```
CheckTranslation/
├── CheckTranslation.slnx           # Fichier solution
├── CheckTranslation.csproj         # Fichier projet (WinExe, net10.0-windows)
├── Program.cs                      # Point d'entrée (STAThread)
│
├── Forms/
│   ├── MainForm.cs                 # UI principale (DataGridView, filtres, tri, menus)
│   ├── MainForm.Designer.cs        # Code généré par le designer
│   ├── MainForm.resx               # Ressources du formulaire
│   ├── ConfigForm.cs               # Dialog de configuration (prompts + providers IA)
│   ├── ConfigForm.Designer.cs      # Code généré par le designer
│   ├── ConfigForm.resx             # Ressources du dialog
│   └── WaitCursorScope.cs          # Helper IDisposable pour le curseur d'attente
│
├── Models/
│   ├── TranslationRow.cs           # Modèle d'une ligne de traduction (multi-langues)
│   ├── AiProvider.cs               # Enum : OpenAI | Anthropic
│   └── AppConfig.cs                # Configuration persistante (JSON + DPAPI)
│
├── Services/
│   ├── ExcelReader.cs              # Lecture/écriture Excel via ClosedXML
│   └── Translator.cs               # Appels API IA (traduction/vérification batch)
│
├── Controls/
│   └── SortableBindingList.cs      # BindingList<T> triable côté client
│
├── Resources/                      # Icônes PNG (15 fichiers)
│   ├── CheckTranslation.ico        # Icône de l'application
│   ├── open.png, save.png          # Boutons toolbar
│   ├── config.png, columns.png     # Boutons toolbar
│   ├── eyes.png, pencil.png        # Icônes onglets (aperçu/édition)
│   └── en-US.png, de-DE.png, es-ES.png, it-IT.png, nl-NL.png, pl-PL.png, zh-CN.png, fr-FR.png
│
├── Input.xlsx                      # Fichier Excel exemple (~3 Mo)
├── README.md                       # Documentation utilisateur
├── CLAUDE.md                       # Guide pour assistants IA
└── ANALYSE_PROJET.md               # Ce fichier
```

---

## 4) Flux fonctionnel de bout en bout

### 4.1) Démarrage
```
Program.Main()
  └── AppConfig.Load()              // Charge CheckTranslation.config.json
  └── ApplicationConfiguration.Initialize()
  └── Application.Run(new MainForm())
```

### 4.2) Ouverture d'un fichier Excel
1. L'utilisateur clique sur **Ouvrir** et sélectionne un `.xlsx`
2. `ExcelReader.Load(filePath, allColumns, activeColumn, progress)` :
   - Ouvre le workbook via ClosedXML
   - Ignore les lignes où la colonne D contient `@Invariant`
   - Charge Project, File, Key, French (colonnes A-E)
   - Charge les traductions de toutes les langues configurées
3. Les données sont affichées dans un `DataGridView` via `SortableBindingList<TranslationRow>`

### 4.3) Navigation multi-langues
- 7 langues définies statiquement avec leur colonne Excel :

| Code | Nom | Colonne Excel |
|------|-----|---------------|
| en-US | Anglais | 9 |
| de-DE | Allemand | 7 |
| es-ES | Espagnol | 11 |
| it-IT | Italien | 13 |
| nl-NL | Néerlandais | 15 |
| pl-PL | Polonais | 17 |
| zh-CN | Chinois | 19 |

- Boutons drapeaux dans la toolbar (mode radio)
- `TranslationRow.SwitchLanguage(oldCol, newCol)` bascule les valeurs actives sans recharger le fichier

### 4.4) Traduction / Vérification IA
- **Clic droit** sur la colonne Traduction → menu contextuel
- Actions disponibles :
  - Traduire (une ligne ou la sélection)
  - Vérifier (une ligne ou la sélection)
- Traitement par lots de **20 entrées** (`BatchSize = 20`)
- Parsing des réponses via regex `^\s*(\d+)[.)]\s*(.+)$`

### 4.5) Sauvegarde
- `ExcelReader.Save(filePath, activeColumn, rows)` :
  - Synchronise `Translation` vers `Translations[activeColumn]`
  - Réécrit toutes les colonnes de traduction dans le workbook
  - Sauvegarde le fichier

---

## 5) Modèle de données

### `TranslationRow`
```csharp
internal sealed class TranslationRow
{
    public int RowNumber { get; set; }          // Numéro de ligne Excel (pour sauvegarde)
    public string Project { get; set; }          // Colonne A
    public string File { get; set; }             // Colonne B
    public string Key { get; set; }              // Colonne C
    public string French { get; set; }           // Colonne E (source)

    // Vue "active" (langue sélectionnée)
    public string Translation { get; set; }      // Traduction éditable
    public string Comment { get; set; }          // Commentaire/score vérification

    // Stockage multi-langues (clé = numéro de colonne Excel)
    public Dictionary<int, string> Translations { get; }
    public Dictionary<int, string> Comments { get; }

    public void SwitchLanguage(int oldColumn, int newColumn);
}
```

### `AiProvider`
```csharp
internal enum AiProvider
{
    OpenAI,
    Anthropic,
}
```

---

## 6) Configuration (`AppConfig`)

### Fichier de configuration
- Emplacement : `CheckTranslation.config.json` (même dossier que l'exécutable)
- Format : JSON indenté

### Valeurs par défaut
| Propriété | OpenAI | Anthropic |
|-----------|--------|-----------|
| URL | `https://api.openai.com/v1` | `https://api.anthropic.com` |
| Modèle | `gpt-5.2` | `claude-sonnet-4-6` |

### Sécurité des clés API
- Chiffrement via **DPAPI** (`ProtectedData.Protect`) avec `DataProtectionScope.CurrentUser`
- Les clés sont stockées en Base64 dans le fichier JSON
- Si le déchiffrement échoue (autre utilisateur), la valeur brute est retournée

### Prompts
- **TranslatePrompt** : instructions détaillées pour la traduction technique (électrotechnique, normes, PV)
- **VerifyPrompt** : grille de notation sur 100 points avec critères précis
- Variables : `{language}` est remplacé par le nom de la langue cible

---

## 7) UI principale (`MainForm`)

### Barre d'outils
| Bouton | Icône | Action |
|--------|-------|--------|
| Ouvrir | `open.png` | Sélection fichier Excel |
| Sauvegarder | `save.png` | Sauvegarde dans le fichier |
| Détails | `columns.png` | Affiche/masque Projet, Fichier, Clé |
| Drapeaux | `xx-XX.png` | Sélection de la langue active |
| Config | `config.png` | Ouvre le dialog de configuration |

### DataGridView
- **Colonnes fixes** (créées dans Designer) : French, Translation
- **Colonnes dynamiques** (créées par code) : Project, File, Key, Comment
- **Double-buffering** activé pour éviter le scintillement
- **Tri** : clic sur l'en-tête (ascendant/descendant)
- **Filtre** : clic sur l'icône entonnoir dans l'en-tête → popup TextBox

### Coloration des scores de vérification
- Regex : `^\s*(\d{1,3})\s*[-–]` pour extraire le score
- Dégradé rouge (score 0) → vert (score 100) via `ScoreToTextColor()`

### Barre de statut
- Nom du fichier ouvert
- Nombre de lignes (total / filtrées)
- Langue active avec drapeau
- Provider IA + modèle sélectionné
- Nombre de lignes sélectionnées
- Barre de progression

---

## 8) Dialog de configuration (`ConfigForm`)

### Structure
- **SplitContainer horizontal** pour les prompts (Traduction / Vérification)
- **TabControl** pour chaque prompt : onglet Aperçu (WebBrowser) + onglet Édition (TextBox)
- **SplitContainer vertical** pour les providers : OpenAI (gauche) / Anthropic (droite)

### Sélection du modèle
- Les `ComboBox` de modèle chargent la liste des modèles disponibles **à l'ouverture du dropdown**
- Appel API :
  - OpenAI : `OpenAIModelClient.GetModelsAsync()`
  - Anthropic : `AnthropicClient.Models.List()`
- Cache invalidé si Key ou URL changent
- Saisie manuelle toujours possible

### Aperçu Markdown
- Rendu via **Markdig** avec extensions avancées
- CSS embarqué pour un style cohérent
- Debounce de 250ms sur les modifications

---

## 9) Service IA (`Translator`)

### Paramètres
| Constante | Valeur |
|-----------|--------|
| `BatchSize` | 20 |
| `Temperature` | 0.1 |
| `AnthropicMaxTokens` | 2048 |

### Méthodes principales
```csharp
// Traduction
Task<string[]> TranslateBatchAsync(texts, config, targetLanguage)
Task<IReadOnlyList<string[]>> TranslateInBatchesAsync(texts, config, targetLanguage, progress)

// Vérification
Task<string[]> VerifyBatchAsync(pairs, config, targetLanguage)
Task<IReadOnlyList<string[]>> VerifyInBatchesAsync(pairs, config, targetLanguage, progress)
```

### Appels API
- **OpenAI** : `ChatClient.CompleteChatAsync()` avec `SystemChatMessage` + `UserChatMessage`
- **Anthropic** : `AnthropicClient.Messages.Create()` avec `System` + `Messages[Role.User]`

### Parsing des réponses
- Regex : `^\s*(\d+)[.)]\s*(.+)$`
- Supporte les entrées multi-lignes (concaténation)
- Retourne un tableau de la taille attendue (entrées vides si format non conforme)

### Gestion d'erreurs Anthropic
- `AnthropicRateLimitException` (429) → message explicite sur le quota

---

## 10) Helpers

### `SortableBindingList<T>`
- Étend `BindingList<T>` pour supporter le tri côté client
- Implémente `ApplySortCore()` avec `List<T>.Sort()`
- Utilisé par le `DataGridView` pour le tri en mémoire

### `WaitCursorScope`
- Struct `IDisposable` pour afficher le curseur d'attente
- Restaure l'état précédent du curseur dans `Dispose()`

---

## 11) Format du fichier Excel (ResX Resource Manager)

| Colonne | Index | Contenu |
|---------|-------|---------|
| A | 1 | Project |
| B | 2 | File |
| C | 3 | Key |
| D | 4 | Comment (contient `@Invariant` pour ignorer) |
| E | 5 | French (langue par défaut) |
| F | 6 | Comment.de-DE |
| G | 7 | .de-DE (Allemand) |
| H | 8 | Comment.en-US |
| I | 9 | .en-US (Anglais) |
| ... | ... | Autres langues (pattern : Comment.xx-XX, .xx-XX) |

---

## 12) Commandes de build

```bash
# Build debug
dotnet build

# Build release
dotnet build -c Release

# Exécution
dotnet run

# Publication
dotnet publish -c Release

# Nettoyage
dotnet clean
```

---

## 13) Points d'attention / limitations

1. **Parsing batch** : La robustesse dépend du respect des formats imposés dans les prompts IA
2. **Sauvegarde partielle** : `ExcelReader.Save()` ne persiste pas les colonnes `Comment` (seulement `Translation`)
3. **AppConfig.Load()** : Retourne un nouvel objet si le fichier n'existe pas, mais n'affecte `Current` que si le fichier existe et est valide
4. **Icônes fallback** : Les icônes œil/crayon ont un fallback GDI+ si les fichiers PNG sont absents
5. **Listing modèles** : Nécessite une clé API valide et un accès réseau ; silencieux en cas d'échec
6. **Single-threaded UI** : Les appels API sont asynchrones mais le traitement batch bloque l'UI pendant chaque appel

---

## 14) Conventions de code

- **Namespace unique** : `CheckTranslation` (indépendamment des sous-dossiers)
- **Références nullables** : Activées (`?` sur les types référence)
- **Usings implicites** : Activés
- **Namespaces à portée de fichier** : `namespace CheckTranslation;`
- **Modificateurs d'accès** : `internal` par défaut, `public` pour les formulaires
- **Ne PAS modifier** : `*.Designer.cs`, `*.resx` (générés automatiquement)

---

*Document généré automatiquement à partir de l'inspection des fichiers sources du workspace.*
