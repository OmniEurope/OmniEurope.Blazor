<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Plans d'implémentation

Tous les plans numérotés du dépôt vivent ici. Le code, les tests et les ADR restent les sources
structurelles ; un plan décrit un travail, pas l'état du produit.

## Nomenclature

- Format obligatoire : `PLAN-NNN-description-minimale.md`.
- `NNN` est dense et suit l'ordre du registre ci-dessous.
- La description emploie le moins de mots possible tout en restant non ambiguë.
- Un plan partiellement livré est réduit à son seul reliquat réel.
- Toute suppression ou renumérotation met à jour les références documentaires actives ; les mentions historiques datées restent inchangées.
- Une annexe d'un plan (journal d'exécution, maquette de référence) porte le numéro de son plan et un suffixe.

## Plans actifs

| Plan | Travail restant | ADR |
|---|---|---|
| [PLAN-001](PLAN-001-remise-equerre-kit.md) | Phase A du plan maître le kit PLAN-001 (lot 7) : lots 1 et 2 livrés, lot 3 (contrats) et lot 4 (`STD-I18N` de la vitrine) ouverts | [ADR-001](../adr/ADR-001-global-json-rollforward-latestpatch.md) |
| [PLAN-005](PLAN-005-remediation-audit-2026-09-27.md) | Remédiation de l'audit 360 du 2026-09-27 : lots 1 à 4 livrés ; reste SCR-001, à corriger dans le kit | - |
| [PLAN-006](PLAN-006-editeur-html-extensible.md) | Éditeur HTML extensible : lots 1 à 4 livrés ; reste la consommation par une application cliente (PLAN-009 d'une application cliente) | - |

Total comparable : **2 lots ouverts** dans PLAN-001 (3 et 4), plus SCR-001 de PLAN-005, hors dépôt.

## Plans livrés (conservés pour la traçabilité)

Déplacés de `plans/` (dossier racine ignoré par Git) vers ce répertoire le 2026-09-20 et renumérotés
en dense (PLAN-001, lot 2). Leurs anciens numéros locaux étaient respectivement 004, 007 et 008.

| Plan | Objet | État |
|---|---|---|
| [PLAN-002](PLAN-002-grille-complete.md) | Grille complète et virtualisation d'`OmniDataGrid` | Terminé, 22 cases sur 22 |
| [PLAN-003](PLAN-003-besoins-application-cliente.md) | Ajouts OE demandés par la migration d'une application cliente | Terminé, lots 2 à 9 livrés, constaté contre `src/` le 2026-09-27 |
| [PLAN-004](PLAN-004-themes-palettes-sdk.md) | Réunion des branches d'une application cliente, bande SDK libre, 10 thèmes et 10 palettes | Terminé, 11 lots ; annexes [journal d'exécution](PLAN-004-execution-log.md) et [maquette de référence](PLAN-004-maquette-themes.html) |
