# Procédure clean-room

La procédure clean-room impose de reproduire uniquement des capacités générales de composants d'interface, sans reprendre l'expression originale du code Radzen.

## Sources autorisées

- besoins et comportements observables dans les applications OmniEurope ;
- standards publics HTML, ARIA, CSS et WAI-ARIA Authoring Practices ;
- documentation publique des plateformes .NET et Blazor ;
- tests écrits à partir des attentes propres aux applications.

## Sources interdites

- copie ou traduction de code C#, Razor, JavaScript, Sass ou CSS Radzen ;
- reprise de noms internes, commentaires, structures privées ou snapshots de tests Radzen ;
- extraction d'assets, d'icônes ou de thèmes Radzen ;
- décompilation d'assemblies pour guider l'implémentation.

## Flux de travail

1. Décrire la capacité requise depuis un cas d'usage OmniEurope.
2. Écrire une API et des critères d'acceptation indépendants.
3. Implémenter avec les primitives de plateforme et le design system OmniEurope.
4. Vérifier le rendu, le clavier, l'accessibilité et le contrat CSP.
5. Documenter la correspondance de migration sans prétendre à une compatibilité totale.

Toute nouvelle capacité ou évolution substantielle doit utiliser la [fiche clean-room](clean-room-component-sheet.md). Les composants déjà créés ne disposent pas de fiches historiques remplies ; il ne faut pas en fabriquer rétroactivement. L'inventaire étendu [radzen-surface-inventory.md](radzen-surface-inventory.md) et les [contrats observés](component-contracts.md) sont générés depuis le corpus externe décrit par [radzen-corpus.json](radzen-corpus.json), en excluant ce dépôt. Ce manifeste conserve les projets, statuts, révisions, fichiers et SHA-256 ; les générateurs refusent toute dérive du corpus avant d'écrire leurs rapports. Une reproduction exige donc de disposer des fichiers externes correspondant exactement à ces empreintes.

Ces inventaires décrivent des usages observables, mais ne prouvent pas à eux seuls la provenance de l'implémentation. Toute comparaison de provenance future doit être isolée du travail d'implémentation et conserver les versions, empreintes, manifestes, paramètres et résultats nécessaires à une reproduction indépendante.

