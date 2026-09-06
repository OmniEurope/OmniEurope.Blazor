# Plan - Correction exhaustive des findings d'audit

> Canonical plan: `plans/PLAN-003-correction-findings-audit.md`
> Last updated: 2026-08-11
> Source audit: `.claude/audit/2026-08-11/audit-report.html`
> Scope: resolve every original finding `A360-001` through `A360-325`, then prove that no equivalent or new finding remains.

## Phase 1 - Baseline et traçabilité [done]
- [x] Conserver l'audit 360 complet du 2026-08-11 comme baseline immuable de 325 findings.
- [x] Vérifier la baseline: 241/241 fichiers, 325 IDs uniques, 0 critique, 97 élevés, 188 moyens et 40 faibles.
- [x] Découper les 325 IDs sans trou ni recouvrement en 22 lots de 15 findings au maximum.
- [x] Créer et tenir à jour `.claude/audit-remediation.md` avec preuve de correction et vérification pour chaque lot.
- [x] Relever avant chaque lot les fichiers concernés et les tests qui prouvent réellement le comportement attendu.

## Phase 2 - Lots 01 à 05 [in progress]
- [x] Lot 01: corriger et vérifier `A360-001` à `A360-015`.
- [ ] Lot 02: corriger et vérifier `A360-016` à `A360-030` (`A360-023` reste la seule preuve externe GitHub).
- [x] Lot 03: corriger et vérifier `A360-031` à `A360-045`.
- [x] Lot 04: corriger et vérifier `A360-046` à `A360-060`.
- [x] Lot 05: corriger et vérifier `A360-061` à `A360-075`.
- [ ] Gate: chaque ID des lots 01 à 05 possède une correction observable et un test ou contrôle adapté.

## Phase 3 - Lots 06 à 10 [completed]
- [x] Lot 06: corriger et vérifier `A360-076` à `A360-090`.
- [x] Lot 07: corriger et vérifier `A360-091` à `A360-105`.
- [x] Lot 08: corriger et vérifier `A360-106` à `A360-120`.
  - [x] GEN004-A: migrer les 7 composants Actions, puis compiler.
  - [x] GEN004-B: migrer les 11 premiers composants Charts, puis compiler.
  - [x] GEN004-C: migrer les 10 composants Charts restants, puis compiler.
  - [x] GEN004-D: migrer Data, Editor et Feedback, soit 15 composants, puis compiler.
  - [x] GEN004-E: migrer les 9 premiers composants Forms, puis compiler.
  - [x] GEN004-F: migrer les 8 composants Forms restants, puis compiler.
  - [x] GEN004-G: migrer les 12 composants Layout, puis compiler.
  - [x] GEN004-H: migrer les 13 composants Navigation, puis compiler.
  - [x] GEN004-I: migrer les 11 composants Overlays et Scheduling, puis compiler.
  - [x] GEN004-J: migrer Selection et les trois samples restants, soit 12 composants, puis compiler.
  - [x] Câbler et promouvoir les analyseurs GEN applicables après zéro bloc inline.
- [x] Lot 09: corriger et vérifier `A360-121` à `A360-135`.
- [x] Lot 10: corriger et vérifier `A360-136` à `A360-150`.
- [x] Gate: chaque ID des lots 06 à 10 possède une correction observable et un test ou contrôle adapté.

## Phase 4 - Lots 11 à 15 [completed]
- [x] Lot 11: corriger et vérifier `A360-151` à `A360-165`.
- [x] Lot 12: corriger et vérifier `A360-166` à `A360-180`.
- [x] Lot 13: corriger et vérifier `A360-181` à `A360-195`.
- [x] Lot 14: corriger et vérifier `A360-196` à `A360-210`.
- [x] Lot 15: corriger et vérifier `A360-211` à `A360-225`.
- [x] Gate: chaque ID des lots 11 à 15 possède une correction observable et un test ou contrôle adapté.

## Phase 5 - Lots 16 à 20 [completed]
- [x] Lot 16: corriger et vérifier `A360-226` à `A360-240`.
- [x] Lot 17: corriger et vérifier `A360-241` à `A360-255`.
- [x] Lot 18: corriger et vérifier `A360-256` à `A360-270`.
- [x] Lot 19: corriger et vérifier `A360-271` à `A360-285`.
- [x] Lot 20: corriger et vérifier `A360-286` à `A360-300`.
- [x] Gate: chaque ID des lots 16 à 20 possède une correction observable et un test ou contrôle adapté.

## Phase 6 - Lots 21 et 22 [completed]
- [x] Lot 21: corriger et vérifier `A360-301` à `A360-315`.
- [x] Lot 22: corriger et vérifier `A360-316` à `A360-325`.
- [x] Gate: chaque ID des lots 21 et 22 possède une correction observable et un test ou contrôle adapté.

## Phase 7 - Vérification exhaustive [todo]
- [x] Exécuter le build Release gardé avec zéro erreur et zéro avertissement.
- [x] Exécuter tous les tests unitaires, intégration, navigateur et hôtes disponibles sans affaiblir leurs gates.
- [x] Vérifier les runtimes Server, WebAssembly, Interactive Auto et MAUI Hybrid avec les preuves adaptées.
- [x] Régénérer couverture, métriques, inventaires, API publique, paquet, SBOM et provenance avec des outils fiables.
- [x] Rejouer un audit 360 complet sur la révision corrigée: 490/490 fichiers en mode Full.
- [x] Réconcilier chaque ID original avec sa preuve de fermeture et tout finding nouvellement découvert: 324/325 originaux fermés localement, 77 findings frais consolidés.
- [ ] Gate finale: 325/325 findings originaux fermés, aucun finding critique, élevé, moyen ou faible restant, aucune régression et audit frais à zéro finding.

## Phase 8 - Findings frais, lots 23 à 28 [in progress]
- [x] Lot 23 (10): corriger les comportements de la bibliothèque, l'i18n, `Disabled`, Scheduler, DataGrid, DatePicker, overlay, projection et cibles tactiles.
- [x] Lot 24 (9): corriger les hôtes Catalog, Auto, Hybrid et WebAssembly, puis les vérifier dans leurs runtimes réels.
- [ ] Lot 25 (15 maximum): renforcer les analyseurs, PublicApiGuard et les gates anti-faux-vert avec fixtures positives et négatives.
- [ ] Lot 26 (15 maximum): corriger dépendances, workload set, actions, SBOM, licences, paquet et provenance.
- [ ] Lot 27 (15 maximum): compléter les preuves de tests, les frontières architecturales et le registre de conventions.
- [ ] Lot 28 (solde): réaligner documentation, plans, backlogs, inventaires et scripts générateurs.
- [ ] Gate: les 77 findings frais possèdent tous une correction observable et une preuve adaptée.

## Phase 9 - Clôture [todo]
- [ ] Rejouer toutes les validations après les lots 23 à 28.
- [ ] Rejouer un nouvel audit 360 Full indépendant.
- [ ] Obtenir zéro finding critique, élevé, moyen ou faible.
- [ ] Fermer `A360-023` avec une preuve observable du dépôt GitHub privé.
- [ ] Gate finale: aucune action sûre et pertinente ne reste ouverte.
