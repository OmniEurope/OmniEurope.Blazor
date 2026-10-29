<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09. Prochain numéro : **R1-25**. Version cible : **1.10.1** (décision du propriétaire du 2026-10-29).

## État

Le dépôt est le paquet lui-même : chaque point est d'origine paquet (OE). Les demandes viennent du
propriétaire ou de la recette d'une application cliente, dont le numéro est rappelé dans l'origine.
Traitement direct demandé par le propriétaire le 2026-10-09.

Nettoyage du 2026-10-29 : R1-1 à R1-16, faites et prouvées (livrées avec 1.10.0), sont archivées dans
`recette-oe-fait.md`. Les six lignes restantes ont été revérifiées dans le code le même jour : aucune
n'est encore faite (badge toujours dans `MainLayout.razor:23`, voile `.omni-busy` toujours pulsé,
aucun voile de page, `OmniMain` de la vitrine sans `Scrollable`, aucun commit sur l'espacement des champs
depuis la réserve de la ligne d'erreur).

## Légende

- Type : Bug, Moche, Erreur, Autre.
- État : Ouvert, Ouvert décidé, Question, Fait (prouvé), Écarté, En attente.

## Points

| N° | Page / composant | Type | Constat | Origine | Justification | État |
| --- | --- | --- | --- | --- | --- | --- |
| R1-17 | `OmniFormField` et champs de formulaire | Moche | La ligne d'erreur réservée en permanence (1.10.0) rend chaque champ plus haut d'une ligne : réduire les espacements verticaux du formulaire pour qu'il ne soit pas plus haut qu'avant la réserve, sans qu'aucun champ ne bouge quand un message apparaît. Déjà dit : non. | OE | Recette d'une application cliente (2026-10-29, page de création de compte). Preuve attendue : position des champs identique avant et après l'apparition de l'erreur, hauteur du formulaire mesurée avant et après. Lot A du 2026-10-29 (commit `7092bf20`, branche `worktree-agent-a6940d06f99922b1b`, non fusionné) : la ligne réservée (16 px) coûte plus que l'écart de 12 px d'un `OmniStack` moyen, donc l'espacement seul ne peut pas tenir l'objectif. Mesuré (Edge, vitrine publiée) : formulaire vertical 646,9 px avant la réserve, 756,9 en 1.10.0, 736,9 après le lot ; formulaire dynamique 303,2, 391,2, 339,2 ; positions des 7 champs identiques avec et sans erreur. Question au propriétaire : traitement du reste (+90 px). | Question |
| R1-18 | Voile d'attente de page | Autre | Un voile de page opaque, fixe, de couleur constante adaptée aux modes clair et sombre, sans clignotement ni variation d'opacité, avec un logo fourni par l'application (emplacement ou paramètre) affiché au centre et animé ; OE n'a pas de logo par défaut. Déjà dit : non. | OE | Recette d'une application cliente (R-19 : 31 voiles posés à la main remplacés par un seul mécanisme). `.omni-busy` est un voile de contrôle dont l'opacité pulse de 0,25 à 0,55 (`Styles/10-editors-settings.css:152-168`) ; aucun voile de page n'existe. Décisions du propriétaire du 2026-10-29 : (1) les deux façons de donner le logo, un emplacement `Logo` (balisage libre, image ou SVG) et un paramètre d'URL d'image, l'application prend celle qui lui convient ; (2) OE n'anime rien : chaque application a un logo et un logo animé (voir l'application de référence) et fournit celui qu'elle veut, aucun effet par défaut ; (3) le voile n'apparaît qu'après 100 ms d'attente, pour qu'une attente très brève n'affiche rien. | Ouvert décidé |
| R1-19 | `.omni-busy` posé sur un conteneur de page | Bug | Double ascenseur observé quand le voile couvre une zone qui défile. Piste non mesurée : le `::after` déborde d'un pixel (`inset: calc(-1 * var(--omni-button-border-width, 1px))`, prévu pour la bordure d'un bouton). Déjà dit : non. | OE | Recette d'une application cliente, question posée le 2026-10-29. À reproduire avant correction. | Ouvert |
| R1-23 | `OmniDataGrid`, bouton d'export Markdown (`omni-data-grid__export-button`, icône seule) | Moche | Après un clic, une croix apparaît et l'utilisateur ne sait pas si c'est un succès ou un échec ; attendu : le retour habituel d'un bouton OE, un bref clignotement du bouton, sans croix. Déjà dit : non. | OE | Recette d'une application cliente du 2026-10-29, relayée par sa session. La croix n'est ni un succès ni une erreur : c'est le bouton Annuler (`OmniIconName.Close`) rendu tant que `Export.Running` est posé (`Components/Data/OmniDataGrid.razor:105-111`, `Internal/GridExport.cs:110-146`) ; l'échec s'écrit en texte (`GridExportFailed`, `role="alert"`), le succès par la note `Export.Notice`. Si la croix reste après la fin de l'export, l'export ne se termine pas (`Running` jamais remis à nul) : à mesurer dans l'application cliente avant de changer l'affichage. OE n'a pas de « clignotement » de bouton : le retour de succès existant est l'état copié d'`OmniCopyButton`. | Question |
| R1-24 | Constats de sécurité du pipeline `oe1-security` #3015 (6 constats) | Erreur | Six constats levés par le pipeline de sécurité de la forge : 1296 écoute de message sans contrôle d'origine (`eng/Test-ShowcaseLanguagesProbe.mjs:92`), 1297 pollution de prototype (`eng/Test-ThemeContrastProbe.mjs:958`), 1298 antiforgery désactivé (`samples/OmniEurope.Blazor.Catalog/Program.cs:40`), 1299 `MarkupString` dynamique (`OmniHtmlEditor.razor:323`), 1300 `innerHTML` non littéral (`wwwroot/html-editor/model.js:199`), 1295 clé d'API générique (`TextFieldTypesExample.razor.cs:6`). À regarder et corriger. Déjà dit : non. | OE | Propriétaire, 2026-10-29 ; export `recette-pieces-temporaires/oe1-pipelines-oe1-security-3015-executio-2026-10-29-1313.md`. Lecture du code : six faux positifs. 1296 est un WebSocket CDP en boucle locale, pas un `postMessage` ; 1297 boucle sur les noms de thème de la sonde, jamais sur une entrée analysée ; 1298 est `/csp-report`, que les navigateurs appellent sans jeton, anonyme et borné en taille ; 1299 et 1300 reçoivent du HTML passé par `OmniHtmlSanitizer` ou échappé (`tableHtml`, `escapeHtml`) ; 1295 est l'alphabet des mots de passe de démonstration. Fait le 2026-10-29 : un commentaire d'exception motivé sur chaque ligne, convention de la forge (`nosemgrep - raison`, `gitleaks:allow`). Prouvé : gitleaks 8.30.1 épinglé (image épinglée par la forge) sur le fichier, 1 fuite avant et 0 après ; compilation de la bibliothèque, des exemples et du catalogue verte. Non prouvé : opengrep 1.22.0 n'est pas installé localement, la disparition des cinq constats SAST attend une nouvelle exécution du pipeline. | Partiel |

## Critère de clôture

La recette se ferme quand chaque ligne est faite et prouvée par une observation (mesure, capture,
test qui échouait avant), archivée dans `recette-oe-fait.md`, et que la compilation, la suite de tests
et les gardes d'architecture passent réellement.
