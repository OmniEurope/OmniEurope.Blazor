<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Plans d'implémentation

Tous les plans numérotés du dépôt vivent ici. Le code, les tests et les ADR restent les sources
structurelles ; un plan décrit un travail, pas l'état du produit.

## Nomenclature

- Format obligatoire : `PLAN-NNN-description-minimale.md`.
- `NNN` est dense et suit l'ordre du registre ci-dessous.
- La description emploie le moins de mots possible tout en restant non ambiguë.
- Un plan partiellement livré est réduit à son seul reliquat réel.
- Un plan terminé est déplacé dans l'archive de sa série ([archive/serie-1/](archive/serie-1/) pour PLAN-001 à PLAN-019 de la première série) avec ses annexes ; il garde son numéro.
- Toute suppression ou renumérotation met à jour les références documentaires actives ; les mentions historiques datées restent inchangées.
- Une annexe d'un plan (journal d'exécution, maquette de référence) porte le numéro de son plan et un suffixe.

## Plans actifs

| Plan | Travail restant | ADR |
|---|---|---|
| [PLAN-001](PLAN-001-reliquats-audit-et-retours-ui.md) | Plan unique depuis le 2026-10-12 : reliquats vérifiés de la première série, audit ciblé du 2026-10-11, défauts d'interface reproduits à partir des retours récurrents des applications ; annexe [classement des retours](PLAN-001-annexe-ret003.md) | Sans ADR supplémentaire |
| [PLAN-002](PLAN-002-homogeneite-1-9-0.md) | Homogénéité de l'API publique, défauts et durcissements de l'audit du 2026-10-22, livrés en 1.9.0 (ruptures assumées) | Sans ADR supplémentaire |
| [PLAN-003](PLAN-003-demandes-application-cliente-2026-10-23.md) | Dix demandes d'une application cliente du 2026-10-23, livrées en 1.9.0 en sept lots | Sans ADR supplémentaire |
| [PLAN-004](PLAN-004-demandes-application-cliente-2026-10-23-bis.md) | Seconde demande de la même application cliente du 2026-10-23 (sept points, 1.9.0) | Sans ADR supplémentaire |
| [PLAN-005](PLAN-005-galerie-depuis-demo-applicative.md) | Galerie de la vitrine reprise de la démo d'une application cliente : 71 pages, onglets aperçu, mobile et code, vitrine en anglais et français | Sans ADR supplémentaire |
| [PLAN-006](PLAN-006-demandes-application-cliente-2026-10-28.md) | Neuf demandes d'une application cliente du 2026-10-28 : badges, champ de formulaire, tuile à texte ajusté, filtre de date de grille, trou noir, infobulles | Sans ADR supplémentaire |

## Archives

Plans livrés, conservés pour la traçabilité. La numérotation est repartie à PLAN-001 le 2026-10-12 : la première série (PLAN-001 à PLAN-019) est rangée dans [archive/serie-1/](archive/serie-1/) avec ses numéros d'origine ; un numéro cité sans lien dans un document daté ou un commentaire de code antérieur au 2026-10-12 désigne un plan de cette série. Dans cette série, PLAN-002 à PLAN-004 venaient
de `plans/` (dossier racine ignoré par Git) et ont été renumérotés en dense le 2026-09-20 (PLAN-001,
lot 2) ; leurs anciens numéros locaux étaient respectivement 004, 007 et 008.

| Plan | Objet | État |
|---|---|---|
| [PLAN-001](archive/serie-1/PLAN-001-remise-equerre-kit.md) | Phase A du plan maître le kit PLAN-001 (lot 7) | Terminé le 2026-09-29, 4 lots ; la proposition au kit est abandonnée |
| [PLAN-002](archive/serie-1/PLAN-002-grille-complete.md) | Grille complète et virtualisation d'`OmniDataGrid` | Terminé, 22 cases sur 22 |
| [PLAN-003](archive/serie-1/PLAN-003-besoins-application-cliente.md) | Ajouts OE demandés par la migration d'une application cliente | Terminé, lots 2 à 9 livrés, constaté contre `src/` le 2026-09-27 |
| [PLAN-004](archive/serie-1/PLAN-004-themes-palettes-sdk.md) | Réunion des branches d'une application cliente, bande SDK libre, 10 thèmes et 10 palettes | Terminé, 11 lots ; annexes [journal d'exécution](archive/serie-1/PLAN-004-execution-log.md) et [maquette de référence](archive/serie-1/PLAN-004-maquette-themes.html) |
| [PLAN-005](archive/serie-1/PLAN-005-remediation-audit-2026-09-27.md) | Remédiation de l'audit 360 du 2026-09-27 | Terminé le 2026-09-29, KIT-001/KIT-002 clos par décision (fichiers privés gardés) |
| [PLAN-006](archive/serie-1/PLAN-006-editeur-html-extensible.md) | Éditeur HTML extensible : objet d'extension, primitives, fonctions frontière, suggestion de suite | Terminé le 2026-09-27, consommé par une application cliente (PLAN-009) |
| [PLAN-007](archive/serie-1/PLAN-007-coherence-et-24-langues.md) | Passe de cohérence de la 1.2.0 et textes du paquet et de la vitrine dans les 24 langues de l'Union | Terminé le 2026-09-29, 12 lots sur 12, CI verte à `9889582` |
| [PLAN-008](archive/serie-1/PLAN-008-reliquats-1-2-0.md) | Reliquats de la 1.2.0 : contrats, fichiers privés, comportements différés, fichiers sous 600 lignes effectives | Terminé le 2026-09-29, 5 lots sur 5, CI verte à `35969ce` |
| [PLAN-010](archive/serie-1/PLAN-010-poids-et-langues-longues.md) | Poids de l'assembly et langues longues | Terminé le 2026-10-01, lots 1 à 4 et 6 faits, lot 5 abandonné au profit de la sonde `Languages` |
| [PLAN-011](archive/serie-1/PLAN-011-demandes-application-cliente.md) | Neuf demandes OE de la recette du 2026-10-02 | Terminé le 2026-10-03, 3 lots sur 3, chaque point testé et vérifié en navigateur |
| [PLAN-012](archive/serie-1/PLAN-012-relecture-editeur-html.md) | Relecture branchable dans `OmniHtmlEditor` (orthographe, grammaire) | Terminé le 2026-10-03, 3 lots sur 3, suite à 4101 tests verte |
| [PLAN-009](archive/serie-1/PLAN-009-themes-et-reliquats.md) | Thèmes et reliquats | Archivé le 2026-10-12, code livré ; restent des actions du propriétaire (repris dans le nouveau PLAN-001) |
| [PLAN-013](archive/serie-1/PLAN-013-palette-de-caracteres.md) | Palette de caractères spéciaux sous l'éditeur HTML | Terminé, archivé le 2026-10-12 |
| [PLAN-014](archive/serie-1/PLAN-014-crap-et-zero-point-aveugle.md) | Porte CRAP et zéro point aveugle | Archivé le 2026-10-12 ; reliquat (4 fonctions JavaScript, porte de couverture) repris dans le nouveau PLAN-001 |
| [PLAN-015](archive/serie-1/PLAN-015-barres-de-grille.md) | Barres d'en-tête et de pied de grille | Terminé, archivé le 2026-10-12 |
| [PLAN-016](archive/serie-1/PLAN-016-neutralite-du-depot-public.md) | Neutralité du dépôt public | Archivé le 2026-10-12, code livré ; actions nuget.org et GitHub du propriétaire reprises dans le nouveau PLAN-001 |
| [PLAN-017](archive/serie-1/PLAN-017-corrections-audit-2026-10-07.md) | Corrections de l'audit du 2026-10-07 | Terminé dans la 1.7.0, archivé le 2026-10-12 |
| [PLAN-018](archive/serie-1/PLAN-018-demandes-application-cliente-2026-10-08.md) | Demandes d'une application cliente du 2026-10-08 | Archivé le 2026-10-12 ; chargeur de modules (lots 5 et 6) repris dans le nouveau PLAN-001 |
| [PLAN-019](archive/serie-1/PLAN-019-preferences-en-base.md) | Préférences en base, cache local et colonnes de grille | Archivé le 2026-10-12, livré pour la 1.8.0 ; retrait sec des types de stockage confirmé ; reliquats repris dans le nouveau PLAN-001 |
