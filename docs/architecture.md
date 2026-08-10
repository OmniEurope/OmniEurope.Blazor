# Architecture

`OmniEurope.Blazor` reste volontairement une seule Razor Class Library tant que des frontières supplémentaires ne sont pas justifiées par des consommateurs réels.

## Structure

- `src/OmniEurope.Blazor/Components` : composants publics et primitives d'API ;
- `src/OmniEurope.Blazor/Internal` : utilitaires non exposés ;
- `src/OmniEurope.Blazor/wwwroot` : CSS et éventuels modules JavaScript statiques ;
- `tests/OmniEurope.Blazor.Tests` : contrat de rendu et garde-fous CSP ;
- `docs` : inventaire, décisions clean-room et guides de migration ;
- `eng` : vérifications et génération reproductible ;
- `artifacts/packages` : paquets locaux, ignorés par Git.

Les composants n'héritent pas de types Radzen, ne partagent pas son espace de noms et n'enveloppent pas ses composants. La migration se fait explicitement au niveau des vues consommatrices.

