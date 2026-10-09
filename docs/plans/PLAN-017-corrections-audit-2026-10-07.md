<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-017 : Corrections de l'audit du 2026-10-07

> Statut : **livré** le 2026-10-08, lots 1 à 5 faits, dans la `1.7.0` : suite de 5338 tests, couverture,
> CRAP, API publique, CSP, budgets, paquet, hôtes catalogue et WebAssembly (fr, en) et les huit sondes de
> la vitrine verts. Établi le 2026-10-08. Décision du propriétaire du 2026-10-08 : corriger les
> constats utiles, suivre les recommandations sur les deux points à trancher (légende de droite au
> clavier, promesse des 44 px alignée sur R-025), écarter les constats marginaux.

## Objectif

Corriger les défauts confirmés par l'audit 360 du 2026-10-07 (révision `5845f7f`), chacun prouvé par un
test qui échoue avant le correctif. Écartés sur décision : ARCH-001 (formulation documentaire),
RCL-001 (`aria-busy` du chevron), RCL-005 et RCL-010 (débordements aux bornes théoriques), RCL-009
(`NaN` dans `Report`).

## Lots

### Lot 1 - Identifiants uniques par instance

- RCL-004 : filtres d'`OmniDataGrid`, sélecteur de taille d'`OmniPager` et cellules d'`OmniSpreadsheet`
  prennent un préfixe généré par instance quand `Id` est absent.
- RCL-006 : le filtre `MultiSelect` en liste sans recherche porte un élément d'identifiant réel.
- RCL-014 : `OmniAppMenu` transmet `Id` à son déclencheur.

Contrôle : deux instances sans `Id` rendues côte à côte n'ont aucun identifiant en double ; chaque
`aria-labelledby` et chaque `for` désignent un élément existant.

### Lot 2 - Clavier

- RCL-008 : Entrée ou Espace sur un bouton d'un élément d'arborescence ne sélectionne qu'une fois.
- RCL-017 : `OmniSelectBar` et la fenêtre d'apparence suivent le modèle du groupe radio (un seul arrêt
  de tabulation, flèches, Début, Fin).
- RCL-003 : une entrée de légende à droite qui masque sa série est atteignable par Tab et s'active par
  Entrée ou Espace, sans changer la géométrie.

Contrôle : tests bUnit des attributs et sondes de la vitrine dans le navigateur (une bascule par geste,
flèches, Espace sans défilement de la page).

### Lot 3 - Comportements

- RCL-013 : retirer `Load` du planning annule le chargement en cours et efface son état.
- RCL-011 : un validateur qui change de champ oublie l'erreur de l'ancien.
- RCL-016 : `OmniReturnUrl.Append` place le paramètre avant le fragment.
- RCL-012 : le parseur de diff décode les chemins que Git met entre guillemets.
- RCL-007 : la recherche du journal va d'occurrence en occurrence, comme la documentation le promet.
- RCL-015 : un onglet à `TitleContent` garde `Title` comme nom accessible.

Contrôle : un test par constat, rouge avant le correctif et vert après.

### Lot 4 - Outillage, documentation et catalogue

- ENG-001 : la sonde des scripts refuse un lot inconnu ou une sélection vide.
- ENG-002 : la sonde WebAssembly attend les textes de la langue demandée.
- TEST-001 : le test d'annulation attend la fin de la validation périmée avant de conclure.
- DOC-002 : la documentation promet les tailles de R-025 et nomme les commandes qui gardent 44 px.
- HOST-001 : la table alternative du graphique du catalogue reprend ses trois mesures.

Contrôle : sondes exécutées (lot inconnu refusé, fr et en vertes), recherche des 44 px sans promesse
générale restante, table du catalogue vérifiée en bUnit ou au navigateur.

### Lot 5 - Clôture

Suite complète, portes (couverture, CRAP, CSP, API publique, budgets, paquet) et vitrine complète.

Contrôle : tout est vert, le journal des modifications et la référence d'API publique sont à jour.
