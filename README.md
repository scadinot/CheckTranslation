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
- **Configuration** : prompts (avec apercu), choix du fournisseur IA, cles API (chiffrees via DPAPI), URLs et modeles configurables

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

Les cles API sont chiffrees via DPAPI (scope utilisateur) dans `CheckTranslation.config.json`.

Note : l'integration Anthropic peut etre presente cote configuration; si l'appel API n'est pas implemente, l'application affichera une erreur lors de la traduction.

## Dependances

- [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.104.2 — lecture et ecriture des fichiers Excel
