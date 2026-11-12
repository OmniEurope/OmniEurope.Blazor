<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09, **aucune ligne ouverte** depuis le 2026-11-12 (R1-24 archivée). Prochain numéro : **R1-30**.

## État

Le dépôt est le paquet lui-même : chaque point est d'origine paquet (OE). Les demandes viennent du
propriétaire ou de la recette d'une application cliente, dont le numéro est rappelé dans l'origine.
Traitement direct demandé par le propriétaire le 2026-10-09.

Nettoyage du 2026-10-29 : R1-1 à R1-16, faites et prouvées (livrées avec 1.10.0), sont archivées dans
`recette-oe-fait.md`. Les six lignes restantes ont été revérifiées dans le code le même jour : aucune
n'est encore faite (badge toujours dans `MainLayout.razor:23`, voile `.omni-busy` toujours pulsé,
aucun voile de page, `OmniMain` de la vitrine sans `Scrollable`, aucun commit sur l'espacement des champs
depuis la réserve de la ligne d'erreur).

### Lots du 2026-10-29 (R1-25 à R1-29)

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| Direct | R1-26, R1-27 | orchestrateur | 1 | Fait, archivé |
| A | R1-25 | sonnet high | 1 | Fait, archivé |
| B | R1-28 filtre du menu et de l'Overview | sonnet high | 1 | Fait, archivé |
| C | R1-28 recherche d'en-tête | sonnet high | 1 | Fait, archivé |
| D | R1-29 | sonnet high | 1 | Fait, archivé |

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
