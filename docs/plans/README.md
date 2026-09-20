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
- Un plan numéroté vit ici, jamais sous `.claude/` : `.claude/plan.md` est la vue de contrôle de `/plan` et peut au plus pointer vers le plan numéroté actif.
- Une annexe d'un plan (journal d'exécution, maquette de référence) porte le numéro de son plan et un suffixe.

## Plans actifs

| Plan | Travail restant | ADR |
|---|---|---|
| [PLAN-001](PLAN-001-remise-equerre-kit.md) | Phase A du plan maître `_Generic` PLAN-001 (lot 7) : lots 1 et 2 livrés, lot 3 (contrats) et lot 4 (`STD-I18N` de la vitrine) ouverts | [ADR-001](../adr/ADR-001-global-json-rollforward-latestpatch.md) |
| [PLAN-003](PLAN-003-besoins-aetheus.md) | Ajouts OE demandés par la migration d'Aetheus ; statut « aucun lot démarré » du 2026-09-14, à confirmer contre l'état livré par PLAN-004 | - |

Total comparable : **2 lots ouverts** dans PLAN-001 (3 et 4), plus PLAN-003 à confirmer.

## Plans livrés (conservés pour la traçabilité)

Déplacés de `plans/` (dossier racine ignoré par Git) vers ce répertoire le 2026-09-20 et renumérotés
en dense (PLAN-001, lot 2). Leurs anciens numéros locaux étaient respectivement 004, 007 et 008.

| Plan | Objet | État |
|---|---|---|
| [PLAN-002](PLAN-002-grille-complete.md) | Grille complète et virtualisation d'`OmniDataGrid` | Terminé, 22 cases sur 22 |
| [PLAN-004](PLAN-004-themes-palettes-sdk.md) | Réunion des branches Aetheus, bande SDK libre, 10 thèmes et 10 palettes | Terminé, 11 lots ; annexes [journal d'exécution](PLAN-004-execution-log.md) et [maquette de référence](PLAN-004-maquette-themes.html) |
