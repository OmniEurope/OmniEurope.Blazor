<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Plans d'implémentation

Tous les plans numérotés du dépôt vivent ici. Le code, les tests et les ADR restent les sources
structurelles ; un plan décrit un travail, pas l'état du produit.

## Nomenclature

- Format obligatoire : `PLAN-NNN-description-minimale.md`.
- `NNN` est dense et suit l'ordre du registre ci-dessous.
- La description emploie le moins de mots possible tout en restant non ambiguë.
- Un plan partiellement livré est réduit à son seul reliquat réel.
- Un plan terminé est déplacé dans [archive/](archive/) avec ses annexes ; il garde son numéro.
- Toute suppression ou renumérotation met à jour les références documentaires actives ; les mentions historiques datées restent inchangées.
- Une annexe d'un plan (journal d'exécution, maquette de référence) porte le numéro de son plan et un suffixe.

## Plans actifs

| Plan | Travail restant | ADR |
|---|---|---|
| [PLAN-009](PLAN-009-themes-et-reliquats.md) | Décisions du propriétaire (historique public, Trou noir) ; prévenir la session d'une application cliente à la livraison (lots 8 et 9) | Sans ADR supplémentaire |

## Archives

Plans livrés, conservés pour la traçabilité dans [archive/](archive/). PLAN-002 à PLAN-004 venaient
de `plans/` (dossier racine ignoré par Git) et ont été renumérotés en dense le 2026-09-20 (PLAN-001,
lot 2) ; leurs anciens numéros locaux étaient respectivement 004, 007 et 008.

| Plan | Objet | État |
|---|---|---|
| [PLAN-001](archive/PLAN-001-remise-equerre-kit.md) | Phase A du plan maître le kit PLAN-001 (lot 7) | Terminé le 2026-09-29, 4 lots ; la proposition au kit est abandonnée |
| [PLAN-002](archive/PLAN-002-grille-complete.md) | Grille complète et virtualisation d'`OmniDataGrid` | Terminé, 22 cases sur 22 |
| [PLAN-003](archive/PLAN-003-besoins-application-cliente.md) | Ajouts OE demandés par la migration d'une application cliente | Terminé, lots 2 à 9 livrés, constaté contre `src/` le 2026-09-27 |
| [PLAN-004](archive/PLAN-004-themes-palettes-sdk.md) | Réunion des branches d'une application cliente, bande SDK libre, 10 thèmes et 10 palettes | Terminé, 11 lots ; annexes [journal d'exécution](archive/PLAN-004-execution-log.md) et [maquette de référence](archive/PLAN-004-maquette-themes.html) |
| [PLAN-005](archive/PLAN-005-remediation-audit-2026-09-27.md) | Remédiation de l'audit 360 du 2026-09-27 | Terminé le 2026-09-29, KIT-001/KIT-002 clos par décision (fichiers privés gardés) |
| [PLAN-006](archive/PLAN-006-editeur-html-extensible.md) | Éditeur HTML extensible : objet d'extension, primitives, fonctions frontière, suggestion de suite | Terminé le 2026-09-27, consommé par une application cliente (PLAN-009) |
| [PLAN-007](archive/PLAN-007-coherence-et-24-langues.md) | Passe de cohérence de la 1.2.0 et textes du paquet et de la vitrine dans les 24 langues de l'Union | Terminé le 2026-09-29, 12 lots sur 12, CI verte à `9889582` |
| [PLAN-008](archive/PLAN-008-reliquats-1-2-0.md) | Reliquats de la 1.2.0 : contrats, fichiers privés, comportements différés, fichiers sous 600 lignes effectives | Terminé le 2026-09-29, 5 lots sur 5, CI verte à `35969ce` |
| [PLAN-010](archive/PLAN-010-poids-et-langues-longues.md) | Poids de l'assembly et langues longues | Terminé le 2026-10-01, lots 1 à 4 et 6 faits, lot 5 abandonné au profit de la sonde `Languages` |
