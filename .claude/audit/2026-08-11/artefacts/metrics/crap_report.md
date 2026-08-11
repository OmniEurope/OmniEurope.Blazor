# Rapport CRAP

> Date: 2026-08-11

## Résultat

CRAP non calculable de façon fiable.

- Couverture: indisponible, car le collecteur `XPlat Code Coverage` n'est pas installé dans le projet de tests.
- Complexité: non fiable, car aucun MCP Roslyn n'est disponible et les outils Python sont désactivés par défaut.
- Aucun score par méthode, classe ou fichier n'est fabriqué.

## Priorisation

Les agents de module doivent signaler les fichiers volumineux et méthodes manifestement complexes à partir de la lecture complète, mais ne doivent pas leur attribuer de score CRAP ou de complexité numérique sans mesure outillée.
