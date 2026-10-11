<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-019 : Préférences en base, cache local et colonnes de grille

> Statut : **livré**, lots 1 à 5 le 2026-10-11 (plan établi le même jour sur décision du propriétaire).
> Version cible : `1.8.0` (changement cassant assumé en version mineure, décision du 2026-10-11 : « on va
> passer en 1.8 »). Preuves : suite unitaire complète verte (5532 tests après les corrections de revue),
> couverture de 99,93 % des lignes, porte CRAP verte, baseline d'API publique vérifiée (4077 signatures),
> contrôles CSP verts, passe complète des sondes avec `-JsCoverage` verte (8 sondes, couverture JavaScript
> validée) ; chemin base de données éprouvé au navigateur par la base de démonstration de la vitrine
> (`?store=demo`, 3 passes sur 3) ; exemple EF Core du guide compilé hors dépôt. Stabilité de la partie
> `columns` : un échec sur 9 passes, jamais capturé, avant la correction de la sonde (attente du focus
> dans le panneau) ; ensuite 20 passes sur 20. Guide [preferences.md](../preferences.md) et migration
> dans `CHANGELOG.md`. Restent : la relecture par des locuteurs natifs des 8 nouveaux textes de la
> grille, la mise en place dans les applications clientes (propriétaire), et la décision sur un alias
> `[Obsolete]` pour les types de stockage de grille retirés.

## Objectif

Toute préférence d'un utilisateur (apparence, largeur du contenu, état des grilles) est enregistrée dans
la base de l'application quand elle en fournit une, et retrouvée à l'identique sur un autre poste. Le
`localStorage` sert de cache de lecture rapide et de repli quand l'application n'a pas de base. La base
est prioritaire : une valeur plus récente en base remplace le cache, et l'affichage se met à jour seul.
Les grilles gagnent le réordonnancement, le masquage et l'affichage des colonnes, enregistrés comme
préférences, et une réinitialisation de disposition implémentée mais masquée par défaut.

Aujourd'hui : chaque application tient son propre état d'apparence (`AppearanceState` dupliqué dans six
applications, cookie ou `localStorage`), et la grille persiste filtres, tris et largeurs par
`IOmniDataGridStateStore` (`localStorage` par défaut), sans cache ni synchronisation. Les colonnes ne se
réordonnent ni ne se masquent.

## Principes

- Le paquet n'a pas de base : il définit le contrat `IOmniPreferenceStore`, l'application l'implémente
  sur sa base, par utilisateur connecté. Sans implémentation, tout reste en `localStorage`.
- Lecture : cache local d'abord (affichage immédiat), puis la base en arrière-plan ; une valeur de la
  base plus récente que le cache le remplace et prévient les composants.
- Écriture : cache local puis base. Un échec d'écriture en base est journalisé et retenté à la
  synchronisation suivante, sans perdre la valeur locale.
- Synchronisation automatique : périodique (intervalle réglable), au retour sur l'onglet et entre onglets
  du même poste (événement `storage`). Pas de détection de conflit simultané (décision du 2026-10-11).
- Les valeurs sont du JSON versionné par clé ; une valeur illisible garde les défauts.

## Lots

### Lot 1 - Socle des préférences

`IOmniPreferenceStore` (lecture d'une clé, lecture de tout ce qui a changé depuis une date, écriture,
suppression, chaque entrée avec sa date de modification), cache `localStorage` interne, service scoped
`OmniPreferences` (`GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, événement de changement par clé),
synchronisation périodique et au retour sur l'onglet, options d'enregistrement
(`AddOmniEuropePreferences`). Sans store, le service fonctionne en local seul.

Contrôle : tests bUnit du service avec un store en mémoire : cache lu avant la base, base plus récente
qui remplace le cache et lève l'événement, écriture locale gardée quand le store échoue puis rejouée,
mode local seul sans store.

### Lot 2 - Apparence portée par OE

Service `OmniAppearanceState` (mode, thème, palette, police, densité, fond animé, taille du texte,
taille des contrôles, largeur du contenu) lu et écrit par `OmniPreferences`. `OmniThemeScope`,
`OmniAppearanceWindow`, `OmniAppearanceSettings`, `OmniAppMenu` et `OmniMain` le suivent sans câblage
de l'application ; la taille du texte et des contrôles est posée par OE sur la racine du document.
L'instantané de `omni-boot.js` reste le chemin du premier affichage.

Contrôle : tests bUnit (un changement dans la fenêtre d'apparence repeint la portée et écrit la
préférence ; une préférence reçue de la base repeint sans action) ; vitrine vérifiée au navigateur,
console propre.

### Lot 3 - État des grilles par les préférences

`StateKey` passe par `OmniPreferences` ; `IOmniDataGridStateStore` et
`OmniLocalStorageDataGridStateStore` sont retirés (cassant). L'état gagne l'ordre et la visibilité des
colonnes ; un état reçu de la base est réappliqué sans recharger la page.

Contrôle : tests bUnit (restauration complète depuis une préférence, état ancien sans ordre ni
visibilité relu sans perte, mise à jour reçue réappliquée).

### Lot 4 - Réordonner, masquer et réinitialiser les colonnes

`AllowColumnReorder` (glisser l'en-tête, et au clavier par les flèches du sélecteur de colonnes : déplacer à gauche, à
droite), `AllowColumnHide` avec un sélecteur de colonnes (cases à cocher, au moins une colonne
visible), `OmniDataGridColumn.Visible` (défaut) et `Hideable`, méthode publique `ResetLayoutAsync` et
bouton « Réinitialiser la disposition » présent mais masqué par défaut (`ShowResetLayout`, `false`).
Textes dans les 24 langues.

Contrôle : tests bUnit, sonde de navigateur (glisser, clavier, masquage, réinitialisation, annonce au
lecteur d'écran) verte ; démonstration dans la vitrine.

### Lot 5 - Documentation et migration

Guide des préférences (contrat, table conseillée, exemple EF Core par utilisateur, synchronisation),
migration depuis `IOmniDataGridStateStore` et depuis un `AppearanceState` d'application, CHANGELOG,
référence d'API publique, guides de familles.

Contrôle : `eng/Test-PublicApi.ps1` vert après reconstruction Release ; suite complète verte ; sondes
ciblées vertes.

## Hors périmètre

- Mise en place dans les applications clientes : faite par le propriétaire après la livraison.
- Affichage par défaut du bouton de réinitialisation : décidé plus tard.

## Critère de clôture

Les cinq lots ont leur preuve, la CI de `develop` est verte, la `1.8.0` est prête à publier.
