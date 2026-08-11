# Préflight de l'audit 360 de remédiation

> Date: 2026-08-11
> Révision source: `717af586cc40f3d87572e8e76b0b452ef4766b04` avec changements de remédiation non commités
> Python: opt-out explicite du workflow, aucun outil ni aucune sonde Python exécutés

| Contrôle | Statut | Preuve et conséquence |
| --- | --- | --- |
| Dépôt Git | OK | Racine `C:/Dev/OmniEurope.Blazor`; inventaire par `git ls-files --cached --others --exclude-standard`. |
| Inventaire | OK | 490 fichiers courants hors `.claude/audit/**`; 47 fichiers d'auto-état exclus et 120 anciens chemins Git supprimés exclus car absents du système de fichiers. |
| Build Release | OK | Solution complète: 0 avertissement, 0 erreur. Hybrid Windows séparé: 0 avertissement, 0 erreur. |
| Tests | OK | 181/181 réussis, aucun ignoré, aucun échec. |
| Couverture | OK | Collecteur `coverlet.collector` 10.0.1; Cobertura exploitable, 2 703 lignes valides, taux lignes 86,64 %. |
| Runtimes | OK | Server, WebAssembly, Interactive Auto et MAUI Hybrid validés par Chromium/WebView2; interactions et consoles propres. |
| Paquet | OK | NuGet 30 entrées, symboles 5, fixture contaminée rejetée, budget 222 783/2 097 152 octets, provenance vérifiée. |
| SDK .NET | OK | `10.0.302`, roll-forward désactivé, workload set `10.0.302`. |
| Complexité Roslyn | ABSENT | Aucun MCP Roslyn/complexité disponible; complexité non fiable. |
| lizard | SKIPPED | Python opt-out; aucune sonde Python exécutée. Complexité non fiable. |
| semgrep | SKIPPED | Python opt-out; aucune sonde Python exécutée. SAST transversal non fiable. |
| gitleaks | ABSENT | Outil absent; scan de secrets outillé non fiable. |
| Analyseurs .NET | OK | GEN001 à GEN008 actifs et build sans avertissement; aucun avertissement CA de sécurité. |
| MCP sémantique | ABSENT | Aucun serveur Roslyn disponible; références et dépendances vérifiées par compilation et recherches textuelles. |

Le suffixe `-remediation` distingue ce nouvel audit intégral de la baseline complète créée le même jour, sans écraser les preuves historiques.
