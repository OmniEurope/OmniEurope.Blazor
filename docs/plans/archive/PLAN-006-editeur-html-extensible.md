<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-006 : Éditeur HTML extensible

> Statut : Établi le 2026-09-27 à la demande du propriétaire (découpage de l'éditeur d'une application cliente) ;
> **terminé** le 2026-09-27 : lots 1 à 4 livrés et consommés par une application cliente (son PLAN-009), qui n'a plus aucun script
> sur la surface de l'éditeur.

## Objectif

`OmniHtmlEditor` garde le cœur générique et offre un objet d'extension par lequel une application ajoute ses
commandes, ses règles de nettoyage, ses raccourcis, ses éléments en ligne actifs et son menu contextuel, sans
script à côté de la surface. Les fonctions génériques qu'une application cliente portait seul (casse, caractères spéciaux,
tableau importé d'un fichier, affichage des blocs) deviennent des commandes intégrées.

## État des lieux (2026-09-27)

- Extension actuelle : `Commands` (liste complète de la barre), `SanitizerPolicy`, `SelectionChanged`,
  `OmniHtmlEditorCommand.Enabled`/`Pressed`, contexte de commande (`InsertHtmlAsync`, `SetHtmlAsync`,
  `ExecuteAsync`, `CommitDomAsync`, `Selection`).
- Une application cliente contourne OE par 8 fonctions de son `interop.js` qui touchent la surface : lecture de la fusion d'une
  cellule, remplacement de la formule sous le curseur, sauvegarde et restauration de la sélection, changement
  de casse, clic sur une note, suivi de contexte. La surface mémorise pourtant déjà la sélection (`remember`,
  `restore`) : ce qui manque, ce sont des primitives et un point d'accroche pour les éléments en ligne.
- Aucune fonction propre à l'AKN dans OE (0 occurrence de `akn`, `latex`, `footnote`).

## Décisions (validées le 2026-09-27)

- D1. Un objet d'extension (`OmniHtmlEditorExtension`, classe abstraite à membres virtuels), passé par le
  paramètre `Extensions` ; plusieurs extensions se cumulent dans l'ordre.
- D2. Les fonctions frontière vont dans le cœur : changement de casse, caractères spéciaux, tableau importé d'un
  fichier (CSV et TSV lus par OE, autres formats fournis par une extension), affichage des blocs.
- D3. La politique de nettoyage d'une extension ne fait qu'élargir la liste blanche, comme `SanitizerPolicy` ;
  les deux se fusionnent et les interdits de `OmniHtmlSanitizerPolicy` restent absolus.
- D4. Les nouvelles valeurs d'`OmniHtmlEditorAction` s'ajoutent en fin d'énumération (valeurs existantes
  inchangées).

## Lots

Chaque lot se termine par : build Release à 0 avertissement, suite OE verte, `eng\Test-PublicApi.ps1` à jour.

### Lot 1 - Socle de l'extension
- [x] `OmniHtmlEditorExtension` : `Commands`, `ArrangeToolbar`, `SanitizerPolicy`, `OnSelectionChangedAsync`.
- [x] Paramètre `Extensions` ; barre = `Commands ?? Default` puis `ArrangeToolbar` de chaque extension.
- [x] Fusion des politiques (`OmniHtmlSanitizerPolicy.Merge`), validation inchangée.
- [x] Suivi de sélection actif dès qu'une extension l'écoute ou qu'une de ses commandes en dépend.
Contrôle : tests bUnit (barre arrangée, politique fusionnée appliquée à la valeur, sélection transmise).

### Lot 2 - Primitives et accroches
- [x] Contexte : `ReplaceClosestAsync(selector, html)`, `GetSelectedTextAsync()`, `InsertTextAsync(text)`.
- [x] `OmniHtmlEditorSelectionNode.ColumnSpan` / `RowSpan` pour une cellule.
- [x] Éléments en ligne actifs (`InlineElements`) : clic ou Entrée sur un élément qui correspond au
  sélecteur, rappel .NET avec ses attributs, son texte et un contexte qui le remplace ou le retire.
- [x] Raccourcis (`Shortcuts`) : combinaison vers une commande ; Ctrl+Z, Ctrl+Y et Ctrl+K restent réservés.
- [x] Menu contextuel (`ContextMenu`) sur la surface, par `OmniContextMenu`.
Contrôle : tests bUnit des rappels et des arguments du script ; preuve navigateur dans la vitrine.

### Lot 3 - Fonctions frontière intégrées
- [x] `ChangeCase` (majuscules, minuscules, casse de titre) en liste.
- [x] `InsertSpecialCharacter` : panneau de catégories et recherche.
- [x] `ImportTable` : panneau de fichier, CSV et TSV lus par OE, lecteurs d'extension pour les autres
  formats, aperçu, première ligne en en-tête.
- [x] `ShowBlocks` : bascule qui dessine le contour des blocs.
- [x] Textes fr/en, icônes, démonstration dans la vitrine (gardes de couverture).
Contrôle : tests bUnit, gardes de vitrine vertes, preuve navigateur de chaque commande.

### Lot 4 - Documentation et livraison
- [x] Guide de l'éditeur : écrire une extension, primitives, fonctions intégrées.
- [x] CHANGELOG, API publique, commit et push de `develop`.
Contrôle : suite OE complète verte, guide relu contre le code.

## Ordre et dépendances

Lots 1, 2, 3 dans l'ordre ; une application cliente PLAN-009 consomme chaque lot dès qu'il est livré.

## Critère de clôture

Une application cliente n'a plus aucune fonction de script qui touche la surface de l'éditeur, chaque lot a sa preuve, le
guide décrit l'extension telle qu'elle est codée.

## Suivi

| Lot | État | Preuve |
|---|---|---|
| 1 et 2 | fait | `HtmlEditorExtensionTests` ; vitrine : clic sur une note, raccourci Ctrl+Maj+N, menu contextuel qui retire la note puis annulation, vérifiés dans le navigateur |
| 3 | fait | `HtmlEditorBuiltInPanelTests` ; vitrine : casse, caractère €, contour des blocs, import d'un CSV avec en-tête, vérifiés dans le navigateur |
| 4 | fait | guide `editor-components.md`, CHANGELOG, API publique (3711 signatures) ; suite OE 2007/2007 |
| Suite | fait | Ajouts demandés par la consommation d'une application cliente : `SetTextAsync` (texte d'un élément activé, attributs gardés) et suggestion de suite (`SuggestsText`/`SuggestAsync`, texte fantôme, Tab) ; vitrine : suggestion affichée puis acceptée, vérifiée dans le navigateur ; suite OE 2011/2011 |
