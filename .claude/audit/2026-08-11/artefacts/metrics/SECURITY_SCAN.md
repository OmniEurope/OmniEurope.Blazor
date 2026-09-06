# Scan de sécurité

> Date: 2026-08-11

## Résultats outillés

- SAST Semgrep: **SKIPPED (Python opt-out)** - portée non fiable.
- Secrets Gitleaks: **ABSENT** - portée non fiable, historique Git non scanné.
- Analyseurs .NET: build Release avec 0 avertissement et 0 erreur; aucun avertissement `CA****` observé. Cette absence ne prouve pas qu'un ensemble sécurité exhaustif est activé.

## Portée manuelle exigée

Chaque fichier doit encore être revu pour les secrets en clair, injections, XSS, validation d'entrée, exposition de données, désérialisation, commandes, chemins, politiques CSP et dépendances. Les conclusions sans outil sont libellées `best-effort, non outillé`.

## Limites

Le SAST cross-language et le scan historique des secrets sont non fiables. Aucune absence de vulnérabilité ne doit être déduite de ces outils manquants.
