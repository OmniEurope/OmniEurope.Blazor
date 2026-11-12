<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09, R1-24 archivée le 2026-11-12 ; nouvelle passe le 2026-11-12, numérotation repartie à 1 après l'archive ; R1-1 à R1-4 de cette passe faites, prouvées et archivées le 2026-11-12 dans `recette-oe-fait.md`. Nouvelle série de retours le 2026-11-12, numérotée à partir de 1 (R1-1 à R1-7), faite, prouvée et archivée le 2026-11-12. Aucun point ouvert. Prochain numéro : **R1-1** (numérotation repartie à 1 après l'archive).

## État

Le dépôt est le paquet lui-même : chaque point est d'origine paquet (OE). Les demandes viennent du
propriétaire ou de la recette d'une application cliente, dont le numéro est rappelé dans l'origine.
Traitement direct demandé par le propriétaire le 2026-10-09.

Historique de la passe précédente (anciens R1-1 à R1-29, nettoyage du 2026-10-29 et lots du 2026-10-29) : voir `recette-oe-fait.md` et l'historique Git de ce fichier.

### Lots du 2026-11-12 (R1-1 à R1-3)

Branche `develop`, quatre agents au plus en parallèle.

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| A, réglage Fond | R1-2 (modèle du fond, défauts par thème, persistance, lignes Fond et Animation, Givre et Trou noir repris) | opus medium | 1 | Fait, prouvé (8430a58e) |
| B, fenêtre compacte | R1-1 | sonnet high | 2 | Fait, prouvé (da0b14e0, 5cbe4cec) |
| C1, fonds Ardoise à Octet | R1-3 (Ardoise, Galet, Halo, Néon, Papier, Rétro, Octet) | sonnet high | 2 | Fait, prouvé (5006db7a, 39f89d03) |
| C2, fonds Nénuphar à Épure | R1-3 (Nénuphar, Velours, Relief, Aplat, Épure) | sonnet high | 2 | Fait, prouvé (6ca60e28) |

### Lots du 2026-11-12, retours sur la fenêtre (deuxième série R1-1 à R1-7)

Branche `develop`, deux agents en parallèle, chacun dans son worktree.

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| D, fenêtre d'apparence du paquet | R1-1, R1-3, R1-4, R1-5, R1-6, R1-7 | sonnet high | 1 | Fait, prouvé (ac6f5d72, 697073b0) |
| E, fenêtre principale de la vitrine | R1-2 | sonnet medium | 1 | Fait, prouvé (64d29d6f) |

## Légende

- Type : Bug, Moche, Erreur, Autre.
- État : Ouvert, Ouvert décidé, Question, Fait (prouvé), Écarté, En attente.

## Points

| N° | Page / composant | Type | Constat | Origine | Justification | État |
| --- | --- | --- | --- | --- | --- | --- |

## Critère de clôture

La recette se ferme quand chaque ligne est faite et prouvée par une observation (mesure, capture,
test qui échouait avant), archivée dans `recette-oe-fait.md`, et que la compilation, la suite de tests
et les gardes d'architecture passent réellement.
