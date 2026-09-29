<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-008 : Reliquats de la 1.2.0

> Statut : **terminé** le 2026-09-29, 5 lots sur 5 (a4cc4cf, 35969ce), CI verte.

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
- [x] `OmniPageHeader` : badges et actions repliés à toute largeur quand ils ne tiennent pas, pas
      seulement sur téléphone (le `data-compact` d'Aetheus).
- [x] `OmniTextBox` avec `Debounce` : le texte en attente n'est plus perdu quand le champ disparaît.
- [x] Export Markdown : « au moins N » quand la source n'annonce pas de total et que la limite est
      atteinte, texte traduit dans les 24 langues.
Controle : un test par point, qui échoue avant le correctif.

### Lot 4 - `OmniDataGrid.razor.cs` sous la limite de taille
- [x] Comportements extraits dans des collaborateurs internes (pas de `partial` supplémentaire),
      API publique inchangée.
Controle : `Test-PublicApi.ps1` vert sans mise à jour, suite complète verte, taille du fichier mesurée.

### Lot 5 - Autres fichiers au-delà de 600 lignes effectives
La limite compte les lignes de code effectives, sans lignes vides ni commentaires (doc XML comprise),
mesurées le 2026-09-29.
- [x] `omni-html-editor.js` (1668), `omni-grid.js` (1054), `omni-focus.js` (642) : modules ES importés
      par le module d'entrée, exports inchangés.
- [x] `OmniMindMap.razor.cs` (1264) et `PhosphorIconGlyphs.cs` (738) : collaborateurs internes.
- [x] `OmniHtmlEditor.razor.cs` (1085) : collaborateurs internes.
- [x] `omnieurope.blazor.css` (2817) : sources découpées, une seule feuille livrée au même chemin.
Controle : aucun fichier de `src/` au-delà de 600 lignes effectives ; API publique, suite et sondes vertes.

## Ordre et dépendances

Lots 1 à 5 en parallèle, fichiers disjoints ; la feuille de style après le lot 3, qui touche le bloc
de l'en-tête.

## Critère de clôture

Chaque lot a sa preuve, CI verte sur `develop`.
