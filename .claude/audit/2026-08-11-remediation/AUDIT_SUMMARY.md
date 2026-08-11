# Audit 360 de remédiation - Synthèse

> Date: 2026-08-11
> Périmètre: état courant du worktree après correction des 325 findings de la baseline
> Révision de base: `717af586cc40f3d87572e8e76b0b452ef4766b04`
> Contrat de cache: `audit-360-cache-v1`

## Verdict

L'audit intégral couvre **490/490 fichiers actuels**. Il ne reste aucune entrée `À faire` ou `En cours`. Les validations d'exécution sont vertes, mais l'audit de contenu révèle **77 findings actionnables** après déduplication: **0 critique, 15 élevés, 46 moyens et 16 faibles**. Le dépôt n'est donc pas encore prêt à déclarer la remédiation exhaustive terminée.

La structure générale est saine et proportionnée: une RCL, un projet de tests, deux outils de compilation et cinq hôtes de preuve. Aucune dépendance, copie ou surface d'exécution Radzen n'a été observée dans le produit. Les nouveaux findings portent sur des écarts réels de comportement, de preuve, de reproductibilité et de documentation, pas sur une architecture à remplacer.

## Couverture de l'audit

| Mesure | Nombre |
| --- | ---: |
| Fichiers inventoriés et audités | 490 |
| Mode Full | 490 |
| Mode Diff | 0 |
| Mode Cache | 0 |
| Invalidés par le contexte global | 490 |
| Fichiers d'auto-état `.claude/audit/**` exclus | 47 |
| Anciens chemins Git supprimés, absents du système de fichiers | 120 |
| Entrées non `✅ Audité` | 0 |

La modification de `CLAUDE.md`, `.editorconfig`, des règles et des manifests invalidait le contrat et le contexte de chaque module; aucune preuve ancienne n'a donc été réutilisée.

## Compteurs consolidés

| Source | Critique | Élevé | Moyen | Faible | Total |
| --- | ---: | ---: | ---: | ---: | ---: |
| Architecture et domaines | 0 | 0 | 2 | 2 | 4 |
| Kit et conventions globales | 0 | 2 | 8 | 1 | 11 |
| Dépendances, licences et SBOM | 0 | 1 | 6 | 3 | 10 |
| Bibliothèque `OmniEurope.Blazor` | 0 | 2 | 7 | 1 | 10 |
| Repository, docs, scripts et CI | 0 | 0 | 11 | 4 | 15 |
| Tests | 0 | 3 | 6 | 2 | 11 |
| Catalog | 0 | 1 | 1 | 1 | 3 |
| AutoSmoke.Client | 0 | 0 | 0 | 0 | 0 |
| AutoSmoke | 0 | 1 | 1 | 0 | 2 |
| HybridSmoke | 0 | 2 | 0 | 0 | 2 |
| WasmSmoke | 0 | 1 | 0 | 1 | 2 |
| Analyseurs | 0 | 1 | 3 | 0 | 4 |
| PublicApiGuard | 0 | 1 | 1 | 1 | 3 |
| **Total consolidé** | **0** | **15** | **46** | **16** | **77** |

Les notifications `[INFO] [Sur-ingénierie]` sont consultatives, exclues de ces compteurs et sans effet sur le verdict.

## Findings prioritaires

### Élevés

- **Preuves qui peuvent être faussement vertes:** la garde d'API omet plusieurs ruptures SemVer; les gates de conventions et de couverture acceptent des régressions majeures; l'interop éditeur est simulée plutôt qu'exécutée.
- **Analyseur de sécurité contournable:** `GEN007` accepte des attributs d'autorisation homonymes; plusieurs autres diagnostics reposent sur des formes textuelles trop faibles.
- **Internationalisation et accessibilité des hôtes:** Catalog, Auto, WebAssembly et Hybrid figent la langue ou ne la négocient pas; la coque Hybrid n'applique aucune CSP effective.
- **Produit:** Tabs et MultiSelect contiennent encore des textes français codés en dur; le contrat `Disabled` de l'éditeur reste incomplet.
- **Gouvernance:** l'overlay `STD-*` n'est pas fusionnable formellement avec le registre _Generic et la politique bUnit affirme une version désormais fausse.

### Moyens

- **Fonctionnel:** semaine Scheduler culturellement incohérente, colonnes DataGrid laissant un état orphelin, remplacement d'OverlayService non géré, plage DatePicker contradictoire et projection graphique quadratique.
- **Dépendances:** workload set mal déclaré, sanitizer obsolète, graphes Hybrid/Roslyn à réviser et politique de fraîcheur incomplète.
- **SBOM et paquet:** mauvaise version racine, mélange dépôt/artefact, absence de graphe CycloneDX et vérifications d'identité/licences insuffisantes.
- **Tests:** branches d'erreur et interactions publiques non prouvées, couverture graphique incomplète et fixtures analyseur absentes.
- **Documentation:** plusieurs plans, backlogs et contrats décrivent encore l'état antérieur aux corrections.

### Faibles

- Taxonomie de familles, références de projets, fichiers multi-types et carte documentaire à réaligner.
- Stratégie cache WebAssembly, provenance occurrence par occurrence, confinement des chemins de licence et exactitude de provenance CI à renforcer.

## Arbitrage et déduplication

Les artefacts granulaires restent immuables comme preuves de lecture. Le total consolidé exclut les doublons ou constats invalidés suivants:

- `KIT-010` est couvert par `DEP-002`;
- `DEP-004` est couvert avec une sévérité supérieure par `KIT-011`;
- `DEP-005` est couvert avec une sévérité supérieure par `KIT-009`;
- `OE-BLAZOR-010` est détaillé par les findings de tests sur graphiques, DataGrid, SplitButton, overlay et focus;
- `REP-013` est couvert par `DEP-007`;
- `REP-016` est couvert avec une sévérité supérieure par le finding de tests sur le seuil de couverture;
- `REP-019` est couvert par `DEP-009`;
- `REP-012` et `REP-014` sont invalidés: la nouvelle revue `PUBAPI-001/002` confirme que l'extracteur reste incomplet, donc la documentation qui le qualifie de partiel n'était pas obsolète.

## Preuves d'exécution

- Build Release solution: **0 avertissement, 0 erreur**.
- Build Hybrid Windows: **0 avertissement, 0 erreur**.
- Tests: **181/181 réussis**, aucun ignoré.
- Couverture: **2 703 lignes valides, 86,64 % de lignes couvertes**, Cobertura exploitable.
- Runtimes: Server, WebAssembly, Interactive Auto et MAUI Hybrid exercés dans Chromium/WebView2, interactions et consoles propres.
- CSP source: **314 fichiers**; fixtures sûre/dangereuse valides.
- API publique actuelle: baseline de **1 135 signatures** acceptée, avec limites de l'extracteur consignées.
- Paquet: **30 entrées**, symboles **5**, fixture contaminée rejetée, budget **222 783 / 2 097 152 octets**, provenance cohérente avec ses entrées.
- SBOM de dépôt: **114 couples package/version** et **12 textes de licence** hachés; exactitude d'artefact encore à corriger.
- Corpus externe rafraîchi: **32 projets, 4 604 fichiers**, hashes vérifiés; générateurs stables sur deux exécutions.
- Ports des sondes: tous libérés après exécution.

## Limites de l'audit

- Complexité et CRAP: **non fiables**, faute de MCP Roslyn/complexité; aucun outil Python n'a été sondé ou exécuté.
- SAST transversal: **non fiable**, Semgrep désactivé par le Python opt-out.
- Secrets et historique: **non fiables**, `gitleaks` absent.
- Sécurité manuelle: **best-effort, non outillée** pour les dimensions non couvertes par compilation, CSP, paquet et revue intégrale.
- Cycles de symboles: best-effort par manifests, compilation et recherches, sans graphe sémantique Roslyn.
- Paramètres GitHub externes: l'activation du signalement privé de vulnérabilités reste non observable depuis le dépôt local.

## Artefacts

- Architecture: `artefacts/AUDIT_ARCHITECTURE.md`
- Kit: `artefacts/AUDIT_KIT.md`
- Dépendances: `artefacts/AUDIT_DEPENDENCIES.md`
- Registres et findings: `artefacts/AUDIT_REGISTER_*.md`, `artefacts/AUDIT_FINDINGS_*.md`
- Métriques: `artefacts/metrics/`
- Dashboard: `audit-report.html`

## Résumé succinct des findings

- Les preuves CI peuvent encore rester vertes malgré des ruptures d'API, une forte baisse de couverture ou un JavaScript éditeur cassé.
- Les quatre hôtes n'alignent pas correctement langue, accessibilité et CSP avec les contrats bilingues et sécurisés.
- Le workload, plusieurs dépendances et le SBOM ne sont pas encore reproductibles ou exacts au niveau de l'artefact publié.
- Quelques comportements produit restent incorrects ou insuffisamment testés dans Scheduler, DataGrid, overlays, graphiques et éditeur.
- Les règles _Generic, l'analyseur et la documentation doivent être réalignés sur les garanties réellement appliquées.
