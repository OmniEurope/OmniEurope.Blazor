<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-010 : Poids de l'assembly et langues longues

> Statut : **ouvert**. Établi le 2026-09-30, après la publication de la 1.3.0 ; décisions du propriétaire du même jour (recommandations 1 et 2 sur l'assembly, les trois volets des langues longues, la page d'export).

## Objectif

Rendre de la marge à l'assembly du paquet sans rien changer à son API, et faire tenir les textes
des langues longues (allemand, finnois, grec, hongrois) dans les composants, en mesurant d'abord
où ils débordent.

## État des lieux (2026-09-30, 1.3.0)

- `OmniEurope.Blazor.dll` : 2 031 104 octets pour un budget de 2 097 152 (96,9 %). Les métadonnées en
  font 1,31 Mo ; les textes littéraux 344 Ko, dont 171 Ko pour les 252 tracés d'icônes Phosphor
  (`Internal/PhosphorIconGlyphs*.cs`), stockés en UTF-16. En UTF-8 ils font 85 553 octets, 23 091
  compressés en Brotli.
- 6 800 attributs de nullabilité (`NullableAttribute`, `NullableContextAttribute`), environ 120 Ko
  avec leurs lignes de table, sur les membres publics comme privés.
- La langue de la page est posée (`omniInterop.js`, `omni-boot.js`) mais seul le planning coupe les
  mots (`hyphens: auto`) ; aucune mesure ne dit où un texte long déborde.
- `docs/data-components.md` présente `OmniMarkdownExportButton` comme le moyen d'exporter les lignes
  d'une grille, que la barre `ExportFormats` remplace.

## Lots

### Lot 1 : icônes hors du code

Les tracés quittent les fichiers C# pour une ressource incorporée compressée en Brotli, lue une seule
fois au premier rendu d'une icône. `OmniIconName` et le rendu d'`OmniIcon` sont inchangés.

- [ ] Contrôle : la taille de l'assembly baisse d'au moins 130 Ko ; les tests d'icônes et la baseline
  d'API publique passent sans changement ; la sonde `Density` voit les mêmes icônes.

### Lot 2 : nullabilité publique seulement

`<Features>nullablePublicOnly</Features>` sur le projet du paquet.

- [ ] Contrôle : gain mesuré et consigné ici ; `docs/public-api.txt` inchangé ; 0 avertissement.

### Lot 3 : sonde des langues longues

Septième sonde, `Languages` : les pages de la vitrine en allemand, finnois, grec et hongrois, et
chaque texte qui déborde de sa boîte (bouton, onglet, libellé, en-tête de grille, menu, pastille) ou
se coupe sans points de suspension.

- [ ] Contrôle : la sonde échoue sur un débordement provoqué exprès, puis la liste des débordements
  réels est consignée ici.

### Lot 4 : corrections dans le paquet

Là où la sonde trouve un débordement : `hyphens: auto` et `overflow-wrap` sur les libellés, titres,
alertes et cartes ; barres de boutons qui passent à la ligne ; largeurs fixes remplacées par des
largeurs qui suivent le contenu.

- [ ] Contrôle : la sonde `Languages` passe, les six autres aussi.

### Lot 5 : garde sur la longueur des traductions

Un test signale les textes des emplacements étroits (boutons, onglets, en-têtes) dont la traduction
dépasse 1,6 fois le texte français ; la traduction trop longue reçoit une variante plus courte.

- [ ] Contrôle : le test échoue sur une traduction allongée exprès, puis passe sur les ressources.

### Lot 6 : page d'export

`docs/data-components.md` : pour une grille, la barre `ExportFormats` ; `OmniMarkdownExportButton`
pour un export hors grille.

- [ ] Contrôle : la page ne présente plus le bouton comme l'export d'une grille.
