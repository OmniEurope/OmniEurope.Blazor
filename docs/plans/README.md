<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Plans d'implémentation

Ce répertoire ne liste que les plans contenant du travail encore ouvert. Le code, les tests et les ADR restent les sources structurelles ; Git conserve l'historique des plans terminés ou supprimés.

## Nomenclature

- Format obligatoire : `PLAN-NNN-description-minimale.md`.
- `NNN` est dense et suit l'ordre du registre ci-dessous.
- La description emploie le moins de mots possible tout en restant non ambiguë.
- Un plan partiellement livré est réduit à son seul reliquat réel.
- Un plan terminé, remplacé, hors produit ou entièrement transféré est supprimé du registre actif.
- Toute suppression ou renumérotation met à jour les références documentaires actives ; les mentions historiques datées restent inchangées.
- Un plan numéroté vit ici, jamais sous `.claude/` : `.claude/plan.md` est la vue de contrôle de `/plan` et peut au plus pointer vers le plan numéroté actif.
- Les plans locaux de `plans/` (racine, ignoré par Git) ne sont pas encore déplacés ici : leur déplacement et leur renumérotation attendent la validation de l'utilisateur ([PLAN-001](PLAN-001-remise-equerre-kit.md), lot 2).

## Plans actifs

| Plan | Travail restant | ADR |
|---|---|---|
| [PLAN-001](PLAN-001-remise-equerre-kit.md) | Phase A du plan maître `_Generic` PLAN-001 (lot 7) : lot 1 livré, lots 2 à 4 à valider | ADR à créer si la décision 1 retient l'exception `STD-SDKPIN` |

Total comparable : **3 lots ouverts** (2, 3, 4).
