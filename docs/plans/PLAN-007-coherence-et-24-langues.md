<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-007 : Passe de cohérence 1.2.0 et traduction en 24 langues

> Statut : **terminé** le 2026-09-29, 12 lots sur 12 (lot 1 : 240d4f3 ; lots 2 à 11 : 60315bb ; lot 12 : 24fbdef, 879bba8, 9889582), CI verte. Établi le 2026-09-29 après la passe de cohérence du même jour (5 revues, environ
> 150 constats) ; exécuté en parallèle par sous-agents, un propriétaire par chantier.

## Objectif

La 1.2.0 sort cohérente : aucun doublon de composant ou de paramètre, un seul nom par concept, les
défauts d'accessibilité et les écarts de doc corrigés, la doc XML livrée, et les textes du paquet comme
de la vitrine disponibles dans les 24 langues officielles de l'Union européenne.

## État des lieux (2026-09-29)

Constats détaillés dans la passe de cohérence (8 lots) ; 16 d'entre eux sondés dans le code, tous
exacts. Le paquet a 619 clés de ressources (fr neutre, en), la vitrine 542, figée en `fr-FR`.

## Décisions (validées le 2026-09-29)

- La version reste **1.2.0** ; `docs/versioning.md` reçoit une section « Rupture assumée : 1.2.0 ».
- Ruptures faites directement, **sans alias `[Obsolete]`** : le propriétaire est le seul consommateur.
- Givre garde la direction **B** (verre aérien, palette Opale).
- 24 langues : `fr` (neutre), `en`, `bg`, `cs`, `da`, `de`, `el`, `es`, `et`, `fi`, `ga`, `hr`, `hu`,
  `it`, `lt`, `lv`, `mt`, `nl`, `pl`, `pt`, `ro`, `sk`, `sl`, `sv`. Cultures neutres (`pt`, pas `pt-BR`).
- La vitrine reçoit un sélecteur de langue (les 24) : le resx anglais n'est plus mort.
- Règles de nommage (écrites dans `docs/public-api-conventions.md`) :
  - Nom accessible : `Label` (`string?`, `null` = texte localisé par défaut). Plus aucun paramètre
    `AriaLabel`. Composant à déclencheur : `Label` nomme le déclencheur, `MenuLabel` (ou `PopupLabel`
    pour un popover) nomme la surface ouverte.
  - Texte optionnel : `string?`, jamais `string = ""` comme sentinelle.
  - Liaison : `Value` / `ValueChanged` pour toute valeur ou sélection, y compris multiple.
  - Événement d'action : `On` + sujet + verbe au présent (`OnClick`, `OnRowClick`, `OnItemMove`,
    `OnAppointmentMove`, `OnTaskClick`, `OnCopy`) ; échec : `On…Error` (`OnLoadError`, `OnSearchError`) ;
    liaison : `XChanged`. `OnClick` est partout `EventCallback<MouseEventArgs>`.
  - Délégués : clé `KeyOf`, valeur vers texte `FormatValue`.
  - Recherche dans les options : `Filterable` ; texte indicatif : `FilterPlaceholder`.
  - Retour à la ligne : `Wrap`. Durée : `TimeSpan`. Occupation : `Busy`. Repli : `Expanded`.
  - Fragments : sans contexte `…Content`, par élément `…Template`.
  - Collections en paramètre : `IReadOnlyList<T>`.
  - Ton : `OmniTone { Neutral, Accent, Info, Success, Warning, Danger }` (Badge, ProgressBar, intention
    de dialogue, item de menu) ; remplissage : `OmniFill { Tonal, Outline, Solid }` (Alert, Badge).
    `OmniSeverity` reste la sévérité ; `OmniButtonVariant` reste l'emphase du bouton.
  - `@attributes` d'abord, attributs propres au composant ensuite (le composant gagne) ; `class` et
    `id` en minuscules refusés (garde runtime et OE0001), `Class`/`Id` existent. `Class` va sur
    l'élément le plus externe ; `Id` va sur le contrôle focusable.
- Un seul `OmniMenuItem` pour tous les menus (débordement, contextuel, bouton scindé, profil), un seul
  moteur JS de menu.
- Densité de l'apparence : `OmniDensity` (3 valeurs), plus de `DensityLevel` 1 à 10.
- Jetons CSS jamais lus par la feuille (`*-fill-hover/-active`, `on-*`, `info-subtle`) : conservés,
  documentés comme contrat public pour les composants des hôtes.
- Doc XML générée et obligatoire (CS1591 en erreur à la clôture).

## Lots

Chaque lot a un propriétaire unique (fichiers disjoints). Le CHANGELOG, `docs/public-api.txt` et les
commits restent à l'intégrateur.

### Lot 1 - Givre B (intégrateur) (fait, 240d4f3)
- [x] Construire, tester et commiter la direction B déjà appliquée.
Controle : build 0 avertissement, suite unitaire verte, commit poussé sur `develop`.

### Lot 2 - Traduction du paquet (4 sous-agents, 5 à 6 langues chacun) (fait, 60315bb)
- [x] `src/OmniEurope.Blazor/Resources/AppStrings.<lang>.resx` pour les 22 langues nouvelles.
- [x] Test de parité : chaque culture a exactement les clés du neutre, mêmes marqueurs `{n}`.
Controle : test de parité vert pour 24 cultures ; satellite chargé pour `de` dans un test bUnit.

### Lot 3 - Vitrine multilingue (1 agent pour le sélecteur, 4 sous-agents pour les textes) (fait, 60315bb)
- [x] Sélecteur des 24 langues (endonymes), culture mémorisée, données ICU complètes.
- [x] `ShowcaseStrings.<lang>.resx` pour les 22 langues nouvelles, parité testée.
Controle : la vitrine rendue en `de` et en `el` affiche leurs textes (capture CDP) ; parité verte.

### Lot 4 - Doc et CHANGELOG existants (lot 6 de la passe) (fait, 60315bb)
- [x] Corrections du CHANGELOG, `versioning.md` (rupture 1.2.0), README, AGENTS.md, CONTRIBUTING.
- [x] `build-and-ci-pitfalls.md`, `testing.md`, `test-config.md`, `csp-contract.md`, `analyzers.md`,
      `dependencies.md`, `localization.md`, `accessibility-contract.md`, `architecture.md`, `code-rules.md`.
- [x] Docs de composants : Pastel, Essentiel, Aplat, `ShowSeconds`, pager, filtres, tableau cassé,
      phrase tronquée ; 26 composants non documentés ; famille Theming.
- [x] Plans : PLAN-005 (SCR-001 réglé par dce120d), PLAN-004 terminé, liens ADR-002, références PLAN-008.
Controle : `grep` sans « Défaut + Défaut », « PLAN-008 » hors historique daté, « 110/110 », `#c77ddf`.

### Lot 5 - Feuille de style (lot 5 et volet CSS du lot 1) (fait, 60315bb)
- [x] Barres latérales retirées, pager courant stylé, `--omni-color-primary` et `--omni-color-border-strong`.
- [x] Anneau partagé sur tous les contrôles, anneau invalide et anneau intérieur en jetons.
- [x] Remise à zéro des jetons de forme en portée imbriquée, échelle de z-index et de durées.
- [x] Valeurs en dur remplacées par les jetons, règles doublées ou mortes, RTL, cibles 44 px en compact.
- [x] Suites Givre : couleur de série 7, givre en un seul crochet, isolation, surfaces givrées.
Controle : sondes Density et Contrast vertes, aucune règle `border-inline-start`/`inset` d'accent.

### Lot 6 - Actions, Overlays, Navigation (fait, 60315bb)
- [x] `OmniMenuItem` unique et moteur JS commun ; `OmniOverflowMenu` `Open`/`OpenChanged`.
- [x] Popover, SplitButton, ContextMenu, ProfileMenu alignés ; `OmniDialog` (croix, `CloseOnEscape`,
      `Intent` en `OmniTone`) ; `OmniNotification.CloseLabel` ; `OmniTooltip.TabIndex` ; `OmniLink rel`.
Controle : tests bUnit clavier des quatre menus verts ; démo du menu contextuel au clavier.

### Lot 7 - Feedback, Forms, Foundation, Layout, Theming, bases (fait, 60315bb)
- [x] `OmniTone`, `OmniFill` ; Badge, ProgressBar (faux 25 %), Alert, StatusStrip, FormField.
- [x] Règle `@attributes`, refus de `class`/`id`, OE0001 étendu ; aide `LocalizeOr`.
- [x] Header, AppearanceSettings et AppearanceWindow, SelectableCard (+ groupe), TemplateForm.
Controle : tests des bases et de l'analyseur verts ; plus aucun `class=` minuscule sur un `Omni*`.

### Lot 8 - Data (fait, 60315bb)
- [x] Grille : `LoadRequested`, `RowClick`, `KeyProperty` retirés, textes par instance retirés, noms.
- [x] Filtres (attributs, `OmniTextMatch`), pager, arbre, journal, kanban, contrat chargement/erreur.
Controle : suite Data verte, virtualisation et chargement distant couverts.

### Lot 9 - Selection, Editor (fait, 60315bb)
- [x] DropDown, Autocomplete (clavier combobox), MultiSelect, listes, Slider, Rating, ListBox, sélecteurs.
- [x] Éditeurs : `aria-label`, `Wrap`, CodeBlock/CodeViewer, `ReadOnly`/`Disabled`.
Controle : tests clavier de l'autocomplétion verts ; libellé de champ annoncé (test ARIA).

### Lot 10 - Scheduling, Charts, Diagram, Pages (fait, 60315bb)
- [x] `Clock` partout, Scheduler (ARIA mois, culture), Gantt, timelines, énums identiques.
- [x] Graphiques : `OmniBarSeries` fusionnée, valeurs accessibles, culture des nombres, légende.
Controle : suites concernées vertes ; nom accessible d'une jauge contient sa valeur.

### Lot 11 - Vitrine, tests, outillage (lot 7 de la passe) (fait, 60315bb)
- [x] Liens morts, `DemoCatalog` introuvable, exclusion obsolète, alias de palette, en-tête généré.
- [x] Sonde carte mentale branchée, `Test-Package` sur tous les fichiers statiques, compteurs uniques,
      paires de contraste, commentaires et sélecteurs morts, `/documentation` sondé.
Controle : `Test-ShowcaseHost.ps1` (5 sondes) vert ; `Test-Package.ps1` vérifie chaque fichier de `wwwroot`.

### Lot 12 - Clôture (après les lots 2 à 11) (fait)
- [x] Doc XML de chaque membre public, `GenerateDocumentationFile`, CS1591 en erreur.
- [x] Constantes des chemins de modules JS ; docs de composants resynchronisées avec les renommages.
- [x] Clés ajoutées pendant les lots traduites dans les 22 langues.
- [x] Textes des démos de la vitrine passés en ressources (environ 915 clés) et traduits ; 20 défauts relevés pendant la doc XML corrigés avec test (19 corrigés, 1 non avéré).
- [x] CHANGELOG, baseline d'API, build, tests, sondes (5 vertes), CI verte sur `develop` au commit 9889582.
Controle : CI verte sur `develop`, build 0 avertissement, `Test-PublicApi.ps1` vert.

## Ordre et dépendances

Lot 1 d'abord. Lots 2 à 11 en parallèle, fichiers disjoints. Lot 12 en dernier.

## Critère de clôture

Chaque lot a sa preuve, CI verte, et le registre `docs/plans/README.md` déplace le plan en « livrés ».
