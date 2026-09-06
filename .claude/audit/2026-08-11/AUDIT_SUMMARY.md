# Synthèse de l'audit 360 - OmniEurope.Blazor

> Date: 2026-08-11  
> Révision auditée: `717af586cc40f3d87572e8e76b0b452ef4766b04`  
> Portée: inventaire Git complet hors état auto-généré `.claude/audit/**`  
> Verdict: **NEEDS ATTENTION**

## Résumé exécutif

L'audit couvre intégralement les **241 fichiers inventoriés**. Les 241 entrées de registre sont `✅ Audité`, toutes en mode `Full`; aucun fichier ne reste à auditer. Les fichiers de findings contiennent 241 blocs, et les index persistants contiennent 241 enregistrements JSON valides, un par chemin unique.

Le dépôt compile en Release avec **0 avertissement et 0 erreur**, et la suite exécute **57 tests réussis, 0 échoué et 0 ignoré**. Ces deux résultats sont fiables pour les chemins effectivement compilés et exercés. Ils ne compensent pas l'absence de couverture, de mesure de complexité, de CRAP fiable, de SAST, de scan Gitleaks et d'analyse Roslyn.

Après harmonisation, l'audit retient **325 findings actionnables**: **0 Critique, 97 Élevé, 188 Moyen et 40 Faible**. Le risque dominant n'est pas le graphe de projets, qui est sain et acyclique, mais l'écart entre la surface annoncée et sa preuve réelle: règles du kit non câblées, 105 composants Razor de production avec logique inline, localisation absente, composants avancés fonctionnellement incomplets, sécurité navigateur et HTML insuffisamment prouvée, générateurs regex non authentiques, et publication non reliée à l'artefact exact validé.

## Couverture et preuve de complétion

| Mesure | Résultat |
|---|---:|
| Fichiers inventoriés hors `.claude/audit/**` | 241 |
| Fichiers `✅ Audité` | 241 |
| Fichiers non audités | 0 |
| Mode `Full` | 241 |
| Mode `Diff` | 0 |
| Mode `Cache` | 0 |
| Candidats invalidés par le contexte | 0 consigné |
| Blocs de findings par fichier | 241 |
| Enregistrements d'index JSONL valides | 241 |
| Chemins uniques dans les index | 241 |
| Fichiers d'état d'audit exclus après initialisation | 16 |

La somme `Full + Diff + Cache` vaut exactement 241. Aucun candidat `Cache` ou `Diff` n'a été réutilisé ou invalidé dans les registres de cette exécution: l'audit est une lecture intégrale en mode `Full`.

| Module | Fichiers | Full | Non audités |
|---|---:|---:|---:|
| AutoSmoke | 6 | 6 | 0 |
| AutoSmokeClient | 5 | 5 | 0 |
| Catalog | 9 | 9 | 0 |
| HybridSmoke | 4 | 4 | 0 |
| Library | 128 | 128 | 0 |
| Repository | 58 | 58 | 0 |
| Tests | 25 | 25 | 0 |
| WasmSmoke | 6 | 6 | 0 |
| **Total** | **241** | **241** | **0** |

## Comptage consolidé et déduplication

| Source | Critique | Élevé | Moyen | Faible | Total |
|---|---:|---:|---:|---:|---:|
| Findings des 8 modules | 0 | 92 | 178 | 36 | 306 |
| `AUDIT_ARCHITECTURE.md` | 0 | 1 | 3 | 1 | 5 |
| `AUDIT_KIT.md` | 0 | 4 | 4 | 0 | 8 |
| `AUDIT_DEPENDENCIES.md` | 0 | 0 | 3 | 3 | 6 |
| **Total consolidé** | **0** | **97** | **188** | **40** | **325** |

Le corpus contient 326 occurrences actionnables brutes. `D-001` est décrit deux fois dans `HybridSmoke`, une fois depuis le manifeste et une fois depuis son verrou. Ces deux occurrences portent le même ID, le même module et le même écart de pile MAUI; une seule est retenue dans le total du module. Les violations par fichier sans ID identique restent distinctes, même lorsqu'elles relèvent d'un même thème transversal. Cette règle produit exactement les 325 findings attendus.

Les catégories dominantes sont `Style` 185, `Fiabilité` 34, `Architecture` 26, `Sécurité` 20, `Authenticité` 16 et `Tests` 13. Parmi les seuls findings élevés, la distribution est: `Style` 58, `Authenticité` 11, `Fiabilité` 8, `Sécurité` 8, `Architecture` 7, `Conventions` 2, puis un finding chacun en `Correctness`, `Stack` et `Tests`.

## Priorités élevées

### 1. Sécurité des frontières HTML, URI et hôtes

Les écarts élevés les plus directement exploitables se trouvent dans `Library`, `Catalog`, `WasmSmoke` et les workflows:

- `src/OmniEurope.Blazor/Internal/OmniHtmlSanitizer.cs` assainit du HTML destiné à `MarkupString` avec des expressions régulières. Cette frontière ne dispose pas d'une garantie structurelle contre les formes malformées et le mXSS.
- `src/OmniEurope.Blazor/Internal/CspAttributeGuard.cs` ne bloque les attributs `on*` que pour une valeur exactement de type `string`, ce qui laisse une voie de contournement par valeurs splattées d'autres types.
- `OmniBreadcrumbItem.razor`, `OmniLink.razor`, `OmniPanelMenuItem.razor` et `OmniProfileMenuItem.razor` rendent ou transmettent `Href` sans politique commune de schémas sûrs; un schéma actif tel que `javascript:` n'est pas refusé.
- `src/OmniEurope.Blazor/Components/OmniUpload.razor` expose directement `exception.Message` à l'utilisateur et traite les métadonnées MIME/taille du client comme seules validations.
- `samples/OmniEurope.Blazor.Catalog/Program.cs` lit des rapports CSP sans borne adaptée, les conserve sans borne dans un singleton et expose leur contenu brut par `/csp-status`.
- `samples/OmniEurope.Blazor.WasmSmoke/wwwroot/index.html` omet `'wasm-unsafe-eval'`, place `frame-ancestors` dans une balise `meta` où la directive n'est pas appliquée, et ouvre `connect-src` à tout `ws:` et `wss:`.
- `samples/OmniEurope.Blazor.AutoSmoke/Program.cs` persiste les clés Data Protection dans un chemin temporaire prévisible sans protection explicite au repos.
- Les actions GitHub sont référencées par tags majeurs mutables, y compris `NuGet/login@v1` dans un job avec `id-token: write`.

Remédiation Codex-exécutable: Codex peut introduire une politique URI partagée, remplacer le sanitiseur regex par un parseur HTML maintenu avec allowlist, durcir la garde CSP et les tests adversariaux, borner les endpoints CSP, corriger les politiques WebAssembly par environnement, neutraliser les erreurs publiques d'upload, restaurer un stockage de clés adapté à l'hôte, puis épingler les actions CI sur des SHA officiels. Chaque lot sera fermé par tests négatifs, tests bUnit et preuve navigateur sous la CSP réelle.

### 2. Authenticité, publication et provenance

Le dépôt possède **16 findings d'authenticité, dont 11 élevés**. Ils montrent que plusieurs gates mesurent la présence ou la syntaxe au lieu de la capacité réelle:

- `docs/component-coverage.json`, `docs/component-coverage.md` et `eng/Generate-ComponentCoverage.ps1` transforment l'existence d'un fichier `Omni*.razor` en statut `implemented`. Les graphiques empilés, le dialogue et l'éditeur HTML peuvent donc être déclarés implémentés malgré leurs écarts fonctionnels ouverts.
- Le catalogue annonce `110/110` et une validation de 110 capacités alors que `Home.razor` ne rend que 36 balises Omni distinctes et que la documentation reconnaît une démonstration partielle.
- `docs/component-contracts.*` et `eng/Generate-RadzenSurfaceInventory.ps1` utilisent des regex qui confondent expressions Razor, paramètres et simples identifiants contenant `Radzen`.
- `docs/public-api.txt` et `eng/Test-PublicApi.ps1` ne couvrent pas de nombreuses catégories de symboles publics, dont méthodes, propriétés, constructeurs, interfaces, structs, delegates, contraintes et paramètres requis.
- Les inventaires issus de `C:\Dev` ne conservent pas systématiquement corpus, statut, révision, empreinte et provenance par occurrence. Archives, miroirs et sorties techniques contaminent certains agrégats.
- `eng/Test-CatalogHost.ps1` affirme zéro violation CSP sans charger le catalogue dans un navigateur; un collecteur vide passe mécaniquement.
- `eng/Test-Package.ps1` inspecte surtout les noms ZIP et le `.nuspec`, pas la DLL, le PDB, le CSS et le JavaScript qui constituent l'artefact livré.
- `.github/workflows/publish-nuget.yml` reconstruit puis publie un paquet distinct au lieu de publier l'artefact exact, identifié par hash, ayant passé les gates CI.

Remédiation Codex-exécutable: Codex peut remplacer les extracteurs regex par une analyse C#/Razor sémantique, versionner les manifestes de corpus avec révisions et hashes, définir une matrice capacité-vers-tests/catalogue/navigateur, générer une baseline API complète, produire une gate CSP réellement navigateur, inspecter le contenu binaire et statique du paquet, puis faire publier uniquement l'artefact CI inchangé avec provenance vérifiée.

### 3. Kit `_Generic`, code-behind et localisation

Le projet est une RCL clean-room qui ne correspond à aucun archétype actuel du kit. L'intention de remplacer Radzen est légitime, mais aucun overlay `.claude/code-rules.md` ne formalise les règles adaptées. Aucun analyseur `GEN001-GEN008` n'est chargé, `GEN004` ne reçoit pas les fichiers Razor comme `AdditionalFiles`, et `GEN008` n'est pas appliqué. Le build à zéro warning ne prouve donc pas ces conventions.

La passe granulaire relève **108 violations explicites de code-behind**: 105 fichiers Razor de production, plus `AutoProbe.razor`, le catalogue `Home.razor` et le smoke WebAssembly. La passe kit confirme que 105 des 106 fichiers Razor de production contiennent un bloc `@code`. Les violations de production ont été harmonisées en `Moyen` lorsqu'elles sont mécaniques et isolées; l'absence systémique de câblage `GEN004/GEN008` reste `Élevé`.

La localisation est absente au niveau projet: aucun `IStringLocalizer<AppStrings>`, marqueur `AppStrings` ou `.resx`. Les findings par fichier contiennent **53 occurrences explicites `STD-I18N`**, auxquelles s'ajoute le finding global élevé du kit. Sont concernés les labels accessibles, messages de validation, états de chargement/erreur, commandes de navigation, DataGrid, Scheduler, Upload, graphiques et samples.

Remédiation Codex-exécutable: Codex peut formaliser un archétype RCL et son overlay Omni, câbler les analyseurs applicables sans suppression, définir le contrat de ressources et de surcharge d'une bibliothèque réutilisable, puis migrer les composants en lots d'environ 15 fichiers. Chaque lot conservera API et rendu, ajoutera les ressources nécessaires, exécutera le build gardé à zéro warning et les tests ciblés, puis activera progressivement `GEN004` et `GEN008` comme gates réelles.

### 4. Composants avancés: graphiques, DataGrid, Scheduler et éditeur

#### Graphiques

`AUDIT_ARCHITECTURE.md` identifie `ARCH-01` comme écart élevé. `OmniChart` ne construit pas de domaine partagé; `OmniChartGeometry`, les séries et les marqueurs bornent les valeurs sur 0-100, tandis que `OmniValueAxis` affiche des bornes indépendantes. Les séries dites empilées repartent de zéro au lieu de cumuler des baselines positives et négatives. `ChartComponentTests.cs` vérifie surtout rôles, textes et nombres d'éléments, sans coordonnées SVG, domaines décalés, valeurs négatives ou cumul.

Remédiation Codex-exécutable: Codex peut introduire une projection interne immuable enregistrant axes et séries, calculer domaines, catégories et baselines, faire rendre toutes les séries depuis cette projection, puis fermer l'écart avec des tests SVG indépendants sur valeurs négatives, domaines non 0-100, empilements et données vides.

#### DataGrid

`OmniDataGrid.razor` concentre 455 lignes de rendu, chargement distant, filtres, tri, pagination, sélection, édition, expansion, regroupement et redimensionnement. Un résultat distant vide peut relancer la requête à chaque rendu, le pipeline local risque des incohérences filtre/tri/page et le rendu répète des recherches linéaires. Les tests ne prouvent pas réellement pagination, annulation concurrente, regroupement, expansion, édition ou redimensionnement.

Remédiation Codex-exécutable: Codex peut séparer un moteur pur `GridProjection<TItem>` et un état de chargement générationnel, distinguer jamais chargé de chargé vide, indexer colonnes et clés, puis ajouter un test falsifiable par branche publique avant d'alléger le fichier Razor.

#### Scheduler

`OmniScheduler.razor` construit les bornes de plage selon le fuseau de la machine plutôt que `TimeZone`, ce qui produit des requêtes erronées hors fuseau local et autour des transitions DST. La clé de chargement n'intègre pas complètement vue, date, fuseau et délégué, et l'état vide n'est pas durable. `OmniMonthView.razor` n'aligne pas les jours sur le jour de semaine réel. Les tests n'exercent pas une vraie annulation concurrente et n'assertent pas les positions ou durées autour des transitions DST.

Remédiation Codex-exécutable: Codex peut construire les bornes avec l'offset du fuseau ciblé, introduire une clé de génération complète et une horloge contrôlable, corriger la matrice mensuelle, puis tester fuseaux non locaux, DST printemps/automne, chevauchements et annulation de requêtes.

#### Éditeur HTML

`OmniHtmlEditor.razor` présente gras, italique, indice et exposant comme des commandes de sélection, mais transforme la chaîne HTML entière. Le sanitiseur regex interne alimente ensuite `MarkupString`. Les tests actuels ne ferment ni cet écart d'authenticité ni un corpus mXSS.

Remédiation Codex-exécutable: Codex peut soit implémenter une sélection réelle par interop avec contrat testable, soit réduire les commandes au comportement exact annoncé; il peut remplacer simultanément le sanitiseur par un parseur allowlist et ajouter des fixtures de sélection, HTML malformé, URI dangereuses et mXSS.

### 5. Superpositions, accessibilité et preuve navigateur

La frontière de portail annoncée n'est pas complète. `OmniComponentsHost` gère un dialogue courant et des notifications, tandis que tooltip et menu contextuel suivent d'autres cycles. Il n'existe pas de pile centrale pour ordre, imbrication, Escape, verrouillage du scroll et restauration de focus. `OmniDialog`, `OmniContextMenu`, `OmniSplitButton`, `OmniTabs`, `OmniSteps` et certains wrappers de tooltip ont des lacunes de focus ou de clavier. Le CSS conserve plusieurs cibles interactives sous 44 par 44 px.

Les hôtes Interactive Auto, WebAssembly et Hybrid disposent surtout de preuves de compilation ou publication. `STD-UIVERIFY` reste ouvert pour l'hydratation, les clics, la console, WebView2 et les violations CSP. Le catalogue lui-même n'est pas exercé comme un utilisateur par sa gate CSP.

Remédiation Codex-exécutable: Codex peut conserver la façade publique, ajouter un coordinateur interne de portail, implémenter les patrons WAI-ARIA complets, restaurer le focus, corriger les cibles tactiles, puis automatiser des scénarios navigateur et CDP couvrant imbrication, Escape, focus, console, CSP, WebAssembly, Interactive Auto et Hybrid.

## Architecture et API

L'architecture physique est saine et proportionnée: une RCL constitue le produit; tests, catalogue et quatre sondes de plateforme convergent vers elle. Les 7 projets forment un graphe `ProjectReference` acyclique, sans dépendance de la RCL vers les tests ou samples, sans infrastructure de persistance et sans dépendance Radzen.

Les écarts concernent le découpage logique interne:

- `OmniDataGrid` concentre trop de responsabilités;
- graphiques et superpositions n'ont pas encore leurs contextes de composition attendus;
- `OmniDataGridColumnDefinition<TItem>`, `OmniDataGridContext<TItem>`, `OmniTabsContext`, `OmniStepsContext` et `OmniTreeContext<TValue>` exposent publiquement des mécanismes observés comme internes;
- les familles varient entre 12 dans la roadmap, 8 dans `component-families.md` et 10 dans le catalogue;
- les 120 fichiers de `Components/` sont à plat et la baseline API n'est pas sémantiquement exhaustive.

Remédiation Codex-exécutable: Codex peut garder un seul paquet et un seul namespace public, établir la taxonomie canonique dans `docs/architecture.md`, organiser les dossiers par famille, internaliser les contextes avant 1.0 après vérification sémantique des consommateurs, puis régénérer une baseline API complète et déterministe.

## Dépendances, licences et reproductibilité

L'analyse couvre 7 `.csproj`, 7 verrous NuGet, la gestion centralisée, le SDK, la solution et 2 workflows. Elle recense 81 noms de packages et 87 couples nom/version. Les scans NuGet fiables pour la révision ne détectent **aucune vulnérabilité connue** et **aucune dépréciation**. Les 7 verrous sont suivis et les restaurations CI utilisent `--locked-mode`.

Les écarts sont:

- HybridSmoke reste sur `Microsoft.Maui.Controls` et `Microsoft.AspNetCore.Components.WebView.Maui` 10.0.20 alors que 10.0.90 est disponible; plusieurs transitifs restent en 10.0.0 à côté du socle 10.0.10;
- le collecteur demandé par `XPlat Code Coverage` est absent;
- `bunit` 2.8.6 est derrière 2.9.0 et conserve un graphe de tests ancien;
- `global.json` autorise `latestPatch` et le workload MAUI n'est pas épinglé par workload set;
- aucun SBOM ni registre versionné des notices ne couvre les 81 packages; les termes spécifiques du sample Hybrid nécessitent une provenance de redistribution.

Remédiation Codex-exécutable: Codex peut mettre à niveau la pile MAUI de façon coordonnée, aligner et épingler une seule chaîne de couverture, tester la montée bUnit isolément, épingler le SDK/workload réellement validé, générer un SBOM et des notices depuis les verrous, puis fermer chaque changement par restore verrouillé, build Hybrid Windows, tests et contrôle de licence CI.

## Tests, métriques et fiabilité

La suite de 57 tests est réelle et verte, sans test ignoré. Le module `Tests` conserve toutefois 16 findings: 1 élevé, 11 moyens et 4 faibles. Les lacunes les plus importantes portent sur la géométrie des graphiques, les branches du DataGrid, la pile de superpositions, l'annulation concurrente du Scheduler et de l'autocomplete, les transitions DST, un test de validation basé sur une attente murale fixe, et des budgets de performance fondés sur un seul chronométrage sans warm-up.

La couverture n'a pas été produite parce que le collecteur `XPlat Code Coverage` est absent. Sans couverture et sans complexité outillée, aucun score CRAP n'a été fabriqué. Le volume de `OmniDataGrid.razor` est signalé par lecture intégrale, pas par un score numérique.

Remédiation Codex-exécutable: Codex peut réparer la chaîne de couverture, séparer les tests multi-comportements, injecter temps et délais contrôlables, provoquer réellement les concurrences et erreurs annoncées, puis établir des budgets reproductibles avec warm-up et statistiques. La couverture et le CRAP ne seront déclarés fiables qu'après génération et validation d'un rapport exploitable.

## Documentation et gouvernance

Le dépôt contient 26 documents spécialisés utiles, mais `CLAUDE.md` ne fournit ni carte documentaire, ni stack, ni structure, ni commandes, ni règles adaptées à la RCL. Les documents canoniques de standards, tests, analyseurs et patterns sont absents ou dispersés, sans ADR pour les décisions structurantes. `.claude/test-config.md` laisse `integration` et `e2e` vides malgré les gates d'hôtes, CSP et paquet.

Remédiation Codex-exécutable: Codex peut transformer `CLAUDE.md` en point d'entrée concis, créer uniquement les documents de gouvernance nécessaires à la RCL, formaliser les décisions en ADR tracés, relier le registre local de règles et classer les gates existantes dans la configuration de tests sans dupliquer les documents métier déjà valides.

## Limites de l'audit

- **Couverture non fiable:** `dotnet test --collect:"XPlat Code Coverage"` n'a trouvé aucun collecteur compatible. Les 57 tests passent, mais leur couverture n'est pas mesurée.
- **CRAP non fiable:** aucun score n'est calculable sans couverture et complexité compatibles. Aucun score estimé n'est présenté comme factuel.
- **Complexité cyclomatique non fiable:** aucun MCP Roslyn n'est disponible et les outils Python ont été désactivés conformément au Python opt-out. Les fichiers volumineux sont qualifiés par lecture, sans nombre inventé.
- **SAST non fiable:** Semgrep n'a pas été sondé ni exécuté en raison du Python opt-out. La revue de sécurité par fichier est `best-effort, non outillée` pour ce périmètre.
- **Secrets et historique Git non fiables:** Gitleaks est absent. L'absence de secret dans la lecture courante ne vaut pas scan de l'historique.
- **Roslyn non fiable ou indisponible:** aucun graphe sémantique exhaustif de symboles, cycle de types, références, API ou code mort n'a pu être produit. Le graphe de projets est factuel; les conclusions de types et d'usages reposent sur recherches textuelles recoupées.
- **Analyseurs sécurité .NET de portée limitée:** le build ne remonte aucun `CA****`, mais aucun artefact ne prouve qu'un jeu exhaustif de règles sécurité est activé.
- **Validation visuelle et runtime incomplète:** plusieurs compatibilités sont prouvées par compilation ou publication, pas par interaction navigateur, console sans erreur, WebView2 ou collecte réelle des violations CSP.

Résultats qui restent fiables malgré ces limites:

- build Release de `OmniEurope.Blazor.slnx`: **0 avertissement, 0 erreur**;
- tests: **57 réussis, 0 échoué, 0 ignoré**;
- inventaire et lecture: **241/241 fichiers audités en mode Full**;
- vulnérabilités et dépréciations NuGet connues au moment du scan: **0** sur les versions verrouillées.

## Notification de proportionnalité et sur-ingénierie

Cette section est **consultative, non actionnable, exclue des 325 findings et sans effet sur le verdict**.

La structure actuelle est proportionnée: une RCL, un projet de tests, un catalogue et quatre sondes de plateforme constituent une solution plus simple que des projets Domain/Application/Infrastructure, un repository générique, une interface par composant ou un bus d'événements sans second consommateur. Les callbacks annulables, les contextes locaux et les utilitaires internes répondent aux contraintes observées.

Trois observations consultatives ressortent des artefacts:

- `OmniSchedulerTypes.cs`: `RecurrenceRule` expose une flexibilité sans lecteur ni moteur observé; l'alternative conceptuellement plus simple serait de ne créer ce contrat qu'avec un cas d'usage réel.
- `AutoProbe.razor`: un gestionnaire synchrone retournant `Task.CompletedTask` ajoute une indirection asynchrone sans attente.
- `OmniHtmlSanitizer.cs`: les méthodes partielles `GeneratedRegex` sont proportionnées pour éviter la compilation répétée de motifs internes; elles ne constituent pas un finding de sur-ingénierie.

Aucune de ces observations ne crée une remédiation, un suivi ou un changement de sévérité dans cet audit.

## Verdict final

L'audit est exhaustif au niveau fichiers et son état durable est cohérent. Aucun finding critique n'est présent, mais les **97 findings élevés** empêchent de considérer la bibliothèque prête pour une publication de confiance. Le chemin de fermeture prioritaire est: sécuriser les frontières HTML/URI/CSP, rendre authentiques les inventaires et la publication, formaliser l'overlay RCL et la localisation, corriger les composants avancés, puis renforcer les preuves navigateur, la couverture et les tests de branches.

La synthèse ne modifie aucun fichier source et toutes les remédiations proposées sont exécutables de bout en bout par Codex.
