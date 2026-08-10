# Guide de migration vers OmniEurope.Blazor

Ce guide prépare les remplacements futurs sans modifier un projet consommateur. Le registre exhaustif [component-coverage.md](component-coverage.md) fournit la correspondance des 110 balises observées et [component-contracts.md](component-contracts.md) conserve les paramètres et templates réellement utilisés.

## Préparer un hôte

1. Référencer le paquet `OmniEurope.Blazor` avec la version décidée pour la vague de migration.
2. Charger `_content/OmniEurope.Blazor/omnieurope.blazor.css` dans la page hôte.
3. Importer `OmniEurope.Blazor.Components` dans `_Imports.razor`.
4. Ajouter `OmniComponentsHost` au niveau racine lorsque dialogs, notifications ou menus contextuels sont utilisés.
5. Conserver une CSP sans `unsafe-inline` ni `unsafe-eval` et activer la collecte des rapports pendant la validation.

## Traduire un usage

Les composants OmniEurope expriment des capacités et ne reproduisent pas l'API Radzen. Pour chaque écran, partir du contrat observé plutôt que d'effectuer un remplacement textuel global.

| Besoin observé | Contrat OmniEurope |
|---|---|
| Liaison simple | `Value`, `ValueChanged` et `ValueExpression`, ou `@bind-Value`. |
| Options de sélection | `Items` et sélecteurs typés de valeur/texte. |
| Chargement distant | Callback annulable, état de chargement, erreur et reprise explicites. |
| Validation | `OmniTemplateForm`, validateurs Omni et messages `role="alert"`. |
| Superposition | `OmniOverlayService` et `OmniComponentsHost`, sans JavaScript inline. |
| DataGrid | Colonnes `OmniDataGridColumn<TItem>`, clés stables, tri/filtre/pagination contrôlés. |
| Graphique | Séries SVG typées, axes et options déclaratives. |
| Temps | `DateTimeOffset` et `TimeZoneInfo` explicite. |
| HTML | Valeur sanitizée par allowlist avant aperçu et persistance. |

## Ordre de remplacement d'un écran

1. Capturer le rendu, les parcours clavier, les requêtes, les erreurs et les performances de référence.
2. Identifier chaque balise, type, service, ressource CSS et script Radzen avec l'inventaire généré.
3. Remplacer une famille cohérente à la fois sans changement métier opportuniste.
4. Rejouer tests unitaires, intégration, accessibilité, CSP et scénarios métier.
5. Supprimer les imports et ressources devenus inutiles seulement après la preuve de non-usage.
6. Régénérer l'inventaire et exiger zéro référence Radzen avant de fermer la migration du projet.

## Points de contrôle par famille

- Formulaires : culture, saisie incomplète, soumission invalide, focus et annonce de l'erreur.
- Sélecteurs : valeurs nulles, gros volumes, rechargement asynchrone et annulation.
- Superpositions : `Escape`, clic extérieur, ordre du focus, imbrication et restauration du focus.
- Navigation : route active, annulation de navigation, historique et clavier.
- Collections et grille : clés stables, sélection, édition, détails, groupes et chargements distants.
- Graphiques et planification : labels accessibles, fuseaux, DST, chevauchements et grands jeux de données.
- Éditeur HTML : vecteurs XSS, sérialisation, round-trip et CSP navigateur.

Les phases 14 et 15 du [plan canonique](../plans/PLAN-002-remplacement-radzen.md) restent l'autorité pour l'ordre des projets et les gates de migration.
