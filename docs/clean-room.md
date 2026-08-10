# Procédure clean-room

Le projet reproduit des capacités générales de composants d'interface, jamais l'expression originale du code Radzen.

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

