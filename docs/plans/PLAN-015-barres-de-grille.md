<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-015 : Barres d'en-tête et de pied d'`OmniDataGrid`, export en icônes

> Statut : **livré sur develop**, version 1.7.0 déclarée, non publiée. Établi le 2026-10-05 à la demande d'une application cliente, transmise par le propriétaire.
> Version visée : 1.7.0, non publiée pour l'instant (décision du propriétaire du 2026-10-05).

## Objectif

La barre d'export d'`OmniDataGrid` est aujourd'hui une rangée à part, sans fond ni bordure, sous le pager,
avec le libellé « Tout exporter » et des boutons icône et texte. Elle devient une barre d'en-tête et une
barre de pied rattachées au tableau, dans son cadre, qui portent les boutons d'export (icône seule) et le
contenu du consommateur.

## Décisions

- **Barres** : `ShowHeaderBar` et `ShowFooterBar` (`bool`, `false`) ; dans le cadre du tableau, même
  bordure et même fond. Sans barre, le rendu de la grille ne change pas.
- **Boutons dans une barre** : `HeaderBarExport` et `FooterBarExport` (`OmniDataGridBarExport` : `None` par
  défaut, `Start`, `End`) ; une barre masquée ne porte rien.
- **Formats un par un** (décision du propriétaire) : `ExportMarkdown`, `ExportCsv`, `ExportExcel`,
  `ExportPdf` (`bool`, `true`). Pdf n'apparaît qu'avec un moteur de l'hôte qui l'écrit, comme avant.
- **Contenu du consommateur** : `HeaderBarContent` et `FooterBarContent` (`RenderFragment`).
- **Boutons** : icône seule, plus petits que `Small`, nom accessible et infobulle du format ; plus de
  libellé « Tout exporter » (le groupe garde son nom accessible).
- **Existant** (décision du propriétaire) : `ExportFormats` et `ExportPosition` restent pris en charge,
  la barre qui porte leurs boutons s'affiche d'office dans le cadre, et leur usage avertit à la
  compilation (`[Obsolete]`, CS0618) en nommant les nouveaux paramètres.

## Lots

### Lot 1 - Composant
- [x] Paramètres, énumération, cadre et barres, boutons en icône, logique de l'existant.
- [x] Feuille : cadre partagé, barres, boutons plus petits suivant la densité.
Contrôle fait : 9 tests bUnit (`DataGridBarsTests`), les tests d'export existants passent par le nom accessible
des boutons ; CS0618 observé sur l'usage de `ExportFormats` de la vitrine avant sa migration (erreur ici, le
dépôt traitant les avertissements en erreurs).

### Lot 2 - Vitrine, documentation, API
- [x] Démonstrations de la grille migrées, une démonstration des barres.
- [x] `docs/data-components.md`, CHANGELOG, `docs/public-api.txt`.
Contrôle fait : build Release sans avertissement, 5 266 tests verts, huit sondes de la vitrine et porte JS
vertes, barres mesurées dans Chromium à 1280 px (pied à la fin à 8 px du bord, en-tête de 41 px, boutons de
24 px).

### Lot 3 - Réponse à une application cliente
- [x] Noms des paramètres, exemple pour une barre de pied, boutons à droite, Markdown seul ; version.
Contrôle : réponse transmise au propriétaire.
