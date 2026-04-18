# CheckTranslation

Application de bureau Windows Forms (.NET 8.0) destinee au controle, a la traduction et a la verification des ressources d'un logiciel. Elle lit un fichier Excel exporte par ResX Resource Manager (via Visual Studio), filtre les entrees marquees `@Invariant` et affiche les traductions dans un tableau pour verification et edition.

## Fonctionnalites

### Affichage et édition
- **Chargement Excel** : ouverture d'un fichier `.xlsx` exporte par ResX Resource Manager, avec barre de progression en lignes lues
- **Affichage en tableau** : texte francais (lecture seule) et traduction (editable) cote a cote + commentaire de verification
- **Colonnes de detail** : bouton bascule pour afficher/masquer les colonnes Projet, Fichier et Cle (memorise)
- **7 langues supportees** : Anglais, Allemand, Espagnol, Italien, Neerlandais, Polonais, Chinois
- **Selection de langue** : boutons drapeaux dans la barre d'outils (mode radio), langue memorisee
- **Persistance de disposition** : taille de fenêtre et largeur des colonnes memorisees (dictionnaires distincts selon le mode "détails" visible ou non)

### Filtrage, tri et couleur
- **Filtres par colonne** : icone loupe dans les en-tetes, saisie de texte pour filtrer (toutes colonnes, debounce 300 ms)
- **Filtre par score de verification** : ComboBox dans l'en-tete du commentaire — "Non vérifiés", "≤ 50..100", "≥ 90"
- **Tri par colonne** : clic sur l'en-tete pour trier (ascendant/descendant)
- **Colorisation automatique** : degrade rouge → vert des cellules traduction/commentaire selon le score de verification (format `XXX - commentaire`)

### IA (OpenAI / Anthropic)
- **Traduction IA** : clic droit sur une cellule de traduction pour appeler l'API
- **Verification IA** : controle qualitatif des traductions avec score/commentaire par langue
- **Batch + parallelisation** : decoupage en lots de 20, jusqu'a 4 lots en parallele, retry automatique avec backoff exponentiel + jitter (Polly) sur les erreurs HTTP transitoires
- **Progression temps réel** : la barre de progression s'incrémente au fur et à mesure des retours API (paliers de 20 items)
- **Auto-traduire (sans IA)** : copie des traductions deja presentes pour un meme texte francais
- **Caches en session** : cache memoire distinct pour traductions et verifications (clef = provider + url + model + langue + fingerprint glossaire + texte), deduplication, compteurs dans la barre d'etat ; invalidation automatique dès qu'une entrée du glossaire change
- **Vidage des caches** : boutons dedies dans la configuration

### Glossaire métier
- **Editeur de glossaire par langue** : bouton dédié dans la barre d'outils — entrées `Source` / `Destination` / `Context` (phrase courte), persistées en JSON dans `%LocalAppData%\CheckTranslation`
- **Extraction IA assistée** : menu contextuel "Extraire les termes métier…" sur une sélection — l'IA propose des termes candidats que vous validez un par un avant ajout au glossaire
- **Injection automatique dans les prompts** : le placeholder `{glossary}` est remplacé par la section glossaire de la langue active au moment de la traduction ou de la vérification — garantit la cohérence terminologique entre les appels IA
- **Invalidation du cache** : un fingerprint SHA256 du glossaire est inclus dans les clés de cache — toute modification d'une entrée fait retraduire les lignes concernées à la prochaine demande

### Fichier : sauvegarde, rafraichissement, fusion
- **Sauvegarde** : ecriture des traductions et des commentaires de verification dans le fichier Excel d'origine
- **Rafraichir (F5)** : recharge le fichier du disque en conservant les traductions en memoire, detecte les changements du francais/commentaire source et demande confirmation
- **Fusion** : report des traductions d'un fichier source vers un fichier destination via la cle `Projet|Fichier|Cle`, avec une boite de dialogue de resolution des conflits par ligne (choix : reporter le francais+commentaire et/ou la traduction+commentaire de traduction)

### Configuration
- **Prompts** : traduction + verification avec apercu Markdown (utiliser `{language}` pour la langue cible et `{glossary}` pour la section glossaire injectée automatiquement)
- **Fournisseur IA** : OpenAI (ChatGPT) ou Anthropic (Claude)
- **Parametres OpenAI / Anthropic** : cle API (chiffree via DPAPI), URL, modele configurables (liste des modeles chargee via l'API a l'ouverture de la combo)
- **Boutons "Vider cache"** : purge manuelle des caches traduction/verification

### Copier / utilitaires
- **Copier le texte francais** : menu contextuel sur la colonne Francais (mono ou multi-selection)
- **Indicateur "Rafraichir en attente"** : le bouton Rafraichir affiche `*` apres une edition/traduction/verification pour signaler qu'un re-application des filtres/tri peut etre utile

## Prerequis

- SDK .NET 8.0
- Windows (application WinForms)

## Build et execution

```bash
# Build
dotnet build

# Lancer l'application
dotnet run

# Build release
dotnet build -c Release
```

## Configuration

Au premier lancement, cliquer sur l'icone de configuration pour definir :

- **Prompts** : traduction + verification (utiliser `{language}` comme placeholder pour la langue cible)
- **Fournisseur IA** : OpenAI (ChatGPT) ou Anthropic (Claude)
- **OpenAI** : `Key`, `Url`, `Model`
- **Anthropic** : `Key`, `Url`, `Model`

Pour **OpenAI** et **Anthropic**, la liste des modeles est recuperee automatiquement depuis l'API au moment ou vous ouvrez la liste deroulante **Model** (la saisie manuelle reste possible).

Les cles API sont chiffrees via DPAPI (scope utilisateur) dans `CheckTranslation.config.json`.

Le fichier de configuration est enregistre dans `%LocalAppData%\CheckTranslation\CheckTranslation.config.json`.
L'ancien emplacement dans le dossier de l'application reste lu pour compatibilite.

Sont egalement persistes :
- la langue active, la visibilite des colonnes de detail
- la taille de la fenetre
- la largeur des colonnes (deux jeux distincts selon le mode "détails" visible ou non)

## Raccourcis

| Raccourci | Action |
|-----------|--------|
| `F5`      | Rafraichir (recharge du fichier + reapplication filtres/tri) |

## Dependances

- [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.104.2 — lecture et ecriture des fichiers Excel
- [OpenAI](https://www.nuget.org/packages/OpenAI) 2.8.0 — appels API OpenAI
- [Anthropic](https://www.nuget.org/packages/Anthropic) 12.8.0 — appels API Anthropic
- [Markdig](https://github.com/xoofx/markdig) 0.38.0 — rendu Markdown pour l'apercu des prompts
- [Polly](https://github.com/App-vNext/Polly) 8.5.2 — retry + backoff exponentiel sur les appels IA
- [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) 9.0.0 — conteneur d'injection de dependances
