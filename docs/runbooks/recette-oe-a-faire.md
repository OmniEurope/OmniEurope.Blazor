<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09. Prochain numéro : **R1-29**. Version cible : **1.10.1** (décision du propriétaire du 2026-10-29).

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
| R1-24 | Constats de sécurité du pipeline `oe1-security` #3015 (6 constats) | Erreur | Six constats levés par le pipeline de sécurité de la forge : 1296 écoute de message sans contrôle d'origine (`eng/Test-ShowcaseLanguagesProbe.mjs:92`), 1297 pollution de prototype (`eng/Test-ThemeContrastProbe.mjs:958`), 1298 antiforgery désactivé (`samples/OmniEurope.Blazor.Catalog/Program.cs:40`), 1299 `MarkupString` dynamique (`OmniHtmlEditor.razor:323`), 1300 `innerHTML` non littéral (`wwwroot/html-editor/model.js:199`), 1295 clé d'API générique (`TextFieldTypesExample.razor.cs:6`). À regarder et corriger. Déjà dit : non. | OE | Propriétaire, 2026-10-29 ; export `recette-pieces-temporaires/oe1-pipelines-oe1-security-3015-executio-2026-10-29-1313.md`. Lecture du code : six faux positifs. 1296 est un WebSocket CDP en boucle locale, pas un `postMessage` ; 1297 boucle sur les noms de thème de la sonde, jamais sur une entrée analysée ; 1298 est `/csp-report`, que les navigateurs appellent sans jeton, anonyme et borné en taille ; 1299 et 1300 reçoivent du HTML passé par `OmniHtmlSanitizer` ou échappé (`tableHtml`, `escapeHtml`) ; 1295 est l'alphabet des mots de passe de démonstration. Fait le 2026-10-29 : un commentaire d'exception motivé sur chaque ligne, convention de la forge (`nosemgrep - raison`, `gitleaks:allow`). Prouvé : gitleaks 8.30.1 épinglé (image épinglée par la forge) sur le fichier, 1 fuite avant et 0 après ; compilation de la bibliothèque, des exemples et du catalogue verte. Non prouvé : opengrep 1.22.0 n'est pas installé localement, la disparition des cinq constats SAST attend une nouvelle exécution du pipeline. Décision du propriétaire du 2026-10-29 : pas de téléchargement d'opengrep, la preuve viendra de la prochaine exécution du pipeline de sécurité. | Partiel |
| R1-25 | Vitrine, page Mises en page de formulaires (`/components/form-layouts`), résumé des erreurs | Moche | Le résumé « 1 error(s) to correct » en haut du formulaire décale tout quand il apparaît ou disparaît, ce qui rend le test pénible. Demandé : pouvoir choisir où il s'affiche (en haut, en bas) ou le masquer, et un titre qui gère seul le singulier et le pluriel (« 1 erreur », « 2 erreurs ») au lieu de « error(s) ». Déjà dit : non. | OE et vitrine | Propriétaire, 2026-10-29, capture à l'appui. Le résumé n'est pas un composant d'OE : chaque exemple pose une `OmniAlert` à la main avec `$"{Errors.Count} error(s) to correct"` (`site/OmniEurope.Blazor.Showcase.Examples/Forms/VerticalFormExample.razor:11`, idem `VerticalFormChoicesExample.razor`). OE n'a pas de résumé de validation. Question au propriétaire : composant de résumé dans OE (position, masquage, pluriel) ou correction des seuls exemples. | Question |
| R1-26 | `OmniAppMenu`, panneau ouvert (langue, mode, thème, version) | Bug | Une barre de défilement horizontale apparaît en bas du panneau du menu de l'application, à la ligne Langue ; à retirer. Déjà dit : non. | OE | Propriétaire, 2026-10-29, capture à l'appui. Cause non mesurée : à mesurer (`scrollWidth` contre `clientWidth` du panneau et de la ligne Langue) avant correction. | Ouvert |
| R1-27 | Vitrine, menu latéral de la galerie | Moche | Retirer l'entrée « Back to the site » (« Retour au site »), qui ne sert plus. Déjà dit : non. | Vitrine | Propriétaire, 2026-10-29. `site/OmniEurope.Blazor.Showcase/Components/Gallery/GalleryMenu.razor:5`, clé `GalleryBackToSite` des deux `ShowcaseStrings`. | Ouvert |
| R1-28 | Vitrine, recherche des composants | Autre | Une recherche globale dans la barre du haut qui trouve un composant ; sur la page Overview, la saisie filtre les composants affichés ; dans le menu latéral, la saisie filtre le menu. Déjà dit : non. | OE et vitrine | Propriétaire, 2026-10-29. OE n'a ni champ de recherche d'en-tête ni palette de commandes (`oe_search` : seulement `OmniTextBox` avec icône) ; `OmniPanelMenu` n'a pas de filtre. Question au propriétaire : nouveau composant d'OE (recherche d'en-tête, filtre de menu) ou recherche propre à la vitrine. | Question |

## Critère de clôture

La recette se ferme quand chaque ligne est faite et prouvée par une observation (mesure, capture,
test qui échouait avant), archivée dans `recette-oe-fait.md`, et que la compilation, la suite de tests
et les gardes d'architecture passent réellement.
