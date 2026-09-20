<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-004 : Réunion des branches Aetheus, bande SDK libre, 10 thèmes et 10 palettes

> Statut : en exécution depuis le 2026-09-18 (journal : `PLAN-004-execution-log.md`). Rédigé le 2026-09-17 sur `develop` (`8314ce1`), finalisé le même
> jour après les décisions de l'utilisateur et la validation de la maquette, puis révisé le 2026-09-18
> sur huit passes de revue de la maquette (T17 à T24), la dernière couvrant les 10 thèmes en clair et
> en sombre. Ce plan est clos côté spécification : rien n'y reste en attente d'une décision.
> Référence visuelle obligatoire : `PLAN-004-maquette-themes.html`, validée par l'utilisateur.
> L'ouvrir dans un navigateur avant d'écrire la moindre ligne des lots 4 à 9.
> `plans/` est ignoré par Git (`.gitignore:16`) : ce fichier est local, il ne voyage pas avec un clone.
> Ce plan est écrit pour être exécuté par un autre agent sans le contexte de la conversation d'origine.
> Tout ce qui est marqué **À VÉRIFIER** est une information non prouvée par l'auteur du plan.

## Objectif

1. Ramener dans `develop` tout le travail OE fait pour Aetheus, afin de n'avoir qu'une base.
2. Terminer les composants du lot 9 laissés en `wip`, pour qu'ils passent les gardes du dépôt.
3. Permettre de construire le dépôt avec n'importe quel SDK .NET 10, sans épingler une bande.
4. Remplacer les 20 thèmes actuels (couleur et forme soudées) par **10 thèmes de forme** et
   **10 palettes de couleurs** combinables librement, chacun en clair et en sombre, avec des
   contrastes prouvés, états de survol compris.
5. Faire du thème `Défaut` **l'apparence livrée** du paquet, et non plus un simple preset.

## Règles à respecter pendant tout le plan

- Lire `AGENTS.md` et `docs/code-rules.md` avant d'écrire du code. Aucun bloc `@code` dans un
  `.razor` livré (`GEN004` est une erreur), CSP stricte : aucun attribut `style`, les valeurs passent
  par le CSSOM (`omni-theme.js`).
- Aucun outil Python (`python`, `pip`, `py`, `pytest`, `semgrep`, `lizard`).
- Zéro faux vert : ne jamais affaiblir un test, un budget, un avertissement ou le restore verrouillé
  pour obtenir un passage. Un échec se rapporte tel quel.
- Une autre session peut travailler en même temps sur ce dépôt. Avant chaque lot : `git status`.
  Ne jamais toucher un fichier non commité qui n'est pas le sien. Builds et tests en séquence, un
  seul nœud MSBuild (`-m:1`) : deux builds simultanés se disputent la feuille générée (RET-002 n°59).
- **Tout se passe sur `develop`.** Ne créer aucune branche de travail, de finition ou de secours, et
  ne pas proposer d'en créer. Ce qui est cassé se répare sur `develop`. Jamais de push sur `main`.
  Commits courts, un par lot ou sous-lot, message en anglais au format Conventional Commits.
- **Standardisation.** Pas d'écart ponctuel sans justification écrite. Un composant, une variante ou
  un jeton se comporte partout de la même façon ; une exception se motive dans le code ou la doc, au
  même endroit que la règle à laquelle elle déroge. Exemple du travers à éviter, relevé dans Aetheus :
  l'action principale du tableau de bord est rendue en `ButtonStyle.Info` au lieu de la variante
  primaire (`Aetheus/src/Aetheus.Front/Pages/Dashboard/Home.razor:16`), ce qui fait porter à une
  couleur d'information le rôle d'une action principale. Si un thème rend la variante primaire
  inutilisable au point qu'on lui préfère une autre, c'est le thème qu'il faut corriger.
- **Aucune barre latérale.** Jamais de trait, liseré ou barre d'accent sur le bord d'un élément
  (alertes, lignes sélectionnées, cartes, éléments de menu) : l'utilisateur l'a refusé plusieurs fois.
  Un état se marque par le fond, une pastille, une icône ou le poids du texte.
- **Tout ce que la maquette montre est à porter dans le paquet.** Elle est la spécification visuelle,
  pas une proposition : ce qui y figure n'est pas à rediscuter, seulement à porter et à prouver.
- **Le numéro de version ne s'écrit qu'au lot 11.** `<Version>` dans
  `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj` reste `1.0.0` jusque-là, puis passe à `1.0.1` (D3).
- Hors périmètre, à ne pas toucher : la publication NuGet, les dépôts Aetheus, Bellwether et Orpheus.
- Commandes de validation (section Validation d'`AGENTS.md`) :
  `dotnet restore OmniEurope.Blazor.slnx --locked-mode`, puis
  `dotnet build OmniEurope.Blazor.slnx --configuration Release --no-restore -m:1`, puis
  `dotnet test OmniEurope.Blazor.slnx --configuration Release --no-build`.
  Contrôler le **nombre de tests exécutés**, pas seulement le code de sortie (RET-002 n°32).
  Les autres portes sont cataloguées dans `docs/test-config.md` (`eng/Test-*.ps1`).

## Décisions de l'utilisateur

| # | Décision | État |
|---|---|---|
| D1 | Fusionner `feature/aetheus-migration` dans `develop` avant la refonte des thèmes | **Accordée** |
| D2 | Le thème `Défaut` devient **l'apparence livrée** : ses valeurs remplacent celles de `:root` et des blocs `[data-omni-theme]` de la feuille. Conséquence assumée : tout consommateur qui ne pose pas de `OmniThemeScope` change d'aspect, Bellwether et Aetheus compris | **Accordée** |
| D3 | Version `1.0.1`. Motif : ce sera **la version par défaut**, les deux versions publiées (`0.1.0-alpha.1` et `1.0.0`) étant délistées par l'utilisateur. La rupture n'atteint aucun consommateur réel, car personne ne consomme le paquet NuGet : Bellwether (`Bellwether/Bellwether.csproj:69`) et Aetheus (`OmniEuropeBlazorRoot`) passent tous deux par `ProjectReference`. Nuance à ne pas maquiller : délister n'est pas supprimer, un projet qui épinglerait `1.0.0` continuerait de le restaurer. Cela déroge à la lettre de `docs/versioning.md` (« à partir de `1.0`, une suppression ou une modification incompatible exige une version majeure ») : le lot 11 doit inscrire l'exception et son motif dans `docs/versioning.md` et dans `CHANGELOG.md`, sinon la règle et le dépôt se contredisent | **Accordée**, `1.0.1`. Le numéro n'est écrit qu'au lot 11 |
| D4 | Fusionner **les deux** branches d'appoint, `feature/aetheus-icons` et `feature/aetheus-lot9`. Le lot 9 étant un `wip` sans test ni vitrine, sa finition devient le lot 2 de ce plan | **Accordée** |
| D5 | Liste des 10 thèmes et des 10 palettes. Prévisualisée dans `PLAN-004-maquette-themes.html`, parcourue par l'utilisateur sur Défaut, Néon, Rétro et Octet le 2026-09-17, puis les 10 thèmes revus en clair et en sombre le 2026-09-18 (T24) | **Validée** le 2026-09-17, amendée le 2026-09-18 à la demande de l'utilisateur : la palette Soleil est remplacée par Braise (T23). Le lot 5 implémente la liste telle quelle, il ne la rouvre pas |

Un lot qui dépend d'une décision ouverte s'arrête et la pose, il ne la tranche pas.

## Contexte vérifié

### Branches

| Branche | Worktree | Écart | Contenu |
|---|---|---|---|
| `feature/aetheus-migration` | `C:\Dev\OmniEurope.Blazor.worktrees\aetheus` | 7 commits d'avance, 2 de retard | Composants de page (en-tête, fil d'Ariane, coquille de détail, connexion, état vide, assistant), glyphes Phosphor, classes `omni-u-*`, **+307 lignes dans `omnieurope.blazor.css`**, jetons clairs rehaussés (RET-002 n°51, À VÉRIFIER dans le diff). C'est la branche qu'Aetheus compile (`OmniEuropeBlazorRoot`). |
| `feature/aetheus-icons` | `...\aetheus-icons` | 1 commit absent de `migration` : `635e664` | Fermeture de dialogue opt-in, clamp numérique, variante et icône de split button, `CollapsedChanged` du fieldset, `MaxHeight` de grille. Fini et additif. |
| `feature/aetheus-lot9` | `...\aetheus-lot9` | 1 commit absent de `migration` : `a9f3cb0` | `wip` déclaré inachevé, 40 fichiers, 2 645 insertions, **aucun fichier sous `tests/` ni `site/`**. Voir inventaire ci-dessous. |

Les trois worktrees étaient propres le 2026-09-17. `develop` possède deux commits absents de
`migration` : `8314ce1` et `6e36dd4`.

### Inventaire de `feature/aetheus-lot9` (`a9f3cb0`)

Composants publics nouveaux : `OmniKanban` (+ `OmniKanbanColumn`, `OmniKanbanMove`), `OmniGitGraph`
(+ `OmniGraphDirection`, `OmniGraphLayout`, `OmniGraphLayoutEdge`, `OmniGraphLayoutNode`,
`OmniGraphLayoutOptions`, `OmniGraphLayoutResult`, `OmniGraphPoint`), `OmniDiffViewer`,
`OmniStatusStrip` (+ `OmniStatusStripItem`, `OmniStatusStripShape`), `OmniBootSplash`,
`OmniStepTimelineColumn`. Composants modifiés : `OmniMindMap` (disposition en couches),
`OmniStepTimeline`, `OmniStepTimelineStep`. Interne : `GitGraphLayout`, `GraphLayeredLayout` (580
lignes), `MindMapGeometry`, `StepTimelineLayout`, trois ponts d'interop. JavaScript : `omni-boot.js`,
`omni-kanban.js`, modifications de `omni-code-editor.js`, `omni-mindmap.js`, `omniInterop.js`.
Ressources : `AppStrings.resx` et `AppStrings.en.resx`.

Le message du commit dit lui-même : « The lot is not finished; gates and browser proofs were not run. »
`AGENTS.md` rend obligatoire la couverture de vitrine de tout composant public et de toute valeur
d'énumération, par garde. **La fusion seule casse donc le build** tant que le lot 2 n'est pas fait.

### SDK

- `global.json` : `10.0.401`, `rollForward: latestPatch`. `eng/Test-SdkBand.ps1` refuse toute autre bande.
- Cause : `Microsoft.CodeAnalysis.CSharp 5.9.0` dans `Directory.Packages.props`, référencé seulement par
  `eng/OmniEurope.Analyzers` (netstandard2.0, `GEN001` à `GEN008`) et `eng/OmniEurope.Analyzers.Tests`.
  Un analyseur ne peut pas référencer un compilateur plus récent que celui du SDK qui le charge
  (`CS9057`, voir `docs/mistakes.md`).
- L'analyseur n'est **pas** livré dans le NuGet. Un consommateur du paquet n'a aucune contrainte.
  Bellwether et Aetheus la subissent parce qu'ils référencent OE par `ProjectReference`, ce qui
  embarque `Directory.Build.props`, donc l'analyseur. Aetheus est en `10.0.202` + `rollForward: feature`.
- SDK installés sur le poste : `10.0.202`, `10.0.303`, `10.0.401`.
- `docs/reproducibility.md` : l'empaquetage déterministe (`DeterministicTimestamp`) exige `10.0.400+`.
  Cela concerne `dotnet pack`, pas le build.

### Thèmes aujourd'hui

- `src/OmniEurope.Blazor/Internal/Theming/ThemeCatalog.cs` : 20 `ThemeDefinition`. Chacune porte 5
  couleurs (accent, succès, info, avertissement, danger), 4 valeurs de surface et de texte (clair et
  sombre), un `DarkAccent` optionnel et un dictionnaire `Shape` (rayons, bordures, ombres, polices,
  boutons, titres).
- `ThemePresetFactory.cs` dérive pour chaque mode les 27 jetons de couleur en **poussant** les couleurs
  jusqu'aux ratios WCAG (4,5 texte et texte sur aplat, 3,0 accent), puis pose `Shape` par-dessus.
- API publique : `OmniThemePreset(Name, Description, Light, Dark)` (record), `OmniThemePresets.All`,
  `OmniThemeScope.Preset`. Application par `wwwroot/omni-theme.js` (`apply`/`clear`, CSSOM, suit
  `prefers-color-scheme` en mode système).
- Tests : `tests/OmniEurope.Blazor.Tests/ThemeScopePresetTests.cs` (dont
  `The_catalogue_ships_twenty_themes_with_both_halves`), `ShowcaseThemeTests.cs` (contrastes),
  `ShowcaseThemeStateTests.cs`, `ShowcaseCustomizerTests.cs`.
- Vitrine : `site/OmniEurope.Blazor.Showcase/Components/Pages/Customizer.razor`,
  `Theming/ThemeState.cs` (`ExportCss`), `Theming/ThemeTokenReader.cs`, `wwwroot/showcase-theme.js`.
- Jetons de forme disponibles dans la feuille : `--omni-radius`, `--omni-radius-sm`, `--omni-radius-lg`,
  `--omni-border-width`, `--omni-button-radius`, `--omni-button-border-width`,
  `--omni-button-border-color`, `--omni-button-shadow`, `--omni-button-text-transform`,
  `--omni-button-letter-spacing`, `--omni-button-font-weight`, `--omni-card-radius`,
  `--omni-card-border-width`, `--omni-card-shadow`, `--omni-shadow-md`, `--omni-shadow-lg`,
  `--omni-focus-ring`, `--omni-font-family`, `--omni-heading-font-family`,
  `--omni-heading-font-weight`, `--omni-heading-letter-spacing`, `--omni-heading-text-transform`.
  Polices : piles système uniquement, le paquet ne livre aucune webfont.

### Le défaut « survol blanc en sombre »

Cause déjà établie une fois (commentaire dans la feuille, bloc `[data-omni-theme="dark"]`) : un jeton de
couleur défini dans `:root` **sans valeur sombre** retombe sur sa valeur claire, et tout survol bâti
dessus s'allume en quasi blanc. `--omni-color-surface-hover` a été corrigé ainsi. Le bloc sombre de la
feuille ne redéfinit que 7 jetons sur les 22 jetons `--omni-color-*` de `:root` :
`surface-highlight`, `text-muted`, les quatre `*-subtle`, `inverse-surface`, les `on-*` et les
sévérités n'ont pas de valeur sombre propre. Un preset les fournit tous, la feuille nue non.
Les 33 règles `:hover` s'appuient surtout sur `--omni-color-accent` (15) et
`--omni-color-surface-muted` (13).

**Le lot 7 supprime cette classe de défaut à la racine** : les valeurs de la feuille seront générées
par la même fabrique que les presets, qui émet tous les jetons pour les deux modes.

### Leçons des rétrospectives (à appliquer, pas à relire)

Sources : `C:\Dev\Bellwether\docs\retrospectives\retro-2026-09-17-migration-omni.md` et
`C:\Users\Woluwe\.codex\worktrees\8114\Aetheus\docs\retrospectives\RET-002-migration-radzen-omnieurope.md`.

| Réf. | Leçon | Conséquence pour ce plan |
|---|---|---|
| RET-002 n°51 | Des couleurs conformes sur le papier manquaient de marge une fois peintes | Viser une marge au-dessus des seuils et mesurer dans Chromium (lots 8 et 10) |
| RET-002 n°52 | Une mesure pendant la transition de thème mélange deux thèmes | Attendre la stabilisation après chaque changement avant de mesurer (lot 10) |
| RET-002 n°17 | Les variantes de badge portent un sens, pas une couleur | Chaque palette garde succès, info, avertissement, danger distinguables entre eux |
| RET-002 n°48, n°53 | Une règle de densité a cassé le modèle de table et rogné les boutons icône | Vérifier grilles et boutons icône seuls sur les thèmes à bordures épaisses ou ombres décalées (lot 10) |
| RET-002 n°50 | Une campagne déclarée verte contenait des étapes expirées | Toute sonde doit faire échouer son propre registre d'échecs |
| RET-002 n°41, n°42 | Un `--no-restore` après déplacement d'une référence compile contre l'ancien graphe | Restaurer après la fusion avant d'interpréter une erreur |
| RET-002 n°6 | `OmniDataList` virtualisé violait la CSP par les espaceurs de `Virtualize` | Le lot 2 : tout composant du lot 9 qui virtualise ou positionne doit passer `eng/Test-Csp.ps1` et une preuve WASM |
| RET-002 n°7 | Un cycle d'ouverture dépendant de `requestAnimationFrame` restait bloqué en arrière-plan | Le lot 2 : `omni-boot.js` et `omni-kanban.js` à vérifier sur ce point |
| RET-002 n°39, n°40 | Un défaut connu a été consigné, pas maquillé en correction | Un composant du lot 9 qui ne peut pas être fini se consigne comme tel, il ne se déclare pas prêt |
| Bellwether, cause 2 | Preset applicatif, jetons historiques et feuille Radzen actifs ensemble | Un thème ne pose ses valeurs que sur `.omni-theme-scope`, jamais sur `:root`. Seule la feuille livrée écrit `:root` (lot 7) |
| Bellwether, cause 3 | bUnit vert ne prouve ni couleur peinte ni géométrie | Le verdict visuel (lot 10) est séparé du verdict fonctionnel |
| Frontière Aetheus | Couleur, état, focus et contraste appartiennent à OE | Le survol sombre se corrige dans OE, avec test de régression |

### La référence du thème `Défaut` : Radzen Material, celui de la production Aetheus

La primaire de Material **change avec le mode** : indigo en clair, violet clair en sombre. Une capture
de la production Aetheus est en sombre par défaut, on y voit donc le violet `#bb86fc` sur les liens et
les icônes, jamais l'indigo. Ne pas en conclure que l'indigo est faux. Autre piège de lecture : le
bouton bleu « Manage » du tableau de bord n'est pas la primaire, c'est
`ButtonStyle="ButtonStyle.Info"` (`Aetheus/src/Aetheus.Front/Pages/Dashboard/Home.razor:16`), donc
`#2196f3`.

Aetheus n'utilisait pas le thème « Default » de Radzen mais **Material**. Preuve :
`Aetheus/src/Aetheus.Front/wwwroot/index.html:14` charge
`_content/Radzen.Blazor/css/material-dark-base.css?v=11.3.2`, et
`Aetheus/src/Aetheus.Front/Services/RadzenAssetUrls.cs:8-9` nomme `material-dark-base.css` et
`material-base.css`. Le sombre est chargé en premier, le clair par bascule.

Valeurs lues dans le paquet `Radzen.Blazor 11.3.2` du cache NuGet local
(`~/.nuget/packages/radzen.blazor/11.3.2/staticwebassets/css/`) :

| Rôle | Clair (`material-base.css`) | Sombre (`material-dark-base.css`) |
|---|---|---|
| Primaire | `#4340d2` (indigo) | `#bb86fc` (violet clair) |
| Secondaire | `#e31c65` | `#01a299` |
| Info | `#2196f3` | `#2196f3` |
| Succès | `#4caf50` | `#4caf50` |
| Avertissement | `#ff9800` | `#ff9800` |
| Danger | `#f44336` | `#f44336` |
| Surface (carte, panneau) | `#ffffff` | `#1e1e1e` |
| Fond de page | `#f5f5f5` | `#121212` |
| Texte | `#424242` | `#e0e0e0` |
| Rayon | `4px` | `4px` |

### Trouvailles de la maquette

Treize défauts réels de la bibliothèque, un essai (T14), une amélioration des grilles (T15), un arrondi unifié (T16) et huit revues de la maquette (T17 à T24), constatés en construisant `PLAN-004-maquette-themes.html`.
La maquette porte déjà le correctif de chacun, commenté sur place : la reprendre comme référence.

| # | Constat | Correctif retenu | Lot |
|---|---|---|---|
| T1 | Un seul jeton par sévérité sert de remplissage **et** de texte. Comme il doit tenir 4,5 en texte, la fabrique assombrit la couleur de marque jusqu'à la perdre : le vert Material `#4caf50` ressort en `#327535`. Les boutons deviennent ternes, et c'est probablement ce qui a poussé Aetheus à peindre son action principale en `ButtonStyle.Info` | Un jeton de remplissage distinct (`--omni-color-success-fill` à côté de `--omni-color-success`). Le remplissage ne bouge que jusqu'à 3,0 contre la surface (WCAG 1.4.11, limite d'un composant), et c'est le **texte posé dessus** qu'on pousse jusqu'à 4,5, pas la couleur de marque. Mesuré dans la maquette : `#4340d2`, `#2196f3` et `#f44336` ressortent intacts, `#4caf50` devient `#48a64c`, seul `#ff9800` bouge vraiment (`#d07c00`), parce qu'un orange vif ne tient pas 3,0 sur du blanc | 4 |
| T2 | La sévérité « information » n'a **aucun** jeton de couleur, elle n'existe que comme `--omni-chart-color-3`, et `OmniButtonVariant` n'a pas de valeur `Info` | Ajouter `--omni-color-info`, `--omni-color-info-subtle`, `--omni-color-on-info` et `--omni-color-info-fill` (avec survol et appui), produits comme les autres sévérités, et `OmniButtonVariant.Info` **en fin d'énumération** | 4 |
| T3 | Dans le paquet, seul `--primary` a un survol (`accent-strong`) : Secondaire, Succès, Avertissement et Danger n'en ont **aucun**. Et le mélange de 14 % de la couleur du texte essayé d'abord dans la maquette ne se voyait pas sur l'indigo, l'utilisateur l'a relevé | La fabrique produit deux teintes par remplissage, `X-fill-hover` (20 %) et `X-fill-active` (34 %), qui s'**éloignent de la couleur du texte posé dessus** : le contraste ne peut que monter et l'écart se voit. Mesuré sur `Défaut` clair : `#4340d2` → `#3633a8` → `#2c2a8b`. Conséquence assumée et dite à l'utilisateur : sur un remplissage à texte sombre (vert, orange, rouge en clair), le survol et l'appui **éclaircissent**, car foncer ferait passer le texte sous 4,5. Pour les fonds de page et les lignes, garder le modèle de la grille (T8) | 4 et 7 |
| T4 | L'interlettrage ajoute une chasse après la dernière lettre, qui décale visiblement le libellé vers la gauche. Flagrant sur Néon, présent sur Ardoise, Octet et Rétro | Retrancher l'interlettrage du retrait de fin : `padding-inline: X calc(X - var(--omni-button-letter-spacing, 0em))`, et donner `0em` comme valeur par défaut au jeton, pas `normal`, pour que le `calc` reste valide. Même correctif sur les onglets | 7 |
| T5 | Une ombre dure décalée tirée de `--omni-color-text` disparaît quand le bouton est rempli de cette même couleur : Rétro rendait un bloc uni, sans relief. Vrai dans les deux modes, où le remplissage vire au jaune | Poser un liseré à la couleur de la surface avant l'ombre : `0 0 0 2px var(--omni-color-surface), 0.25rem 0.25rem 0 var(--omni-color-text)` | 7 |
| T6 | Les boutons secondaires annulaient `--omni-button-shadow`, donc perdaient la signature du thème : sur Octet, `Détails` et `Annuler` n'avaient plus de cadre en escalier et sortaient du thème au milieu des autres | `Secondary` garde le relief du thème. Seul le bouton fantôme reste plat, et c'est justifié en commentaire : il n'a délibérément pas de corps. Exactement le cas d'écart que la règle de standardisation vise | 7 |
| T7 | `.omni-progress__label` et `.omni-progress__circle` n'ont pas `flex: none`, alors que `.omni-progress__track` fait 100 % de large. Ils se font écraser sous leur contenu et le pourcentage déborde sur la barre | `flex: none` sur les deux | 7 |
| T8 | La grille de données, elle, fait déjà les choses correctement : l'état d'une ligne est un jeton `--omni-row-fill` peint sur les **cellules**, jamais un fond posé sur la ligne, pour qu'une cellule figée opaque le montre aussi ; alternance à 4 % de la couleur du texte, survol à 7 %, sélection à 14 % d'accent | Ne rien changer ici, mais prendre ce mécanisme comme modèle pour T3 et pour toute règle d'état ajoutée ailleurs | 7 |
| T9 | Aucun état d'appui dans tout le paquet : aucune règle `:active`. Un clic ne produit rien de visible, l'utilisateur l'a relevé, et il veut un appui **différent pour chaque thème** | `:active` pose `X-fill-active` et lit deux jetons de forme, `--omni-button-press-transform` et `--omni-button-press-shadow`. Sans eux : `translateY(1px)` et l'ombre du thème, c'est l'appui de `Défaut`, validé tel quel. Les neuf autres thèmes ont chacun le leur (Ardoise se creuse sans bouger, Galet s'écrase, Halo resserre son halo, Néon s'embrase, Papier donne un coup de tampon, Rétro descend dans son ombre, Octet tombe d'un cran avec une ombre de pixel, Nénuphar lance une onde, Velours s'enfonce et perd son reflet). Valeurs dans les formes de la constante `THEMES` de la maquette ; mesuré : 10 couples transformation plus ombre distincts. Le fantôme reste sans relief à l'appui | 5 et 7 |
| T10 | `Secondary` (surface blanche, bordure grise, texte de page) n'a ni corps ni couleur en clair : `Détails`, `Annuler` et les boutons icônes étaient jugés laids en clair ; en sombre, sa bordure se lisait comme un cadre. La maquette avait aussi inventé deux variantes, « neutre » et « contour », absentes d'`OmniButtonVariant` (`Primary`, `Secondary`, `Ghost`, `Danger`, `Success`, `Warning`), puis essayé une secondaire indigo que l'utilisateur a refusée | `Secondary` devient un **gris plein sans bordure**, texte de page : `--omni-color-neutral-fill`, soit un dixième du texte dans la surface (douze centièmes en sombre) puis un vingtième d'accent, pour que le gris appartienne à la palette. Survol et appui vers le texte tant qu'il reste à 4,5. `Défaut` clair : `#e4e3eb` → `#d4d3da` → `#c7c6cd` ; sombre : `#3c393f` → `#4c4a4f` → `#5a575c`. La maquette n'utilise plus que les variantes réelles, sauf `Info`, proposée en fin d'énumération (T2) | 4 et 7 |
| T11 | Voile d'occupation (`.omni-busy`) : posé à `inset: 0` sous `overflow: hidden`, il ne couvre que la boîte de remplissage. La bordure d'un pixel reste hors du voile et se lit comme un liseré clair autour d'un bouton assombri, flagrant en sombre (relevé par l'utilisateur sur « Déploiement en cours ») | `inset: calc(-1 * var(--omni-button-border-width, 1px))` et plus d'`overflow: hidden` : le `border-radius: inherit` du voile suffit à arrondir les coins | 7 |
| T12 | Cercle de progression indéterminé : `@keyframes omni-spin { to { transform: rotate(360deg) } }` part de la rotation au repos du cercle, `rotate(-90deg)`. Chaque tour parcourt 450 degrés puis saute de 90 en arrière, d'où le « reset » visible relevé par l'utilisateur | Une animation propre au cercle, `from { rotate(-90deg) } to { rotate(270deg) }` | 7 |
| T13 | L'ombre des boutons et des cartes est un noir à 22 % : jolie en clair, invisible sur une surface sombre, où l'effet de relief disparaît (relevé par l'utilisateur) | Trois jetons de mode produits par la fabrique : `--omni-elevation-shadow` (22 % en clair, 60 % en sombre), `--omni-elevation-shadow-soft` (12 %, 40 %) et `--omni-elevation-highlight`, un reflet de 7 % de blanc sur l'arête haute, en sombre seulement. Les ombres de `Défaut` les lisent au lieu d'un `rgb()` fixe | 4 et 5 |
| T14 | **Essai, réversible.** L'utilisateur aime le calque des menus de Windows 11 (fond clair, filet presque invisible, ombre large et douce, rayon de 8 px, pastilles d'icône grises) et le veut pour `Défaut` sur les cartes, tuiles, alertes, notifications, dialogues et menus, avec son pendant sombre. Il a prévenu qu'il pourrait revenir en arrière | Jetons de mode produits par la fabrique : `--omni-layer-fill`, `--omni-layer-stroke`, `--omni-layer-shadow`, `--omni-layer-shadow-deep`, `--omni-layer-acrylic`. Jetons de forme lus par les composants, avec repli sur l'existant : `--omni-card-background`, `--omni-card-border-color`, `--omni-alert-radius` (dans `Défaut`, égal à l'arrondi des boutons), `--omni-overlay-background`, `--omni-overlay-filter` (flou d'arrière-plan des surfaces flottantes), `--omni-overlay-shadow`. Seule la forme de `Défaut` les pose ; les retirer rend l'aspect précédent sans autre changement. En sombre, le calque est un cran plus clair que la page, avec un filet clair et un reflet sur l'arête haute : l'ombre seule n'y détache rien. Valeurs recréées d'après le rendu de Windows, aucun code ni fichier de Microsoft repris. Nouveau composant de maquette à reporter : la pastille d'icône `.omni-disc`, disque gris de la palette | 4, 5 et 7 |
| T15 | Grilles de données : jugées jolies, mais améliorables sur six points, tous validés par l'utilisateur dans la maquette | (1) Colonnes numériques alignées à droite (`.omni-data-grid__cell--numeric`, chiffres à chasse fixe). (2) Le cadre est le conteneur de défilement, habillé comme une carte (fond, filet, rayon, ombre du calque T14) ; `border-collapse: separate` et `border-spacing: 0`, car `border-radius` est ignoré sur un tableau en `collapse`. **À VÉRIFIER** dans le paquet, où la maquette ne fait que le supposer. (3) Séparateurs de lignes seulement sans alternance, qui code déjà la même chose. (4) Statut compact dans les listes : pastille de la couleur de remplissage suivie du texte de page (`.omni-status`), les badges pleins restant pour les statuts isolés. (5) Ligne sélectionnée : teinte d'accent, texte en 600 comme second indice indépendant de la couleur, et `aria-selected` ; **aucun trait latéral** (refusé par l'utilisateur). (6) En-tête fin et collant : pas de fond propre, texte atténué en 600, trait plus marqué que les séparateurs, `position: sticky` dans le cadre. Le mécanisme `--omni-row-fill` (T8) est conservé tel quel | 7 et 9 |
| T16 | Badges et cases à cocher prennent `calc(var(--omni-radius) / 2)`, un arrondi calculé à part qui ne suit aucun jeton : dans `Défaut` il valait 1,25 px à côté des 2,5 px du reste | Lire `var(--omni-radius-sm, calc(var(--omni-radius) / 2))` : le petit arrondi du thème, avec l'ancien calcul en repli. Test : dans `Défaut`, tous les coins arrondis de la vitrine mesurent 2,5 px, hors disques, pastilles et pilules | 7 |
| T17 | Revue de la maquette du 2026-09-18 : (a) la barre de défilement d'une grille restait sombre en mode clair ; (b) l'en-tête d'une colonne semblait sélectionné sans que ses cellules le soient ; (c) il manquait un formulaire vertical complet avec ses états, des réglages en onglets et une barre supérieure avec menu | (a) Le scope porte `color-scheme` selon le mode, comme `[data-omni-theme]` dans le paquet ; **À VÉRIFIER** que `OmniThemeScope` avec un `Preset` et `Appearance=System` le pose bien, sinon barres de défilement et calendriers natifs suivent la page hôte. Barre fine teintée du texte (`scrollbar-color`, `scrollbar-width: thin`). (b) Montrer l'option réelle `OmniDataGrid.HighlightActiveColumn` (teinte la colonne triée ou filtrée, en-tête compris) : colonne à 6 % d'accent, plus légère que la ligne sélectionnée (14 % et trait), pour qu'elles se distinguent et se superposent ; le paquet vaut 10 %, à ramener à 6 %. (c) Références visuelles pour la vitrine (lot 9) : formulaire vertical (libellé au-dessus, astérisque d'obligation, aide, erreur avec icône et `aria-invalid`, récapitulatif d'erreurs en tête, radio, case, interrupteur, date, nombre, zone de texte, liste, états désactivé et lecture seule distincts), réglages en onglets à icône sur le modèle `OmniSettingsSection` et `OmniSettingsTile` (page de réglages d'Orpheus), barre supérieure avec navigation, recherche, cloche à pastille et menu de compte | 7 et 9 |
| T18 | Revue du 2026-09-18, suite : (a) le calendrier est laid ; (b) la coquille n'utilisait pas le menu latéral configuré ; (c) le groupe radio « Stratégie de déploiement » paraissait décentré ; (d) les alertes ne reprenaient pas les couleurs des boutons | (a) **À porter** : `OmniDatePicker` rend un `<input type="date">` natif, dont la fenêtre est dessinée par le navigateur et **ne se stylise pas**. Il est remplacé par le calendrier maison de la maquette sur le calque des surfaces flottantes (semaines du lundi, mois en français, aujourd'hui entouré, date choisie pleine, Aujourd'hui et Effacer). Le portage comprend : clavier de grille (flèches, Page précédente et suivante, Début, Fin), `role="grid"`, saisie texte localisée, bornes min et max, et la vitrine le prouve au lot 10. `OmniMonthView` existe déjà, à évaluer comme base. (b) Coquille : barre supérieure avec bouton de repli, menu latéral `omni-panel-menu` à icônes qui se replie en rail d'icônes, contenu ; menu de compte fermé au repos. (c) Deux causes réelles : un `<fieldset>` se dimensionne à son contenu minimal, pas à son conteneur (il faisait 115 px et sa légende passait sur deux lignes, corrigé par `inline-size: 100%` et `min-inline-size: 0`) ; le point du radio, 7 px dans 14 px, tombait sur un demi-pixel (8 px désormais). (d) Alertes : voir T19, qui remplace ce premier dessin (le trait de début a été refusé) ; texte de page, variantes info, succès, attention et danger, bouton de fermeture ; le récapitulatif d'erreurs du formulaire les reprend. Aussi : une règle de case à cocher plus précise l'emportait sur l'interrupteur, exclu désormais par `:not(.omni-switch)`. **À VÉRIFIER** dans la feuille du paquet pour les trois défauts (c) et l'interrupteur | 7 et 9 |
| T19 | Revue du 2026-09-18, troisième passe : barres latérales refusées (alertes et ligne sélectionnée) ; alertes jugées ternes ; sélecteurs d'heure et de date et heure manquants ; téléversement de fichiers manquant | **Alertes** : fond en dégradé à 135° de la couleur de l'intention vers la surface (17 %, puis 6 % à 65 %, puis 3 %), filet à 24 % de la même couleur, reflet et ombre teintée comme les boutons, pastille d'icône pleine et ombrée, titre en 600, **texte de page** et non texte atténué (mesuré : l'atténué tombait jusqu'à 3,95 sur le haut du dégradé, sur 10 thèmes, 2 modes, 4 intentions ; le texte de page tient de 8,0 à 13,4), variantes info, succès, attention, danger, bouton de fermeture. **Heure et date et heure** : même champ que la date, panneau sur le calque des surfaces flottantes ; l'heure en deux colonnes qui défilent (heures, minutes par pas de 5), la date et heure en calendrier et colonnes côte à côte, Maintenant et Valider ; un seul panneau ouvert à la fois, fermeture par Échap. `OmniDateTimePicker` existe et sera réécrit ; `OmniTimePicker` n'existe pas et sera créé. **Fichiers**, sur les classes d'`OmniUpload` : sélection simple (champ en lecture seule et bouton Parcourir qui déclenche un `input type="file"` caché, donc la fenêtre du système), zone de dépôt d'un fichier (glisser, cliquer ou Entrée), zone de dépôt multiple avec liste : vignette d'image par `URL.createObjectURL`, taille, barre de préchargement, état Préchargé, erreur avec Réessayer, Retirer. Dans la maquette la progression est **simulée** ; dans le paquet, elle doit refléter la lecture réelle ou l'envoi, jamais un minuteur | 7 et 9 |
| T20 | Revue du 2026-09-18, quatrième passe : alertes en dégradé refusées (« pas joli, plus épais ») ; tuiles de réglages invisibles en clair ; champ de fichier et bouton séparés ; liste de pièces jointes trop longue | **Alertes pleines**, dessinées comme les boutons : fond = jeton de remplissage de l'intention, texte = `on-X-fill` (4,5 garanti par la fabrique), reflet blanc de 18 % sur l'arête haute, ombre neutre et ombre teintée, pastille d'icône en transparence de l'encre ; aucune teinte pâle, aucun dégradé, aucun trait. Remplace le dessin de T19 ; couleurs remplacées par T22. **Tuiles de réglages** : fond propre, 6 % du texte dans le fond de la carte (`#f4f4f4` environ sur `Défaut` clair), sinon identique à la carte. **Fichier simple** : champ et bouton soudés (coins intérieurs droits, aucun espace) ; cliquer le champ ou y appuyer sur Entrée ouvre aussi la fenêtre de sélection. **Pièces jointes** : voir T21 pour la liste réduite | 7 et 9 |
| T21 | Revue du 2026-09-18, cinquième passe : la liste réduite est une **option du composant**, pas un interrupteur dans l'écran ; il faut une densité réglable globalement et par section | **Liste réduite** : paramètre booléen d'`OmniUpload` (ajouté en fin de liste). Activé, sous la liste et toujours visibles : le compte (« 4 fichiers »), « Afficher tout (n) » ou « Réduire » avec un chevron qui pivote (masqué s'il y a trois fichiers ou moins), et « Tout retirer » poussé à droite ; seuls les trois fichiers les plus récents restent affichés. **Densité** : reprendre `OmniDensity` (`Compact`, `Comfortable`, `Spacious`, libellés « Compacte », « Confortable », « Aérée ») et le mécanisme `data-omni-density` déjà posé par `OmniThemeScope.Density`. Valeur globale sur le scope ; toute section, et tout composant qui en a le sens, peut porter la sienne, qui l'emporte par héritage des propriétés. Jetons à définir pour les trois valeurs (valeurs de la maquette : hauteur de contrôle 1,875 / 2,25 / 2,75 rem, marge horizontale des boutons, marge verticale des champs, des cellules de grille, des cartes, des alertes, des éléments de menu, des onglets, des tuiles, espacement du formulaire). Mesuré : bouton, cellule et champ à 30, 36 à 38, 44 à 46 px ; une section réglée seule ne change qu'elle. Paramètre `Density` à ajouter aux composants qui ne l'ont pas (`OmniButton`, champs, `OmniCard`, `OmniAlert`, menus, onglets, `OmniSettingsTile`, `OmniUpload`), nullable pour hériter ; `OmniDataGrid.Density` existe déjà. Les règles propres à un contexte gardent la main (`:where` pour la spécificité des règles de densité) | 4, 7 et 9 |
| T22 | Revue du 2026-09-18, sixième passe : compacte pas assez serrée ; densité mal appliquée (calendrier et plusieurs composants ne suivaient pas) ; alertes attention et erreur difficiles à lire (texte noir sur orange et rouge vifs) | **Compacte resserrée** : hauteur de contrôle 1,625 rem, corps 0,75 rem ; confortable et aérée inchangées. **Couverture** : tout composant qui a une taille la lit dans la densité ; jetons ajoutés `--omni-cal-cell`, `--omni-pop-pad`, `--omni-field-gap`, `--omni-check-size`, `--omni-switch-h`, `--omni-icon-box`, `--omni-badge-pad-y`, `--omni-section-pad` (valeurs des trois niveaux dans la maquette). Calendrier (cases, en-tête, pied), colonnes d'heure, bouton du champ date, badges, notifications, dialogues, menus, disques, cases, radios, interrupteurs (dimensions dérivées de la hauteur), tuiles, zone et liste de fichiers, zone de texte, barre supérieure, avatar, logo. **Contrôle** : un test parcourt la vitrine en compacte puis en aérée et échoue si un élément à dimension propre garde la même hauteur ; seuls le texte, les pastilles de statut, les barres de progression et les séparateurs en sont exemptés, liste motivée dans le test. **Alertes** (septième passe) : info et succès sur fond clair `--omni-color-X-bright` = remplissage éclairci jusqu'à ce que `--omni-color-on-bright` (`#111111`) y tienne 4,5 ; attention et erreur sur fond profond `--omni-color-X-deep` = remplissage foncé jusqu'à ce que `--omni-color-on-deep` (blanc cassé `#faf7f2`) y tienne 4,5 (`Défaut` clair : info `#2196f3` 6,04, succès `#48a64c` 6,15, attention `#a26000` 4,67, erreur `#d13a2e` 4,51). Le texte choisi par la fabrique pour `X-fill` ne convient pas ici : il devient blanc sur les palettes aux bleus ou verts sombres, et tombait à 4,35 sur Lagune. **Icônes d'alerte** : glyphe seul dans le disque (i, coche, point d'exclamation, croix), sans cercle ni triangle intérieur, dessiné centré sur (12, 12) ; disque et glyphe en tailles entières de même parité par densité (`--omni-alert-icon` 18, 24, 28 px, `--omni-alert-glyph` 12, 16, 18 px) ; la hauteur de ligne du titre et le bouton de fermeture prennent la taille du disque, donc les trois partagent le même axe (mesuré : écart 0 px dans les trois densités). **Boutons Attention et Danger, badges pleins et marque de notification d'échec** (validé par l'utilisateur) : même fond profond et texte blanc ; survol et appui foncent encore (`--omni-color-warning-deep-hover`, `-active`, idem `danger`, 20 % et 34 % vers le noir), le contraste ne peut que monter. Contrôle : 10 thèmes, 10 palettes, 2 modes, 6 états, 1 200 paires, minimum 4,50 | 4, 7 et 9 |
| T23 | Revue du 2026-09-18, huitième passe : Halo et Lavande, Galet et Forêt, Papier, Nénuphar et Lagune moins jolis en sombre qu'en clair ; Or ancien et Soleil identiques ; impression que toutes les palettes sont des nuances de vert ou de violet | **Cause mesurée** : sans accent sombre d'auteur, la fabrique éclaircissait l'accent clair en le mélangeant au blanc, donc le délavait (saturation en sombre : Forêt 24 %, Prune 23 %, Lavande 43 % ; Lagune restait sombre à 33 % de luminosité). **Palettes** : chaque palette porte un accent sombre saturé, teintes réparties sur le cercle (Braise 17°, Or ancien 46°, Forêt 145°, Lagune 172°, Électrique 186°, Océan 211°, Lavande 251°, Défaut 267°, Prune 313°), surfaces sombres teintées de leur palette ; Soleil remplacée par **Braise** (orange braise, Rétro la prend par défaut). Valeurs dans la table des palettes. **Thèmes** : un thème peut porter des réglages propres au sombre, fusionnés par-dessus sa forme (`shapeDark` dans la maquette ; dans le paquet, un bloc de jetons sous `[data-omni-mode="dark"]` du thème). Galet : carte relevée de 5 % du texte, liseré à 12 %, ombre noire à 45 %. Halo : carte teintée de 7 % d'accent, liseré et halo d'accent plus forts. Papier : filets à 58 % du texte au lieu du texte pur, criard sur fond presque noir, carte relevée de 4 %. Nénuphar : carte teintée de 6 % d'accent, liseré d'accent à 32 %, ombre noire et reflet d'accent. **Fabrique** : un remplissage où ni le blanc ni le noir n'atteignent 4,5 est poussé loin de la meilleure encre (`readableFill`) ; corrigeait Lagune clair, 3,25 sur l'accent et 4,35 sur l'info. **Audit** : les jetons sont résolus par le navigateur (un `color-mix` revient en `color(srgb ...)`), les bordures de Halo, Néon, Papier, Rétro et Octet affichaient NaN. Contrôle : 10 thèmes, 10 palettes, 2 modes, 43 paires ; seul reste l'écart connu texte atténué sur surface survolée (lot 8) | 4 et 7 |
| T24 | Revue finale du 2026-09-18 : les 10 thèmes avec leur palette par défaut, en clair et en sombre, passés en capture (boutons, alertes, cartes, menu, formulaire, calendrier, grilles, coquille) | **Boutons info et succès désaccordés des alertes** : sur sept palettes (Forêt, Électrique, Or ancien, Braise, Mono, Lagune, Prune) le bouton portait un remplissage plus sombre à texte blanc quand l'alerte de même intention était claire à texte noir. Règle unique désormais, pour boutons, badges pleins, marques de notification et alertes : info et succès sur `--omni-color-X-bright` avec `--omni-color-on-bright` (survol et appui `-bright-hover`, `-bright-active`, éclaircis de 20 % et 34 %, le contraste ne fait que monter), attention et erreur sur `--omni-color-X-deep` avec `--omni-color-on-deep`. Contrôle : 200 combinaisons, bouton et alerte de chaque intention ont exactement le même fond et la même encre ; 47 paires d'audit, seul reste l'écart connu texte atténué sur surface survolée (lot 8). **Rien d'autre relevé** sur les 20 vues : bouton fantôme de 6,2 à 21 sur la surface, bordures, calendrier, grilles et coquille conformes. **Bouton désactivé, tranché** : il reste tel quel (opacité 0,55, entre 2,0 et 2,9 de contraste effectif en clair sur Halo, Néon, Nénuphar et Papier). La norme exempte les contrôles désactivés et l'utilisateur a choisi de ne pas y toucher le 2026-09-18 : ne pas le « corriger » au lot 7 ni le compter comme échec au lot 10 | 4 et 7 |

À vérifier au lot 10, sans préjuger d'un défaut : `--omni-busy-veil-color` vaut `#000000` en dur, et le
commentaire du paquet affirme que le voile se lit dans les deux apparences. Le mesurer plutôt que le croire.

### Radzen et le droit de s'en inspirer

`radzenhq/radzen-blazor` est sous licence MIT (Radzen Ltd). Les thèmes gratuits sont en SCSS MIT, les
thèmes premium et les « swatches » sont payants. Règles : **ne copier aucun fichier ni extrait SCSS ou
CSS** de Radzen (une copie imposerait la mention MIT dans `NOTICE.md` et
`docs/third-party-packages.json`), ne pas nommer un thème ou une palette « Radzen » ni « Material »
(marques), recréer l'allure avec nos jetons. Reprendre une dizaine de valeurs de couleur et un rayon
n'est pas une copie de code : ce sont des données, pas une expression protégeable, donc aucune
attribution n'est requise. Si l'utilisateur préfère la prudence, il peut demander une mention dans
`NOTICE.md` ; ne pas en ajouter une de sa propre initiative, cela laisserait croire à une copie.

## Cible fonctionnelle

Un **thème** décide la forme : rayons, épaisseur et couleur relative des bordures, ombres et lueurs,
polices, dessin des boutons et des titres. Il n'écrit aucune couleur en dur : une ombre ou une bordure
colorée s'exprime par rapport à un jeton (`var(--omni-color-accent)`, `color-mix(...)`), pour suivre
n'importe quelle palette. Exception tolérée : les ombres neutres `rgb(0 0 0 / x%)`.

Une **palette** décide les couleurs : accent, accent sombre optionnel, succès, info, avertissement,
danger, surface et texte en clair, surface et texte en sombre. La fabrique en dérive les 27 jetons par
mode, comme aujourd'hui.

Chaque thème nomme sa palette par défaut. Toute palette s'applique à tout thème : 10 x 10 = 100
combinaisons, 200 jeux de jetons avec les deux modes. Le couple `Défaut` + `Défaut` est en plus
l'apparence livrée (D2).

### Les 10 thèmes (D5, validés sur maquette)

Les dix formes sont écrites et éprouvées dans `PLAN-004-maquette-themes.html`, constante
`THEMES` : les recopier depuis là, elles portent déjà les correctifs T4, T5 et T6 et ne contiennent
plus aucune couleur en dur. Ne pas repartir de `ThemeCatalog.cs`, dont les formes gardent des valeurs
comme `rgb(120 60 30 / 25%)`, `rgb(80 50 140 / 14%)` ou `rgb(7 40 80 / 28%)`, qui ne suivent aucune
palette.

| # | Thème | Source actuelle | Signature de forme | Palette par défaut |
|---|---|---|---|---|
| 1 | Défaut | nouveau | **Un seul arrondi partout**, celui des boutons : `0.15625rem` (2,5 px) pour `--omni-radius`, `-sm`, `-lg`, le bouton, la carte, l'alerte et les surfaces flottantes (décision de l'utilisateur le 2026-09-18, après avoir constaté quatre arrondis différents : 2,5, 8, 2 et 1,25 px). Seules exceptions, des formes et non des coins : les disques, les pastilles de statut et la piste de progression en pilule. C'est **volontairement plus petit** que les 4 px de Radzen Material, choisi par l'utilisateur le 2026-09-18 : ne pas le « corriger » vers 4 px au nom de la fidélité à Aetheus. Bordure 1 px, ombre de carte douce et neutre, boutons pleins sans relief, police système sans empattement, titres en graisse 600. Allure de la production Aetheus. **À VÉRIFIER** : casse des boutons (Material met les libellés en capitales, à confirmer sur le rendu réel avant de poser `--omni-button-text-transform`) et élévation des cartes | Défaut |
| 2 | Ardoise | Ardoise | Rayon 0 partout, aucune ombre, boutons et titres en capitales espacées | Océan |
| 3 | Galet | Galet | Boutons pilule (999 px), cartes à 1,5 rem, pas de bordure de carte, ombre diffuse avec liseré d'un pixel, police arrondie | Forêt |
| 4 | Halo | Lavande | Grandes rondeurs, boutons pilule, **halo coloré** sous les boutons et les cartes tiré de l'accent, bordure teintée d'accent. C'est le thème préféré de l'utilisateur, le conserver à l'identique côté forme | Lavande |
| 5 | Néon | Néon | Rayon court, lueurs vives d'accent autour des boutons et des cartes, bordure à 45 % d'accent, capitales très espacées, police technique | Électrique |
| 6 | Papier | Papier | Rayon 0, filets de 2 px couleur du texte, aucune ombre, empattements partout | Or ancien |
| 7 | Rétro | Rétro | Contours de 2 px couleur du texte, ombres dures décalées sans flou, capitales grasses, police géométrique | Braise |
| 8 | Octet | Octet | Rayon 0, cadres en escalier (`Staircase`), police d'écran et titres console en capitales | Mono |
| 9 | Nénuphar | Nénuphar | Coins asymétriques en feuille (`1.125rem 0.25rem`, `2rem 0.375rem`), lueur douce d'accent, pas de bordure de carte | Lagune |
| 10 | Velours | Velours | Biseaux (reflet interne clair), ombres profondes, titres à empattements | Prune |

Disparaissent comme thèmes : Terracotta, Forêt, Océan, Mono, Bonbon, Gravure, Affiche, Cahier, Sable,
Béton, Givre. Une partie survit comme palette.

### Les 10 palettes (D5, validées sur maquette)

Ordre des valeurs : accent, succès, info, avertissement, danger, surface claire, texte clair, surface
sombre, texte sombre, puis accent sombre s'il existe. Ce sont des valeurs **d'auteur** : la fabrique
les déplace jusqu'aux ratios, ne pas les ajuster à la main pour faire passer un test. Elles sont
reprises telles quelles dans la constante `PALETTES` de la maquette.

| # | Palette | Origine | Valeurs |
|---|---|---|---|
| 1 | Défaut | Radzen Material, production Aetheus | `#4340d2`, `#4caf50`, `#2196f3`, `#ff9800`, `#f44336`, `#ffffff`, `#424242`, `#1e1e1e`, `#e0e0e0`, sombre `#bb86fc`. Valeurs relevées dans le paquet, voir la section de référence ci-dessus. Le texte clair `#424242` est volontairement celui de Radzen : si la fabrique doit le pousser pour tenir 4,5 sur `#ffffff`, la laisser faire |
| 2 | Océan | Océan | `#0b63ce`, `#0f8f7d`, `#0891b2`, `#b97f00`, `#d33a3a`, `#f1f7fc`, `#0f2a44`, `#0a1a2e`, `#d6e8f7`, sombre `#5aa9ff` |
| 3 | Forêt | Galet | `#2f6f4f`, `#3c8d40`, `#2d7d9a`, `#a87a0b`, `#b5433a`, `#f3f6f1`, `#1e2d24`, `#121e17`, `#dcebdf`, sombre `#62cf8f` |
| 4 | Lavande | Halo | `#7c5cbf`, `#3f9e7a`, `#5b7fd6`, `#c48d2a`, `#d0506a`, `#fbf9ff`, `#2d2540`, `#1a1730`, `#ece7fb`, sombre `#b4a3ff` |
| 5 | Électrique | Néon | `#008c9e`, `#1f9d55`, `#7c4dff`, `#c28a00`, `#d6246e`, `#f5f3ff`, `#1a1036`, `#0b0a1a`, `#e6e3ff`, sombre `#00e5ff` |
| 6 | Or ancien | Papier | `#8a6a1c`, `#3f7a3a`, `#2f5f8a`, `#c2571a`, `#a82a2a`, `#fdfbf5`, `#1d1b16`, `#1b1813`, `#ece2c9`, sombre `#d4af37` |
| 7 | Braise | Rétro | `#c2410c`, `#2f855a`, `#2563c9`, `#a16207`, `#be123c`, `#fff3ea`, `#2a140c`, `#1f1411`, `#f8e6dc`, sombre `#ff8a5b`. Remplace Soleil, jugée identique à Or ancien (teintes mesurées 50° et 46°) |
| 8 | Mono | Mono | `#000000`, `#1a7f37`, `#0550ae`, `#8a5a00`, `#cf222e`, `#ffffff`, `#000000`, `#000000`, `#ffffff`, sombre `#ffffff` |
| 9 | Lagune | Nénuphar | `#0e8a7a`, `#2f8f46`, `#2a7fb8`, `#c07f00`, `#d1495b`, `#f3faf8`, `#10302b`, `#082127`, `#d8eef0`, sombre `#2dd4bf` |
| 10 | Prune | Velours | `#7b2d6e`, `#2f7d5b`, `#4a6fa5`, `#b7791f`, `#c23a3a`, `#fbf6f9`, `#2a1426`, `#20111e`, `#f4e6f0`, sombre `#e883d2` |

## Lots

### Lot 1 : réunir les trois branches Aetheus dans `develop`

1. Sur `develop`, arbre propre. Relever les SHA des quatre têtes.
2. `git merge --no-ff feature/aetheus-migration`. Conflits attendus : `omnieurope.blazor.css`,
   `omniInterop.js` (garder les fonctions des deux côtés), fichiers `.resx` (une seule racine XML,
   RET-002 n°19), `docs/public-api.txt`, `packages.lock.json`, SBOM.
   Résoudre selon le comportement attendu, jamais en écartant un côté en bloc.
3. Restaurer (sans `--no-restore`), puis validation complète. Commit de fusion isolé.
4. `git merge --no-ff feature/aetheus-icons` (`635e664`, fini et additif). Validation complète.
5. `git merge --no-ff feature/aetheus-lot9` (`a9f3cb0`). **Le build sera rouge** : les gardes de
   couverture de vitrine exigent une démonstration de chaque composant public et de chaque valeur
   d'énumération. C'est attendu, assumé par l'utilisateur, et c'est le lot 2 qui le referme sur
   `develop`. Ne pas désactiver ni assouplir une garde pour faire passer cette étape, et ne pas
   dévier le travail sur une branche.
6. Ne supprimer aucune branche ni aucun worktree. Ne pas modifier Aetheus : son
   `OmniEuropeBlazorRoot` pointe encore vers le worktree `aetheus`, c'est une décision de ce dépôt-là.

**Contrôle** : après l'étape 4, validation complète verte avec un nombre de tests supérieur ou égal au
plus grand des deux côtés avant fusion (871 côté `migration` d'après RET-002, à relever côté
`develop`) ; `eng/Test-Budgets.ps1`, `eng/Test-PublicApi.ps1`, `eng/Test-Csp.ps1` verts.
Après l'étape 5, consigner la liste **exacte** des gardes rouges : c'est le cahier des charges du
lot 2. `git branch --no-merged develop` ne liste plus aucune des trois branches.

### Lot 2 : finir le lot 9

Périmètre : les composants listés dans l'inventaire ci-dessus. Objectif : qu'ils tiennent les mêmes
exigences que le reste du paquet, pas qu'ils gagnent des fonctions.

1. Relire chaque composant avant d'écrire : `OmniKanban` (458 lignes de code-behind),
   `GraphLayeredLayout` (580 lignes), `OmniDiffViewer` (264 lignes), `OmniGitGraph`,
   `OmniStatusStrip`, `OmniBootSplash`, et les modifications d'`OmniMindMap` et `OmniStepTimeline`.
   Consigner ce qui est inachevé avant de coder.
2. Conventions : `docs/public-api-conventions.md` et `docs/ui-conventions.md`. Aucun `@code` livré,
   aucun attribut `style`, libellés en ressources, paramètres nommés selon les familles existantes.
3. Tests bUnit par composant : rendu, paramètres, valeurs d'énumération, accessibilité (rôles et noms
   accessibles), clavier pour tout ce qui est interactif. Le glisser-déposer du Kanban a un chemin
   clavier obligatoire.
4. Pages de vitrine : une démonstration par composant, couvrant chaque valeur d'énumération, sinon les
   gardes restent rouges. Ressources françaises et anglaises.
5. JavaScript : `omni-boot.js`, `omni-kanban.js` et les modifications de `omni-code-editor.js` et
   `omni-mindmap.js` doivent passer la CSP stricte (aucun attribut `style`, aucun `eval`). Vérifier
   qu'aucun cycle fonctionnel ne dépend uniquement de `requestAnimationFrame` (RET-002 n°7).
   Vérifier la libération des `DotNetObjectReference` à la destruction (RET-002 n°49).
6. Documentation : `docs/data-components.md`, `docs/diagram-components.md`,
   `docs/editor-components.md`, `docs/foundation-components.md` selon la famille, plus `CHANGELOG.md`.
7. Un composant qui ne peut pas être fini honnêtement est **retiré du lot avant la fusion**, pas
   livré à moitié (RET-002 n°39, n°40). Le signaler à l'utilisateur.

**Contrôle** : toutes les gardes listées à la fin du lot 1 sont vertes, sans qu'aucune n'ait été
assouplie (le montrer par `git diff` sur les fichiers de garde : il doit être vide, ou motivé).
Validation complète verte, `eng/Test-Csp.ps1`, `eng/Test-PublicApi.ps1`, `eng/Test-Budgets.ps1`,
`eng/Test-WasmHost.ps1` verts. Preuve navigateur pour le Kanban (déplacement souris et clavier), le
graphe Git, le comparateur de différences et l'écran de démarrage. Nombre de tests avant et après.

### Lot 3 : libérer la bande SDK

1. Abaisser `Microsoft.CodeAnalysis.CSharp` à la version Roslyn livrée avec le SDK `10.0.100`
   (**À VÉRIFIER** : `5.0.0`, à confirmer sur nuget.org ou dans `sdk\10.0.202\Roslyn`). Règle : un
   analyseur référence le compilateur le plus ancien qu'il doit supporter.
2. `global.json` : `version` `10.0.100`, `rollForward` `latestFeature`, `allowPrerelease` `false`.
3. `eng/Test-SdkBand.ps1` : la règle devient « majeure 10, mineure 0, SDK supérieur ou égal au
   plancher ». Garder le script et son appel en CI, ne pas le supprimer. Le test de convention qui
   dérive de `global.json` suit.
4. `eng/dependency-policy.json` : bloc `toolchain` aligné, raison du statut `toolchain-bound` de
   Roslyn réécrite (plancher de bande, plus « bande épinglée »).
5. Empaquetage : si la porte de déterminisme exige `10.0.400+`, l'exprimer comme garde du seul job de
   pack (`ci.yml`, `eng/Test-Package.ps1`), pas dans `global.json`.
6. Docs : `docs/reproducibility.md`, `docs/dependencies.md`, `docs/mistakes.md` (nouvelle entrée :
   cause, correctif, garde). Fichiers de verrouillage régénérés.

**Contrôle** : build Release + tests des analyseurs verts sous **chacun** des trois SDK installés.
Forcer le SDK par un `global.json` temporaire dans une copie de travail jetable (dossier scratch),
jamais en éditant puis restaurant celui du dépôt. Sortie attendue : trois lignes
`dotnet --version` différentes, zéro `CS9057`, zéro avertissement. Contrôle négatif : avec Roslyn
`5.9.0` et le SDK `10.0.202`, `CS9057` doit réapparaître. Si un analyseur utilise une API Roslyn
absente de la version plancher, s'arrêter et le rapporter.

### Lot 4 : séparer forme et couleur dans le modèle et l'API

Conception additive, pour ne casser aucune signature publique :

- Interne : `ThemeDefinition` ne garde que `Name`, `Description`, `Shape`, `ShapeDark` (jetons de
  forme propres au sombre, fusionnés par-dessus `Shape` pour le mode sombre, T23 ; vide pour six
  thèmes sur dix) et `DefaultPalette` (nom).
  Nouveau `PaletteDefinition` avec les 9 couleurs et `DarkAccent`. `ThemeCatalog` garde les thèmes,
  nouveau `PaletteCatalog`. `ThemePresetFactory.Build` prend une palette et une forme.
- Public, nouveau : `OmniThemePalette(Name, Description, Light, Dark)` (record, dictionnaires de
  jetons de couleur par mode) et `OmniThemePalettes.All`.
- Public, étendu : `OmniThemePreset` reçoit une propriété **non positionnelle**
  `IReadOnlyDictionary<string, string> Shape { get; init; }` (vide par défaut) et une méthode
  `With(OmniThemePalette palette)` qui rend un nouveau preset : jetons de la palette, puis `Shape`
  posé par-dessus. Ne pas ajouter de paramètre au constructeur du record (rupture binaire).
- Public, étendu : `OmniThemeScope.Palette` (`OmniThemePalette?`, facultatif). Avec `Preset` non nul,
  le scope applique `Preset.With(Palette)`. Sans `Preset`, la palette s'applique seule (couleurs
  seulement). `omni-theme.js` ne change pas : il reçoit toujours deux dictionnaires.
- Attention au cache de `OnAfterRenderAsync` (`ReferenceEquals(Preset, _appliedPreset)`) : y ajouter
  la palette, sinon un changement de palette seul ne repeint pas.
- `OmniThemePresets.All` rend les 10 thèmes, chacun avec sa palette par défaut.

Deux ajouts de jetons viennent des trouvailles de la maquette. Le portage JavaScript de la fabrique,
dans la maquette, les produit déjà : s'y reporter pour l'ordre exact des opérations.

- **T1, remplissage distinct du texte.** Pour l'accent et chaque sévérité, produire `X-fill` et
  `on-X-fill` en plus des jetons actuels : `fill = Visible(couleur, surface)` (3,0 seulement), puis
  `onFill = PushApart(ReadableOn(fill), fill, 4.5)`. C'est le **texte** qu'on pousse, jamais la couleur
  de marque. Les jetons existants ne changent pas : ce qui s'écrit en texte sur la page continue de
  les utiliser, donc rien de ce qui est déjà lisible ne régresse.
- **T2, sévérité d'information.** Ajouter `--omni-color-info`, `--omni-color-info-subtle`,
  `--omni-color-on-info` et `--omni-color-info-fill`, produits comme les autres sévérités.
  `--omni-chart-color-3` continue de valoir la couleur d'information. Ajouter `OmniButtonVariant.Info`
  **après** `Warning`, jamais au milieu : les valeurs de l'énumération sont publiées.
- **T3, états des remplissages.** Pour chaque remplissage, produire `X-fill-hover` et
  `X-fill-active` en éloignant la teinte de la couleur de son texte (20 % puis 34 % vers le pôle
  opposé à ce texte). La fonction `awayFrom` de la maquette donne l'ordre exact des opérations.
- **T10, gris secondaire.** Produire `--omni-color-neutral-fill` (`neutralFill` dans la maquette) et
  ses états `-hover` (10 %) et `-active` (18 %) en le rapprochant du texte, en reculant tant que le
  texte ne tient pas 4,5 dessus (`towardWhileReadable`).
- **T13, élévation.** Produire `--omni-elevation-shadow`, `--omni-elevation-shadow-soft` et
  `--omni-elevation-highlight`, qui dépendent du mode et pas de la palette.
- **T14, calque (essai).** Produire les cinq jetons `--omni-layer-*` comme dans la maquette. Les
  garder regroupés et commentés comme essai, pour qu'un retour en arrière tienne en un commit.
- **T23, remplissage lisible.** Avant tout le reste, chaque `X-fill` passe par `readableFill` : s'il
  n'existe aucune encre (blanc ou noir) qui tienne 4,5 dessus, le pousser loin de la meilleure des deux
  jusqu'à ce qu'elle tienne. Sur `Défaut` clair, aucune valeur ne bouge (le contrôle chiffré ci-dessous
  reste valable) ; sur Lagune clair, l'accent passait de 3,25 à 4,5.
- **T22 et T24, clair et profond.** Pour info et succès : `--omni-color-X-bright` = `X-fill` poussé
  loin de `#111111` jusqu'à 4,5, `--omni-color-on-bright` = `#111111`, plus `-bright-hover` et
  `-bright-active` (`awayFrom` à 20 % et 34 %). Pour attention et danger : `--omni-color-X-deep` =
  `X-fill` poussé loin de `#faf7f2` jusqu'à 4,5, `--omni-color-on-deep` = `#faf7f2`, plus
  `-deep-hover` et `-deep-active`. Ce sont les fonds des boutons `Info`, `Success`, `Warning`,
  `Danger`, des badges pleins, des marques de notification et des alertes (règle unique, T24).
  `X-fill` et `on-X-fill` restent produits pour les usages où la marque prime (pastilles de statut,
  barres de progression, graphiques). Contrôle chiffré, `Défaut` clair : `--omni-color-info-bright`
  `#2196f3`, `--omni-color-success-bright` `#48a64c`, `--omni-color-warning-deep` `#a26000`,
  `--omni-color-danger-deep` `#d13a2e`.

**Contrôle** : tests bUnit nouveaux dans `ThemeScopePresetTests.cs` : palette seule envoyée, thème +
palette envoyés, changement de palette seul repeint, retrait de la palette revient à la palette par
défaut du thème, scope sans preset ni palette ne charge pas le script. `eng/Test-PublicApi.ps1` montre
uniquement des ajouts à ce stade. Les gardes de vitrine échoueront tant que `Palette` n'est pas
démontré : c'est attendu, le lot 9 les referme.
Contrôle chiffré de T1, à écrire comme test : avec la palette `Défaut`, en clair,
`--omni-color-accent-fill` vaut `#4340d2`, `--omni-color-info-fill` `#2196f3`,
`--omni-color-danger-fill` `#f44336`, `--omni-color-success-fill` `#48a64c` et
`--omni-color-warning-fill` `#d07c00`. Ces cinq valeurs sont celles que la maquette produit ; un écart
signale un portage infidèle de la fabrique. Même contrôle pour les états, toujours `Défaut` clair :
`--omni-color-accent-fill-hover` `#3633a8`, `--omni-color-accent-fill-active` `#2c2a8b`,
`--omni-color-neutral-fill` `#e4e3eb`, `--omni-color-neutral-fill-hover` `#d4d3da`,
`--omni-color-neutral-fill-active` `#c7c6cd`.

### Lot 5 : écrire les 10 thèmes et les 10 palettes

1. Recopier les 10 `ThemeDefinition` depuis la constante `THEMES` de la maquette et les 10
   `PaletteDefinition` depuis `PALETTES`. La liste est validée, ne pas la rouvrir, ne pas renommer,
   ne pas substituer un thème.
2. Pour `Défaut`, comparer le rendu obtenu à la production Aetheus (lancer `Aetheus/src/Aetheus.Front`
   ou sa route figée `/radzen`, qui garde le thème d'origine) : c'est la référence visuelle du thème
   `Défaut`, pas une capture du site de Radzen. Ne copier aucun CSS.
3. `Staircase` exige un rayon nul : le garder réservé à Octet.

**Contrôle** : 10 thèmes et 10 palettes présents, noms uniques, `Défaut` en premier de chaque liste,
bijection thème vers palette par défaut. Chaque valeur de forme est identique à celle de la maquette.

### Lot 6 : verrouiller la liste par des tests

**Contrôle** : test « les thèmes se distinguent » : pour chaque paire de thèmes, au moins 3 jetons
parmi `--omni-radius`, `--omni-button-radius`, `--omni-card-radius`, `--omni-border-width`,
`--omni-card-shadow`, `--omni-button-shadow`, `--omni-font-family`, `--omni-button-text-transform`
diffèrent (valeur absente = valeur par défaut de la feuille). Test « aucune couleur en dur dans une
forme » : aucune valeur de `Shape` ne contient `#` ni `rgb(` autre que `rgb(0 0 0 /` et
`rgb(255 255 255 /`. Le test `The_catalogue_ships_twenty_themes...` est remplacé par un test à 10
thèmes. Pour chaque palette et chaque mode, succès, avertissement et danger restent distinguables deux
à deux (seuil à fixer et à documenter dans le test) (RET-002 n°17).

### Lot 7 : l'apparence livrée, et les corrections relevées par la maquette (D2, T3 à T24)

Corrections de `omnieurope.blazor.css`, chacune avec son test de non-régression. Elles sont
indépendantes du changement d'apparence et peuvent être commitées avant lui.

- **T4, centrage des libellés.** `letter-spacing: var(--omni-button-letter-spacing, 0em)` (pas
  `normal`, sinon le `calc` suivant est invalide), puis
  `padding-inline: X calc(X - var(--omni-button-letter-spacing, 0em))` sur `.omni-button` et
  `.omni-tabs__tab`. Test : sur chaque thème dont l'interlettrage n'est pas nul, le retrait de fin
  calculé est inférieur au retrait de début.
- **T5, ombres dures.** Liseré à la couleur de la surface avant l'ombre décalée, dans la forme de
  Rétro. Test : la valeur de `--omni-button-shadow` de Rétro contient `var(--omni-color-surface)`.
- **T6, relief des boutons secondaires.** `Secondary` ne remet plus `box-shadow` à zéro. Le fantôme
  reste plat, avec sa justification en commentaire. Test : aucune règle de variante de bouton ne pose
  `box-shadow: 0 0 #0000` hors `.omni-button--ghost`.
- **T7, progression.** `flex: none` sur `.omni-progress__label` et `.omni-progress__circle`. Test de
  rendu : avec un libellé, la piste et le libellé ne se chevauchent pas.
- **T10, secondaire grise.** `.omni-button--secondary` : fond `neutral-fill`, texte de page, **aucune**
  bordure. Le bloc CSS des boutons de la maquette est la référence.
- **T11, voile d'occupation.** `.omni-busy::after` à `inset: calc(-1 * var(--omni-button-border-width, 1px))`,
  `overflow: hidden` retiré de `.omni-busy`. Test de rendu : en sombre, aucun liseré non voilé.
- **T12, cercle.** Animation `from rotate(-90deg) to rotate(270deg)` pour `.omni-progress__circle`.
  Test : les deux extrémités de l'animation diffèrent d'exactement 360 degrés.
- **T3, survols.** Chaque variante pleine lit `X-fill-hover` au survol, la secondaire lit
  `neutral-fill-hover`, le fantôme `surface-hover`. Pour les fonds de page et les lignes, le modèle
  de `--omni-row-fill` de la grille (T8). Test : chaque variante de bouton a une règle de survol, et
  aucune règle `:hover` ne pose une couleur littérale ni un `color-mix` avec `white` ou `#fff`.
- **T9, appui.** `:active:not(:disabled)` lit `X-fill-active`, `--omni-button-press-transform`
  (défaut `translateY(1px)`) et `--omni-button-press-shadow` (défaut : l'ombre du bouton). Transition
  de 80 ms sur `transform` et de 120 ms sur `box-shadow`, retirées sous `prefers-reduced-motion`. Le
  fantôme garde `box-shadow` nul à l'appui. Test : chaque variante a une règle `:active`, et les dix
  thèmes donnent dix couples transformation plus ombre distincts.
- **T14, calque (essai).** `.omni-card`, `.omni-alert`, `.omni-notification`, `.omni-dialog` et les
  menus flottants lisent les jetons de forme du calque avec repli sur l'existant ; `.omni-disc` rejoint
  la feuille. `backdrop-filter` est une propriété CSS, compatible avec la CSP stricte. Test : sans les
  jetons, le rendu calculé de chaque composant est identique à celui d'avant T14.
- **T15, grilles.** Reporter les six points depuis le bloc « Grille de données » du CSS de la
  maquette. `.omni-status` est un nouveau composant visuel, à exposer par une option de colonne de
  statut ou à garder en classe utilitaire : le poser à l'utilisateur, c'est un choix d'API. Test de
  rendu : cellule numérique alignée à la fin, coins du cadre arrondis, en-tête toujours visible après
  défilement, texte de la ligne sélectionnée en 600, aucune `box-shadow` ni bordure latérale sur ses cellules.

- **T16, petit arrondi.** Badges et cases lisent `var(--omni-radius-sm, calc(var(--omni-radius) / 2))`.
  Test : dans `Défaut`, tous les coins arrondis de la vitrine mesurent 2,5 px, hors disques, pastilles
  et pilules.
- **T18 (c), formulaire.** `fieldset` à `inline-size: 100%` et `min-inline-size: 0` ; point du radio
  à 8 px ; règle de case à cocher exclue de l'interrupteur par `:not(.omni-switch)`. **À VÉRIFIER**
  d'abord que la feuille du paquet a bien ces trois défauts.
- **T20, tuiles et fichier.** `.omni-settings-tile` : fond à 6 % du texte dans le fond de la carte.
  `OmniUpload` en sélection simple : champ et bouton soudés, coins intérieurs droits, clic ou Entrée
  sur le champ ouvre la sélection.
- **T21, densité.** Les jetons des trois niveaux (constante de la maquette, bloc
  `[data-omni-density]`) et les règles `:where(.stage)` transposées sur `.omni-theme-scope`. Chaque
  composant dimensionné lit la densité, calendrier compris (T22). Test : la vitrine parcourue en
  compacte puis en aérée, échec si un élément à dimension propre garde la même hauteur ; exemptions
  motivées (texte, pastilles de statut, barres de progression, séparateurs).
- **T22 et T24, alertes et intentions.** Alertes pleines : fond `X-bright` ou `X-deep` selon
  l'intention, encre `on-bright` ou `on-deep`, reflet de 14 % sur l'arête haute, ombre neutre et ombre
  teintée, glyphe seul dans un disque de `--omni-alert-icon` (18, 24, 28 px) avec un glyphe de
  `--omni-alert-glyph` (12, 16, 18 px), titre à la hauteur de ligne du disque. Boutons `Info` et
  `Success` sur `X-bright`, `Warning` et `Danger` sur `X-deep`, idem badges pleins et marques de
  notification. Test : pour chaque intention, le bouton et l'alerte ont le même fond et la même encre
  calculés, sur les 200 combinaisons.
- **T23, sombre par thème.** Les jetons de `ShapeDark` s'appliquent quand le scope est en sombre
  (bloc sous `[data-omni-mode="dark"]` ou fusion dans le dictionnaire sombre du preset, au choix du
  lot 4, mais un seul mécanisme). Test : Galet, Halo, Papier et Nénuphar en sombre donnent des jetons
  de carte différents de leur clair ; les six autres thèmes donnent les mêmes.

Puis l'apparence livrée.

Ne pas recopier les valeurs à la main dans la feuille : les **générer**, sinon elles dériveront.

1. Un test ou un générateur produit, à partir du couple `Défaut` + `Défaut`, les jetons des deux modes.
   Les blocs `:root`, `[data-omni-theme="light"]`, `[data-omni-theme="dark"]` et
   `@media (prefers-color-scheme: dark) [data-omni-theme="system"]` de `omnieurope.blazor.css`
   reçoivent ces valeurs. Les jetons hors fabrique (espacements, tailles de police, `--omni-grid-dark-*`,
   `--omni-mindmap-*`, `--omni-chart-color-5` à `7`) ne sont pas touchés ici.
2. Test de conformité : pour chaque jeton produit par la fabrique, la valeur du bloc clair de la
   feuille égale la valeur claire générée, et celle des deux blocs sombres égale la valeur sombre.
   Ce test remplace l'idée d'une liste d'exemptions : plus aucun jeton ne peut rester sans valeur
   sombre, donc la classe de défaut du survol blanc disparaît.
3. Les jetons `--omni-grid-dark-*` sont des couleurs fixes pensées pour le sombre par défaut :
   vérifier qu'elles s'accordent à la palette `Défaut` et les dériver des jetons si elles jurent.
   La grille sait déjà faire sans elles (T8), donc la question est de savoir si elles servent encore.
4. Conséquence à documenter dans `CHANGELOG.md` en tête des changements de comportement : **l'aspect
   par défaut du paquet change**. Tout consommateur qui ne pose pas de `OmniThemeScope` est repeint.
   Donner la recette pour retrouver l'ancien aspect si quelqu'un le souhaite (poser explicitement un
   scope avec les anciennes valeurs, à fournir dans le changelog ou la doc).

**Contrôle** : test de conformité vert. Contrôle négatif : changer une valeur de la feuille à la main
doit le faire échouer. `eng/Test-Budgets.ps1` et `eng/Test-Csp.ps1` verts. Capture avant et après de
la vitrine sans thème posé, en clair et en sombre.

### Lot 8 : contrastes statiques sur les 200 jeux

Étendre `ShowcaseThemeTests.cs` (ou un fichier dédié) en matrice sur les **200** jeux de jetons, pas
seulement sur les paires par défaut. Reprendre la marge de rendu introduite par RET-002 n°51 (arrivée
avec la fusion du lot 1, la lire dans le test existant plutôt que d'en inventer une). Paires
obligatoires, ratio 4,5 sauf mention :
texte sur `surface`, `surface-muted`, `surface-hover`, `surface-highlight` ;
`text-muted` sur `surface`, `surface-muted`, `surface-hover` ;
`accent-strong` sur `surface`, `accent-subtle`, `surface-muted`, `surface-hover` ;
chaque `on-X` sur `X` (accent, succès, avertissement, danger) ; `on-inverse` sur `inverse-surface` ;
chaque sévérité sur `surface` et sur son `*-subtle` ;
chaque `on-X-fill` sur son `X-fill` (T1) ;
`on-bright` sur `info-bright`, `success-bright` et leurs `-hover` et `-active` ; `on-deep` sur
`warning-deep`, `danger-deep` et leurs `-hover` et `-active` (T22, T24) ;
`accent` sur `surface` à 3,0, chaque `X-fill` sur `surface` à 3,0, et `border` sur `surface` au
plancher de 1,7 propre à OE, motivé dans `ShowcaseThemeTests.EveryPalette_KeepsItsBordersVisible`.
Écart à arbitrer et à consigner : la WCAG 1.4.11 demanderait 3,0 pour la limite d'un composant.
Les 47 paires de la maquette, dans sa constante `PAIRS`, sont le point de départ de cette matrice.
La paire texte atténué sur surface survolée est la seule que la maquette ne tient pas (4,21 à 4,47) :
c'est ici qu'elle se corrige, dans la fabrique.

La fabrique ne garantit pas aujourd'hui les paires sur `surface-hover` et `surface-highlight` : si
elles échouent, corriger **la fabrique** (dériver puis pousser), pas les valeurs d'auteur.

**Contrôle** : matrice verte sur 200 jeux, avec le nombre de cas affiché. Comme le lot 7 fait de la
feuille une sortie de la fabrique, la matrice couvre aussi l'apparence livrée.

### Lot 9 : vitrine, personnalisateur et export

- `Customizer.razor` : deux sélecteurs, thème puis palette. Choisir un thème remet sa palette par
  défaut, choisir une palette ne change pas le thème. Logique dans le code-behind, pas de `@code`.
- `ThemeState` : porte le thème et la palette. `ExportCss` exporte la combinaison, toujours sur le
  scope de thème et avec les variantes clair, sombre, système (piège du handoff : les sélecteurs
  `[data-omni-theme]` de la feuille écrasent une valeur posée sur `html`).
- `ThemeTokenReader` : les jetons de forme restent classés dans leur famille.
- Galerie : démontrer `OmniThemeScope.Palette`, sinon les gardes de couverture échouent.
- La page de combinaison thème + palette du lot 5 **reste** dans la vitrine. Ne pas la retirer, ne pas
  poser la question.
- Libellés en ressources (`.resx` français neutre et anglais), rien en dur.

**Contrôle** : `ShowcaseCustomizerTests`, `ShowcaseThemeStateTests`, `ShowcaseThemeTests` adaptés et
verts, plus : export d'un thème avec une palette étrangère contenant les couleurs de la palette et la
forme du thème. Gardes de couverture de la vitrine vertes.

### Lot 10 : preuve visuelle dans un vrai navigateur

Verdict séparé des lots précédents. Lancer la vitrine (le dépôt n'a pas de `ylaunch.ps1` ; suivre
`docs/test-config.md` et les scripts `eng/Test-WasmHost.ps1`, `eng/Test-CdpProbe.mjs`,
`eng/Serve-StaticWithHeaders.mjs`). Écrire une sonde Node (`eng/Test-ThemeContrastProbe.mjs`) qui,
par CDP, pour chaque thème x palette x mode :

1. applique la combinaison, **attend la fin des transitions** (RET-002 n°52) ;
2. parcourt une liste fermée d'éléments représentatifs : bouton plein, contour, fantôme, bouton icône
   seul, lien, onglet, élément de menu, ligne et en-tête de grille, option de liste déroulante,
   badge de chaque sévérité, alerte, champ de saisie, carte, plus une colonne de Kanban et une bande
   d'état venues du lot 2 ;
3. mesure au repos, **au survol forcé** (`CSS.forcePseudoState`) et au focus : couleur de texte
   calculée contre fond effectif (remonter les ancêtres tant que le fond est transparent, résoudre
   `color-mix`), ratio avec la marge du lot 8. Mesurer aussi un contrôle sous voile d'occupation
   (`.omni-busy`), dont la couleur est `#000000` en dur : vérifier que son contenu reste lisible en
   sombre au lieu de croire le commentaire du paquet ;
4. vérifie la géométrie : les cellules de grille restent `table-cell`, un bouton icône seul a une
   icône visible non rognée (RET-002 n°48, n°53), aucun débordement horizontal à 375 px sur
   `document.body.scrollWidth` (RET-002 n°28) ;
5. écrit un registre JSON de tous les échecs et **sort en erreur s'il n'est pas vide** (RET-002 n°50).

Captures : les 10 thèmes avec leur palette par défaut, clair et sombre, plus Halo et Néon avec trois
palettes étrangères, plus la vitrine sans thème posé (l'apparence livrée du lot 7). Les regarder : un
survol qui vire au blanc, une lueur invisible en sombre, une carte sans bord sur fond sombre sont des
échecs même si les ratios passent.

**Contrôle** : sonde à zéro échec sur les 200 combinaisons, nombre de mesures affiché, captures
produites dans `artifacts/`. Contrôle négatif : une couleur de survol volontairement cassée en local
doit faire échouer la sonde. Si la sonde ne peut pas tourner dans l'environnement, le dire : ne pas
déclarer le lot fait sur la foi de bUnit.

### Lot 11 : documentation et portes finales

- `docs/foundation-components.md` : thèmes, palettes, combinaison, exemple `OmniThemeScope`.
- `CHANGELOG.md` : changement de l'apparence livrée en tête, puis ajouts (`OmniThemePalette`,
  `OmniThemePalettes`, `OmniThemeScope.Palette`, `OmniThemePreset.Shape`/`With`, les jetons de
  remplissage et la sévérité d'information, les composants du lot 2), 20 thèmes vers 10 avec la
  correspondance ancien thème vers thème + palette, corrections T3 à T13 et T16 à T24, essai T14, grilles T15, politique SDK.
- `docs/ui-conventions.md` : la règle T1 en toutes lettres, un jeton de remplissage ne sert jamais de
  couleur de texte et réciproquement. Sans elle, la distinction se reperdra au premier ajout.
- `docs/public-api.txt` régénéré, `docs/test-config.md` si la sonde du lot 10 devient une porte.
- Aucune mention de Radzen comme source de code. `NOTICE.md` inchangé si rien n'a été copié.
- Version : `<Version>` passe à `1.0.1` (D3). `docs/versioning.md` reçoit l'exception motivée
  (rupture admise dans un correctif parce que les versions antérieures sont délistées et que `1.0.1`
  devient la seule version installable), et `CHANGELOG.md` l'énonce en tête. Sans cette mention, le
  dépôt contredirait sa propre règle : ne pas livrer le lot sans elle.
- Le délistage des versions antérieures relève de `docs/publishing.md` et de l'utilisateur. Ne rien
  publier ni délister soi-même ; rappeler que le numéro `1.0.1` ne se justifie qu'une fois fait.

**Contrôle** : validation complète, `eng/Test-PublicApi.ps1`, `eng/Test-Budgets.ps1`,
`eng/Test-Csp.ps1`, `eng/Test-DependencyPolicy.ps1`, `eng/Test-SdkBand.ps1`, `eng/Test-Package.ps1`
verts, avec leur sortie. Diff d'API publique présenté à l'utilisateur avec le rappel du délistage.

## Compte rendu attendu en fin d'exécution

Par lot : fait, non fait ou partiel, avec la preuve (commande et sortie utile). Liste des
**À VÉRIFIER** levés ou non, et des trouvailles T1 à T24 portées ou non. Composants du lot 9 retirés le
cas échéant, avec le motif. Arbitrage du plancher de contraste des bordures. Rappel du délistage
restant à faire par l'utilisateur. Branche courante.

## Ce que ce plan ne rouvre pas

La liste des thèmes et des palettes (D5, validée), le numéro de version (D3, `1.0.1`), le choix de
faire du thème `Défaut` l'apparence livrée (D2), la fusion des trois branches dans `develop` sans
branche intermédiaire (D1, D4). La feuille CSS n'a plus de budget de taille (retiré par l'utilisateur le 2026-09-18, `e18547e`) : ne pas en réintroduire ni en parler. Un lot qui voudrait revenir sur l'un de ces points s'arrête
et le pose à l'utilisateur.
