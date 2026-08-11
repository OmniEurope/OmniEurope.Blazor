# Findings d’audit 360 - Repository

> Audit frais : 2026-08-11
> Périmètre : 108 fichiers, mode Full, lecture intégrale.
> Les constats globaux Architecture/Kit ne sont pas dupliqués. Chaque fichier possède un bloc explicite.

## Synthèse

| Critique | Élevé | Moyen | Faible | INFO |
|---:|---:|---:|---:|---:|
| 0 | 0 | 16 | 4 | 1 |

<a id="claudeaudit-remediationmd"></a>
## `.claude/audit-remediation.md`

RAS.

<a id="claudeauditsessionmd"></a>
## `.claude/auditsession.md`

- [REP-001] [Moyen] [Documentation] Le backlog affirme que les éléments résolus sont élagués, mais conserve comme ouverts la sonde CSP navigateur, l’extracteur d’API sémantique, les graphiques, les superpositions, la provenance et l’état d’erreur Autocomplete, alors que les lots de remédiation documentent leur fermeture. Il mélange ainsi travaux encore valides et diagnostics périmés sans statut exploitable - lignes 4, 12-26 - preuve : `.claude/audit-remediation.md:126-177, 442-455` - recommandation : retirer les entrées fermées ou ajouter à chacune un statut et un lien vers sa preuve de fermeture.

<a id="claudechallenge-sessionmd"></a>
## `.claude/challenge-session.md`

RAS.

<a id="claudecode-rulesmd"></a>
## `.claude/code-rules.md`

RAS. Les divergences d’analyseurs sont couvertes globalement par `KIT-004` à `KIT-008`.

<a id="claudeplanmd"></a>
## `.claude/plan.md`

- [REP-002] [Moyen] [Documentation] Le plan actif laisse les cinq lots de correction décochés alors que le journal canonique établit 324/325 fermetures et identifie uniquement `A360-023` comme dépendance externe; son état ne permet plus de piloter ni de reprendre correctement le travail - lignes 13-17 - preuve : `.claude/audit-remediation.md:6-35` et `plans/PLAN-003-correction-findings-audit.md` - recommandation : refléter les lots terminés et isoler explicitement l’unique blocker externe.

<a id="claudesuggestionsmd"></a>
## `.claude/suggestions.md`

- [REP-003] [Faible] [Documentation] Deux suggestions restent actives bien que la publication depuis l’artefact CI et la garde de provenance aient été implémentées et câblées dans les workflows; le backlog n’est donc plus dédupliqué avec l’état courant - lignes 10-11 - preuve : `.github/workflows/publish-nuget.yml:21-48`, `.github/workflows/ci.yml` et `eng/Test-PackageProvenance.ps1` - recommandation : archiver les suggestions réalisées et dater leur preuve de fermeture.

<a id="claudetest-configmd"></a>
## `.claude/test-config.md`

RAS.

<a id="editorconfig"></a>
## `.editorconfig`

RAS.

<a id="githubdependabotyml"></a>
## `.github/dependabot.yml`

RAS.

<a id="githubworkflowsciyml"></a>
## `.github/workflows/ci.yml`

RAS.

<a id="githubworkflowspublish-nugetyml"></a>
## `.github/workflows/publish-nuget.yml`

RAS.

<a id="gitignore"></a>
## `.gitignore`

RAS.

<a id="changelogmd"></a>
## `CHANGELOG.md`

RAS.

<a id="claudemd"></a>
## `CLAUDE.md`

RAS. La forme de la carte documentaire est couverte globalement par `KIT-003`.

<a id="contributingmd"></a>
## `CONTRIBUTING.md`

RAS.

<a id="directorybuildprops"></a>
## `Directory.Build.props`

RAS.

<a id="directorypackagesprops"></a>
## `Directory.Packages.props`

RAS. La fraîcheur et la politique des dépendances sont couvertes globalement par `KIT-009` à `KIT-012`.

<a id="license"></a>
## `LICENSE`

RAS.

<a id="noticemd"></a>
## `NOTICE.md`

RAS.

<a id="nugetconfig"></a>
## `NuGet.Config`

RAS.

<a id="omnieuropeblazorslnx"></a>
## `OmniEurope.Blazor.slnx`

RAS. L’absence du projet de tests d’analyseur est couverte globalement par `KIT-005`.

<a id="readmemd"></a>
## `README.md`

RAS.

<a id="securitymd"></a>
## `SECURITY.md`

RAS.

<a id="docsaccessibility-contractmd"></a>
## `docs/accessibility-contract.md`

- [REP-004] [Moyen] [Documentation] Le contrat affirme encore que `OmniTabs` ne possède ni `role="tablist"` ni déplacement de focus DOM, alors que ces deux comportements ont été implémentés et vérifiés dans Chromium; il décrit donc comme ouverte une lacune fermée - ligne 26 - preuve : `.claude/audit-remediation.md:163-177` - recommandation : conserver seulement les limites d’accessibilité réellement non prouvées et relier le comportement Tabs à sa sonde navigateur.

<a id="docsagentsmd"></a>
## `docs/agents.md`

RAS. L’absence du fichier racine attendu est couverte globalement par `KIT-002`.

<a id="docsanalyzersmd"></a>
## `docs/analyzers.md`

RAS. La divergence des règles et l’absence de projet de tests dédié sont couvertes globalement par `KIT-004` et `KIT-005`.

<a id="docsarchitecturemd"></a>
## `docs/architecture.md`

RAS. La direction de dépendance du renderer et la taxonomie physique sont couvertes globalement par `ARCH-R-02` et `ARCH-R-04`.

<a id="docsbrowser-scenariosjson"></a>
## `docs/browser-scenarios.json`

RAS.

<a id="docscatalog-scenariosjson"></a>
## `docs/catalog-scenarios.json`

RAS.

<a id="docsclean-room-component-sheetmd"></a>
## `docs/clean-room-component-sheet.md`

RAS.

<a id="docsclean-roommd"></a>
## `docs/clean-room.md`

- [REP-005] [Moyen] [Authenticité] Le document affirme que le générateur ne conserve pas de manifeste d’origine, alors que `docs/radzen-corpus.json` versionne projets, révisions, empreintes et fichiers et que les générateurs activent sa vérification stricte - ligne 27 - preuve : `docs/radzen-corpus.json`, `eng/RadzenCorpus.ps1` et `eng/Test-RadzenCorpus.ps1` - recommandation : décrire le manifeste actuel et borner la limite résiduelle à la nécessité de disposer du corpus externe correspondant aux hashes.

<a id="docscompatibilitymd"></a>
## `docs/compatibility.md`

- [REP-006] [Faible] [Documentation] La matrice décrit un unique module JavaScript limité au focus de la première erreur, alors que le paquet contient `omniInterop.js` et `omni-focus.js` et que l’interop couvre aussi Tabs, sélection d’éditeur et superpositions - ligne 10 - preuve : `src/OmniEurope.Blazor/wwwroot/omniInterop.js`, `src/OmniEurope.Blazor/wwwroot/omni-focus.js` et les sondes d’hôtes - recommandation : documenter les deux modules et leurs responsabilités CSP.

<a id="docscomponent-contractsjson"></a>
## `docs/component-contracts.json`

RAS.

<a id="docscomponent-contractsmd"></a>
## `docs/component-contracts.md`

RAS.

<a id="docscomponent-coveragejson"></a>
## `docs/component-coverage.json`

RAS.

<a id="docscomponent-coveragemd"></a>
## `docs/component-coverage.md`

RAS.

<a id="docscomponent-familiesmd"></a>
## `docs/component-families.md`

RAS.

<a id="docscomponent-inventoryjson"></a>
## `docs/component-inventory.json`

RAS.

<a id="docscomponent-inventorymd"></a>
## `docs/component-inventory.md`

RAS.

<a id="docscomponent-roadmapmd"></a>
## `docs/component-roadmap.md`

- [REP-007] [Moyen] [Documentation] La roadmap laisse la pile de dialogues et la restauration de focus à prouver dans un navigateur, et regroupe encore sélection et IME de l’éditeur parmi les manques, alors que dialogue/focus et sélection ont déjà des preuves Chromium; seule la composition IME reste ouverte dans ce groupe - lignes 36 et 44 - preuve : `.claude/audit-remediation.md:189-207, 442-455` - recommandation : séparer capacités fermées et limites restantes pour éviter de sous-déclarer l’état réel.

<a id="docscsp-contractmd"></a>
## `docs/csp-contract.md`

- [REP-008] [Moyen] [Authenticité] La conclusion présente toujours le collecteur vide avant interaction comme l’unique preuve du catalogue, alors que la gate charge désormais le catalogue dans Chromium, exerce plusieurs familles et vérifie violations client, collecteur serveur et console après interaction - ligne 40 - preuve : `.claude/audit-remediation.md:442-455` et `eng/Test-CatalogProbe.mjs` - recommandation : décrire la preuve navigateur actuelle et conserver uniquement ses limites de couverture explicites.

<a id="docsdependenciesmd"></a>
## `docs/dependencies.md`

RAS. Les constats de fraîcheur et de politique sont couverts globalement par `KIT-009` à `KIT-012`.

<a id="docsform-componentsmd"></a>
## `docs/form-components.md`

RAS.

<a id="docsfoundation-componentsmd"></a>
## `docs/foundation-components.md`

RAS.

<a id="docslocalizationmd"></a>
## `docs/localization.md`

RAS. Le contournement de `IStringLocalizer` est couvert globalement par `ARCH-R-01`.

<a id="docsmigration-aetheusmd"></a>
## `docs/migration-aetheus.md`

RAS.

<a id="docsmigration-guidemd"></a>
## `docs/migration-guide.md`

- [REP-009] [Moyen] [Documentation] Le tableau annonce toujours que l’Autocomplete distant n’expose ni erreur ni reprise, alors que `SearchFailed` et un état récupérable ont été ajoutés et testés - ligne 22 - preuve : `.claude/audit-remediation.md:143` - recommandation : mettre la ligne de migration en cohérence avec l’API et documenter le canal d’erreur sans fuite de détail.

<a id="docsperformance-budgetsmd"></a>
## `docs/performance-budgets.md`

RAS.

<a id="docspublic-api-conventionsmd"></a>
## `docs/public-api-conventions.md`

- [REP-010] [Moyen] [Documentation] Les conventions indiquent encore que `OmniAutocomplete` ne possède pas d’état d’erreur ou de reprise, en contradiction avec `SearchFailed` et la récupération déterministe désormais publiés - ligne 9 - preuve : `.claude/audit-remediation.md:143` et la baseline `docs/public-api.txt` - recommandation : actualiser le contrat public et citer les signatures correspondantes.

<a id="docspublic-apitxt"></a>
## `docs/public-api.txt`

RAS.

<a id="docsradzen-corpusjson"></a>
## `docs/radzen-corpus.json`

RAS.

<a id="docsradzen-surface-inventoryjson"></a>
## `docs/radzen-surface-inventory.json`

- [REP-011] [Faible] [Fiabilité] La provenance d’une observation ne conserve que chemin, ligne et hash : 66 clés `kind/name/path/line` sont dupliquées, jusqu’à huit occurrences identiques sur une ligne, ce qui empêche d’identifier précisément chaque occurrence malgré la promesse de provenance complète du rapport Markdown - preuve : regroupement des 24 597 observations et `eng/Generate-RadzenSurfaceInventory.ps1:32-42,82-87` - recommandation : ajouter colonne, offset ou ordinal stable à chaque observation et à chaque preuve de contrat, puis régénérer les deux rapports.

<a id="docsradzen-surface-inventorymd"></a>
## `docs/radzen-surface-inventory.md`

RAS.

<a id="docsreproducibilitymd"></a>
## `docs/reproducibility.md`

- [REP-012] [Moyen] [Authenticité] L’inventaire des contrôles rejouables qualifie encore l’API de partielle, omet les gates SBOM/provenance et affirme que le catalogue n’est pas exercé dans un navigateur, alors que l’extracteur est sémantique et que ces gates existent en CI - lignes 15-22 et 27 - preuve : `docs/public-api-conventions.md:25`, `eng/Test-Sbom.ps1`, `eng/Test-PackageProvenance.ps1` et `.claude/audit-remediation.md:442-455` - recommandation : régénérer la section depuis les gates réellement exécutées et distinguer preuve actuelle, preuve historique non archivée et limite résiduelle.

<a id="docssbomcdxjson"></a>
## `docs/sbom.cdx.json`

- [REP-013] [Moyen] [Authenticité] Le composant racine du SBOM est déclaré en version `1.0.0` alors que le projet packagé est `0.1.0-alpha.1`; le SBOM identifie donc un produit différent de l’artefact courant - lignes 8-10 - preuve : `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj:5` et `eng/Generate-Sbom.ps1:148-152` - recommandation : dériver la version depuis le projet ou le paquet, puis faire vérifier l’égalité par `Test-Sbom.ps1`.

<a id="docsselection-componentsmd"></a>
## `docs/selection-components.md`

RAS.

<a id="docstestingmd"></a>
## `docs/testing.md`

RAS.

<a id="docsthird-party-licensesmicrosoftwebwebview2--10317945--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.Web.WebView2--1.0.3179.45--LICENSE.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowssdkbuildtoolsmsix--17202508291--sdklicensetxt"></a>
## `docs/third-party-licenses/Microsoft.Windows.SDK.BuildTools.MSIX--1.7.20250829.1--sdk_license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdk--18260508005--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK--1.8.260508005--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkai--1876--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.AI--1.8.76--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkbase--18251216001--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.Base--1.8.251216001--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkdwrite--1825122902--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.DWrite--1.8.25122902--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkfoundation--18260505001--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.Foundation--1.8.260505001--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkinteractiveexperiences--18260430001--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.InteractiveExperiences--1.8.260430001--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkml--182197--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.ML--1.8.2197--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkruntime--18260508005--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.Runtime--1.8.260508005--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkwidgets--18251231004--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.Widgets--1.8.251231004--license.txt`

RAS.

<a id="docsthird-party-licensesmicrosoftwindowsappsdkwinui--18260505002--licensetxt"></a>
## `docs/third-party-licenses/Microsoft.WindowsAppSDK.WinUI--1.8.260505002--license.txt`

RAS.

<a id="docsthird-party-packagesjson"></a>
## `docs/third-party-packages.json`

RAS.

<a id="docsui-conventionsmd"></a>
## `docs/ui-conventions.md`

RAS.

<a id="docsversioningmd"></a>
## `docs/versioning.md`

- [REP-014] [Moyen] [Documentation] La politique SemVer continue de présenter la baseline publique comme partielle alors que la gate sémantique couvre types, composants, membres, delegates et contraintes génériques; cette description obsolète brouille la garantie de rupture réellement appliquée - ligne 9 - preuve : `docs/public-api-conventions.md:25` et `eng/Test-PublicApi.ps1` - recommandation : décrire la baseline exhaustive actuelle et ses limites résiduelles vérifiables.

<a id="enggenerate-componentcoverageps1"></a>
## `eng/Generate-ComponentCoverage.ps1`

RAS.

<a id="enggenerate-radzeninventoryps1"></a>
## `eng/Generate-RadzenInventory.ps1`

RAS.

<a id="enggenerate-radzensurfaceinventoryps1"></a>
## `eng/Generate-RadzenSurfaceInventory.ps1`

RAS.

<a id="enggenerate-sbomps1"></a>
## `eng/Generate-Sbom.ps1`

- [REP-015] [Faible] [Sécurité] Pour une licence NuGet de type `file`, la valeur du nuspec est jointe au répertoire du paquet puis lue sans canonicalisation ni contrôle de confinement; une métadonnée malveillante contenant des segments parents peut faire copier et hasher un fichier situé hors du paquet - lignes 67-83 - preuve : revue du flux `Join-Path`/`Copy-Item` - recommandation : résoudre le chemin complet, exiger qu’il reste sous le répertoire canonique du paquet et ajouter une fixture de traversée rejetée.

<a id="engnew-packageprovenanceps1"></a>
## `eng/New-PackageProvenance.ps1`

RAS.

<a id="engpowershellmessagespsd1"></a>
## `eng/PowerShellMessages.psd1`

RAS.

<a id="engradzencorpusps1"></a>
## `eng/RadzenCorpus.ps1`

RAS.

<a id="engradzensyntaxps1"></a>
## `eng/RadzenSyntax.ps1`

RAS.

<a id="engserve-staticwithheadersmjs"></a>
## `eng/Serve-StaticWithHeaders.mjs`

RAS.

<a id="engtest-autohostps1"></a>
## `eng/Test-AutoHost.ps1`

RAS.

<a id="engtest-budgetsps1"></a>
## `eng/Test-Budgets.ps1`

RAS.

<a id="engtest-cataloghostps1"></a>
## `eng/Test-CatalogHost.ps1`

RAS.

<a id="engtest-catalogprobemjs"></a>
## `eng/Test-CatalogProbe.mjs`

RAS.

<a id="engtest-cdpprobemjs"></a>
## `eng/Test-CdpProbe.mjs`

RAS.

<a id="engtest-coverageps1"></a>
## `eng/Test-Coverage.ps1`

- [REP-016] [Moyen] [Tests] La gate accepte toute couverture strictement supérieure à zéro et seulement 57 tests, alors que l’artefact courant compte 181 tests et 86,64 % de lignes; une suppression massive de tests ou de couverture resterait verte - lignes 5, 31-39 - preuve : `metrics/tests.trx` (181/181) et `metrics/coverage.cobertura.xml` (2 342/2 703 lignes) - recommandation : fixer des seuils de tests, lignes et branches proches de la baseline, avec une politique explicite de mise à jour sans baisse silencieuse.

<a id="engtest-cspps1"></a>
## `eng/Test-Csp.ps1`

- [REP-017] [Moyen] [Sécurité] Le scanner exclut les fichiers CSS et ne détecte donc ni `@import` distant ni `url(http[s]://...)`, alors que ces ressources peuvent contourner l’objectif annoncé de ressources statiques locales sans `unsafe-inline` - lignes 15-28 - preuve : extensions autorisées limitées à Razor, C#, JavaScript et HTML - recommandation : inclure CSS, interdire les imports/URL distants par fixtures positives et négatives, puis conserver la sonde navigateur comme preuve complémentaire.

<a id="engtest-cspfixturesps1"></a>
## `eng/Test-CspFixtures.ps1`

RAS.

<a id="engtest-dependencypolicyps1"></a>
## `eng/Test-DependencyPolicy.ps1`

RAS.

<a id="engtest-hybridhostps1"></a>
## `eng/Test-HybridHost.ps1`

RAS.

<a id="engtest-packageps1"></a>
## `eng/Test-Package.ps1`

- [REP-018] [Moyen] [Fiabilité] Le contrôle du paquet exige seulement qu’au moins un fichier existe sous `compliance/licenses/`; il ne compare ni le nombre ni les chemins/hashes des licences emballées avec `third-party-packages.json`, de sorte qu’un paquet amputé de la majorité des 12 licences locales peut passer - lignes 68-80 - preuve : registre courant de 114 paquets dont 12 licences préservées - recommandation : lire le registre dans l’archive et vérifier exactement chaque licence locale et son SHA-256, ainsi que l’absence de fichier inattendu.

<a id="engtest-packagefixturesps1"></a>
## `eng/Test-PackageFixtures.ps1`

RAS.

<a id="engtest-packageprovenanceps1"></a>
## `eng/Test-PackageProvenance.ps1`

RAS.

<a id="engtest-publicapips1"></a>
## `eng/Test-PublicApi.ps1`

RAS.

<a id="engtest-radzencorpusps1"></a>
## `eng/Test-RadzenCorpus.ps1`

RAS.

<a id="engtest-radzensyntaxps1"></a>
## `eng/Test-RadzenSyntax.ps1`

RAS.

<a id="engtest-sbomps1"></a>
## `eng/Test-Sbom.ps1`

- [REP-019] [Moyen] [Authenticité] Après validation du registre contre les verrous, la vérification SBOM ne compare aux paquets attendus que le nombre de composants; des composants arbitraires, uniques et munis d’une licence peuvent donc remplacer les vrais `id/version/purl` tout en restant verts, et la version racine erronée n’est pas contrôlée - lignes 58-69 - preuve : absence de comparaison des clés attendues et `docs/sbom.cdx.json:8-10` - recommandation : comparer exactement nom, version, bom-ref, purl, licences et propriétés à chaque entrée du registre, puis valider le composant racine contre le projet.

<a id="engtest-wasmheadersps1"></a>
## `eng/Test-WasmHeaders.ps1`

RAS.

<a id="engtest-wasmhostps1"></a>
## `eng/Test-WasmHost.ps1`

RAS.

<a id="engupdate-radzencorpusmanifestps1"></a>
## `eng/Update-RadzenCorpusManifest.ps1`

RAS.

<a id="engdependency-policyjson"></a>
## `eng/dependency-policy.json`

RAS.

<a id="engfixturescspsafesaferazor"></a>
## `eng/fixtures/csp/safe/Safe.razor`

RAS.

<a id="engfixturescspunsafeunsafehtml"></a>
## `eng/fixtures/csp/unsafe/Unsafe.html`

RAS.

<a id="globaljson"></a>
## `global.json`

RAS.

<a id="plansplan-001-composants-blazormd"></a>
## `plans/PLAN-001-composants-blazor.md`

RAS.

<a id="plansplan-002-remplacement-radzenmd"></a>
## `plans/PLAN-002-remplacement-radzen.md`

- [REP-020] [Moyen] [Documentation] Le plan successeur conserve comme non réalisés de nombreux travaux désormais prouvés : parseur Razor, navigateur CSP, pile/focus des dialogues, erreur Autocomplete, Tabs, axes/empilements, sélection éditeur et presque toute la consolidation d’audit; ses cases ne correspondent plus à l’état observable - lignes 23, 30-31, 57-65, 88, 103, 140-145, 172, 178-196 - preuve : `.claude/audit-remediation.md` lots 03-22 - recommandation : réconcilier chaque case avec les preuves de remédiation et laisser ouvertes uniquement les capacités réellement incomplètes, notamment couverture comportementale, virtualisation, IME et accessibilité outillée.

<a id="plansplan-003-correction-findings-auditmd"></a>
## `plans/PLAN-003-correction-findings-audit.md`

RAS.

## Notification de proportionnalité (INFO)

- [REP-INFO-001] Le renforcement proposé reste proportionné : il cible des affirmations déjà publiées par les gates et les rapports. La preuve clean-room ne doit toutefois pas être élargie en archivant des sources tierces propriétaires dans ce dépôt; manifeste, hashes, scanner isolé et résultats bornés suffisent.

