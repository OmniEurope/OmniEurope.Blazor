# Architecture

`OmniEurope.Blazor` reste volontairement une seule Razor Class Library tant que des frontières supplémentaires ne sont pas justifiées par des consommateurs réels.

## Structure

- `src/OmniEurope.Blazor/Components` : point d'import commun et familles `Actions`, `Charts`, `Data`, `Editor`, `Feedback`, `Forms`, `Foundation`, `Layout`, `Navigation`, `Overlays`, `Scheduling` et `Selection` ; l'espace de noms public reste `OmniEurope.Blazor.Components` indépendamment du dossier ;
- `src/OmniEurope.Blazor/Internal` : utilitaires non exposés ;
- `src/OmniEurope.Blazor/wwwroot` : CSS et éventuels modules JavaScript statiques ;
- `tests/OmniEurope.Blazor.Tests` : contrat de rendu et garde-fous CSP ;
- `docs` : inventaire, décisions clean-room et guides de migration ;
- `eng` : scripts de vérification et de génération ; les inventaires dépendant de l'instantané externe `C:\Dev` ne sont pas reproductibles depuis le seul dépôt, comme décrit dans [reproducibility.md](reproducibility.md) ;
- `artifacts/packages` : paquets locaux, ignorés par Git.

Les composants n'héritent pas de types Radzen, ne partagent pas son espace de noms et n'enveloppent pas ses composants. La migration se fait explicitement au niveau des vues consommatrices.

Les composants complexes conservent une façade déclarative publique et délèguent leurs mécanismes à des moteurs internes : projection/chargement pour la grille, projection/domaines pour les graphiques et coordination ordonnée pour les superpositions. Les contextes en cascade de grille, onglets, étapes et arbre ne font pas partie de l'API publique.

## Direction des dépendances

Les hôtes de démonstration et les tests dépendent de la RCL; la RCL ne dépend d'aucun hôte. Les composants publics peuvent dépendre des moteurs `Internal`, mais ces moteurs ne doivent pas dépendre d'un composant public concret. La composition entre enfants et conteneurs passe par des contextes internes, tandis que les consommateurs ne voient que les paramètres et services publics documentés.

La taxonomie canonique est celle des 12 dossiers de `Components`. Toute vue agrégée dans la roadmap, le catalogue ou les registres de couverture doit conserver ces noms, quitte à présenter ensuite des regroupements secondaires explicitement étiquetés.

