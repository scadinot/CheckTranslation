# GLOSSAIRE.md — Process du glossaire transversal

Process de gouvernance du glossaire métier : constitution, contrôle par les équipes externes,
et retraduction ciblée des lignes impactées par une correction.

> Guide technique : voir [CLAUDE.md](CLAUDE.md). Feuille de route : voir [ROADMAP.md](ROADMAP.md).

---

## 1. Le modèle : un glossaire transversal

Une entrée = un terme métier français, avec toutes ses traductions. Le terme est l'unité de
gouvernance ; les langues n'en sont que les colonnes.

| Champ | Contenu |
|---|---|
| Source | Terme français canonique (singulier, non conjugué), identité de l'entrée |
| Contexte | Définition métier courte, commune à toutes les langues |
| en-US … zh-CN | La traduction imposée par langue, vide tant que non tranchée |
| Statut | Proposé / En contrôle / Validé |
| Commentaire réviseur | Rempli par les équipes externes au retour |

Deux règles : **seuls les termes Validé sont injectés** dans les prompts (traduction ET
vérification), et l'injection est une **projection par langue** — un terme sans traduction
allemande n'apparaît pas dans les prompts allemands. L'empreinte de cache est calculée par
langue sur cette projection : corriger une colonne n'invalide que les caches de sa langue.

## 2. Le cycle en cinq phases

1. **Constitution** — l'extraction IA (« Extraire les termes métier… ») verse des termes Proposé,
   l'éditeur consolide. Les termes naissent Proposé.
2. **Export pour contrôle externe** — classeur Excel au format du tableau ci-dessus (une ligne
   par terme), daté et portant l'empreinte du glossaire à l'export. Seuls les termes Proposé
   passent En contrôle : les Validé restent injectés dans les prompts pendant toute la durée du
   contrôle, et repassent Validé au retour s'ils reviennent inchangés.
3. **Retour et import** — lecture du classeur corrigé, différences présentées terme par terme,
   backup daté de l'ancien glossaire avant remplacement, compte
   rendu des changements. Les termes acceptés passent Validé. Un terme En contrôle revenu
   strictement inchangé repasse Validé **seulement si le classeur couvre toutes les langues** :
   un classeur restreint à une équipe de langue ne dit rien des autres colonnes, son silence ne
   clôt pas le contrôle. Un classeur complet revenu sans aucune correction clôt, lui, le contrôle
   de tous ses termes.
4. **Retraduction ciblée** — à la fermeture de l'éditeur de glossaire, la projection injectée
   dans les prompts (termes Validé, par langue) est comparée à celle de l'ouverture : tout terme
   apparu ou modifié (destination, contexte) — qu'il vienne d'un import, d'une promotion Validé
   ou d'une édition manuelle — sélectionne les lignes dont le français le contient, qui sont
   retraduites puis re-vérifiées, langue par langue, après confirmation avec le compte par
   langue. L'empreinte garantit que le cache ne ressert pas les anciennes traductions. La
   re-vérification se fait comme depuis l'interface, glossaire compris, mais la section injectée
   en vérification porte un garde-fou : **la conformité au glossaire ne justifie à elle seule
   aucune note** — un vérificateur qui a le glossaire sous les yeux tend sinon à constater la
   conformité et à noter 100 sans juger la langue. À la fin, la grille est **filtrée sur les
   lignes retraduites** (pseudo-filtre `translation:retranslated`, par langue) pour les relire.
   Deux décisions assumées : un terme **supprimé** ne déclenche rien (une suppression lève une
   contrainte, elle n'invalide pas l'existant — un terme erroné se corrige, il ne se supprime
   pas) ; la détection est une **inclusion insensible à la casse** — les formes fléchies
   éloignées du terme canonique peuvent lui échapper, et l'approximation retraduira parfois une
   ligne de trop plutôt qu'une de moins.
   La détection automatique ne voit que l'éditeur : un glossaire modifié hors de l'application
   (à la main, par un collègue via git, par un versement de contextes) ne déclenche rien. Le
   bouton **Retraduire les écarts au glossaire** rattrape ce cas : il sélectionne, toutes langues
   confondues, les traductions qui n'emploient pas le terme imposé — même définition que
   `glossary.py check` — et enchaîne sur la même retraduction. C'est aussi la commande à utiliser
   pour rattraper un corpus traduit avant l'existence du glossaire.
5. **Boucle** — le glossaire vit ; chaque campagne rejoue les phases 2 à 4. Le tableau de bord
   mesure l'effet (scores avant / après par langue).

## 3. État d'avancement des chantiers

| Chantier | Contenu | État |
|---|---|---|
| 1. Schéma v2 + migration | `GlossaryTerm` transversal, statuts, projection par langue, empreinte compatible | ✅ Fait |
| 2. Éditeur multi-langues | `GlossaryForm` en grille terme × langue, colonne statut | ✅ Fait |
| 3. Export / import Excel | ClosedXML, diff à l'import, backup daté | ✅ Fait |
| 4. Retraduction ciblée | Détection des lignes impactées, batchs enchaînés par langue, compte rendu | ✅ Fait |

Les deux décisions ouvertes ont été tranchées avec le chantier 4 (voir phase 4 ci-dessus) : un
terme supprimé ne déclenche pas de retraduction, et la correspondance reste une inclusion
insensible à la casse, sans détection des formes fléchies.

## 4. Où vit le glossaire : partagé avec les sources

Le glossaire d'un logiciel appartient à ses sources, pas au poste de celui qui traduit. Quand la
solution ouverte possède un répertoire `.claude`, CheckTranslation lit et écrit
**`.claude/glossary.json` à côté du `.sln` / `.slnx`** ; c'est le même fichier que lisent les
skills et l'outillage `resx-tools` du dépôt (elec calc : `glossary.py check` contrôle les
traductions, `glossary.py extract` impose les termes dans les prompts des agents). Une seule
terminologie, versionnée avec le code, relue en revue de code comme lui. Sans `.claude`,
l'application retombe sur le magasin global du profil utilisateur.

Le format est celui de l'application (schéma v2, statuts en toutes lettres), sauvegardé dans un
ordre stable pour que le diff git ne montre que ce qui change. Les deux consommateurs appliquent la
même règle : **seuls les termes Validé font autorité** — un Proposé ou un En contrôle n'existe ni
pour les prompts de l'application ni pour ceux des skills.

Une cellule de traduction peut porter plusieurs formes acceptées séparées par « / »
(`kabel / kabl`, `surge protective device / SPD`) : la première est celle à écrire, que
l'application injecte dans ses prompts ; les suivantes ne servent qu'au contrôle des formes
fléchies par `glossary.py check`. Convention héritée du `glossary.md` d'elec calc, dont le tableau
a été migré tel quel en termes Validé.

---

*Document vivant — chaque chantier met ce process et son état à jour.*
