<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009, lot 7 : recette transversale

Recette locale du 2026-10-30 sur `develop`, base `fd0eb60f6a2d41e3fe92487cee0f6cb4aa57cd7c` et corrections des lots 5 à 7. **Lot 7 terminé** : dix suites complètes Release consécutives passent, couverture et CRAP acceptés ; paquet, quatre hôtes et sept sondes de démo vérifiés. Les lots 5 à 7 ne changent aucun code produit, seuil, baseline, exception, numéro de version ou politique. Les limites de cette recette locale restent explicites.

**Portée de ces preuves** : elles valent jusqu'à `fd0eb60f` et au paquet 1.10.0 local. Rejoué le 2026-11-02 sur la 1.11.0 publiée : `eng/Test-ShowcaseHost.ps1` sur `a172cb12`, code de sortie 0 pour Pickers, Density, AutoFit, MindMap, Modules, Languages et Scripts (contraste minimal des étiquettes sur barre : 4,98, aucune violation CSP) ; Contrast non rejouée, elle ne tourne que nommée. Un consommateur neuf, cache privé et nuget.org pour seule source, restaure `OmniEurope.Blazor` 1.11.0 (2 214 563 octets, SHA-256 `9C9A3C2A0921A53EFBAF536EBF24599AD69305C1C59E2B8BA69541F5C7D6A066`), publie et sert l'agenda et les graphiques en mode interactif, observés en navigateur. Les dix suites consécutives n'ont pas été rejouées.

## Contrôles observés

| Contrôle | Résultat et portée |
|---|---|
| Restauration et compilation | Solution restaurée en Release avec `--locked-mode`, build complet sans avertissement ni erreur. Hybrid restauré séparément en mode verrouillé et compilé sans avertissement. SDK 10.0.403 accepté par le plancher 10.0.100. |
| Bibliothèque | Dix suites Release complètes consécutives de 10 981 réussites, zéro expiration, échec ou test ignoré, chacune avec son TRX et son Cobertura. Les traces des deux échecs intermittents antérieurs restent conservées. |
| Analyseurs | Neuf tests de l’analyseur livré et dix tests des conventions passent. Les compilations du consommateur négative et positive sont détaillées au lot 6. |
| API de démonstration | 71 tests passent, dont les intégrations avec PostgreSQL réel lancé par Testcontainers. Les requêtes de ces intégrations utilisent WebApplicationFactory et son serveur en mémoire ; cela ne vaut pas un test réseau de toute l’API. Certains tests ciblés utilisent leurs doubles déclarés. |
| API publique et index | 4 521 signatures acceptées, 162 composants et 1 314 paramètres référencés dans quinze guides. Le premier essai API lisait la copie antérieure de la bibliothèque dans le dossier du garde ; leurs empreintes et dates différaient. Le build complet actualise cette copie et le garde passe sans toucher au contrat. |
| CSP et JavaScript | Garde CSP : 1 059 fichiers, auto-tests acceptés. 74 tests JavaScript passent. Les sept sondes de la démo et la porte V8 passent, dix relevés, 52 scripts, 44 fonctions non exécutées déjà listées ou admises. Aucune nouvelle exception. |
| Couverture et CRAP | Rapport final unique et frais, 27 680 lignes valides, 99,96 % des lignes et 99,61 % des branches, accepté sans nouvelle admission. Six fichiers initiaux et OmniPassTracker sans manque. CRAP : 3 835 méthodes, treize exceptions inchangées. Le contrôle des dix suites exige aussi total=executed=passed=10981 et timeout=failed=error=aborted=0, en plus du code nul de l’exécuteur. |
| Dépendances et licences | Politique acceptée, SBOM régénéré sans dérive : 160 composants, licences cohérentes. Le catalogue signale TrxReport 2.5.2 au lieu de 2.5.1, hors périmètre. Aucune mise à jour. |
| Paquet 1.10.0 local | 118 entrées, 60 assets, 23 cultures satellites, cinq entrées de symboles ; dépendances et analyseur acceptés. Les fixtures contaminées PDB, content files et Roslyn sont rejetées. DLL : 2 555 904 octets, nupkg : 2 185 289 octets, chacun sous 3 MiB. Aucun paquet publié. |

## Consommation réelle et hôtes

Un nouveau consommateur ne référence que le nupkg local 1.10.0, sans ProjectReference et avec un cache privé neuf. Sa restauration initiale produit son verrou ; une seconde restauration verrouillée puis une publication locale réussissent. L’archive effectivement restaurée a le même SHA-256 que le candidat : `F336DE251B01F6046767338CFD13E38337E6C89AC5333B3DEEA1580C98D52D4A`.

Les empreintes de `omni-grid.js`, du CSS livré, de `omni-preferences.js`, de `html-editor/tables.js` et de `omni-mail-preview.js` concordent entre source, nupkg, cache privé, publication et téléchargement HTTP réel. La bibliothèque et l’analyseur empaquetés concordent avec leur build et leur copie restaurée. Le CSS inclut les ajouts de la fusion externe ; le module de grille conserve l’empreinte du lot 4, `F59EB8DF6EE116FFE03A73A2AE1D44660DBEF96A8720DBF34019CC58D49DC709`.

Dans Chromium, le consommateur éprouve successivement le rendu HTTP statique sans JavaScript, le refus réseau imposé du module de grille, puis son chargement normal. Les deux lignes statiques apparaissent sans marqueur interactif ; l’erreur explicite dévoile la table de secours et est réellement journalisée par le serveur ; le parcours normal reçoit ses largeurs de 136 et 1 126 px. Deux clics réels de renommage vérifient aussi les ancêtres du menu filtré. Captures et JSON observés. Les erreurs réseau volontairement imposées sont identifiées ; les parcours réussis ont une console et une CSP propres. Une interop qui resterait pendante sans rejet n’est pas un secours temporisé, conformément au contrat de stabilité choisi au lot 4.

Les sondes natives passent sur les publications fraîches : catalogue Server (cinq familles, dialogue, focus, notification, sélection éditeur), Wasm (clic et progression), Auto (prérendu, hydratation et interaction), Hybrid (clic dans sa vraie WebView2). En-têtes, assets, CSP et consoles sont contrôlés selon chaque sonde. Wasm et Auto livrent le runtime 10.0.14 ; les fixtures du garde acceptent le runtime conforme et rejettent les cas ancien, absent et incohérent. Ces smoke tests ne prétendent pas couvrir toutes les interactions de grille dans Auto et Hybrid.

## Démo et clôture par constat

La publication fraîche passe les sept sondes habituelles en 523 s, Parallel 4, couverture JavaScript active et stress CPU de Requests activé : Pickers, Density, AutoFit, MindMap, Modules, Languages et Scripts. Density compare 11 046 mesures sur 79 pages ; 74 éléments absents de la passe aérée restent explicitement non comparés. Languages fusionne exactement quatre fragments et 316 mesures. Le contraste exhaustif, optionnel, n’est pas rejoué.

| Constats | Preuve conservée et recette finale |
|---|---|
| A360-001, A360-002 | Reproductions et preuves réelles du [lot 2](PLAN-009-lot-2-verifications.md), dont quota natif. Suppression du rowspan terminal rejouée par Scripts ; préférences et synchronisation rejouées. Le quota natif n’est pas annoncé comme rejoué par la recette globale. Assets finaux identiques à ceux éprouvés. |
| A360-003, A360-005, A360-006 | [Lot 3](PLAN-009-lot-3-verifications.md), puis Pickers et Scripts frais : verrouillages pendant l’ouverture, insertion au caret et remplacement en source du courriel, autres faces et blocs, action de fermeture Information et restitution du focus. |
| A360-004, A360-009, A360-010 | [Lot 4](PLAN-009-lot-4-verifications.md), puis Requests et Density frais : cadres contraints et automatiques, surfaces principales et de dialogue, apparition différée et redimensionnement, chevrons, styles des deux éditeurs aux deux modes et largeurs. |
| A360-007 | Rapport global final et TRX de 10 981 réussites acceptés par couverture et CRAP, après dix suites complètes consécutives ; voir le [lot 5](PLAN-009-lot-5-verifications.md). |
| A360-008, A360-011 | Preuves du [lot 1](PLAN-009-lot-1-verifications.md), dont compteur concurrent ; Languages frais confirme les quatre couples exhaustifs. |
| A360-012 à A360-015, VEIL-DOC-001 et 002 | [Lot 6](PLAN-009-lot-6-verifications.md), puis Languages et voile rejoués dans la recette fraîche. |

## Intermittences de tests : diagnostic et correction

La reproduction initiale de DiffViewer échoue dans l’attente des boutons après montage : zéro vérification exécutée, cinq rendus. Une reprise instrumentée de la suite synchrone passe, ce qui ne clôt pas l’intermittence. Les traces relèvent les phases load/mount/navigation, les threads, les travaux en attente et le délai inchangé de 1 000 ms. Le moteur de rendu exécute les assertions sur son propre contexte, alors que le test synchrone retient un thread du pool pendant son attente. Le correctif ne touche que ce test : trois attentes asynchrones et deux clics attendus, avec les mêmes assertions et le même délai.

Pendant la suite de diagnostic, DataGridPreferenceStateTests expire à son tour dans l’attente synchrone de la seconde requête, zéro vérification terminée et six rendus. Son traitement est autorisé séparément. La trace du scénario relève deux requêtes avant cette attente, la synchronisation rendue après environ 15 ms et le chargement lent toujours suspendu. Après sa libération, une ligne est affichée et l’application de l’état se termine. Le correctif ne remplace que les trois attentes de ce test par leurs formes asynchrones ; la borne de synchronisation de cinq secondes et la libération du faux dans finally restent intactes. Aucun changement de grille, de préférences ou de configuration globale du pool.

Un essai de contention bornée sur deux cœurs retarde principalement l’amorçage du vérificateur ; sa restriction est retirée des seuls processus possédés et cet essai n’est pas utilisé comme preuve de stabilité. La saturation extérieure de la reproduction d’origine n’est pas recréée à l’identique. La vérification finale reprend la suite complète parallèle, le garde à quatre processeurs et sa priorité réduite, sous charge ambiante mesurée (instantanés avant/après de 10 à 43 %).

Après retrait de toutes les traces temporaires, dix suites complètes consécutives passent sur les mêmes fichiers : 10 981 tests chacune, les deux scénarios présents une fois et réussis, aucune expiration, aucun test omis. Le contrôleur s’arrête au premier échec et refuse un TRX incomplet ; aucune relance de secours, nouvelle tentative automatique, quarantaine ou modification de délai. Les dix Cobertura et TRX restent distincts. Le rapport de la dixième passe alimente les portes de couverture et CRAP. Les tests utilisent les doubles JavaScript et de préférences déclarés ; ils ne prouvent pas un déplacement réel de Monaco ni une requête HTTP de stockage. Les parcours navigateur et PostgreSQL sont décrits séparément plus haut.

Le lanceur local officiel a atteint Ready avec -s ; catalogue, démo et disponibilité API ont répondu HTTP 200. Les journaux, TRX, Cobertura, empreintes, JSON et captures sont conservés dans le dossier local de preuves du 2026-10-30. La provenance CI distante et le contraste exhaustif optionnel ne sont pas exercés ; aucun paquet, déploiement ou push n’est publié.

Le précontrôle du lanceur signale huit références NuGet plus anciennes et 401 signalements bruts du scanner mécanique en mode avertissement, même décompte qu’au lancement précédent de cette recette. Ce décompte n’est pas qualifié comme autant de défauts ; leur revue exhaustive est hors de cette intervention. Aucune exclusion ni règle du scanner n’est modifiée.

La suite Debug complète passe aussi par le lanceur officiel : 10 981 réussites, zéro échec, zéro test ignoré ; TRX `TestResults/Launcher/library-tests-20261030-200721868.trx`. Le build Debug ne produit aucun avertissement ni erreur.
