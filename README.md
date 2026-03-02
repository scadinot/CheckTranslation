# CheckTranslation

Application de bureau Windows Forms (.NET 10.0) destinee au controle et a la traduction des ressources d'un logiciel. Elle lit un fichier Excel exporte par ResX Resource Manager (via Visual Studio), filtre les entrees marquees `@Invariant` et affiche les traductions dans un tableau pour verification et edition.

## Fonctionnalites

- **Chargement Excel** : ouverture d'un fichier `.xlsx` exporte par ResX Resource Manager
- **Affichage en tableau** : texte francais (lecture seule) et traduction (editable) cote a cote
- **Colonnes de detail** : bouton bascule pour afficher/masquer les colonnes Projet, Fichier et Cle (memorise)
- **7 langues supportees** : Anglais, Allemand, Espagnol, Italien, Neerlandais, Polonais, Chinois
- **Selection de langue** : boutons drapeaux dans la barre d'outils (mode radio)
- **Filtres par colonne** : icone entonnoir dans les en-tetes, saisie de texte pour filtrer (toutes colonnes)
- **Tri par colonne** : clic sur l'en-tete pour trier (ascendant/descendant)
- **Traduction IA** : clic droit sur une cellule de traduction pour appeler l'API (OpenAI / Anthropic)
- **Sauvegarde** : ecriture des modifications dans le fichier Excel d'origine
- **Configuration** : prompts (avec apercu), choix du fournisseur IA, cles API (chiffrees via DPAPI), URLs et modeles configurables (liste des modeles chargee via l'API)

## Prerequis

- SDK .NET 10.0
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

- **Prompt** : instructions pour le modele de traduction (utiliser `{language}` comme placeholder pour la langue cible)
- **Prompts** : traduction + verification (utiliser `{language}` comme placeholder pour la langue cible)
- **Fournisseur IA** : OpenAI (ChatGPT) ou Anthropic (Claude)
- **OpenAI** : `Key`, `Url`, `Model`
- **Anthropic** : `Key`, `Url`, `Model`

Pour **OpenAI** et **Anthropic**, la liste des modeles est recuperee automatiquement depuis l'API au moment ou vous ouvrez la liste deroulante **Model** (la saisie manuelle reste possible).

Les cles API sont chiffrees via DPAPI (scope utilisateur) dans `CheckTranslation.config.json`.

## Dependances

- [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.104.2 — lecture et ecriture des fichiers Excel
- [OpenAI](https://www.nuget.org/packages/OpenAI) — appels API OpenAI
- [Anthropic](https://www.nuget.org/packages/Anthropic) — appels API Anthropic
- [Markdig](https://github.com/xoofx/markdig) — rendu Markdown pour l'aperçu des prompts
