# Guide de migration vers OmniEurope.Blazor

Ce guide prépare les remplacements futurs sans modifier un projet consommateur. Le registre exhaustif [component-coverage.md](component-coverage.md) fournit la correspondance des 110 balises observées et [component-contracts.md](component-contracts.md) conserve les paramètres candidats et templates extraits des usages, avec les limites heuristiques qui y sont décrites.

## Préparer un hôte

1. Référencer le paquet `OmniEurope.Blazor` avec la version décidée pour la vague de migration.
2. Charger `_content/OmniEurope.Blazor/omnieurope.blazor.css` dans la page hôte.
3. Importer `OmniEurope.Blazor.Components` dans `_Imports.razor`.
4. Ajouter `OmniComponentsHost` au niveau racine lorsque les dialogs ou notifications pilotés par `OmniOverlayService` sont utilisés. `OmniContextMenu` et `OmniTooltip` sont instanciés et contrôlés directement.
5. Conserver une CSP sans `unsafe-inline` ni `unsafe-eval` et activer la collecte des rapports pendant la validation.

## Traduire un usage

Les composants OmniEurope expriment des capacités et ne reproduisent pas l'API Radzen. Pour chaque écran, partir du contrat observé plutôt que d'effectuer un remplacement textuel global.

| Besoin observé | Contrat OmniEurope |
|---|---|
| Liaison simple | `Value`, `ValueChanged` et `ValueExpression`, ou `@bind-Value`. |
| Options de sélection | `Options`, sous forme de `IReadOnlyList<OmniOption<TValue>>` ; chaque option porte `Value`, `Text`, `Disabled` et `Group`. |
| Chargement distant de listes, grilles et scheduler | Callback annulable ; `OmniDataList`, `OmniDataGrid` et `OmniScheduler` rendent chargement et erreur observables et proposent une reprise. |
| Autocomplete distant | Callback annulable ; les états de chargement, d'erreur et de reprise ne sont pas encore exposés. |
| Validation | `OmniTemplateForm`, validateurs Omni et messages `role="alert"`. |
| Superposition | `OmniOverlayService` et `OmniComponentsHost` pour dialogs et notifications ; context menu et tooltip sont contrôlés directement. |
| DataGrid | Colonnes `OmniDataGridColumn<TItem>`, clés stables, pagination contrôlée par `Page`/`PageChanged` ; tri et filtres internes transmis au callback `Load`. |
| Graphique | Séries SVG typées, axes et options déclaratives. |
| Temps | `DateTimeOffset` et `TimeZoneInfo` explicite. |
| HTML | Valeur sanitizée par allowlist avant aperçu et persistance. |

## Ordre de remplacement d'un écran

1. Capturer le rendu, les parcours clavier, les requêtes, les erreurs et les performances de référence.
2. Identifier chaque balise avec [component-inventory.md](component-inventory.md), puis chaque type, service, package, namespace, ressource CSS et script Radzen avec [radzen-surface-inventory.md](radzen-surface-inventory.md).
3. Remplacer une famille cohérente à la fois sans changement métier opportuniste.
4. Rejouer tests unitaires, intégration, accessibilité, CSP et scénarios métier.
5. Supprimer les imports et ressources devenus inutiles seulement après la preuve de non-usage.
6. Régénérer l'inventaire des balises et l'inventaire étendu de surface ; exiger zéro balise, symbole, package, namespace, ressource statique et token CSS/JavaScript Radzen avant de fermer la migration du projet.

## Points de contrôle par famille

- Formulaires : culture, saisie incomplète, soumission invalide, focus et annonce de l'erreur.
- Sélecteurs : valeurs nulles, gros volumes, rechargement asynchrone et annulation.
- Superpositions : `Escape`, clic extérieur, ordre du focus, imbrication et restauration du focus.
- Navigation : route active, annulation de navigation, historique et clavier.
- Collections et grille : clés stables, sélection, édition, détails, groupes et chargements distants.
- Graphiques et planification : labels accessibles, fuseaux, DST, chevauchements et grands jeux de données.
- Éditeur HTML : vecteurs XSS, sérialisation, round-trip et CSP navigateur.

Les phases 14 et 15 du [plan canonique](../plans/PLAN-002-remplacement-radzen.md) restent l'autorité pour l'ordre des projets et les gates de migration.
