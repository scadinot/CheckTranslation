# CheckTranslation

Application de bureau Windows Forms (.NET 8.0) destinée au contrôle, à la traduction et à la vérification des ressources d'un logiciel. Elle lit un fichier Excel exporté par **ResX Resource Manager** (extension Visual Studio), filtre les entrées `@Invariant` et affiche les traductions dans un tableau pour vérification et édition.

> **Documentation technique complète (architecture, conventions, suivi, roadmap) :** voir [CLAUDE.md](CLAUDE.md).

---

## Fonctionnalités

### Affichage et édition
- **Chargement Excel** : ouverture d'un `.xlsx` ResX Resource Manager, progress bar en lignes lues
- **Tableau** : français (lecture seule) + traduction (éditable) + commentaire de vérification (score)
- **Colonnes de détail** : Projet / Fichier / Clé affichables via un bouton bascule (mémorisé)
- **7 langues** : Anglais, Allemand, Espagnol, Italien, Néerlandais, Polonais, Chinois (boutons drapeaux)
- **Persistance de disposition** : taille de fenêtre + largeur des colonnes, mémorisées entre les sessions (deux jeux distincts selon le mode « détails » visible ou non)

### Filtrage, tri et colorisation
- **Filtres par colonne** : icône loupe dans chaque en-tête, saisie texte (debounce 300 ms)
- **Filtre par score de vérification** : ComboBox dans l'en-tête Commentaire — « Non vérifiés », « ≤ 50 … 100 », « ≥ 90 »
- **Tri** : clic sur l'en-tête (asc/desc)
- **Colorisation** : dégradé rouge → vert des cellules Traduction / Commentaire selon le score (format `XXX - commentaire`)

### IA (OpenAI / Anthropic)
- **Traduction** et **Vérification** : menu contextuel sur la cellule Traduction, mono ou multi-sélection
- **Batch + parallélisation** : lots de 20 items, 4 lots en parallèle max, retry Polly (backoff exponentiel + jitter) sur les erreurs HTTP transitoires
- **Progression temps réel** : la barre de progression s'incrémente au fur et à mesure des retours API (paliers de ~20 items)
- **Auto-traduire** (sans IA) : recopie des traductions déjà présentes pour un même texte français
- **Caches en session** : caches mémoire distincts pour traductions et vérifications, déduplication, compteurs dans la status bar, invalidation automatique dès qu'une entrée du glossaire change (via fingerprint SHA256)
- **Vidage des caches** : deux boutons dédiés dans la configuration

### Glossaire métier
- **Éditeur par langue** : bouton toolbar dédié — entrées `Source` / `Destination` / `Context`, persistées en JSON dans `%LocalAppData%\CheckTranslation`
- **Extraction IA assistée** : menu contextuel « Extraire les termes métier… » sur une sélection — l'IA propose des termes candidats, l'utilisateur valide un par un avant ajout
- **Injection dans les prompts** : le placeholder `{glossary}` des prompts de traduction / vérification est remplacé par la section glossaire de la langue active — garantit la cohérence terminologique d'un appel à l'autre
- **Invalidation de cache** : un fingerprint SHA256 du glossaire est inclus dans les clés de cache ; toute modification d'une entrée fait retraduire les lignes concernées au prochain appel

### Fichier : sauvegarde, rafraîchissement, fusion
- **Sauvegarde** : réécriture des traductions et des commentaires de vérification dans le fichier Excel d'origine
- **Rafraîchir (F5)** : recharge le fichier du disque en conservant les traductions en mémoire, détecte les changements du français/commentaire source et demande confirmation avant d'écraser
- **Fusion** : report des traductions d'un fichier source vers un fichier destination via la clé `Projet | Fichier | Clé` ; dialogue de résolution des conflits ligne par ligne

### Configuration
- **Prompts** : traduction + vérification avec aperçu Markdown. Placeholders supportés : `{language}` (langue cible) et `{glossary}` (section glossaire injectée automatiquement)
- **Fournisseur IA** : OpenAI (ChatGPT) ou Anthropic (Claude)
- **Paramètres par provider** : clé API (chiffrée via DPAPI, scope utilisateur), URL, modèle. Liste des modèles chargée via l'API à l'ouverture de la ComboBox

### Raccourcis & utilitaires
- **F5** : rafraîchir (recharge du fichier + réapplication filtres/tri)
- **Copier le texte français** : menu contextuel sur la colonne Français (mono ou multi-sélection)
- **Indicateur « Rafraîchir * »** : le bouton Rafraîchir affiche une étoile après édition/traduction/vérification, pour signaler qu'une ré-application des filtres/tri peut être utile

---

## Prérequis

- SDK **.NET 8.0**
- **Windows** (application Windows Forms)

## Build et exécution

```bash
# Build debug
dotnet build

# Lancer
dotnet run

# Build release
dotnet build -c Release

# Publier
dotnet publish -c Release
```

## Configuration

Au premier lancement, cliquer sur l'icône ⚙ de la toolbar pour définir :

- **Prompts** (traduction + vérification) — utiliser `{language}` et `{glossary}` comme placeholders
- **Fournisseur IA** — OpenAI ou Anthropic
- **Clé / URL / Modèle** par fournisseur

Les **clés API sont chiffrées via DPAPI** (scope utilisateur) dans le fichier de configuration.

### Emplacement du fichier de config

Le fichier `CheckTranslation.config.json` est enregistré dans :

```
%LocalAppData%\CheckTranslation\CheckTranslation.config.json
```

(Lecture de compatibilité sur l'ancien emplacement dans le dossier de l'application.)

### Contenu persisté

- Prompts, fournisseur et paramètres par provider
- Langue active
- Visibilité des colonnes de détail
- Taille de la fenêtre
- Largeur des colonnes (deux jeux distincts selon le mode « détails »)

Le glossaire est persisté séparément, fichier JSON par langue dans le même dossier.

---

## Dépendances

- [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.104.2 — lecture/écriture Excel
- [OpenAI](https://www.nuget.org/packages/OpenAI) 2.8.0 — client OpenAI
- [Anthropic](https://www.nuget.org/packages/Anthropic) 12.8.0 — client Anthropic
- [Markdig](https://github.com/xoofx/markdig) 0.38.0 — aperçu Markdown
- [Polly](https://github.com/App-vNext/Polly) 8.5.2 — retry + backoff exponentiel
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) 9.0.0 — conteneur DI
