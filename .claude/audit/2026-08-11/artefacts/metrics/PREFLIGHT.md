# Préflight - Audit 360

> Date: 2026-08-11
> Révision source: `717af586cc40f3d87572e8e76b0b452ef4766b04`
> Contrat de cache: `audit-360-cache-v1`

## Dépôt et inventaire

- Dépôt Git: **OK** - racine `C:/Dev/OmniEurope.Blazor`.
- Working tree source avant audit: **OK** - propre.
- Inventaire: **OK** - 241 fichiers suivis ou non ignorés, hors état auto-généré `.claude/audit/**`.
- État auto-généré exclu après initialisation: **16 fichiers**.
- Langages détectés: Razor (128), C# (36), PowerShell (9), CSS (3), JavaScript (1), HTML (1), YAML (2), JSON (12) et Markdown (34).
- Partition: **OK** - 8 modules physiques (7 projets `.csproj` et un module Repository).

## Build et tests

- .NET SDK: **OK** - `10.0.302` via `C:\Program Files\dotnet\dotnet.exe`.
- Build Release: **OK** - `OmniEurope.Blazor.slnx`, 0 avertissement, 0 erreur, exécuté via le garde .NET partagé.
- Tests unitaires: **OK** - 57 réussis, 0 échoué, 0 ignoré.
- Couverture XPlat: **FAILURE** - le collecteur `XPlat Code Coverage` est absent (`Unable to find a datacollector with friendly name 'XPlat Code Coverage'`). La couverture et le CRAP fondé sur la couverture sont **non fiables**.

## Outils de métriques

- Roslyn MCP: **ABSENT** - aucune complexité sémantique, détection de cycles ou analyse de références outillée n'est disponible.
- `lizard`: **SKIPPED (Python opt-out)** - aucun outil Python n'a été sondé ou exécuté.
- Complexité cyclomatique: **non fiable (outils Python désactivés et Roslyn MCP absent)**.
- CRAP: **non calculable de façon fiable** faute de complexité et de couverture compatibles.

## Outils de sécurité

- `gitleaks`: **ABSENT** - le scan de secrets outillé et l'historique Git sont non fiables.
- `semgrep`: **SKIPPED (Python opt-out)** - SAST cross-language non fiable.
- Analyseurs de sécurité .NET: **OK, portée limitée** - le build ne remonte aucun avertissement `CA****`, sans preuve qu'un jeu de règles sécurité exhaustif est activé.
- La revue manuelle par fichier reste obligatoire et sera qualifiée de `best-effort, non outillé` pour les dimensions sans outil.

## MCP disponibles

- `mcp__playwright`: navigation et inspection navigateur.
- `mcp__node_repl`: exécution JavaScript isolée.
- `mcp__codex_apps`: connecteurs applicatifs, sans capacité d'analyse sémantique .NET.
- Aucun MCP Roslyn n'est disponible.

## Conséquences

- Le build et les 57 tests sont fiables pour les chemins qu'ils exécutent.
- La couverture, le CRAP, la complexité, le SAST et le scan de secrets historique restent explicitement non fiables et doivent figurer dans les limites de la synthèse.
- L'audit continue conformément à la politique de dégradation gracieuse.
