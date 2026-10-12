<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-001 : Reliquats, audit du 2026-10-11 et retours UI récurrents

> Statut : **ouvert**. Établi le 2026-10-12 : les plans actifs de la première série (PLAN-009, PLAN-013
> à PLAN-019) ont été vérifiés contre le code puis archivés dans [archive/serie-1/](archive/serie-1/) ;
> leurs reliquats réels sont repris ici. Lot 1 fait sauf la CI (branche non publiée), lot 2 en cours, lot 5 fait, lots 3, 4 et 6 à venir.

## Objectif

Un seul plan pour tout le travail restant d'OmniEurope.Blazor : fermer les reliquats vérifiés de la
première série, appliquer ce qui est utile dans l'audit ciblé du 2026-10-11, et corriger à la source,
dans le paquet, les défauts d'interface que les retours récurrents des applications (RET-003 du kit,
travers D22 à D35) ont permis de reproduire, pour que le bon comportement soit celui par défaut.

## État des lieux (2026-10-12)

- Série 1 vérifiée plan par plan contre `src/`, `tests/`, `eng/` et `git log` : PLAN-013, PLAN-015 et
  PLAN-017 livrés ; PLAN-009 et PLAN-016 livrés côté code (restent des actions du propriétaire) ;
  PLAN-014, PLAN-018 et PLAN-019 partiels (reliquats repris aux lots 1 à 4).
- CI de `develop` rouge sur `bc7acdb` (étape « Verify coverage artifact ») : 3 fichiers des préférences
  ont des lignes ou branches jamais exécutées (`OmniPreferences.cs`, `GridStatePersistence.cs`,
  `AppearanceDocumentRoot.cs`).
- Audit ciblé du 2026-10-11 (révision `c60a9f6`) : 5 constats, ROB-001 à ROB-003, ARCH-001, KIT-001.
- RET-003 : 715 citations D22 à D35 lues, 341 hors périmètre (projets sans OE, code et contenu des
  applications, comportement de l'agent), 374 regroupées en 82 groupes par composant. Classement et
  preuves : [annexe](PLAN-001-annexe-ret003.md). Reproduction faite dans la vitrine publiée
  (navigateur intégré, `artifacts/showcase-smoke`, build du 2026-10-11) pour chaque candidat
  reproductible sans modifier la démo.

## Décisions (validées le 2026-10-12)

- Types de stockage de grille (`IOmniDataGridStateStore`, `OmniLocalStorageDataGridStateStore`,
  `OmniDataGrid.StateStore`) : retrait sec en 1.8.0, sans alias `[Obsolete]`, pas de code mort.
- Plans : numérotation repartie à PLAN-001, un seul plan actif ; la première série garde ses numéros
  sous `archive/serie-1/`.
- Constats d'audit : appliqués seulement s'ils sont reproduits à `HEAD` et utiles ; ARCH-001 est une
  décision d'architecture, pas un correctif (voir « Décisions à prendre »).

## Décisions prises le 2026-10-12 (sur recommandation, accord du propriétaire)

- D1, ARCH-001 : la dépendance des collaborateurs de grille et de carte mentale à leur composant est
  assumée ; `docs/architecture.md` distingue désormais moteurs purs et adaptateurs de composant. Pas de
  refonte.
- D2, `ScrollMode` : `Paged` reste le défaut. Le défilement virtuel exige une hauteur bornée ; changer
  le défaut casserait toute grille posée dans une page sans hauteur. À fixer dans le préréglage de
  grille de chaque application.
- D3, lignes alternées : actives par défaut (`AllowAlternatingRows = true`), 4 % en clair, 5 % en sombre
  (`--omni-grid-stripe-share`).
- D4, police d'en-tête : égale à celle des cellules (`--omni-grid-header-font-size`, `1em`).
- D5, en-tête d'une colonne numérique : reste aligné sur ses chiffres (usage courant des tableaux de
  données, une seule plainte).
- D6, identifiants entiers : restent alignés comme des nombres ; `TextAlign` règle la colonne (citations
  contradictoires).
- D7 : les 6 corrections A du lot 5 sont appliquées.

## Lots

Chaque lot se termine par un contrôle mesurable avant de passer au suivant.

### Lot 1 - Audit du 2026-10-11 et CI rouge
- [x] ROB-001 : magasin de notifications sous un verrou unique, instantané en lecture seule, génération
  par compte à rebours (`OmniNotificationStore.cs`) ; 3 tests rouges avant, verts après.
- [x] ROB-002 : un envoi annulé n'est plus annoncé terminé ni attaché, même si le rappel revient
  normalement ; rien après la libération (`OmniUpload.razor.cs`) ; 2 tests rouges avant, verts après.
- [x] ROB-003 : le scénario de l'audit (`localStorage` refusé) ne reproduit plus à `HEAD` (gardé en
  test de régression) ; la variante par le script des préférences (`JSException` à la lecture, au quota,
  à l'import) cassait encore la grille : 2 tests rouges, corrigé dans `GridStatePersistence`, un seul
  avertissement par grille, erreurs d'un `IOmniPreferenceStore` inchangées.
- [x] Porte de couverture : écarts des 3 fichiers fermés (code mort retiré, tests de comportement),
  aucune exception ajoutée : 99,93 % des lignes, 99,5 % des branches.
- [x] KIT-001 : garde STD-FILESIZE (`tests/.../Guards/FileSizeAuditTests.cs`, `RepositoryScan.cs`) ;
  601 lignes effectives refusées, 600 acceptées, fichier d'essai retiré.
Controle (2026-10-12, sauf CI) : suite complète 5567 sur 5567, `eng/Test-Coverage.ps1`, `eng/Test-Crap.ps1`, `eng/Test-PublicApi.ps1`
verts ; la garde de taille échoue sur un fichier temporaire de 601 lignes effectives ; CI de `develop`
verte après publication de la branche.

### Lot 2 - Petits reliquats de la série 1
- [x] `docs/localization.md`, « Limites connues » : les 8 textes de grille de la 1.8.0 non relus par
  des locuteurs natifs (origine PLAN-019).
- [x] `OmniPopover` : ne pas déplacer le focus s'il est déjà dans le panneau (`omni-focus.js`,
  `attachPopover`), durcissement sans défaut observé ; vérifié dans Chromium sur une page isolée, la
  sonde `Modules` reste à rejouer.
- [ ] Couverture JavaScript : fermer ou admettre avec raison les 4 fonctions de
  `eng/js-coverage-baseline.json` (`omni-boot.js`, 2 dans `omni-grid.js`, `setDocumentMetadata` dans
  `omniInterop.js`) (origine PLAN-014).
Controle : grep des 8 clés dans `docs/localization.md` ; `eng/js-coverage-baseline.json` à
`"files": []` et `eng/Test-ShowcaseHost.ps1 -JsCoverage` vert.

### Lot 3 - Chargeur unique des modules de script (origine PLAN-018, lot 5)
- [ ] Un chargeur interne pour tous les `import` de modules : `JSException` d'un circuit perdu traitée
  comme telle, une nouvelle tentative, puis repli.
- [ ] Remplacer les 46 appels directs `InvokeAsync<IJSObjectReference>("import", ...)` de la RCL.
- [ ] Test de garde : aucun `import` de module hors du chargeur ; tests bUnit du repli et de la
  nouvelle tentative.
Controle : la garde échoue sur un appel direct ajouté pour l'essai ; tests du chargeur verts.

### Lot 4 - Clôture du chargeur (origine PLAN-018, lot 6)
- [ ] Suite complète, portes (couverture, CRAP, CSP, API publique, budgets, paquet), vitrine et hôtes.
- [ ] `CHANGELOG.md` à jour.
Controle : toutes les portes vertes, sortie de chaque commande consignée.

### Lot 5 - Défauts d'interface reproduits (RET-003), trié par messages × projets
Chaque case a été reproduite dans la vitrine le 2026-10-12 ; la même mesure, refaite après correction en
clair et en sombre, à 375 et 1440 px, sans erreur console, fait le contrôle.
- [x] G-D23-03, `OmniStatTile` (7 messages, 2 projets) : tuiles d'une même rangée de hauteurs inégales
  (84 et 68 px mesurés sur `/composants/pages`) ; `block-size: 100%` comme `OmniNavTile`.
- [x] G-D35-02, `OmniChart` (4, 2) : sans `AspectRatio`, le graphique est carré et plus haut que la
  fenêtre (1126 × 1126 px dans une fenêtre de 1080 px, `/composants/graphiques`) ; plafond de hauteur
  par défaut et paramètre `MaxHeight`.
- [x] G-D25-03, `OmniButton` (3, 2) : un bouton icône seule sans `Label` n'est pas carré (34 × 20 px
  contre 20 × 20 px, `/composants/boutons`) ; carré selon le contenu, `title` en repli d'`aria-label`.
- [x] G-D29-03, `OmniDataGrid` (2, 1) : Entrée n'enregistre pas et Échap n'annule pas une ligne en
  édition (`/composants/grille`, `demo-grid-edit`, touches réelles) ; Entrée enregistre, Échap annule,
  hors zone de texte multiligne.
- [x] G-D26-04, `OmniDataGrid` (1, 1) : la ligne grandit en passant en édition (40 puis 45 px,
  `demo-grid-edit`) ; hauteur de ligne en édition contenue pour toutes les grilles, éditeurs enveloppés
  compris.
- [x] G-D26-05, `OmniLogViewer` (1, 1, latent) : les numéros de ligne partent avec le défilement
  horizontal (`/composants/listes`, 375 px) ; numéro et heure collants.
Controle : par case, la mesure de reproduction redonne la valeur attendue (hauteurs égales, hauteur
plafonnée, bouton carré, ligne quittée par Entrée et Échap, hauteur inchangée, numéro fixe), test bUnit
ou sonde ajouté, suite complète verte.
Fait le 2026-10-12, vitrine republiée, navigateur intégré : tuiles 84, 84, 84 px ; graphique 895 × 512 px
(plafond 32rem) à 1440 px, 325 × 325 px à 375 px ; bouton icône seule 20 × 20 px même sans `aria-label` ;
ligne 40 px en lecture et en édition ; Entrée enregistre, Échap annule, focus rendu à la ligne (touches
réelles) ; gouttière du journal immobile au défilement à 375 px, teinte de ligne suivie en sombre ;
alternance 5 % en sombre ; aucune erreur console liée. Suite complète 6043 sur 6043, couverture, CRAP,
API publique (4081 signatures) et CSP vertes.

### Lot 6 - Candidats à reproduire avant classement (RET-003)
Preuve dans le code, pas encore de reproduction : chacun commence par un test ou une démo qui échoue,
sinon il est clos en B ou C.
- [ ] G-D28-02, `OmniPanelMenuItem` (3, 3) : un groupe avec `Href` navigue sans se déplier.
- [ ] G-D28-01, `OmniPanelMenuItem` (4, 2) : deux frères dont l'un préfixe l'autre sont actifs ensemble.
- [ ] G-D23-09, `OmniNotification` (3, 2) : cartes courtes et longues de largeurs différentes dans une
  même pile.
- [ ] G-D30-01, `OmniDataAnnotationsValidator` (2, 2) : un `IValidatableObject` affiche la clé de
  ressource brute.
- [ ] G-D28-04, menus (4, 2) : menu ouvert resté invisible quand le placement abandonne.
- [ ] G-D34-02, `OmniTooltip` : `title` d'un descendant qui double l'infobulle.
- [ ] G-D24-04, `OmniDataGrid` : alternance des lignes peu visible en sombre.
- [ ] G-D22-04, `OmniDataGrid` : ascenseur horizontal dû au plancher de 8rem par colonne.
- [ ] Débordement en largeur à 390 px en fi, el et hu (jusqu'à 72 px) sous certains thèmes (Néon, Halo),
  vu par la sonde `Languages` quand le navigateur gardait le thème laissé par Contrast (2026-10-12) ;
  la passe par défaut, qui mesure sous le thème livré, ne le voit pas.
Controle : pour chaque case, test rouge puis vert, ou classement motivé en B ou C dans l'annexe.

### Lot 7 - Demandes d'applications clientes
- [x] R3-6 (2026-10-12) : `OmniFormField` affiche la marque d'obligation quand la propriété liée au contrôle
  (expression de `@bind-Value`) porte `[Required]` ; un `Required` posé explicitement reste prioritaire
  (vrai force, faux masque), aucun effet sans expression. Change un rendu par défaut visible.
- [x] `OmniDataGrid.FillAvailableHeight` (demande du 2026-10-12) : dans un conteneur défilant étroit (390 × 844,
  réglages au-dessus), avec `MinHeight=12rem`, la grille mesurait 0 px et son cadre 2 px, lignes présentes
  mais invisibles, en virtuel comme en pagination. Départager `attachFill` (`omni-grid.js`, espace
  disponible, `MinHeight`, barre d'export) d'une règle d'application en flex (`flex: 1 1 0`,
  `min-block-size: 0`) ; une grille arborescente dans une carte sans hauteur gardait aussi un cadre de
  2 px malgré `Height`. Reproduire dans la vitrine, avec et sans cette règle.
  Résultat : reproduit dans la vitrine (`#demo-grid-fill-box`), à l'identique avec et sans la règle
  d'application (grille 0 px, cadre 2 px, viewport 480 px en virtuel, 192 px en pagination) : défaut du
  paquet. La mesure 0 d'`attachFill` est juste ; le plancher manquait dans la feuille (racine
  `min-block-size: 0`, cadre `overflow: hidden` donc écrasable). Corrigé : racine
  `min-block-size: min-content`, cadre `overflow: clip` (en colonne flex en remplissage), viewport
  `flex: 1 1 0`. Après : grille 227 px (virtuel) ou 267 px (pagination), viewport 192 px, cellules
  visibles une fois l'aperçu défilé, boutons d'export entiers, à 390 et 1280 px, avec ou sans la règle.
  Grille arborescente sous la règle hôte : cadre 452 px au lieu de 2 px, mais la grille reste à 0 px
  (règle de l'application) : contrat d'hôte écrit dans `docs/data-components.md`. Couvert par
  `GridFillFloorTests` et l'étape 8 de `eng/scripts-probe/requests.mjs`.
- [x] `OmniPreferences.ReadAsync` : deux lectures concurrentes sur un cache vide ; la seconde peut rendre
  « absent » avant la fin de la requête de la première (`MarkRefreshed` posé trop tôt), et un ancien choix
  local peut alors écraser le choix enregistré en base.
- [x] Zone de danger des pages de réglages (demande du 2026-10-12, décision du propriétaire) : une section
  générique d'aspect destructif, à côté d'`OmniSettingsSection` et `OmniSettingsTile`, dont les tuiles agissent
  par un `OmniButton` `Danger` avec confirmation (exemple : supprimer un projet).
  Résultat : `OmniSettingsDangerZone` (rubrique au filet et au titre `--omni-color-danger`, région nommée
  par son titre, « Zone de danger » par défaut) et `OmniSettingsDangerAction` (tuile, bouton `Danger` décrit
  par le nom de la tuile, confirmation par `OmniOverlayService.ConfirmAsync`, bouton occupé pendant
  `OnConfirm`, échec vers `OnConfirmError`, `Disabled`) ; 3 textes dans les 24 langues sans relecture
  native. Couvert par `SettingsDangerZoneTests` (11 tests) ; démonstration : onglet « Projet » de
  « Réglages en onglets ». Vérifié dans la vitrine publiée (clair et sombre, Échap et Annuler rendent le
  focus au bouton, occupé puis libéré, échec notifié, console sans erreur) ; suite complète, couverture,
  CRAP, API publique, CSP et passe de sondes par défaut vertes.
- [x] Investigation Trou noir (2026-10-12) : défaut établi seulement en WebGL logiciel (navigateurs
  d'automatisation sans GPU) : une image coûte 125 ms (WARP) à 5 s (SwiftShader) et retarde l'accusé des clics
  CDP de 128 ms à 5 s ; avec un vrai GPU, 60 images/s et aucune tâche longue ; animation coupée et
  `prefers-reduced-motion` déjà respectés. Correctif proposé, à décider : `failIfMajorPerformanceCaveat`
  et passage à l'image fixe après 3 images hors budget ; écarts mineurs : cadence réelle 21,7 Hz pour 30
  annoncés, reprise non suivie quand `prefers-reduced-motion` est retiré.
  Résultat (correctif accordé par le propriétaire, appliqué le 2026-10-12) : contexte demandé avec
  `failIfMajorPerformanceCaveat` (WARP refusé, fond CSS) ; image fixe après 3 images espacées de plus de
  100 ms (SwiftShader, que le drapeau laisse passer) ; marge de 2 ms sur `FRAME_MS` ; écouteur `change`
  de `prefers-reduced-motion`, retiré à l'arrêt. Mesuré par CDP, sur le même parcours de 20 clics :
  - WARP : accusé du clic 128/135 ms (médiane/p95) avant, 2,4/5,2 ms après ; tâches longues 37,9 s
    avant, 0,2 s après ;
  - SwiftShader : accusé 2,9/5,1 s avant, 5,5 à 34/50 à 214 ms après ; tâches longues 192 s avant,
    0,4 à 0,5 s après, mais environ 15 s saturées après l'activation du thème, le temps des 3 images ;
  - vrai GPU : 30,2 images/s au lieu de 21,7, aspect inchangé ; `prefers-reduced-motion` posé en direct,
    0 image ; retiré, l'animation repart (60 images en 2 s).
  La sonde (étape 9 de `eng/scripts-probe/overlays.mjs`) retire le drapeau, pour que le dessin tourne
  sous le WebGL logiciel de son navigateur, et bascule `prefers-reduced-motion`.
Controle : test bUnit rouge puis vert (avec et sans `[Required]`, forcé, masqué, sans expression),
vérification dans la vitrine, suite complète verte.

## Actions du propriétaire (hors code)

- Avis sur Trou noir (« trop grand et trop lumineux ») ; prévenir l'application cliente des livraisons
  des lots 8, 9 et 14 de l'ancien PLAN-009.
- nuget.org : dépréciation des paquets 1.0.1 à 1.2.0, secret `NUGET_USER` de l'environnement `release`,
  publication de confiance ; purge des anciens commits par le support GitHub (ancien PLAN-016).
- Préférences en place dans les applications ; relecture native des 8 textes de grille ; publication
  de la 1.8.0 (ancien PLAN-019).
- Applications encore en 1.5.0 ou 1.6.0 : plusieurs retours RET-003 sont déjà corrigés dans OE
  (groupes « A clos » de l'annexe) et disparaissent avec la mise à jour du paquet.

## Ordre et dépendances

Lot 2 à finir (couverture JavaScript). Lot 3 puis lot 4. Lot 6 case par case, indépendant des autres.

## Critère de clôture

Chaque case a sa preuve ou son classement motivé, CI de `develop` verte, aucune référence documentaire
vers un plan déplacé, registre à jour.
