<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-010 : Poids de l'assembly et langues longues

> Statut : **terminé** le 2026-10-01. Établi le 2026-09-30, après la publication de la 1.3.0 ; décisions du propriétaire du même jour (recommandations 1 et 2 sur l'assembly, les trois volets des langues longues, la page d'export). Lots 1, 2, 3, 4 et 6 faits ; lot 5 abandonné par le propriétaire le 2026-10-01.

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

- [x] Contrôle : la sonde se vérifie elle-même au départ de chaque langue sur une boîte qu'elle fait
  déborder exprès, et échoue si elle ne la voit pas. Première passe (2026-09-30, 4 langues, 1280 et
  390 px, 320 pages) : 77 textes hors de leur boîte, en 19 formes.
  - Badges (`/composants/badges`, 390 px, les quatre langues) : un badge (`white-space: nowrap`) se
    laissait écraser sous son texte par une rangée qui ne passe pas à la ligne, jusqu'à 19 px.
  - Paragraphe de la démonstration de coquille (`/composants/coquille`, `coquille-application`,
    `/personnalisation`, 390 px et en grec à 1280 px) : le menu latéral `push` ouvert ne laisse que
    68 px au contenu, et un mot comme « Darstellung » (71 px) en sortait, jusqu'à 40 px.
  - Libellés des boutons d'une rangée `Overflow="Collapse"` (`/composants/mise-en-page`, 390 px) :
    rognés à 1 px exprès pour rester lus ; faux positif, exempté par la sonde (boîte de 1 px sous
    `clip-path`).
  - Corps de carte à 1 px (`/composants/mise-en-page`, finnois, 390 px).
  - Écartés en réglant le détecteur, avant la liste ci-dessus : 2 à 3 px en hauteur sur les titres et
    boutons (la boîte de la police dépasse une hauteur de ligne serrée ; seuil passé à une demi-ligne)
    et les libellés SVG des graphiques, placés par coordonnées : qu'un libellé long tienne dans son
    graphique n'est mesuré nulle part.
- Portée réelle : la page telle qu'elle se rend, menus, listes et dialogues fermés, en quatre langues.
- Ajout du 2026-10-01 (revue de session) : la page ne doit pas défiler en largeur, puisqu'un badge
  qui garde son texte entier peut pousser sa rangée hors de la fenêtre ; une largeur passagère (une
  notification qui entre) est remesurée 600 ms plus tard. Première passe : la page des boutons
  défilait de 415 px à 1280 px, en français aussi (rangées de démonstration sans `Wrap`) ; à 390 px,
  les superpositions et les retours (rangées sans `Wrap`, dont une infobulle dont la largeur dépend
  du chargement de la police) et les listes (texte masqué d'`OmniKanban` placé en absolu hors du
  conteneur qui défile, 52 px).

### Lot 4 : corrections dans le paquet

Là où la sonde trouve un débordement : `hyphens: auto` et `overflow-wrap` sur les libellés, titres,
alertes et cartes ; barres de boutons qui passent à la ligne ; largeurs fixes remplacées par des
largeurs qui suivent le contenu.

Fait : `overflow-wrap: break-word` sur `.omni-theme-scope` (le code garde ses lignes) ;
`.omni-badge` ne rétrécit plus (`flex-shrink: 0`) ; `.omni-kanban__columns` est positionné, pour que
le texte masqué de ses en-têtes défile avec lui ; les rangées de badges, de boutons, des
superpositions et des retours de la démonstration passent à la ligne (`Wrap`). La césure
automatique (`hyphens: auto`) a été posée puis retirée le 2026-10-01 : les navigateurs de vérification (Chromium intégré,
Edge sans tête) n'ont aucun dictionnaire (un mot long de 60 px reste sur une ligne en allemand comme
en anglais), si bien que son rendu n'a jamais été observé ; seul `overflow-wrap` est mesuré. La
reprendre demande une mesure dans un navigateur doté des dictionnaires.

- [x] Contrôle : sonde `Languages` validée sur 320 pages (de, fi, el, hu à 1280 et 390 px), aucun
  texte hors de sa boîte, aucune page qui défile en largeur (2026-10-01), aucune violation CSP,
  console propre.
- [x] Décision du propriétaire (2026-10-01) : pas de `push` sur téléphone. Sous 40rem, un
  `OmniSidebar` `Push` que l'hôte peut fermer (`OpenChanged` lié) se superpose au contenu, voile
  compris, et se ferme par le voile, Échap ou le choix d'une entrée ; celui que l'hôte ne ferme jamais
  reste poussé, puisqu'un voile que rien ne lève bloquerait la page. La largeur est suivie par
  `watchNarrow` (`omni-focus.js`). Mesuré dans la vitrine (`/composants/coquille`) : à 390 px, menu
  superposé, voile, croix sur le bouton, 322 px de contenu au lieu de 68, Échap le ferme ; à 1280 px,
  poussé, hamburger.

### Lot 5 : garde sur la longueur des traductions

Un test signale les textes des emplacements étroits (boutons, onglets, en-têtes) dont la traduction
dépasse 1,6 fois le texte français ; la traduction trop longue reçoit une variante plus courte.

Mesure avant d'écrire le test (textes du paquet de 20 caractères au plus, sans argument, seuil le plus
grand de 1,6 fois et de 6 caractères de plus) : 245 traductions dépassent, dans les 23 langues. Ce
sont pour la plupart des traductions justes et plus longues par nature (« Casse » devient
« Groß-/Kleinschreibung », « Rose » devient « Vaaleanpunainen »), que la sonde du lot 3 ne voit pas
déborder. Un test sur le seul rapport de longueurs demanderait 245 exceptions ou 245 raccourcis faits
sans locuteur natif.

- [x] Décision du propriétaire (2026-10-01) : lot abandonné au profit de la sonde `Languages`, qui
  mesure le rendu (320 pages en quatre langues, à 1280 et 390 px).

### Lot 6 : page d'export

`docs/data-components.md` : pour une grille, la barre `ExportFormats` ; `OmniMarkdownExportButton`
pour un export hors grille.

- [x] Contrôle : la page ne présente plus le bouton comme l'export d'une grille (`docs/data-components.md`,
  `docs/component-families.md`).
