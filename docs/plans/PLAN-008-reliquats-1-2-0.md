<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-008 : Reliquats de la 1.2.0

> Statut : **ouvert**. Établi le 2026-09-29 après la clôture de PLAN-007 ; lots exécutés en parallèle.

## Objectif

Solder ce que PLAN-007 a laissé : les contrats rangés selon le standard du kit, la décision sur les
fichiers privés, trois comportements différés, et `OmniDataGrid.razor.cs` ramené sous la limite de
taille par l'extraction de vrais collaborateurs.

## Décisions (validées le 2026-09-29)

- Publication NuGet de la 1.2.0 et migration des consommateurs : plus tard, hors de ce plan.
- Contrats : `docs/contracts/`, selon le standard du kit.
- KIT-001/KIT-002 (PLAN-005) : le dépôt est public, les fichiers d'agent et de règles restent ignorés
  par Git.
- Proposition au kit `_Generic` (règle `STD-I18N`) : abandonnée.
- Relecture des traductions : faite par le propriétaire.

## Lots

### Lot 1 - Contrats vers `docs/contracts/` (reprend PLAN-001 lot 3)
- [x] `git mv` de `csp-contract.md` et `accessibility-contract.md`, références mises à jour.
Controle : `git grep` des anciens chemins : 0 hors `CHANGELOG.md` et plans datés.

### Lot 2 - Fichiers privés (clôt PLAN-005 KIT-001/KIT-002)
- [x] PLAN-005 clos avec la décision ; registre à jour.
Controle : PLAN-005 dans « Plans livrés ».

### Lot 3 - Comportements différés
- [ ] `OmniPageHeader` : badges et actions repliés à toute largeur quand ils ne tiennent pas, pas
      seulement sur téléphone (le `data-compact` d'Aetheus).
- [ ] `OmniTextBox` avec `Debounce` : le texte en attente n'est plus perdu quand le champ disparaît.
- [ ] Export Markdown : « au moins N » quand la source n'annonce pas de total et que la limite est
      atteinte, texte traduit dans les 24 langues.
Controle : un test par point, qui échoue avant le correctif.

### Lot 4 - `OmniDataGrid.razor.cs` sous la limite de taille
- [ ] Comportements extraits dans des collaborateurs internes (pas de `partial` supplémentaire),
      API publique inchangée.
Controle : `Test-PublicApi.ps1` vert sans mise à jour, suite complète verte, taille du fichier mesurée.

## Ordre et dépendances

Lots 1 à 4 en parallèle, fichiers disjoints.

## Critère de clôture

Chaque lot a sa preuve, CI verte sur `develop`.
