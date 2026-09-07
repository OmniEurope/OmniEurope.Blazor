# Contribuer

Toute contribution doit respecter la procédure clean-room décrite dans `docs/clean-room.md` et le contrat CSP dans `docs/csp-contract.md`.

Une pull request de composant doit inclure :

- un besoin observable issu d'un projet consommateur ;
- une API documentée indépendamment de l'implémentation Radzen ;
- des tests de rendu, d'interaction et d'accessibilité proportionnés ;
- une preuve qu'aucun style ou gestionnaire de script inline n'est émis ;
- une démonstration dans `site/OmniEurope.Blazor.Showcase/Components/Demos`, enregistrée dans `Demos/DemoCatalog.cs`, couvrant le composant et chaque valeur de ses paramètres énumérés ; les gardes `ShowcaseCoverageTests` et `ShowcaseVariantCoverageTests` font échouer la compilation sinon ;
- une entrée dans `CHANGELOG.md` lorsque le comportement public change.

