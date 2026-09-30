<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-010 : Poids de l'assembly et langues longues

> Statut : **ouvert**. Établi le 2026-09-30, après la publication de la 1.3.0 ; décisions du propriétaire du même jour (recommandations 1 et 2 sur l'assembly, les trois volets des langues longues, la page d'export). Lots 1, 2 et 6 faits.

## Objectif

Rendre de la marge à l'assembly du paquet sans rien changer à son API, et faire tenir les textes
des langues longues (allemand, finnois, grec, hongrois) dans les composants, en mesurant d'abord
où ils débordent.

## État des lieux (2026-09-30, 1.3.0)

- `OmniEurope.Blazor.dll` : 2 031 104 octets pour un budget de 2 097 152 (96,9 %). Les métadonnées en
  font 1,31 Mo ; les textes littéraux 344 Ko, dont environ 170 Ko pour les 243 tracés d'icônes
  Phosphor (`Internal/PhosphorIconGlyphs*.cs`), stockés en UTF-16.
- 6 800 attributs de nullabilité (`NullableAttribute`, `NullableContextAttribute`), environ 120 Ko
  avec leurs lignes de table, sur les membres publics comme privés.
- La langue de la page est posée (`omniInterop.js`, `omni-boot.js`) mais seul le planning coupe les
  mots (`hyphens: auto`) ; aucune mesure ne dit où un texte long déborde.
- `docs/data-components.md` présente `OmniMarkdownExportButton` comme le moyen d'exporter les lignes
  d'une grille, que la barre `ExportFormats` remplace.

## Lots

### Lot 1 : icônes hors du code

Les tracés quittent les fichiers C# pour une ressource incorporée (`Internal/PhosphorIcons.txt`), lue
une seule fois au premier rendu d'une icône. `OmniIconName` et le rendu d'`OmniIcon` sont inchangés.

Écart au plan, sur mesure : la ressource devait être compressée. Compressée en Deflate (28 774
octets), elle gagnait 151 552 octets bruts, mais sa lecture charge `System.IO.Compression` dans une
application WebAssembly (+11 264 octets en brotli) alors que l'assembly en brotli, ce que télécharge le
navigateur, ne gagnait que 7 407 octets : le téléchargement grossissait. Elle reste en UTF-8 non
compressé, sans tâche de build.

- [x] Contrôle : assembly Release à 1 900 544 octets avec le lot 2 (2 031 104 avant), soit −88 064
  octets pour ce lot, sous le seuil de 130 Ko prévu pour la version compressée ; en brotli,
  `OmniEurope.Blazor.wasm` publié passe de 510 061 à 500 585 octets. Tests d'icônes et baseline d'API
  (3 677 signatures) inchangés ; icônes rendues en WebAssembly sans `System.IO.Compression` chargé.

### Lot 2 : nullabilité publique seulement

`<Features>nullablePublicOnly</Features>` sur le projet du paquet.

- [x] Contrôle : −42 496 octets mesurés (1 879 552 à 1 837 056 sur la variante compressée du lot 1) ;
  `docs/public-api.txt` inchangé ; 0 avertissement.

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

- [x] Contrôle : la page ne présente plus le bouton comme l'export d'une grille (`docs/data-components.md`,
  `docs/component-families.md`).
