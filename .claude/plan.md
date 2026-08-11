# Plan - Correction exhaustive des findings d'audit

> Canonical plan: `plans/PLAN-003-correction-findings-audit.md`
> Last updated: 2026-08-11
> Current scope: close all 325 findings from the 2026-08-11 audit before resuming consumer migrations from PLAN-002.

## Phase 1 - Baseline et traçabilité [done]
- [x] Conserver et vérifier la baseline de 325 findings uniques.
- [x] Découper `A360-001` à `A360-325` en 22 lots de 15 findings au maximum.
- [x] Tenir `.claude/audit-remediation.md` à jour avec une preuve par lot.

## Phase 2 - Correction par lots [in progress]
- [ ] Corriger les lots 01 à 05, soit `A360-001` à `A360-075`.
- [ ] Corriger les lots 06 à 10, soit `A360-076` à `A360-150`.
- [ ] Corriger les lots 11 à 15, soit `A360-151` à `A360-225`.
- [ ] Corriger les lots 16 à 20, soit `A360-226` à `A360-300`.
- [ ] Corriger les lots 21 et 22, soit `A360-301` à `A360-325`.

## Phase 3 - Vérification exhaustive [todo]
- [ ] Exécuter toutes les gates pertinentes sans les affaiblir.
- [ ] Rejouer un audit 360 complet.
- [ ] Prouver 325/325 findings originaux fermés et zéro finding restant.
