<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009 : Thèmes excellents et reliquats

> Statut : **ouvert**. Établi le 2026-09-30 ; lots 1 à 13 faits (le 11 dans le kit, non commité). Restent les décisions en attente du propriétaire.

## Objectif

Un seul plan pour tout le travail restant du dépôt. D'abord les thèmes : chacun tient sa promesse
jusque dans les alertes, Givre montre un vrai verre dépoli sur un fond qui bouge à peine, et un
thème Trou noir rejoint le catalogue. Ensuite les demandes OE des autres projets, puis les décisions
qui attendent le propriétaire.

## État des lieux (2026-09-30)

Revue du code de `ThemeCatalog.cs` et des captures du jour (`artifacts/theme-probe/`, 14 thèmes) :

- Givre : les quatre taches du champ sont collées aux coins de la fenêtre et s'éteignent avant le
  centre ; en sombre le champ est presque uni. L'en-tête des grilles et les alertes pleines sont opaques.
- `--omni-alert-radius` n'est posé que par Essentiel, Relief, Givre, Aplat et Épure : dans Galet, Halo,
  Néon, Nénuphar et Velours les alertes gardent 2,5 px à côté de cartes très arrondies.
- `--omni-alert-shadow` n'est posé que par Aplat : Ardoise, Papier et Épure, annoncés sans ombre,
  gardent l'ombre portée des alertes pleines ; Rétro et Octet n'y portent pas leur ombre dure.
- Galet, Halo et Velours se distinguent mal en clair ; les ombres de Relief se perdent en sombre ;
  les lueurs de Néon bavent sur une page claire.
- Aucun thème n'anime son fond ; aucun réglage n'existe pour cela.

## Décisions (validées le 2026-09-30)

- Le nombre de thèmes et de palettes n'est pas figé : le test de catalogue ne fixe plus un nombre.
- Fond animé : Givre et Trou noir seulement, avec un réglage pour le couper ; toujours coupé sous
  `prefers-reduced-motion`.
- Trou noir : le mode clair est laissé au choix de l'exécutant (page blanche, même accent ambre).
- Les plans livrés (PLAN-001 à PLAN-008 et leurs annexes) sont archivés dans `docs/plans/archive/`.
- Ce plan reçoit tout le reliquat : les demandes OE d'une application cliente et les décisions en attente.

## Lots

Chaque lot se termine par un contrôle mesurable avant de passer au suivant.

### Lot 1 - Registre et archive
- [x] `git mv` de PLAN-001 à PLAN-008 et de leurs annexes vers `docs/plans/archive/`.
- [x] Références de chemin mises à jour (code, tests, guides, ADR-001).
- [x] Registre `docs/plans/README.md` : un plan actif, une table d'archives.
Controle : `git grep -E "plans/PLAN-00[1-8]-"` hors `docs/plans/` : 0 résultat.

### Lot 2 - Alertes à la forme du thème
- [x] `--omni-alert-radius` dans Galet, Halo, Néon, Nénuphar, Velours.
- [x] `--omni-alert-shadow` : aucune dans Ardoise, Papier, Épure ; ombre dure dans Rétro ; cadre en escalier dans Octet.
- [x] Test : tout thème qui arrondit ses cartes d'au moins 0,5 rem pose le rayon de ses alertes ; tout thème sans ombre de carte n'en donne ni à ses alertes ni à ses boutons (`ThemeFieldMotionTests.A_filled_alert_takes_the_shape_of_its_theme`).
Controle : test vert, captures des alertes relues dans la revue du lot 7.

### Lot 3 - Givre
- [x] Champ : taches plus grandes, décentrées, qui passent sous le contenu ; champ sombre lisible.
- [x] Verre : barre du haut et menu latéral en verre (`--omni-shell-background`) ; alertes pleines en verre teinté (`--omni-alert-fill-opacity`, 82 %) sous un liseré clair.
- [ ] En-tête de grille translucide : écarté. Un en-tête collant translucide laisse lire les lignes qui défilent dessous (défaut relevé par une application cliente le 2026-09-30) ; il reste dense.
- [x] Matrice de contraste et sonde de contraste relues avec le nouveau champ.
Controle : captures clair et sombre ; `ThemeContrastMatrixTests` vert ; Givre avec Opale tient 43 paires sur 47 dans les deux modes (thème marqué « contraste non garanti »).

### Lot 4 - Fond animé et son réglage
- [x] Feuille : `@property --omni-scope-turn`, `@keyframes omni-scope-turn`, crochet `--omni-scope-motion` neutre sans thème, coupé sous `prefers-reduced-motion` et par `data-omni-backdrop-motion="off"`.
- [x] `OmniThemeScope.BackdropMotion` (vrai par défaut) ; ligne « Fond animé » dans `OmniAppearanceWindow` et `OmniAppearanceSettings`, visible seulement si le thème anime son fond et si l'hôte lie le changement.
- [x] Texte du réglage dans les 24 langues ; référence d'API publique régénérée (cinq ajouts, aucun retrait).
- [x] Vitrine : le réglage dans la page Personnalisation et dans la démonstration des thèmes.
- [x] Mesure du coût.
Controle : sous Givre l'angle passe de 2,4 à 8,4 degrés en deux secondes, revient à 0 quand le réglage est coupé, et l'animation vaut `none` sous mouvement réduit. Coût sur 180 images (1440 x 900, page Personnalisation) : image médiane à 6,9 ms avec et sans mouvement ; recalcul de style de 0,16 s en mouvement contre 0,05 s à l'arrêt sous Givre, 0,125 s contre 0,052 s sous Trou noir.

### Lot 5 - Thème Trou noir et palette Horizon
- [x] `PaletteCatalog` : Horizon (noir pur, encre chaude, accent ambre ; blanc pur en clair).
- [x] `ThemeCatalog` : Trou noir (cartes à peine voilées, filet fin, lueur d'accent, anneau en fond dont la lueur tourne).
- [x] Test de catalogue sans nombre figé ; sonde de contraste : dégradé conique lu.
- [x] Guides (`docs/foundation-components.md`, `docs/testing.md`, contrat d'accessibilité) et `CHANGELOG.md`.
Controle : suite unitaire verte, captures clair et sombre, 47 paires sur 47 avec Horizon dans les deux modes ; la sonde de contraste sur toutes les palettes est celle du lot 7.

### Lot 6 - Caractère des autres thèmes
- [x] Velours : cartes réchauffées par l'accent, reflet et ombre profonde, boutons en coussin.
- [x] Halo : liseré et halo d'accent des cartes visibles en clair.
- [x] Galet : ombre neutre plus franche et bouton ombré par-dessous, distinct de Halo.
- [x] Relief sombre : ombres claire et sombre relevées (écart faible à l'œil entre avant et après).
- [x] Néon clair : lueurs resserrées, les larges gardées pour le sombre.
- [x] Ardoise : boutons sans ombre, comme annoncé ; Papier, annoncé sans ombre lui aussi, de même.
Controle : captures de la revue du lot 7 ; matrice de contraste verte. La garde « deux thèmes diffèrent par trois jetons de forme au moins » compte désormais aussi la casse et la graisse des titres : sans ombre de bouton, Ardoise ne différait plus d'Épure que par deux des huit jetons comptés.

### Lot 7 - Revue dans le panel et clôture des thèmes
- [x] Chaque thème choisi dans la fenêtre Apparence de la vitrine, clair et sombre : fenêtre, dialogue, menu, alertes, formulaire, grille (180 captures, 15 thèmes x 2 modes x 6 vues).
- [x] Six sondes de la vitrine (`eng/Test-ShowcaseHost.ps1`) sur une publication fraîche ; CI de `develop`.
- [x] `docs/public-api.txt` régénéré.
Défauts relevés dans la revue et leur sort :
- Sous un thème qui bouge son fond, la ligne « Fond animé » arrivait après le gel des mesures de la fenêtre et se dessinait sur la ligne suivante : corrigé (`omni-dialog.js` rend leur hauteur aux conteneurs), couvert par la sonde `Modules`.
- Sous Givre, le texte de la page se lisait à travers la fenêtre non modale : corrigé (fond posé sur la surface opaque).
- Sous Aplat, un menu ou une fenêtre de la couleur de la page n'avait ni bord ni ombre : corrigé (filet plat de 1 px).
- Vitrine : le bouton « Fenêtre d'apparence » remplissait sa cellule : corrigé.
- Sous Relief clair, le dialogue porte un halo blanc sur le voile : laissé, c'est l'ombre claire du thème.
- Sous Octet, le libellé en capitales espacées du bouton « Défaut » touchait son bord droit : corrigé. La cause était le gel des mesures : la largeur capturée sous le thème d'ouverture restait après un changement de thème (« Aléatoire » débordait de 19 px sous Octet). La fenêtre reprend ses mesures quand le thème, la palette ou la police changent ; le gel tient toujours face à la taille du texte et à la densité. Couvert par la sonde `Modules`.
Controle : suite unitaire verte (4009 tests au 2026-09-30, après les lots 12 et la garde de contraste de l'en-tête de grille). Passe complète des six sondes verte le 2026-09-30 à 18:50 sur la publication de 15:14 (code de `c1f3ab3`) : sélecteurs, densité (2995 éléments, 40 pages), contraste (113 240 mesures, 450 combinaisons, registre vide), ajustement de colonne, carte mentale, modules. CI de `develop` verte à `c1f3ab3`.
Trois passes complètes avaient échoué avant, pour trois causes trouvées et corrigées :
- contraste : titres de colonne à 4,48 pour 1 sous Trou noir avec la palette Électrique en clair ; fond des grilles du thème ramené de 4 % à 3 %, paire désormais mesurée par `ThemeContrastMatrixTests` ;
- modules (intermittent) : la sonde demandait une navigation dès que `#app` existait, avant que le routeur n'écoute ; les sondes attendent `#showcase-theme`, rendu par l'application ;
- carte mentale : la sonde glissait un nœud dès que le zoom dépassait 0,2, vrai au premier rendu (zoom 1), avant que le script de la carte soit attaché ; reproduit en retardant ce script de 1,5 s (même pas, même état : aucun nœud choisi, nœud à sa place), corrigé en attendant l'ajustement de la carte, vérifié avec 1,5 s et 4 s de retard. Aucun défaut du composant : les trois causes sont dans les sondes et dans un jeton de thème.

### Lot 8 - Demandes OE de la recette
Détail dans `(chemin local)`.
- [x] 28 : le calendrier d'un sélecteur de date, d'heure ou de date et heure s'ouvre par-dessus un dialogue, sans barre de défilement. Panneau fixé à la fenêtre et placé par script plutôt que portail ; un dialogue déplaçable bouge par `left` et `top`, plus par `transform`.
- [x] 34 : les tableaux masqués d'`OmniChart` n'allongent plus la zone de défilement (bloc masqué autour d'eux).
- [x] 36 : le bouton « Modifier » d'`OmniAppearanceSettings` en `Primary`.
- [x] 39 : liste déroulante habillée, par `appearance: base-select` (Chromium 135 et suivants) ; l'élément reste un `<select>` natif. La liste personnalisée à la place du `<select>` n'a pas été faite : voir les décisions en attente.
- [x] 40 : la police dans la fenêtre Apparence, en plus de la ligne des réglages.
- [x] 41 : fenêtre Apparence plus large (46rem), sur deux colonnes, 479 px de haut au lieu de 601.
- [x] 42 : bouton « Aléatoire » (thème et palette au hasard, autres que ceux en vigueur).
Controle : mesuré dans la vitrine. 28 : panneau en `position: fixed` à 6 px sous son champ, 362 px au-delà du dialogue, dialogue sans débordement (152 sur 152 px), de même après un glisser de 180 px. 34 : bloc masqué de 1 x 1 px pour un tableau de 136 px. 39 : liste ouverte peinte avec les jetons du calque flottant, clavier natif. 41 : lignes côte à côte, une colonne à 420 px de large. Réponse à la session d'une application cliente : à faire à la livraison.

### Lot 9 - Colonne de grille qui suit le filtre de son tableau
Demande de la session d'une application cliente du 2026-09-30, décision de l'utilisateur prise là-bas.
- [x] `OmniDataGridColumn.Filterable` en `bool?` : `null` suit `OmniDataGrid.Filterable`, colonnes de données seulement (`Property`, `Value`, `FilterPredicate` ou `FilterTemplate`). Écrit par la session d'une application cliente dans ce dépôt, relu et intégré ici. Conséquence relevée à la relecture : le `Filterable` de la grille ne conditionne plus `FilterMode`, une colonne qui demande son filtre dans une grille coupée garde l'éditeur du mode déclaré.
- [x] Tests (`DataGridColumnFilterDefaultTests`, six cas), guide des données, `CHANGELOG.md` (rupture pour tous les consommateurs), référence d'API.
- [x] Vitrine : les colonnes des démonstrations suivent la grille, Montant s'en retire, les six grilles dessinées sans rangée de filtres posent `Filterable="false"` sur la grille.
- [x] Livrés avec ce lot par la session d'une application cliente : ligne à hauteur exacte en `FixedRowHeight` (`box-sizing: border-box`), en-tête collant opaque sous un fond de grille translucide.
Controle : suite unitaire verte (3449 tests, dont deux gardes ajoutées pour la hauteur de ligne et l'en-tête opaque). Mesuré dans la vitrine : sur `/composants/grille`, Référence, Demandeur et Pays portent un filtre sans rien déclarer et Montant n'en a pas ; les six grilles à `Filterable="false"` n'ont pas de rangée de filtres ; sous Givre, l'en-tête collant a un fond opaque (`rgb(247, 249, 252)`). Session d'une application cliente à prévenir à la livraison.

### Lot 10 - Nom d'un projet privé hors des fichiers du dépôt
- [x] Guide des diagrammes, `CHANGELOG.md`, commentaires de code et de test, dossier de données d'essai (devenu `TestData/mindmap`), marque d'essai des tests d'en-tête : le nom est remplacé par une formulation neutre.
Controle : `git grep -i` sur ce nom ne rend plus rien dans le dépôt ; suite verte (3449 tests).

### Lot 11 - `STD-FILESIZE` en lignes effectives dans le kit
- [x] le contrôle des règles du kit, son registre `docs/code-rules.md`, son modèle `docs/tests-template/FileSizeAuditTests.cs` et le texte de ses deux de rappel des règles comptent les lignes effectives : une ligne vide ou faite d'un seul commentaire (`//`, `///`, bloc `/* */`, et dans un `.razor` bloc `@* *@` ou `<!-- -->`) ne compte pas ; du code suivi d'un commentaire compte.
Fait dans l'arbre de travail du kit le 2026-09-30, sur décision du propriétaire, sans commit : le kit porte les modifications non commitées d'une autre session dans les mêmes fichiers, le commit lui revient. Le script de contrôle des règles n'est jamais copié et tourne au lancement de chaque projet : il est à jour pour tous dès qu'il change. Ce qui dérive, ce sont les copies, d'où une règle ajoutée au kit le même jour sur décision du propriétaire, `STD-KITCOPY` : chaque modèle de `docs/tests-template` porte sa version en tête (`// kit-model <Nom> <n>`), une copie garde cette ligne, et le script signale une copie sans version ou en retard, ainsi qu'un crochet déployé dans les crochets de l'agent dont le texte diffère de sa source. Au jour du changement il signale 13 copies dans 7 projets (dont les deux `FileSizeAuditTests.cs` qui comptent encore les lignes brutes) et le crochet de rappel des règles déployé ; OE n'a aucune copie de modèle.
Controle : le contrôle des règles ne rend plus de constat (5 avant : `OmniDataGrid.razor.cs` 365 lignes effectives sur 866, `OmniHtmlEditor.razor.cs` 533 sur 832, `OmniChartContext.cs` 511 sur 732, `omni-html-editor.js` 551 sur 713, `omni-mindmap.js` 560 sur 698). Autres projets, avant puis après : une application cliente 4 puis 2 (dont un par un découpage fait chez lui le même jour), une application cliente 8 puis 5, kit 1 puis 1, les cinq autres 0. Le script et le modèle de test rendent le même compte sur neuf fichiers, `.razor` compris.

### Lot 12 - Pluriels par langue
- [x] Règle de pluriel cardinal de chacune des 24 langues (catégories CLDR : `zero`, `one`, `two`, `few`, `many`, `other`), choisie d'après la culture d'affichage (`PluralRules`).
- [x] Bloc `plural` de la syntaxe ICU dans les ressources, lu par `PluralMessage` : `{0, plural, one {# fichier} other {# fichiers}}`. Aucune surface publique nouvelle ; un remplacement de l'hôte peut porter ses blocs.
- [x] 341 textes accordés dans 23 fichiers (22 clés : téléversement, validation des longueurs, temps relatif, journal, diff, import de tableau, export Markdown, saisie assistée, sélection multiple). Le hongrois n'a rien à accorder ; les langues qui employaient déjà « Libellé : {0} » le gardent là où il se lit.
- [x] Guide de localisation, `CHANGELOG.md`.
Écrit sans locuteur natif, comme le reste des traductions ; l'irlandais et le maltais sont à relire en priorité (`docs/localization.md`).
Garde modifiée : `LibraryTranslationTests` compare désormais les arguments nommés par un texte sans doublon, puisqu'un bloc répète son nombre dans chaque forme ; les formes elles-mêmes sont vérifiées par `PluralTextTests`.
Controle : `PluralTextTests` (110 cas) : règle de chaque langue sur ses nombres charnières, chaque bloc des 24 fichiers porte exactement les catégories de sa langue, et les nombres qui étaient faux se lisent juste (roumain « 20 de rânduri noi », croate « najviše 1 datoteku » et « 21 datoteku », letton « 21 dienas » et « 31 dienas »). Suite verte.

### Lot 13 - Sous-modules JS
- [x] Décision : pas de regroupement au build. Il demanderait un outil de regroupement (esbuild ou Rollup, donc Node) dans la construction du paquet, qui n'en a aucun aujourd'hui, pour un gain limité au premier usage : 6 requêtes au lieu d'une pour la grille, 4 pour le focus, 5 pour l'éditeur, en deux vagues, ensuite servies par le cache du navigateur. Un assemblage maison qui recollerait des modules ES en retirant leurs `import` serait fragile pour le même gain.
Controle : décompte inchangé et déjà consigné dans `docs/performance-budgets.md`.

## Décisions prises le 2026-09-30 (seconde série)

- Version : la version qui portera ce plan est la `1.3.0`. La rupture de `OmniDataGridColumn.Filterable` y passe en version mineure, assumée. Le numéro est posé à la publication.
- Publication : pas de `1.2.1` ; le propriétaire publie plus tard, d'autres changements sont attendus.
- Paquets NuGet anciens à délister : le propriétaire s'en charge. Constaté le 2026-10-01 : `0.1.0-alpha.1` et `1.0.0` sont délistés.
- Listes déroulantes (point 39 de la recette) : l'habillage CSS est gardé (Chromium 135 et suivants, liste du système ailleurs) ; le `<select>` natif reste.
- Nom d'un projet privé du propriétaire, cité dans le dépôt public : retiré des fichiers (lot 10).
- `STD-FILESIZE` : le compteur de lignes effectives est porté dans le contrôle des règles du kit (lot 11).
- Pluriels après un nombre : le standard, soit les règles de pluriel par langue (lot 12).
- Sous-modules JS à regrouper au build : laissé au jugement de l'exécutant (lot 13).

## Décisions en attente du propriétaire

Rien ici n'est exécuté sans réponse.

- Ce même nom dans cinq messages de commit déjà poussés : réécrire ou non l'historique.
- Grille `Load` : charger dès le prérendu avec les défauts des colonnes, ou garder le premier rendu interactif. Avis de l'exécutant : garder le premier rendu interactif. Le rendu interactif part d'un composant neuf et recharge de toute façon : charger au prérendu ferait deux requêtes par affichage, sauf état persisté que l'hôte devrait fournir. L'état enregistré (`StateKey`) n'est pas lisible au prérendu : la première requête ignorerait les filtres et tris gardés, et les lignes changeraient au passage à l'interactif. Enfin le prérendu attendrait les données avant d'envoyer la page, là où la grille montre aujourd'hui son état de chargement tout de suite. Un chargement au prérendu ne vaudrait que comme option à demander, pour un hôte qui veut des lignes dans le HTML servi.
- Trou noir : redessiné depuis (toujours sombre, shader WebGL). Le propriétaire le juge moyen et y reviendra plus tard (2026-10-01).

## Ordre et dépendances

Lots 2 à 7 dans l'ordre (le lot 5 s'appuie sur le crochet du lot 4). Les lots 8 et 9 sont
indépendants des thèmes ; les points 40 à 42 du lot 8 retouchent le dialogue Apparence et passent
après le lot 4, qui y ajoute une ligne.

## Critère de clôture

Chaque lot a sa preuve, la CI de `develop` est verte, les sessions demandeuses sont prévenues. Les
décisions en attente qui restent sans réponse sont reportées dans le plan suivant, pas perdues.
