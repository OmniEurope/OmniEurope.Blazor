# Journal des modifications

Les changements notables de ce projet seront documentés ici selon le format Keep a Changelog.

## [Unreleased]

### Added

- Structure initiale de la Razor Class Library et du paquet NuGet.
- Contrat CSP strict et procédure de développement clean-room.
- Composants pilotes `OmniButton`, `OmniCard`, `OmniStack` et `OmniAlert`.
- Inventaires générés des usages Radzen observés dans l'instantané local des projets consommateurs.
- Cibles Razor pour les 110 balises inventoriées, du socle de formulaires au DataGrid, graphiques, scheduler et éditeur HTML.
- Catalogue Interactive Server avec en-tête CSP strict, collecteur de rapports et matrice de documentation publique.
- Gardes CI pour le scan source CSP, la baseline API actuelle, les budgets, le registre de présence des cibles et le contenu NuGet.
- Sondes de compilation et publication WebAssembly et Interactive Auto, test HTTP du prérendu et des assets Auto, et compilation MAUI Blazor Hybrid.

### Changed

- Remplacement de la référence serveur `Microsoft.AspNetCore.App` par le paquet client-compatible `Microsoft.AspNetCore.Components.Web`.

### Fixed

- Annonce accessible des erreurs de validation, focus automatique sur le premier contrôle invalide et prise en charge des entrées autonomes hors `EditForm`.
- Assainissement de la valeur initiale de l'éditeur HTML et protection contre les résultats asynchrones obsolètes dans Autocomplete, DataGrid et Scheduler.
- Coûts répétés supprimés dans DropDown et les séries Pie/Donut, sans changer leurs contrats publics.

