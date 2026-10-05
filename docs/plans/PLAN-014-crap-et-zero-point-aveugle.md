<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-014 : Porte CRAP à 30 et zéro point aveugle dans les tests

> Statut : **en cours**. Établi le 2026-10-04 à la demande du propriétaire, à partir de l'audit 360 du
> 2026-10-04 (commit `d9d26ae`). Lots 1 à 4 faits ; campagne par famille en cours (Data, Selection, Editor, Navigation, Forms, Pages,
> Overlays faits hors écarts admis et artefacts Razor à traiter).
>
> Mesure de départ du 2026-10-04 (Release, 4 281 tests) : 265 fichiers à écarts, 1 081 lignes jamais
> exécutées, 1 797 branches jamais prises ; 49 méthodes au-dessus d'un CRAP de 30, dont 13 admises.
>
> Mesure courante du 2026-10-05 (Release, 5 185 tests, arbre commité `a5bd39c`) : 42 fichiers à écarts,
> 19 lignes jamais exécutées, 98 branches jamais prises, dont 17 fichiers Charts et `OmniTemplateForm` laissés
> à la session parallèle ; aucune méthode au-dessus de 30 hors des 14 exceptions. Depuis `1f70239`, chaque
> commit est rejoué localement sur un export de l'index (fumées Server, WebAssembly et Auto, tests, portes) :
> une garde retirée par la campagne (`OmniDialog`, module de focus) n'était atteignable que sur Blazor Server.

## Objectif

1. Corriger les cinq constats de l'audit dont le correctif ne change rien pour un hôte qui ne rencontre pas
   le défaut.
2. Une porte CI refuse toute méthode de la RCL dont le score CRAP dépasse 30, sauf exception justifiée et
   figée ; le code généré est exclu.
3. Zéro point aveugle : chaque ligne et chaque branche C# de la RCL est exécutée par un test, sauf exception
   justifiée et figée ; chaque fonction des modules JavaScript du paquet est exécutée par une sonde navigateur.

## Décisions

- **CRAP** : variante de l'audit, `CC² × (1 - couverture des lignes)³ + CC`, complexité et couverture lues
  dans le rapport Cobertura de coverlet. Seuil 30.
- **Code généré exclu** : `BuildRenderTree` des composants Razor, méthodes et classes produites par le
  compilateur (fonctions anonymes `<…>b__`, classes `<>c`, `DisplayClass`). Une méthode `async` n'est pas du
  code généré : son automate (`<Nom>d__N.MoveNext`) est compté sous le nom de sa méthode source.
- **Exception CRAP** : une entrée de `eng/crap-exceptions.json` (méthode, justification non vide, complexité
  plafond). La porte échoue quand une méthode dépasse 30 sans exception, quand une méthode en exception
  dépasse sa complexité plafond, quand une justification est vide, et quand une exception ne sert plus
  (méthode repassée sous 30 ou disparue) : la liste ne fait que rétrécir.
- **Exception admise** : table de correspondance (`switch` d'une valeur vers une autre), règles normatives
  recopiées d'une source externe (CLDR), analyseur de texte dont chaque branche est une forme de la
  grammaire. Une méthode de logique (événements, état, rendu) se découpe au lieu d'être exceptée.
- **Zéro point aveugle, C#** : même mécanisme, `eng/coverage-baseline.json` liste les classes encore
  incomplètes avec leurs lignes et branches non couvertes ; une classe absente de la liste doit être à 100 %
  de lignes et de branches, et une classe de la liste ne peut que progresser. Chaque lot de tests retire des
  classes de la liste. Une ligne réellement inatteignable (garde défensive) passe dans
  `eng/coverage-exceptions.json` avec sa justification, jamais sous `[ExcludeFromCodeCoverage]` silencieux.
- **Zéro point aveugle, JavaScript** : couverture V8 précise (`Profiler.startPreciseCoverage` par CDP)
  relevée pendant les sondes de la vitrine ; chaque fonction de `wwwroot/**/*.js` exécutée au moins une
  fois, sauf exception justifiée.
- **Unitaire ou intégration** : bUnit pour le rendu et les événements .NET (le plus rapide, en CI) ; sonde
  navigateur seulement pour ce que bUnit ne peut pas exécuter (JavaScript, mesures, gestes de confiance).

## Lots

### Lot 1 - Cinq correctifs sans effet de bord
- [x] `RCL-GRID-ROW-INDEX-001` : la case « tout sélectionner » et le bouton « tout déplier » décrivent chaque
  ligne avec l'index sous lequel elle est rendue.
- [x] `RCL-GRID-GROUP-KEY-001` : chemin de groupe sans collision (segments échappés), `null` distinct.
- [x] `RCL-THEME-SYSTEM-001` : `[data-omni-theme="system"]` reçoit les jetons clairs, la surcharge sombre du
  média le remplace en mode sombre ; corrigé dans le générateur, cascade relue dans Chromium sous les deux
  préférences.
- [x] `RCL-PRESET-001` : un preset générique de type incompatible échoue au premier rendu avec un message
  qui nomme le preset et le paramètre ; l'évidence (chaîne pour une collection) échoue dès l'enregistrement.
- [x] `RCL-DYNAMIC-KIND-001` : un champ qui change de genre sous le même nom recharge sa valeur.
Contrôle fait : 12 tests nouveaux échouent sans les correctifs et passent avec ; suite complète verte hors
deux tests d'`OmniTemplateForm` d'un travail parallèle non commité (`RendererInfo` absent sous bUnit).

### Lot 2 - Porte CRAP
- [x] `eng/Test-Crap.ps1` lit le rapport Cobertura, exclut le code généré, rattache les automates `async` à
  leur méthode et applique `eng/crap-exceptions.json`.
- [x] `eng/crap-exceptions.json` : 13 méthodes admises, une justification chacune (voir Décisions).
- [x] Autotest `eng/Test-CrapFixtures.ps1` : le rapport sain passe (code généré et méthode `async` admise
  compris) ; méthode non admise, exception inutile, plafond dépassé, plafond trop large et raison vide
  échouent, chacun pour sa raison.
- [x] Branchement dans `.github/workflows/ci.yml` après `Test-Coverage.ps1` : autotest puis porte.
Contrôle fait : porte verte sur la mesure complète ; autotest vert.

### Lot 3 - Assainissement CRAP
- [x] Méthodes de logique au-dessus de 30 découpées ou testées : `HtmlEditorSourceFace.ExecuteAsync`,
  `OmniDataGridColumn.Matches`, `GridExport.CellOf`, `GridGrouping.GroupedRows`, `MindMapKeyboard.KeyDownAsync`,
  automates asynchrones d'`OmniDialog`, `OmniSpreadsheet`, `OmniAppearanceWindow`, `OmniDiffViewer`,
  `OmniHtmlEditor`, `OmniCodeEditor`, `OmniSelectableCardGroup`, `OmniLogViewer.SyncLines`,
  `OmniDynamicForm.Check`, `GraphLayeredLayout.Build`, `GitGraphLayout.Build`.
- [x] Les 18 entrées sous une complexité de 30 ramenées sous 30 par des tests.
Contrôle fait : `eng/crap-exceptions.json` ne contient que des tables (dont l'égalité champ par champ
d'`OmniDataGridColumn.Matches`), des règles normatives et des analyseurs ; 14 entrées.

### Lot 4 - Porte zéro point aveugle (C#)
- [x] `eng/Test-Coverage.ps1` : contrôle par fichier source contre `eng/coverage-baseline.json` et
  `eng/coverage-exceptions.json`, liste initiale générée depuis la mesure du 2026-10-04 (`-UpdateBaseline`).
Contrôle fait : porte verte ; un écart qui grandit, un fichier retiré de la liste et une liste plus large que
la mesure échouent chacun.

### Lots 5 à N - Campagne de tests par famille
Une famille par lot (environ 15 classes), dans l'ordre du nombre de lignes non couvertes : Data, Editor,
Scheduling, Diagrams, Forms, Selection, Overlays, Navigation, Layout, Theming, Charts, Foundation, Pages,
Feedback, Internal restant, Localization.
Contrôle par lot : aucune classe de la famille dans `eng/coverage-baseline.json` ; suite verte.

Avancement : Data, Selection, Editor, Navigation, Forms, Pages, Overlays, Layout, Theming, Feedback,
Foundation, Localization, Actions, Diagram, Scheduling et Internal traités. Restent dans la liste : les
garde-fous qu'un événement déjà en vol sur Blazor Server atteint seul (à admettre un par un dans
`eng/coverage-exceptions.json`), quelques branches que le compilateur Razor attribue au balisage, et deux
familles laissées de côté parce qu'une session parallèle les modifie sans les avoir encore livrées :
Charts (`OmniChartContext`, `OmniLegend`, séries) et `OmniTemplateForm`.

Internal/Grid* (2026-10-05) : filtre avancé à deux conditions, tri multiple et tri restauré, plage de
dates distante, regroupement (dégroupement, groupes fermés, pieds imbriqués), export d'un arbre et résumé
des filtres exporté, rafraîchissement virtuel dépassé ; `GridTree`, `GridExpansion`,
`GridVirtualDataSource` sortis de la liste, `GridSelection.IndexOfVisible` admis en exception. Restent
surtout des branches partielles (`GridDataView`, `GridColumnLayout`, `GridVirtualViewport`, `GridPaging`).

Seconde passe du 2026-10-05 : helpers internes (Xlsx, MindMap, Gantt, timeline, notifications,
sanitiseur, thèmes), MindMap et ses panneaux, Forms, Feedback, Pages, Navigation, Layout, Theming,
Scheduler, menus et portail, pickers, raccourcis de l'éditeur HTML. Les « branches Razor non
expliquées » sont pour la plupart des ternaires d'attributs que le compilateur attribue à la ligne
voisine (`@ChildContent`, `@if`) : elles se couvrent par la variante d'attribut manquante. Gardes en
vol ou déjà vérifiées par leur unique appelant admises dans `eng/coverage-exceptions.json`. Deux codes
morts relevés par la campagne ont été supprimés (`OmniOverlayCoordinator.CloseTopAsync`,
`MindMapSelection.Many`). `eng/Show-CoverageGaps.mjs` liste les écarts d'un rapport avec leur source
et, avec `--conditions`, l'issue manquante de chaque condition.

### Lot final - JavaScript
- [ ] Couverture V8 relevée par les sondes de la vitrine, porte sur les fonctions de `wwwroot/**/*.js`.
- [ ] Sondes complétées pour chaque fonction non exécutée, ou exception justifiée.
Contrôle : rapport de couverture JS sans fonction non exécutée hors exceptions.
