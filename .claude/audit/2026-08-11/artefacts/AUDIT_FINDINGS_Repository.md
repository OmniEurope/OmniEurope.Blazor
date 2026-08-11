# Findings d'audit 360 - Repository

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

## `.claude/auditsession.md`

RAS

<a id="claudechallengesessionmd"></a>
## `.claude/challenge-session.md`

RAS

## `.claude/plan.md`

RAS

## `.claude/suggestions.md`

RAS

<a id="claudetestconfigmd"></a>
## `.claude/test-config.md`

- [Moyen] [Documentation] La configuration déclare les catégories `integration` et `e2e` vides alors que le dépôt possède des sondes d'hôtes, de paquet et de CSP dans `eng/` et les exécute en CI; `/tests` ne peut donc ni découvrir ni piloter la stratégie de vérification réelle - lignes 17-27 - source: comparaison avec `.github/workflows/ci.yml` et `eng/Test-*.ps1` - recommandation: Codex peut inventorier les gates existantes, classer les tests d'hôte et de paquet en intégration, ajouter la future preuve navigateur à `e2e`, documenter leurs prérequis et faire valider la configuration par le skill de tests.

## `.editorconfig`

- [Moyen] [Style] Le fichier de 16 lignes ne reprend ni les conventions C# du kit, ni les sévérités `GEN001-GEN008`, impose CRLF au lieu du LF canonique et applique le nettoyage des espaces à Markdown; le build vert ne contrôle donc pas les conventions attendues - lignes 3-15 - source: `AUDIT_KIT.md` et `C:\Dev\_Generic\.editorconfig` - recommandation: Codex peut fusionner les règles neutres du kit, expliciter les divergences propres à la RCL, câbler les diagnostics applicables puis prouver un build sans avertissement sans suppression.

## `.github/workflows/ci.yml`

- [Moyen] [Sécurité] Les trois actions tierces sont référencées par tags majeurs mutables (`actions/checkout@v4`, `actions/setup-dotnet@v5`, `actions/upload-artifact@v4`), ce qui permet au code exécuté par la CI de changer sans modification du dépôt - lignes 16-17, 61, 69-70 - source: revue supply-chain `AUDIT_DEPENDENCIES.md` - recommandation: Codex peut résoudre des SHA complets officiels, conserver la version en commentaire et configurer leur renouvellement automatisé contrôlé.
- [Faible] [Fiabilité] Le job Hybrid installe `maui-windows` sans workload set explicite, alors que `global.json` autorise `latestPatch`; les packs construits peuvent donc varier malgré les verrous NuGet - lignes 73-78 - source: `AUDIT_DEPENDENCIES.md` D-006 - recommandation: Codex peut épingler un workload set compatible, capturer sa version et prouver une restauration reproductible sur le runner Windows.

<a id="githubworkflowspublishnugetyml"></a>
## `.github/workflows/publish-nuget.yml`

- [Élevé] [Authenticité] Le workflow de release reconstruit puis publie un paquet distinct sans télécharger l'artefact exact validé par la CI, sans exécuter tests, contrôles API/CSP, inspection de contenu ni preuve de provenance; une release peut donc publier un binaire que les gates annoncées n'ont jamais examiné - lignes 20-30 - source: comparaison avec `.github/workflows/ci.yml:20-64` et `eng/Test-Package.ps1` - recommandation: Codex peut faire produire un artefact unique identifié par hash dans la CI, exiger la réussite de toutes les gates, puis limiter ce workflow au téléchargement, à la vérification et à la publication de cet artefact inchangé.
- [Moyen] [Sécurité] `checkout`, `setup-dotnet` et surtout `NuGet/login` dans un job doté de `id-token: write` sont référencés par tags majeurs mutables; le fournisseur de publication peut changer le code exécuté avec le jeton OIDC sans diff du dépôt - lignes 7-9, 16-17, 24-28 - source: revue supply-chain `AUDIT_DEPENDENCIES.md` D-003 - recommandation: Codex peut épingler chaque action sur un SHA officiel complet, conserver les numéros de version en commentaire et automatiser des mises à jour revues.

## `.gitignore`

RAS

## `CHANGELOG.md`

RAS

## `CLAUDE.md`

- [Moyen] [Documentation] Le fichier ne contient que la reprise de session et omet la vue d'ensemble, la stack, la structure, les commandes, les règles, la carte des 26 documents et les adaptations de la RCL au kit; la gouvernance effective reste dispersée et l'ambiguïté Radzen/Omni n'est pas résolue - lignes 1-5 - source: `AUDIT_KIT.md` - recommandation: Codex peut transformer ce fichier en point d'entrée concis vers la documentation canonique, le registre local de règles, les commandes de validation et les décisions architecturales, tout en conservant `## Session resume`.

## `CONTRIBUTING.md`

RAS

## `Directory.Build.props`

RAS

## `Directory.Packages.props`

- [Moyen] [Dépendances] Les deux packages MAUI sont épinglés en `10.0.20` alors que le scan NuGet les trouve en retard sur le servicing `10.0.90`; le verrou Hybrid résout en conséquence plusieurs composants .NET en `10.0.0` à côté du socle `10.0.10` - lignes 8 et 13 - source: `AUDIT_DEPENDENCIES.md` D-001 - recommandation: Codex peut mettre à niveau les deux packages de manière coordonnée avec le SDK/workload retenu, régénérer le verrou puis prouver restore verrouillé et build Hybrid Windows.
- [Faible] [Dépendances] `bunit` reste en `2.8.6` alors que `2.9.0` est disponible et maintient un graphe transitive plus ancien - ligne 6 - source: `AUDIT_DEPENDENCIES.md` D-004 - recommandation: Codex peut isoler la mise à niveau, régénérer le verrou et conserver le changement seulement après les tests bUnit, CSP et budgets.

<a id="docsaccessibilitycontractmd"></a>
## `docs/accessibility-contract.md`

RAS

## `docs/architecture.md`

- [Faible] [Architecture] Le document décrit seulement les dossiers physiques et ne constitue pas l'autorité de la taxonomie logique: la roadmap compte 12 familles, `component-families.md` en agrège 8 et le catalogue en annonce 10; il omet aussi la direction des dépendances et les frontières de composition - lignes 3-15 - source: `AUDIT_ARCHITECTURE.md` ARCH-05 - recommandation: Codex peut publier ici une carte canonique des familles et dépendances, aligner les documents et le catalogue, puis ranger progressivement sources et tests par dossiers logiques sans scinder la RCL ni modifier l'API publique.

<a id="docscleanroomcomponentsheetmd"></a>
## `docs/clean-room-component-sheet.md`

RAS

<a id="docscleanroommd"></a>
## `docs/clean-room.md`

RAS

## `docs/compatibility.md`

RAS

<a id="docscomponentcontractsjson"></a>
## `docs/component-contracts.json`

- [Élevé] [Authenticité] L'inventaire présenté comme des paramètres de composants contient manifestement des identifiants issus d'expressions consommées (`FailureCount`, `ArticleCount`, `Committee`, `Decision`, `DeploymentTargetAvailable`, `Verdict`, plusieurs `Count` et `Status`) qui ne sont pas des paramètres Radzen; le parseur regex confond donc syntaxe Razor et contrat public, et aucune provenance par entrée ne permet de corriger ou authentifier les 2 992 lignes - lignes 21, 215-303, 397, 449, 547, 765, 1037, 1186, 1442-1478, 1774, 2598 - source: cohérence interne, `.claude/plan.md:23` et `docs/reproducibility.md` - recommandation: Codex peut remplacer l'extraction regex par une analyse Razor/sémantique, conserver pour chaque observation le fichier, la ligne et l'empreinte de l'instantané, ajouter des fixtures négatives sur expressions et régénérer le JSON avant de l'utiliser comme contrat de migration.

<a id="docscomponentcontractsmd"></a>
## `docs/component-contracts.md`

- [Moyen] [Authenticité] Le rapport reconnaît explicitement que son parseur regex ne comprend pas Razor, mais publie tout de même comme candidats des faux paramètres manifestes (`FailureCount`, `Committee`, `Verdict`, plusieurs `Count` et `Status`); la mise en garde empêche une fausse promesse totale, sans rendre la matrice exploitable comme preuve de contrat - lignes 3, 7, 39, 63, 83, 115, 143, 211, 391 - source: cohérence interne et `docs/component-contracts.json` - recommandation: Codex peut régénérer ce document depuis un extracteur Razor/sémantique avec provenance par occurrence et tests négatifs, puis supprimer l'étiquette de non-fiabilité seulement après comparaison automatisée aux contrats réels.

<a id="docscomponentcoveragejson"></a>
## `docs/component-coverage.json`

- [Élevé] [Authenticité] Le registre affirme `implemented: 110` et `planned: 0` pour toutes les cibles alors que le statut signifie seulement qu'un fichier Razor de ce nom existe; il classe notamment dialogue, graphiques empilés et éditeur HTML comme implémentés malgré les capacités fonctionnelles explicitement ouvertes dans le plan et l'architecture - lignes 3-5, 198-201, 348-351, 748-751, 818-821, 918-921 - source: `.claude/plan.md:52-65,137-176`, `AUDIT_ARCHITECTURE.md` ARCH-01/02 et logique de `eng/Generate-ComponentCoverage.ps1` - recommandation: Codex peut remplacer le booléen d'existence par une matrice de capacités liant chaque entrée à des tests nominaux et d'erreur, un scénario de catalogue et, lorsqu'il est visuel, une preuve navigateur; la gate doit échouer sur tout lien absent ou incomplet.

<a id="docscomponentcoveragemd"></a>
## `docs/component-coverage.md`

- [Moyen] [Authenticité] Les avertissements des lignes 3 et 121 reconnaissent que `implémenté` signifie seulement « fichier présent », mais la table et le compteur principal continuent d'afficher 110 cibles « implémentées », y compris dialogue, graphiques empilés et éditeur dont les gates comportementales restent ouvertes; cette terminologie entretient une preuve de complétude plus forte que la mesure réelle - lignes 3-6, 29, 44, 84, 91, 101, 121 - source: `.claude/plan.md` et `docs/component-coverage.json` - recommandation: Codex peut renommer immédiatement le statut en `fichier présent`, puis le remplacer par des statuts de capacités calculés depuis des liens vérifiables vers tests, catalogue et preuves navigateur.

<a id="docscomponentfamiliesmd"></a>
## `docs/component-families.md`

- [Faible] [Architecture] Cette carte agrège la surface en huit familles, tandis que `component-roadmap.md` en définit douze et que le catalogue en annonce dix; sans autorité canonique, les responsabilités, le placement et la couverture par domaine changent selon le document consulté - lignes 5-12 - source: `AUDIT_ARCHITECTURE.md` ARCH-05 - recommandation: Codex peut aligner cette synthèse sur la taxonomie canonique ajoutée à `docs/architecture.md`, puis faire dériver automatiquement les regroupements du catalogue et de la couverture de cette même source.

<a id="docscomponentinventoryjson"></a>
## `docs/component-inventory.json`

- [Moyen] [Fiabilité] L'inventaire est un instantané dépendant de `C:\Dev` qui ne conserve ni révisions/empreintes des 32 projets, ni manifeste des fichiers scannés, ni paramètres/exclusions reproductibles; il agrège en plus projets actifs, modèles, archives et un miroir `_github/Aetheus`, de sorte qu'un tiers ne peut pas reproduire ni interpréter sans ambiguïté les 110 composants et leurs compteurs - lignes 2-4 et ensemble des entrées `projects`/`catalog` - source: `docs/reproducibility.md` et `docs/clean-room.md:27-29` - recommandation: Codex peut versionner un manifeste minimal par projet (statut, chemin logique, révision, empreinte et fichier source), séparer les agrégats actif/archive/modèle/miroir, ajouter un test de déduplication et régénérer l'inventaire depuis un corpus explicitement identifié.

<a id="docscomponentinventorymd"></a>
## `docs/component-inventory.md`

- [Moyen] [Fiabilité] Le rapport fournit une commande dépendante de l'état courant de `C:\Dev`, sans révisions, empreintes ni manifeste des fichiers qui ont produit les compteurs; les archives et le miroir Aetheus participent en outre au tri secondaire, ce qui empêche une reproduction et une lecture stable de la priorité - lignes 3-7, 35-44, 267-273 - source: `docs/reproducibility.md` et JSON voisin - recommandation: Codex peut faire consommer au générateur un manifeste versionné des corpus et révisions, séparer les vues actif/archive/modèle/miroir et publier les empreintes d'entrée avec la commande exacte de régénération.

<a id="docscomponentroadmapmd"></a>
## `docs/component-roadmap.md`

RAS

<a id="docscspcontractmd"></a>
## `docs/csp-contract.md`

RAS

<a id="docsformcomponentsmd"></a>
## `docs/form-components.md`

RAS

<a id="docsfoundationcomponentsmd"></a>
## `docs/foundation-components.md`

RAS

<a id="docsmigrationaetheusmd"></a>
## `docs/migration-aetheus.md`

RAS

<a id="docsmigrationguidemd"></a>
## `docs/migration-guide.md`

RAS

<a id="docsperformancebudgetsmd"></a>
## `docs/performance-budgets.md`

RAS

<a id="docspublicapiconventionsmd"></a>
## `docs/public-api-conventions.md`

RAS

<a id="docspublicapitxt"></a>
## `docs/public-api.txt`

- [Élevé] [Authenticité] La baseline de 541 signatures omet des catégories entières de l'API réellement publique, notamment les paramètres requis, méthodes, propriétés, constructeurs, interfaces, structs, delegates et contraintes génériques; une rupture dans ces membres peut donc laisser la gate verte et le fichier ne peut pas servir de preuve exhaustive de stabilité - ensemble du fichier, notamment lignes 1-541 - source: limites reconnues dans `docs/public-api-conventions.md` et logique regex de `eng/Test-PublicApi.ps1:20-25` - recommandation: Codex peut remplacer la baseline par une extraction Roslyn de symboles publics, couvrir chaque catégorie avec des fixtures positives et négatives, puis régénérer ce fichier dans un format déterministe.

<a id="docsradzensurfaceinventoryjson"></a>
## `docs/radzen-surface-inventory.json`

- [Élevé] [Authenticité] Le champ `symbols` mélange usages Radzen et simples identifiants contenant ce préfixe, dont `RadzenAssets_AreCacheBustedWithTheReferencedPackageVersion`, `RadzenButtonIconAuditTests`, `RadzenLabelAssociationAuditTests` et `RadzenSanitizer_PreservesNativeHeaderSortState`; les 122 symboles et leurs occurrences ne mesurent donc pas la surface API à migrer - lignes 2-496 - source: cohérence interne et regex de `eng/Generate-RadzenSurfaceInventory.ps1:34` - recommandation: Codex peut analyser les arbres C#/Razor, classifier type, balise, namespace, package et ressource, exclure les identifiants de tests non référents, conserver la provenance par occurrence et régénérer le rapport depuis un corpus manifesté.
- [Moyen] [Fiabilité] L'inventaire de 1 457 fichiers dépend de l'état externe courant de `C:\Dev` sans révision, hash ni manifeste d'entrée et inclut miroirs, archives, rapports d'audit et fichiers de couverture; ses compteurs ne sont ni reproductibles ni interprétables comme une vue stable du parc actif - lignes 2-6 et 507-1968 - source: `docs/reproducibility.md` - recommandation: Codex peut faire consommer au générateur un manifeste versionné des corpus, statuts, révisions et empreintes, séparer les vues actives des archives et sorties techniques, puis publier la commande et les hashes d'entrée.

<a id="docsradzensurfaceinventorymd"></a>
## `docs/radzen-surface-inventory.md`

- [Élevé] [Authenticité] Le tableau présente 122 « symboles C#/Razor » alors qu'il contient des noms de tests et d'assertions tels que `RadzenAssets_AreCacheBustedWithTheReferencedPackageVersion`, `RadzenButtonIconAuditTests`, `RadzenLabelAssociationAuditTests` et `RadzenSanitizer_PreservesNativeHeaderSortState`; les totaux affichés ne représentent donc pas une surface Radzen exploitable pour la migration - lignes 3-7, 16, 25, 77 et 116 - source: JSON voisin et regex de `eng/Generate-RadzenSurfaceInventory.ps1:34` - recommandation: Codex peut régénérer ce tableau depuis une analyse sémantique classifiée et ne publier comme symboles que les références dont la provenance et la nature sont conservées.
- [Moyen] [Fiabilité] Le rapport dépend d'un instantané non manifesté de `C:\Dev`, ne sépare pas projets actifs, miroirs, archives et sorties techniques, et n'expose même pas la liste des 1 457 fichiers dans sa vue humaine; le résultat ne peut pas être reproduit ni relu comme une priorité stable - lignes 3-7 et ensemble du tableau - source: JSON voisin et `docs/reproducibility.md` - recommandation: Codex peut ajouter le manifeste d'entrée, les révisions/hashes, les exclusions et des agrégats séparés par statut, puis générer la vue Markdown depuis ces données vérifiables.

## `docs/reproducibility.md`

RAS

<a id="docsselectioncomponentsmd"></a>
## `docs/selection-components.md`

RAS

## `docs/versioning.md`

RAS

<a id="enggeneratecomponentcoverageps1"></a>
## `eng/Generate-ComponentCoverage.ps1`

- [Élevé] [Authenticité] Le générateur attribue `implemented` uniquement lorsque le nom d'un fichier `Omni*.razor` correspond à la cible calculée; il transforme ainsi une présence physique en état d'implémentation pour 110 composants sans vérifier capacité, test, catalogue, interaction ni compatibilité - lignes 11-12 et 50-69 - source: `docs/component-coverage.json`, `AUDIT_ARCHITECTURE.md` ARCH-01/02 et plan canonique - recommandation: Codex peut remplacer cette déduction par une matrice versionnée de capacités avec liens vers tests exécutables, états de catalogue et preuves navigateur, et faire échouer la génération lorsque toute preuve obligatoire manque.
- [Faible] [Style] Le script contient des chaînes non ASCII avec accents et guillemets français, contrairement à la règle du kit imposant l'ASCII pour PowerShell; son comportement dépend donc davantage de l'encodage et de l'hôte que le standard autorisé - lignes 78-92 - source: `coding-standards.md` du kit - recommandation: Codex peut déplacer les libellés français dans une ressource UTF-8 consommée par le script ou translittérer les sorties techniques, puis ajouter une vérification ASCII ciblée aux fichiers `.ps1`.

<a id="enggenerateradzeninventoryps1"></a>
## `eng/Generate-RadzenInventory.ps1`

- [Moyen] [Fiabilité] Le générateur balaie directement tout `C:\Dev`, déduit le statut d'un projet de son préfixe de chemin et ne capture ni révision, empreinte, manifeste des entrées ni preuve de déduplication; deux exécutions sur des états externes différents produisent des priorités incompatibles sous le même format - lignes 3, 18-24, 59-76 et 152-159 - source: `docs/reproducibility.md` et inventaires générés - recommandation: Codex peut rendre obligatoire un manifeste de corpus versionné, valider les racines/statuts, enregistrer révisions et hashes, puis séparer explicitement actifs, modèles, archives et miroirs dans les sorties.
- [Faible] [Style] Le script PowerShell contient de nombreuses chaînes non ASCII, y compris des valeurs fonctionnelles de statut, alors que le kit exige des scripts PowerShell ASCII - lignes 22-23, 56, 131 et 164-215 - source: `coding-standards.md` du kit - recommandation: Codex peut utiliser des clés ASCII stables pour les statuts et externaliser les libellés humains UTF-8, avec une gate ASCII sur `eng/*.ps1`.

<a id="enggenerateradzensurfaceinventoryps1"></a>
## `eng/Generate-RadzenSurfaceInventory.ps1`

- [Élevé] [Authenticité] La regex `Radzen[A-Z][A-Za-z0-9_]*` compte toute sous-chaîne lexicale comme symbole, y compris des noms de tests, méthodes et assertions, tandis que le parseur d'attributs Razor confond des expressions avec des paramètres; les deux rapports de surface et de contrats peuvent donc fabriquer des preuves de migration - lignes 34-39 et 56-75 - source: faux positifs observables dans les JSON générés - recommandation: Codex peut remplacer ces regex par des parseurs C# et Razor, classifier les références, conserver fichier/ligne/hash par occurrence et couvrir les faux positifs actuels par des fixtures négatives.
- [Moyen] [Fiabilité] Le script inspecte l'état courant de `C:\Dev` sans manifeste, révision ni hashes, inclut les rapports et archives qui contiennent le mot Radzen, et publie un compteur global sans statuts de projets; une régénération ne peut pas reproduire ni comparer proprement l'instantané - lignes 3, 14-23 et 89-104 - source: `docs/reproducibility.md` - recommandation: Codex peut alimenter le scan par un manifeste borné et empreinté, exclure explicitement sorties techniques et corpus non actifs, et persister paramètres, versions d'outils et entrées.
- [Faible] [Style] Les libellés générés contiennent des caractères non ASCII dans un fichier PowerShell, en violation de la règle ASCII du kit - lignes 111-143 - source: `coding-standards.md` du kit - recommandation: Codex peut externaliser les textes humains UTF-8 et maintenir uniquement des identifiants et messages techniques ASCII dans le script, puis automatiser ce contrôle.

<a id="engtestautohostps1"></a>
## `eng/Test-AutoHost.ps1`

- [Faible] [Style] Les messages du script PowerShell contiennent des accents et signes non ASCII malgré la règle ASCII du kit - lignes 24, 41, 53-71 et 81 - source: `coding-standards.md` du kit - recommandation: Codex peut translittérer les messages techniques ou les externaliser dans une ressource UTF-8 et ajouter un contrôle ASCII pour `eng/*.ps1`.

<a id="engtestbudgetsps1"></a>
## `eng/Test-Budgets.ps1`

RAS

<a id="engtestcataloghostps1"></a>
## `eng/Test-CatalogHost.ps1`

- [Élevé] [Authenticité] La prétendue preuve de « zéro violation » interroge la page par HTTP puis consulte `/csp-status` sans charger ni exercer le catalogue dans un navigateur; aucun script, composant interactif ou rapport CSP client n'a donc pu s'exécuter, et un collecteur vide passe mécaniquement - lignes 38-63 et 74 - source: `docs/reproducibility.md` et plan canonique phase 13 - recommandation: Codex peut remplacer cette gate par un scénario Playwright qui navigue chaque famille, exerce les interactions critiques, collecte `securitypolicyviolation` et rapports serveur, puis échoue sur toute violation avec traces archivées.
- [Faible] [Style] Les messages du script PowerShell contiennent des caractères non ASCII contrairement à la règle du kit - lignes 24, 41, 55-74 et 84 - source: `coding-standards.md` du kit - recommandation: Codex peut translittérer ou externaliser les libellés et automatiser la vérification ASCII des scripts.

<a id="engtestcspps1"></a>
## `eng/Test-Csp.ps1`

- [Moyen] [Sécurité] Le scanner statique ne cherche que `style=`, les éléments style créés à l'exécution, `eval` et `new Function`; il laisse hors gate les gestionnaires HTML `on*=`, URL `javascript:`, imports/ressources distants et constructions indirectes, alors qu'aucune exécution navigateur ne compense ces angles morts - lignes 15-25 - source: comparaison avec `docs/csp-contract.md` et absence de preuve navigateur dans `docs/reproducibility.md` - recommandation: Codex peut étendre les règles source avec fixtures positives/négatives, puis les compléter par une gate navigateur réelle sous la CSP de référence sans présenter le scan statique comme preuve suffisante.

<a id="engtestpackageps1"></a>
## `eng/Test-Package.ps1`

- [Élevé] [Authenticité] L'absence de Radzen n'est vérifiée que dans les noms d'entrées ZIP et le `.nuspec`; le contenu de la DLL, du PDB, du CSS et du JavaScript n'est jamais inspecté, de sorte qu'un paquet contaminé peut afficher « NuGet content passed » et être publié - lignes 12-35 et 40-47 - source: plan canonique phase 13 et limites de provenance dans `docs/reproducibility.md` - recommandation: Codex peut ajouter un paquet négatif contaminé, scanner sémantiquement assembly/PDB et textuellement ressources, consigner hashes et résultats bruts, puis faire publier uniquement l'artefact exact ayant passé cette gate.

<a id="engtestpublicapips1"></a>
## `eng/Test-PublicApi.ps1`

- [Élevé] [Fiabilité] L'extraction par regex ne couvre qu'une forme de `[Parameter]` et les déclarations `enum|class|record` commençant exactement par `public`; elle ignore paramètres requis, méthodes, propriétés, constructeurs, interfaces, structs, delegates, opérateurs, membres hérités et contraintes, ce qui laisse passer des ruptures publiques importantes - lignes 10-29 - source: limites de `docs/public-api-conventions.md` et baseline voisine - recommandation: Codex peut remplacer l'extraction par Roslyn, sérialiser toutes les signatures publiques pertinentes de façon canonique et ajouter une fixture de rupture par catégorie pour prouver que la gate échoue.

## `global.json`

- [Faible] [Fiabilité] `rollForward: latestPatch` autorise tout patch installé de la bande `10.0.3xx`; contrairement au terme « verrouillé » employé dans le README, le SDK exact et les workloads associés peuvent varier entre développeurs et runners - lignes 2-5 - source: `AUDIT_DEPENDENCIES.md` D-006 et `docs/reproducibility.md` - recommandation: Codex peut choisir une stratégie reproductible de SDK/workload, documenter explicitement le comportement de roll-forward et enregistrer les versions effectives dans les preuves CI.

## `LICENSE`

- [Faible] [Style] Le texte légal officiel contient deux tirets cadratins U+2014, alors que le contrôle typographique du workflow les interdit globalement; une correction mécanique risquerait toutefois d'altérer le texte de référence de l'EUPL - lignes 116 et 278 - source: scan U+2014 exhaustif des 58 fichiers - recommandation: Codex peut borner explicitement l'exception au fichier `LICENSE` après comparaison avec la version officielle EUPL-1.2, puis faire échouer la gate U+2014 partout ailleurs sans réécrire le texte légal.

## `NOTICE.md`

- [Faible] [Dépendances] Le fichier ne recense aucun composant tiers, version, licence ni obligation de redistribution alors que le dépôt dépend de packages Microsoft et bUnit et ne possède ni SBOM ni registre de licences; la conformité des artefacts distribués n'est donc pas vérifiable depuis le dépôt - lignes 1-7 - source: `AUDIT_DEPENDENCIES.md` D-005 - recommandation: Codex peut générer un SBOM et un inventaire de licences depuis les verrous, vérifier les obligations applicables, puis faire dériver les notices nécessaires de cette source contrôlée.

## `NuGet.Config`

RAS

## `OmniEurope.Blazor.slnx`

RAS

<a id="plansplan001composantsblazormd"></a>
## `plans/PLAN-001-composants-blazor.md`

- [Moyen] [Authenticité] Ce plan ancien marque comme terminés le scan de « tous les projets » et la conservation d'un script reproductible, alors que le plan canonique actuel laisse précisément ouverts la correction des parseurs et le manifeste de provenance; il indique aussi la publication GitFlow en attente alors que le plan 002 la marque accomplie - lignes 9-12 et 21-26 - source: `plans/PLAN-002-remplacement-radzen.md` phases 1, 2 et 13 - recommandation: Codex peut clôturer ce plan historique avec un statut supersédé, relier chaque affirmation au plan canonique et ne conserver comme achevées que les preuves encore valides.

<a id="plansplan002remplacementradzenmd"></a>
## `plans/PLAN-002-remplacement-radzen.md`

RAS

## `README.md`

- [Faible] [Documentation] Le README affirme que le SDK `10.0.302` est « verrouillé par global.json », alors que `rollForward: latestPatch` autorise un autre patch `10.0.3xx`; cette instruction peut produire des environnements différents sous une promesse de verrouillage exact - lignes 47-49 - source: `global.json` et `docs/reproducibility.md` - recommandation: Codex peut décrire précisément la bande autorisée et la stratégie workload, ou épingler l'environnement exact retenu puis publier les versions effectives dans la CI.

## `SECURITY.md`

- [Moyen] [Sécurité] Le seul canal de divulgation proposé est la fonctionnalité GitHub privée « lorsque celle-ci est activée », sans confirmer son activation ni fournir de repli; si elle est absente, un chercheur ne dispose d'aucun moyen confidentiel documenté alors que les issues publiques sont interdites - lignes 3-5 - source: revue documentaire best-effort, non outillée - recommandation: Codex peut configurer et vérifier les advisories privés du dépôt, ajouter un contact de sécurité contrôlé et documenter délais d'accusé/réponse, versions supportées et procédure de divulgation.
