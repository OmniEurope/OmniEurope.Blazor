<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Architecture Decision Records

Ce répertoire consigne les décisions d'architecture durables du dépôt, celles qui ont des alternatives
et des conséquences à long terme. Une décision déjà tracée dans un document canonique (`docs/`,
`CHANGELOG.md`) n'a pas besoin d'un ADR en double. Une exception à une règle du kit `_Generic` n'est
valide que si un ADR de ce dépôt la porte.

## Nomenclature

- Format obligatoire : `ADR-NNN-titre-court.md`.
- `NNN` est dense, sur trois chiffres, attribué dans l'ordre de l'index ci-dessous.
- Chaque ADR porte un statut (`Proposé`, `Accepté`, `Remplacé par ADR-NNN`), une date, le contexte,
  la décision et ses conséquences.

## Index

| ADR | Titre | Statut |
|---|---|---|
| [ADR-001](ADR-001-global-json-rollforward-latestpatch.md) | `global.json` garde `rollForward: latestPatch` (dérogation à `STD-SDKPIN` du kit) | Remplacé par ADR-002 |
| [ADR-002](ADR-002-sdk-floor-latestfeature-pinned-implicit-packs.md) | Plancher SDK `10.0.100` en `latestFeature`, paquets implicites du SDK épinglés | Accepté |

Prochain numéro libre : `ADR-003`.
