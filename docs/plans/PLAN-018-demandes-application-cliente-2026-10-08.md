<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-018 : Demandes d'une application cliente du 2026-10-08

> Statut : **lots 1 à 4 livrés** le 2026-10-08 (bUnit, sondes de la vitrine et du catalogue vertes) ; lot 5 à faire dans la version suivante. Cinq demandes issues de l'audit d'une application cliente,
> acceptées toutes les cinq par le propriétaire le 2026-10-08. Lots 1 à 4 livrés avec la `1.7.0` (pas
> encore publiée sur NuGet, son tag sera refait à la publication) ; lot 5 dans une version à part, après
> la publication NuGet de la `1.7.0` (décision du 2026-10-08), codé puis mis de côté hors du dépôt.

## Objectif

Corriger quatre défauts qui touchent tout hôte et ajouter le lien d'évitement qui manque, chacun prouvé
par un test, et dans le navigateur quand il passe par un script.

## Lots

### Lot 1 - Infobulles de titre

Quand `OmniTitleTooltips` retire le `title` d'un élément survolé ou focalisé, un élément que ce titre
nommait seul (bouton à icône seule sans `Label`) garde un nom : le titre devient son `aria-label` le
temps du survol, retiré ensuite.

Contrôle : sonde de la vitrine, nom accessible du bouton identique avant, pendant et après le survol et
le focus ; un élément déjà nommé par son texte ou son `aria-label` n'en reçoit pas.

### Lot 2 - Grille détruite

`SetFiltersAsync`, `ScrollToIndexAsync`, `RefreshAsync` et `ReloadAsync` ne font rien sur une grille en
cours de destruction ; le module de script est oublié une fois libéré.

Contrôle : test bUnit, chaque appel après `DisposeAsync` se termine sans exception ni appel de script.

### Lot 3 - Notifications sur écran étroit

Sous 40 rem, la zone des notifications se pose en bas, sur toute la largeur moins une marge, quelle que
soit sa position.

Contrôle : test de la feuille et mesure au navigateur à 390 px (zone en bas, largeur de la fenêtre moins
les marges, en-tête de page découvert).

### Lot 4 - Lien d'évitement

`OmniSkipLink` : caché jusqu'au focus, il donne le focus à la cible (`OmniMain` par défaut) par script au
lieu de suivre son `href`, qui sous `<base href="/">` mène à une autre adresse. Texte dans les 24 langues,
démonstration dans la vitrine.

Contrôle : tests bUnit, sonde de la vitrine (Tab puis Entrée donne le focus au contenu principal sans
changer l'adresse), couverture de la vitrine et de l'API publique à jour.

### Lot 5 - Chargement des modules de script (version suivante)

Un seul chargeur des modules de script : `JSException` (WebAssembly) traitée comme une perte de circuit,
une nouvelle tentative, puis repli sans casser le composant. Chaque import des composants y passe.
L'adresse n'est pas versionnée par une requête, qui chargerait un second exemplaire des modules que les
autres importent par adresse relative ; la mise en cache des versions revient à `MapStaticAssets`.

Contrôle : test qui échoue sur un import direct restant hors du chargeur ; tests bUnit du repli et de la
nouvelle tentative ; suite complète et vitrine vertes.

### Lot 6 - Clôture

Suite complète, portes (couverture, CRAP, CSP, API publique, budgets, paquet), vitrine complète et hôtes.

Contrôle : tout est vert ; journal des modifications à jour.
