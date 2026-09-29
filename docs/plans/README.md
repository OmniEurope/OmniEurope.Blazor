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
| [PLAN-008](PLAN-008-reliquats-1-2-0.md) | Reliquats de la 1.2.0 : lots 1 et 2 faits (contrats, fichiers privés) ; lots 3 (comportements différés) et 4 (découpage d'`OmniDataGrid`) en cours | - |

Total comparable : **2 lots ouverts** dans PLAN-008 (3 et 4).

## Plans livrés (conservés pour la traçabilité)

PLAN-002 à PLAN-004 ont été déplacés de `plans/` (dossier racine ignoré par Git) vers ce répertoire le
2026-09-20 et renumérotés en dense (PLAN-001, lot 2) ; leurs anciens numéros locaux étaient
respectivement 004, 007 et 008.

| Plan | Objet | État |
|---|---|---|
| [PLAN-001](PLAN-001-remise-equerre-kit.md) | Phase A du plan maître `_Generic` PLAN-001 (lot 7) | Terminé le 2026-09-29, 4 lots ; la proposition au kit est abandonnée |
| [PLAN-002](PLAN-002-grille-complete.md) | Grille complète et virtualisation d'`OmniDataGrid` | Terminé, 22 cases sur 22 |
| [PLAN-003](PLAN-003-besoins-aetheus.md) | Ajouts OE demandés par la migration d'Aetheus | Terminé, lots 2 à 9 livrés, constaté contre `src/` le 2026-09-27 |
| [PLAN-004](PLAN-004-themes-palettes-sdk.md) | Réunion des branches Aetheus, bande SDK libre, 10 thèmes et 10 palettes | Terminé, 11 lots ; annexes [journal d'exécution](PLAN-004-execution-log.md) et [maquette de référence](PLAN-004-maquette-themes.html) |
| [PLAN-005](PLAN-005-remediation-audit-2026-09-27.md) | Remédiation de l'audit 360 du 2026-09-27 | Terminé le 2026-09-29, KIT-001/KIT-002 clos par décision (fichiers privés gardés) |
| [PLAN-006](PLAN-006-editeur-html-extensible.md) | Éditeur HTML extensible : objet d'extension, primitives, fonctions frontière, suggestion de suite | Terminé le 2026-09-27, consommé par Astraia (PLAN-009) |
| [PLAN-007](PLAN-007-coherence-et-24-langues.md) | Passe de cohérence de la 1.2.0 et textes du paquet et de la vitrine dans les 24 langues de l'Union | Terminé le 2026-09-29, 12 lots sur 12, CI verte à `9889582` |
