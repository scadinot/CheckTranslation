# CheckTranslation

Application de bureau Windows Forms (.NET 10.0) destinee au controle et a la traduction des ressources d'un logiciel. Elle lit un fichier Excel exporte par ResX Resource Manager (via Visual Studio), filtre les entrees marquees `@Invariant` et affiche les traductions dans un tableau pour verification et edition.

## Fonctionnalites

- **Chargement Excel** : ouverture d'un fichier `.xlsx` exporte par ResX Resource Manager
- **Affichage en tableau** : texte francais (lecture seule) et traduction (editable) cote a cote
- **7 langues supportees** : Anglais, Allemand, Espagnol, Italien, Neerlandais, Polonais, Chinois
- **Selection de langue** : boutons drapeaux dans la barre d'outils (mode radio)
- **Filtres par colonne** : icone entonnoir dans les en-tetes, saisie de texte pour filtrer
- **Tri par colonne** : clic sur l'en-tete pour trier (ascendant/descendant)
- **Traduction IA** : clic droit sur une cellule de traduction pour appeler l'API ChatGPT (OpenAI)
- **Sauvegarde** : ecriture des modifications dans le fichier Excel d'origine
- **Configuration** : prompt, cle API (chiffree via DPAPI), URL et modele configurables

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

Au premier lancement, cliquer sur l'icone de configuration (engrenage) pour definir :

- **Prompt** : instructions pour le modele de traduction (utiliser `{language}` comme placeholder pour la langue cible)
- **Key** : cle API OpenAI (chiffree dans le fichier de configuration)
- **Url** : URL de base de l'API (ex: `https://api.openai.com/v1`)
- **Model name** : nom du modele (ex: `gpt-4`)

## Dependances

- [ClosedXML](https://github.com/ClosedXML/ClosedXML) 0.104.2 — lecture et ecriture des fichiers Excel
