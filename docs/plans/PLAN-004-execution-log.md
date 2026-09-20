<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-004 : journal d'exécution

Journal local de l'exécution autonome du plan (demande `/goal` du 2026-09-18). Sert au récapitulatif
final et survit à une compaction.

## Lot 1 : fait

- `07be511` fusion `feature/aetheus-migration` (conflit `docs/public-api.txt` résolu en union, base
  régénérée : 30 ajouts, 5 paramètres relâchés d'`EditorRequired`).
- `c8e1aeb` fusion `feature/aetheus-icons`, `a7e859a` fusion `feature/aetheus-lot9`, toutes deux en
  `-s ours` : chaque ligne ajoutée par `635e664` et `a9f3cb0` est déjà dans `develop` via `ec5405f`
  (deux lignes seulement réécrites : `Dismissible && ShowClose`, `CardTemplate!`). Adaptation du
  plan : l'étape 5 annonçait un build rouge, il reste vert car `ec5405f` avait déjà ajouté les démos.
- Validation : build 0 avertissement, 884 + 9 tests (base `develop` : 724 + 9). API, CSP, fixtures CSP
  verts. `/check` : conforme hors budget CSS, que l'utilisateur a fait retirer (`e18547e`), porte des budgets verte depuis.

## Lot 2 : fait

Commits `07e658a`, `7b44756`, `4ed5b4e`, `512a0bc`, `70bf223` (agent). 884+9 -> 977+9 tests. Styles
manquants des composants du lot 9 ajoutés, bug de placement clavier du Kanban corrigé, libérations
corrigées, docs et CHANGELOG. Preuve navigateur Edge/CDP (sonde dans le scratchpad, pas dans `eng/`).
Résidus : mode Monaco d'`OmniDiffViewer` prouvé en bUnit seulement ; échec intermittent préexistant
`InteractionComponentTests.Validators_RejectLengthEmailAndComparisonMismatches` (1 fois, puis 5
passages) ; défilement horizontal préexistant de la page Éditeurs (barre d'`OmniHtmlEditor`).
`/check` : conforme.

## Lot 3 : fait, adapté

`ff88659`. Roslyn 5.9.0 -> 5.0.0 (compilateur de 10.0.100) + test de convention. Bibliothèque et
analyseurs : 0 avertissement sous 10.0.202, 10.0.303, 10.0.401 ; contrôle négatif 5.9.0 sous 10.0.202 =
CS9057. **Adaptation** : `global.json` et `eng/Test-SdkBand.ps1` gardent l'épinglage de bande, car la
restauration verrouillée des hôtes WebAssembly échoue (NU1004) sous une autre bande : leurs paquets
implicites suivent le runtime du SDK. Les consommateurs (Bellwether, Aetheus) ne chargent que la
bibliothèque et l'analyseur, désormais libres. SBOM et NOTICE régénérés. 978+9 tests.

## Lots 4, 5, 6 : faits

`db3875b` (+ `bc7bf5f` par le `/check`, qui a rétabli deux assertions du test remplacé). Modèle forme et
palette, API additive (22 signatures), `OmniButtonVariant.Info`, fabrique portée de la maquette
(vérifiée : 3 278 valeurs identiques). Adaptations : `ThemeColor.Mix` arrondit les milieux loin de
zéro comme `Math.round` en JavaScript (sinon `#cf7c00` au lieu de `#d07c00`) ; seuil des sévérités
en CIE76 ΔE >= 20 (Or ancien clair, attention et erreur à 26,3). `/check` : corrigé et conforme.

## Lot 7 : fait (7A à 7E)

- 7A fait, `3dbbdad` : jetons de l'apparence livrée générés depuis Défaut entre marqueurs
  `omni:theme-tokens`, test de conformité avec réécriture sur `OMNI_WRITE_THEME_TOKENS=1`, contrôle
  négatif prouvé. Correction de conception : la forme est aussi déclarée sur chaque
  `[data-omni-theme]`, sinon ses `var()` se résolvaient en clair dans une portée sombre. Lecteur de
  jetons de la vitrine adapté. CHANGELOG « Changed ». 1002+9 tests.
- 7B fait, `a8c8a79` + `eb01055` : boutons (fills, gris secondaire, survol, appui par thème), T4, T7, T11,
  T12, badges (ajouts `OmniBadgeVariant.Info`, `OmniBadgeFill.Solid`), alertes pleines à glyphe centré,
  calque T14 et `.omni-disc`, T16. 1057+9 tests, rendu vérifié en Edge/CDP (clair et sombre, Défaut).
  Choix de l'agent retenus (exécution autonome) : une alerte sans `Icon` affiche le glyphe de sa
  sévérité ; `Outline` (absente de la maquette) reste le défaut, dessinée en surface de carte, filet
  de la sévérité, sans barre ; pas de flou d'arrière-plan sur le dialogue (il deviendrait le repère des
  éléments fixes qu'il contient). Reportés à 7C : marques de notification en disque.
- 7C fait, `0b1c764` + `92bc145` : densité (jetons des trois niveaux, `Density` nullable sur 10
  composants, 26/36/44 px mesurés), grilles T15/T17 (`color-scheme` d'un scope Système en clair
  corrigé), formulaire (largeur du fieldset corrigée ; radio 7 px et case sur interrupteur absents du
  paquet), tuiles, `OmniUpload.Display` (`Zone`, `Field`) et `ReducedList`, marques de notification en
  disque, rail du menu latéral sans barre d'accent. 1133+9 tests. Écarts : colonnes numériques
  alignées à droite par défaut (détection par le type de `Property`, changement de comportement) ;
  retraits de l'ombre sous l'en-tête de grille, du gras 650 de l'onglet actif, de l'accent des
  éléments du menu de compte ; cibles de 44 px non réduites par la densité (STD-BTN) ; jeton
  `--omni-grid-dark-header` laissé mais plus lu. Reportés : calendrier maison (7D), test de densité
  sur toute la vitrine (7D), barre supérieure avec recherche, cloche et avatar (composition vitrine,
  lot 9).
- 7D fait, `6b241e0` + `705f107` + `2399f6e` : calendrier maison dans `OmniDatePicker` (grille clavier,
  bornes, culture), nouveau `OmniTimePicker`, `OmniDateTimePicker` réécrit, densité lue (26/36/42 px),
  26 ajouts d'API, 1188+9 tests, preuve Edge/CDP (0 violation CSP). Écarts : en date seule, choisir un
  jour ferme le panneau ; cases et heures sous 44 px comme la maquette ; Échap dans un dialogue non
  prouvé. Sonde T22 `eng/Test-ShowcaseDensityProbe.mjs` livrée : rouge sur 32 sélecteurs hors
  sélecteurs (tableur, agenda, Gantt, éditeurs, carte mentale, graphe git, squelettes, entity-picker)
  -> bloc 7E. Assembly à 1 556 992 / 1 572 864 octets (99 % du budget).
- 7E fait, `bb2c863` `78c5dce` `94bc919` `0f1f253` : sonde de densité rouge (311 éléments) puis verte
  (704 mesurés, 30 pages, CSP et console propres). Exemptions motivées : glyphes d'icône (fixes dans la
  maquette), boutons de l'arborescence (cibles STD-BTN gardées par un test), `Height` du consommateur,
  Gantt (géométrie en pixels calculée en C#). Défaut réel trouvé et corrigé : Échap dans un sélecteur
  ouvert dans un `OmniDialog` fermait le dialogue. 1191+9 tests. Restes : hauteur par défaut du journal
  et du comparateur hors densité ; en compacte, cibles de `OmniSelectBar`, outils de l'éditeur et
  options d'`omni-entity-picker` à 34 px comme les boutons.
- `/check` du lot 7 : corrigé et conforme, `90c0d62` ajoute 3 tests (anneau T5, dix couples d'appui
  T9, cartes sombres T23). 1194+9 tests, les deux sondes vitrine vertes, contrôle négatif rejoué.

## Lot 8 : fait

`a117b5b` + `5e00b92` : matrice de 200 jeux, 64 paires par jeu, 12 805 contrôles verts. Seul défaut :
`text-muted` sur `surface-hover` (90 jeux, 4,13 à 4,47), corrigé dans la fabrique (poussé contre la
surface survolée), bloc livré régénéré, maquette alignée (0 différence sur 1 500 jetons). Bordure au
plancher 1,7 avec l'arbitrage WCAG 1.4.11 en commentaire. 1395+9 tests. `/check` : conforme
(mutation : 91 échecs).

## Lot 9 : fait

`59a24ee` `1bee4c3` `6e3b532` : personnalisateur à deux sélecteurs (thème, palette, bouton Défaut, mode,
densité, survol forcé, rail de contrastes), `ThemeState` et export sur la portée avec forme sombre,
familles des nouveaux jetons, 9 démos reprenant la maquette, 309 clés FR et EN. 1452+9 tests, sondes
vitrine vertes (densité : 1023 éléments sur 39 pages), 26 contrôles CDP. Écarts : sonde de densité
exemptant `.omni-form-field__error-icon` (glyphe fixe dans la maquette) ; pied des dialogues selon
`docs/ui-conventions.md` et non la maquette ; pastille de cloche, loupe, logo, avatar, description
d'élément de menu, erreur sous liste radio dessinés par la feuille de la vitrine faute de composant ;
en-tête du menu de compte absent (`OmniProfileMenu` n'a pas d'emplacement).
`/check` du lot 9 : conforme (commit intermédiaire `1bee4c3` compilé et testé : 1438+9).

## Lot 10 : fait, après décision du propriétaire (voir « Décisions du 2026-09-18 » en fin de journal)

Historique ci-dessous : état avant la décision sur le voile d'occupation.

`7de1c1a` `bcb3b37` `573d499` : sonde `eng/Test-ThemeContrastProbe.mjs` (`Test-ShowcaseHost.ps1 -Probe Contrast`),
200 combinaisons, 53 328 mesures, registre JSON, 70 captures, contrôle négatif prouvé. Corrigés à la
source : débordement à 375 px (vitrine, titre, infobulle cachée), action « Tout désélectionner » en
`accent-strong`. **Bloqué, décision utilisateur** : 750 échecs, tous sur le voile d'occupation
(`#000000` au pic 0,55 sur le contenu des boutons occupés : 1,72 à 4,41 en clair, 2,17 à 4,22 en
sombre ; sans effet sur un remplissage noir Mono). Le corriger change le rendu STD-BUSY validé.
Écart constaté : `docs/code-rules.md` décrit un voile « surface-coloured » alors que le paquet dessine
un voile noir verrouillé par `ConventionGuardTests`. Conséquence : `Test-ShowcaseHost.ps1` sort en
erreur tant que ce point n'est pas tranché. Captures sombres Galet et Octet regardées par
l'orchestrateur : cartes détachées, aucun survol blanc. `/check` : partiel (ce seul point).

## Lot 11 : fait

`f1c837d` `0ad1184` `a71add4` `5387113` `bdf9c4e` : `docs/foundation-components.md` (thèmes, palettes,
combinaison, densité), `CHANGELOG.md` (exception `1.0.1` en tête, apparence livrée, correspondance des
vingt anciens thèmes, ajouts, T3 à T24, essai T14, grilles T15, politique SDK), règle T1 dans
`docs/ui-conventions.md`, `docs/versioning.md` (exception motivée), `<Version>1.0.1</Version>`,
`docs/publishing.md` aligné, `Microsoft.NET.Test.Sdk` 18.10.1 (dérive du catalogue NuGet).
Vérification finale rejouée sur `bdf9c4e` (SDK 10.0.401) : restauration verrouillée, build Release
0 avertissement, 1452 + 9 tests (0 ignoré) ; `Test-SdkBand`, `Test-PublicApi` (3 284 signatures ;
diff depuis `8314ce1` : ajouts, 5 `EditorRequired` relâchés au lot 1, `OmniFieldset` gagne
`IAsyncDisposable`), `Test-CspFixtures`, `Test-Csp` (685 fichiers), `Test-DependencyPolicy` (avec
contrôle du catalogue), `Test-Budgets` (et `-PackagePath`), pack `1.0.1`, `Test-Package
-ExpectedVersion 1.0.1` (44 + 5 entrées), `Test-PackageFixtures`, SBOM régénéré sans diff,
`Test-WasmHost` vert, sondes vitrine `Pickers` et `Density` vertes (1023 éléments, 39 pages).
`Contrast` : registre du 2026-09-18 13:44 UTC, 750 échecs sur 53 328 mesures, tous « voile
d'occupation » (lot 10). Aucun `Skip`, `NoWarn` ni suppression ajouté ; seule garde modifiée,
`ConventionGuardTests`, renforcée.

## Points non consignés ailleurs (pour le compte rendu)

- **À VÉRIFIER** du thème Défaut (casse des boutons, élévation des cartes) : tranché dans le code
  sans capitales (`ThemeCatalog` ne pose pas `--omni-button-text-transform` pour Défaut), cartes sur le
  calque T14 ; aucune comparaison au rendu de production Aetheus (lot 5, étape 2) n'est consignée.
- **À VÉRIFIER** RET-002 n°51 (jetons clairs rehaussés de `feature/aetheus-migration`) : non consigné
  au lot 1 ; la feuille livrée est depuis générée par la fabrique (lot 7A), ce qui rend ce point caduc.
- Contrôle du lot 7 « capture avant et après de la vitrine sans thème posé » : seule la capture après
  (apparence livrée, lot 10) est consignée ; aucune capture avant.

## Suite du 2026-09-18 : composants de la barre d'application

`7417444` `3ca1f2e` `03fc10a` `d3eba08` `2300014` `acbf4f1`. Les pièces que la vitrine dessinait avec
sa propre feuille (lot 9) sont dans le paquet, en paramètres de composants existants (aucun nouveau
composant ni nouvelle énumération, sinon les gardes de couverture de la vitrine exigeaient une démo) :
`OmniButton.Indicator` (pastille de cloche), `OmniTextBox.Icon` (loupe), `OmniHeader.Brand` et
`BrandMark` (logo et nom), `OmniProfileMenu` sans `Summary` = avatar (`Initials` ou glyphe, cible de
44 px) et `Header` (identité hors de `role="menu"`, `Summary` perd `EditorRequired`),
`OmniProfileMenuItem.Icon` et `Description`, `OmniRadioButtonList.Error` (ligne d'erreur
d'`OmniFormField`, `aria-invalid`, `aria-describedby` fusionné). Vitrine inchangée (0 fichier sous
`site/`). 24 tests bUnit + 1 cas `FloatingSurfaces` : 1452+9 -> 1477+9, chaque commit compilé et testé
vert ; contrôle négatif (retrait d'`aria-hidden` de la pastille : 1 échec). API 3 293 signatures, CSP
(685 fichiers) et fixtures verts, assembly 1 562 624 / 2 097 152 octets, contrastes 201 verts (aucune
nouvelle paire : initiales = texte sur `neutral-fill`, logo = paire du remplissage d'accent, déjà dans
la matrice). Rendu vu dans le navigateur de l'app (page de sonde du scratchpad, Défaut clair et
sombre, console vide), texte atténué sur le panneau acrylique mesuré à 5,50 et 5,60 (Défaut seul).
Non fait : la vitrine garde sa feuille (consigne) ; texte atténué sur le calque acrylique translucide
hors matrice statique ; point de la pastille sur le bandeau d'accent non mesuré (décoratif) ; pas
d'avatar image.

## Décisions du 2026-09-18 et vérification finale

- `74729e8` : budget de l'assembly relevé de 1,5 à 2 Mio (décision du propriétaire, motif dans
  `docs/performance-budgets.md`). Mesuré : 1 562 624 / 2 097 152 octets, paquet 690 987 / 2 097 152.
- `1f65a13` : voile d'occupation noir gardé (STD-BUSY de `docs/code-rules.md` le décrit). La sonde
  `Contrast` compte ses lectures sous le ratio comme exception déclarée (`acceptedBusyVeil`), seulement
  pour `#000000` au pic <= 0,55 ; un autre voile, un voile illisible ou l'absence de contrôle occupé
  échouent toujours. Plancher des bordures gardé à 1,7 (décision consignée dans
  `ThemeContrastMatrixTests.BorderFloor`).
- Vérification indépendante sur `acbf4f1` : `eng/Test-ShowcaseHost.ps1` (sondes Pickers, Density,
  Contrast) vert, 53 328 mesures sur 200 combinaisons, registre `failures` vide, 750 lectures en
  `acceptedBusyVeil`, 70 captures ; 1477 + 9 tests ; portes API (3 293), CSP (685), budgets, SDK,
  dépendances, paquet `1.0.1` vertes.
