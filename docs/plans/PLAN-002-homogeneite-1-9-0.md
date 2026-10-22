# PLAN-002 : homogénéité, qualité et sécurité (1.9.0)

Source : audit du 2026-10-22 (rapport local de l'audit, constats IDX, L1 à L4). Décision du propriétaire du 2026-10-22 : l'API publique est rendue homogène sans couche de compatibilité ni alias `[Obsolete]` (même motif qu'en 1.2.0, voir [versioning.md](../versioning.md)) ; la version suivante est la `1.9.0`. Exigence première : **tout fonctionne à la fin** (compilation, suites, gardes, vitrine vérifiée en navigateur, éditeur de code Monaco compris).

Branche de travail : `release/1.9.0`, depuis `develop`. Trois lots au plus en parallèle, chacun dans son worktree, intégrés et vérifiés par l'orchestrateur avant le lot suivant.

## Conventions cibles

1. Zone remplaçable : suffixe `Content` (`HeaderContent`, `FooterContent`, `ActionsContent`, `SummaryContent`, `SidePanelContent`) ; `docs/ui-conventions.md` aligné sur [public-api-conventions.md](../public-api-conventions.md).
2. `Title` = titre visible d'une tuile ou d'un conteneur ; `Label` = nom d'un champ ou nom accessible (`OmniStatTile.Label` devient `Title`, `OmniDescriptionItem.Label` devient `Term`, `OmniChart.Title` devient `Label` s'il n'est que le nom accessible).
3. `OmniTextTone` reçoit `Info` et `Warning` ; `OmniHeaderTone` devient `OmniHeaderSurface` (paramètre `Surface`).
4. Une seule énumération d'orientation `OmniOrientation` (ex-`OmniStackOrientation`), sens ARIA (direction de l'élément lui-même) ; les booléens `Vertical` deviennent `Orientation`.
5. Icône : fragment ; `ActionIcon` et `OpenIcon` deviennent des fragments, `IconFor` devient `IconTemplate`.
6. Export : `OmniMarkdownTable*` fusionné dans `OmniTableExport*` ; membres `[Obsolete]` supprimés.
7. Un nom par concept : `Level` (ex-`HeadingLevel`), `View` (ex-`OmniGantt.Scale`), `Justify` (ex-`HorizontalAlign`), `Placement` (ex-`Position`, énumérations `*Position` renommées `*Placement`), `Minimum`/`Maximum` (ex-`Min`/`Max`), `ExtraLarge` (ex-`XLarge`), `FullWidth` (ex-`Full`), `ShowCopy` (ex-`Copyable`), `Display` (ex-`DisplayStyle`, `Presentation`), `Maximized` (ex-`IsMaximized`), `OmniAlignment` (ex-`OmniDataGridTextAlign`), `OmniGantt.Value`/`ValueChanged` (ex-`SelectedTaskId`), événements `On` + sujet + verbe (fin des exceptions `FilesSelected`, `FileRemoved`, `LinkActivated`, `ValidationFailed`).
8. Défauts : `OmniDataGrid.Density` nullable, hérite d'`OmniThemeScope` ; autres écarts alignés ou documentés comme délibérés.
9. `ReadOnly` sur tout composant de saisie.
10. Classes `_Imports` hors de l'API publique si faisable, sinon exception documentée.

## Lots

| Vague | Lot | Contenu | État |
|---|---|---|---|
| 1 | W1-A | Conventions sur Data, Scheduling, Charts, Diagram | à faire |
| 1 | W1-B | Conventions sur Editor, Selection, Forms, Feedback, Foundation, Actions | à faire |
| 1 | W1-C | Conventions sur Layout, Navigation, Overlays, Pages, Theming, Preferences | à faire |
| 2 | W2-A/B/C | Défauts et durcissements de sécurité de l'audit, mêmes familles, un test qui échoue avant et passe après par défaut | à faire |
| 3 | W3-A | Un seul moteur JS de placement et de clic extérieur | à faire |
| 3 | W3-B | Chargeur Monaco unique, portail de menu partagé, corps d'`OmniStatTile`, mise en forme d'export, CSS mort | à faire |
| 3 | W3-C | 90 paramètres documentés, `component-families.md`, porte `eng/Test-ComponentIndex.ps1` en CI | à faire |
| 4 | Livraison | `CHANGELOG.md` (Breaking changes et migrations), `versioning.md` (rupture assumée 1.9.0), version du paquet, `public-api.txt`, couverture, CRAP, sondes de la vitrine (passe complète `-JsCoverage`), vérification en navigateur | à faire |

## Critère de fin de chaque lot

Compilation sans avertissement, lancement des hôtes vert, garde de l'API publique à jour, porte de couverture et CRAP verts sur les fichiers touchés, vitrine et guides alignés sur les nouveaux noms, aucun test désactivé ni assertion retirée.
