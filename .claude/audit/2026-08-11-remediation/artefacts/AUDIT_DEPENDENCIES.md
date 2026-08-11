# Audit 360 Pass 2a - Dépendances externes

Date de contrôle : 2026-08-11  
Périmètre : état courant non commité du worktree `C:\Dev\OmniEurope.Blazor`  
Révision de base observée : `717af586cc40f3d87572e8e76b0b452ef4766b04`

## Verdict

La chaîne de dépendances est largement renforcée : gestion centrale des versions, neuf verrous NuGet, hachages de contenu, restauration CI en mode verrouillé, actions GitHub actuellement immuables, SBOM et licences archivées. Les interrogations officielles de NuGet.org ne signalent aucune vulnérabilité connue ni aucun paquet déprécié.

Il reste **12 findings actionnables** : **0 Critique, 1 Élevé, 6 Moyen, 5 Faible**.

## Inventaire et preuves positives

- 9 manifestes `.csproj`, 1 solution `.slnx`, `Directory.Packages.props`, `Directory.Build.props`, `global.json`, 2 workflows GitHub Actions et 1 configuration Dependabot ont été inspectés.
- 14 versions NuGet centrales couvrent 15 références directes. Aucune version en ligne, flottante ou `VersionOverride` n'a été trouvée; aucune entrée centrale n'est inutilisée.
- 9 fichiers `packages.lock.json` au format 2 couvrent 10 contextes framework/RID, 274 entrées dont 266 paquets et 8 références projet. Les 266 entrées de paquets ont toutes une version résolue et un `contentHash`.
- Les verrous contiennent 114 couples package/version uniques. Les six identifiants présents en `10.0.0` et `10.0.10` le sont dans des projets séparés; aucun verrou individuel ne résout deux versions concurrentes du même identifiant.
- `dotnet list ... package --vulnerable --include-transitive --no-restore` : aucun paquet vulnérable dans les 8 projets de la solution, ni dans `OmniEurope.Blazor.HybridSmoke` traité séparément.
- `dotnet list ... package --deprecated --include-transitive --no-restore` : aucun paquet déprécié dans les 9 projets.
- `dotnet list ... package --outdated --include-transitive --no-restore` : 3 paquets directs obsolètes et des mises à jour transitives, détaillés dans les findings ci-dessous.
- `Microsoft.Maui.Controls` `10.0.90`, `Microsoft.AspNetCore.Components.WebView.Maui` `10.0.90` et `coverlet.collector` `10.0.1` sont les dernières versions stables selon l'API officielle NuGet au moment du contrôle.
- 8 références `uses:` sur 8 sont actuellement épinglées à un SHA Git complet de 40 caractères. Dependabot couvre NuGet et GitHub Actions chaque semaine.
- `eng/Test-DependencyPolicy.ps1` réussit, avec les limites décrites par DEP-001, DEP-004 et DEP-006.
- `eng/Test-Sbom.ps1` réussit : 114 composants, 94 licences par expression (84 MIT, 10 Apache-2.0), 12 licences par fichier conservé et 8 déclarations par URL.
- Le paquet local `artifacts/final-pack/OmniEurope.Blazor.0.1.0-alpha.1.nupkg` passe `eng/Test-Package.ps1` : 30 entrées, 5 entrées de symboles. La fixture contaminée est rejetée et le manifeste de provenance est cohérent avec ses propres valeurs.

## Findings actionnables

### DEP-001 - [Élevé] [Dépendances / Reproductibilité] Le workload set n'est pas réellement épinglé

**Preuves**

- `global.json:7-8` déclare une propriété racine `"workload": { "version": "10.0.302" }`.
- La syntaxe officielle attend `sdk.workloadVersion`, dans l'objet `sdk` : [documentation Microsoft des workload sets](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-workload-sets#use-globaljson-for-the-workload-set-version).
- Dans ce dépôt, `dotnet --version` renvoie bien `10.0.302`, mais `dotnet workload --info` renvoie exactement : `Workload version: 10.0.300-manifests.ec49ebb1` puis `No workload sets are installed.`
- `.github/workflows/ci.yml:104` lance `dotnet workload install maui-windows` sans `--version`; la valeur racine invalide ne peut donc pas imposer `10.0.302`.
- `eng/Test-DependencyPolicy.ps1:26` valide la même structure invalide et produit un faux positif.

**Impact**

La version du SDK est reproductible, mais l'ensemble des workloads MAUI peut dépendre de l'état du runner ou de la dernière version publiée. Le contrôle CI final à `.github/workflows/ci.yml:110-112` peut détecter un écart après installation, mais il ne rend pas l'installation déterministe.

**Remédiation Codex**

Déplacer la valeur vers `sdk.workloadVersion`, adapter `eng/dependency-policy.json` et `eng/Test-DependencyPolicy.ps1`, rendre l'installation CI explicitement versionnée si nécessaire, puis prouver après installation que `dotnet workload --version` vaut exactement `10.0.302`.

### DEP-002 - [Moyen] [Dépendances / Sécurité] Le sanitizer de production est obsolète et absent de la politique sensible

**Preuves**

- `Directory.Packages.props:8` épingle `HtmlSanitizer` en `9.1.973`.
- NuGet.org retourne `9.2.995` comme dernière version stable. Le résultat officiel de `dotnet list` le signale pour le projet publié et ses consommateurs.
- `eng/dependency-policy.json` ne recense pas ce paquet alors qu'il traite directement du contenu HTML non fiable.
- Aucun avis de vulnérabilité connu n'est signalé pour `9.1.973` au moment du contrôle; le finding porte sur la maintenance et l'absence de revue, pas sur un CVE affirmé.

**Impact**

Les corrections fonctionnelles et de durcissement publiées entre les deux versions ne sont pas intégrées, et la gate de politique ne détecte pas cette dérive d'une dépendance de sécurité.

**Remédiation Codex**

Mettre à jour vers `9.2.995`, régénérer tous les verrous et les artefacts de licences/SBOM, ajouter le paquet à la politique revue, puis exécuter les tests de sanitization, CSP, package et compatibilité publique.

### DEP-003 - [Moyen] [Dépendances / Fiabilité] Le smoke hybride conserve un socle runtime transitive non servi

**Preuves**

- Les deux dépendances directes MAUI sont à jour en `10.0.90`.
- Le verrou hybride résout néanmoins `Microsoft.AspNetCore.Components.WebView` en `10.0.0` alors que NuGet.org publie `10.0.10`.
- Le même graphe conserve plusieurs `Microsoft.Extensions.*` en `10.0.0` au lieu de `10.0.10`, `Microsoft.Web.WebView2` en `1.0.3179.45` au lieu de `1.0.4129.50`, ainsi que d'autres candidats transitifs. Le rapport officiel `--outdated` compte 28 entrées transitives dans ce projet.
- `eng/Test-DependencyPolicy.ps1:39-45` ne contrôle que les deux entrées directes et ne classe aucune exception transitive.

**Impact**

Le smoke Windows valide un mélange de lignes de servicing. Les mises à jour majeures des composants Windows App SDK ne doivent pas être forcées aveuglément, mais les correctifs compatibles de WebView et WebView2 restent non évalués et non documentés.

**Remédiation Codex**

Tester d'abord un pin compatible de `Microsoft.AspNetCore.Components.WebView` `10.0.10` et de la ligne WebView2 prise en charge, exécuter le smoke hybride, puis enregistrer explicitement chaque dépendance réellement contrainte par le graphe MAUI comme exception avec sa justification. Étendre la gate aux transitives runtime sensibles.

### DEP-004 - [Faible] [Dépendances / Maintenabilité] Roslyn est resté en 4.12.0

**Preuves**

- `Directory.Packages.props:12` épingle `Microsoft.CodeAnalysis.CSharp` en `4.12.0`.
- NuGet.org publie `5.6.0`, compatible `netstandard2.0` selon la [fiche officielle](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/5.6.0).
- L'écart entraîne aussi onze mises à jour transitives dans `OmniEurope.Analyzers`.

**Impact**

L'analyseur interne reste fonctionnel mais dépend d'une API Roslyn ancienne et d'un graphe transitive daté.

**Remédiation Codex**

Mettre à niveau l'analyseur et ses verrous, ou documenter une borne de compatibilité compilateur vérifiée si la version basse est intentionnelle; dans les deux cas, exécuter les tests analyzers et un build Release sans avertissement.

### DEP-005 - [Faible] [Dépendances / Documentation] La politique et la documentation bUnit sont déjà périmées

**Preuves**

- `Directory.Packages.props:6` et `eng/dependency-policy.json:5-9` déclarent `bunit` `2.8.6` avec le statut `latest-stable`.
- `docs/dependencies.md:7` affirme que `2.9.0` n'existe pas.
- L'API officielle NuGet et `dotnet list ... --outdated` retournent maintenant `2.9.0` comme dernière version stable.

**Impact**

La dépendance n'affecte que les tests, mais la preuve censée justifier son maintien est factuellement fausse et la gate ne vérifie que l'égalité entre deux fichiers locaux.

**Remédiation Codex**

Mettre à jour bUnit et les verrous avec validation complète des tests, ou consigner une dérogation temporaire datée et testée. Corriger la documentation et remplacer le statut `latest-stable` par une preuve générée ou une décision explicite.

### DEP-006 - [Moyen] [Sécurité / CI] La gate d'immutabilité ignore sept actions sur huit

**Preuves**

- Les 8 actions actuelles sont bien épinglées à 40 hexadécimaux.
- `eng/Test-DependencyPolicy.ps1:53` utilise `^\s*uses:`.
- 7 lignes YAML commencent par `- uses:`; une seule commence par `uses:` sous une étape nommée. Une mesure avec la regex de la gate donne `GateMatches=1`, alors que la regex YAML correcte trouve 8 références.
- `eng/Test-DependencyPolicy.ps1:48` ne recherche en outre que `*.yml`, pas `*.yaml`.

**Impact**

Une future action mutable telle que `- uses: owner/action@main` passerait la gate, malgré le message de succès affirmant que les actions sont immuables.

**Remédiation Codex**

Analyser `^\s*-?\s*uses:` sur les extensions `.yml` et `.yaml`, puis ajouter des fixtures positives et négatives qui prouvent le rejet d'un tag, d'une branche et d'un SHA incomplet.

### DEP-007 - [Moyen] [SBOM / Exactitude] Le composant racine du SBOM porte une mauvaise version

**Preuves**

- `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj:5` et le paquet contrôlé portent la version `0.1.0-alpha.1`.
- `eng/Generate-Sbom.ps1:151` écrit en dur `1.0.0`.
- Le fichier `docs/sbom.cdx.json` et le SBOM inclus dans `OmniEurope.Blazor.0.1.0-alpha.1.nupkg` déclarent donc `metadata.component.version = 1.0.0`.
- `eng/Test-Sbom.ps1` et `eng/Test-Package.ps1` passent malgré cette contradiction.

**Impact**

Les consommateurs et outils de conformité rattachent le BOM au mauvais artefact; la traçabilité package, SBOM et provenance est rompue.

**Remédiation Codex**

Dériver `name` et `version` du projet packagé ou de propriétés MSBuild explicites, puis vérifier l'égalité entre la version du nuspec, le nom du fichier, la provenance et le composant racine du SBOM.

### DEP-008 - [Moyen] [SBOM / Complétude] Le SBOM embarqué mélange le produit, les tests et les samples sans graphe

**Preuves**

- `eng/Generate-Sbom.ps1:19-47` agrège récursivement les neuf verrous du dépôt.
- Le package publié possède seulement trois dépendances directes dans son nuspec, mais embarque 114 composants incluant bUnit, xUnit, MAUI, Windows App SDK et les outils analyzers.
- Le document CycloneDX ne contient aucune section `dependencies` reliant le composant racine à ses dépendances réelles.
- `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj:26-28` embarque ce BOM global et toutes les licences du dépôt dans le NuGet distribué.

**Impact**

Le BOM est utile comme inventaire du dépôt, mais il est trompeur comme SBOM de l'artefact NuGet : il surdéclare des dépendances non distribuées et ne permet pas de distinguer runtime, build, test et sample.

**Remédiation Codex**

Produire deux artefacts distincts : un SBOM de dépôt exhaustif et un SBOM du package dérivé du graphe restauré de `src/OmniEurope.Blazor`. Ajouter les relations CycloneDX et n'embarquer dans le NuGet que le SBOM et les notices applicables à l'artefact.

### DEP-009 - [Moyen] [SBOM / Authenticité] La gate SBOM contrôle le nombre, pas l'identité des composants

**Preuves**

- `eng/Test-Sbom.ps1:62-68` vérifie le nombre de composants, l'unicité des `bom-ref` et la présence d'une licence.
- Elle ne compare pas l'ensemble exact `(name, version, purl)` du SBOM avec les verrous ou `docs/third-party-packages.json`.
- Elle ne contrôle ni les propriétés `lock-files`, ni la version du composant racine, ni un schéma CycloneDX complet.
- DEP-007 démontre qu'un SBOM factuellement faux passe actuellement la gate.

**Impact**

Une substitution de composants, une version erronée ou un PURL incohérent peut rester verte tant que le nombre total reste 114.

**Remédiation Codex**

Comparer les ensembles exacts dans les deux sens, vérifier chaque licence et chaque provenance de verrou, valider le composant racine et exécuter une validation contre le schéma JSON CycloneDX 1.6.

### DEP-010 - [Faible] [Licences / Conformité] Une URL de licence ne fournit plus le texte déclaré

**Preuves**

- `Microsoft.Graphics.Win2D` `1.3.2` déclare `http://www.microsoft.com/web/webpi/eula/eula_win2d_10012014.htm`.
- La requête suit maintenant une redirection HTTP 200 vers `https://learn.microsoft.com/en-us/windows/web/`, une page générale et non le texte EULA attendu.
- `eng/Generate-Sbom.ps1` conserve uniquement l'URL pour ce cas et `eng/Test-Sbom.ps1` ne vérifie ni la cible ni son contenu.
- Les sept autres URL déclarées ont répondu HTTP 200 pendant le contrôle.

**Impact**

La preuve de licence de ce composant transitive hybride n'est pas autonome et le lien conservé ne permet plus de relire les conditions applicables à la version verrouillée.

**Remédiation Codex**

Résoudre une source officielle versionnée pour `1.3.2`, conserver localement le texte et son SHA-256 comme pour les licences de type `file`, puis introduire une table d'exception vérifiée pour les anciennes déclarations URL.

### DEP-011 - [Faible] [Package / Complétude] La gate nuspec ne vérifie qu'une dépendance sur trois

**Preuves**

- Le nuspec observé contient `HtmlSanitizer 9.1.973`, `Microsoft.AspNetCore.Components.Web 10.0.10` et `Microsoft.Extensions.Localization 10.0.10`.
- `eng/Test-Package.ps1:99-101` n'affirme que la présence de `Microsoft.AspNetCore.Components.Web 10.0.10`.
- La présence des fichiers de conformité est vérifiée, mais leur cohérence avec le contenu embarqué ne l'est pas dans ce test.

**Impact**

Une dépendance manquante, supplémentaire ou mal versionnée peut échapper au contrôle de package tant que le package reste constructible et que la seule dépendance codée en dur est présente.

**Remédiation Codex**

Dériver l'ensemble attendu des dépendances depuis le projet et les versions centrales, comparer exactement le groupe nuspec, puis valider le SBOM embarqué en mémoire avec les mêmes règles que le fichier source.

### DEP-012 - [Faible] [Provenance / Authenticité] Le run CI inscrit dans la provenance n'est pas relié au run téléchargé

**Preuves**

- `eng/Test-PackageProvenance.ps1:4-5` accepte un commit et un dépôt attendus, mais aucun `ExpectedRunId` ni `ExpectedRunAttempt`.
- `eng/Test-PackageProvenance.ps1:20` exige seulement que ces deux valeurs soient positives.
- `.github/workflows/publish-nuget.yml:35-44` connaît le run CI exact téléchargé, mais ne transmet pas cet identifiant au vérificateur.
- L'artefact local de preuve passe avec `runId = 1`, ce qui démontre la faiblesse de validation sans remettre en cause l'artefact CI réel.

**Impact**

Les hachages, le dépôt et le commit sont correctement protégés, mais un run ou une tentative erronés restent acceptés, ce qui affaiblit l'auditabilité de la provenance.

**Remédiation Codex**

Ajouter les identités attendues au vérificateur et les transmettre depuis le run résolu par le workflow de publication. Conserver les contrôles actuels de commit, dépôt et SHA-256.

## Résultats NuGet détaillés

### Dépendances directes obsolètes

| Package | Résolue | Dernière stable | Portée |
|---|---:|---:|---|
| `HtmlSanitizer` | `9.1.973` | `9.2.995` | runtime du package |
| `Microsoft.CodeAnalysis.CSharp` | `4.12.0` | `5.6.0` | analyseur de build |
| `bunit` | `2.8.6` | `2.9.0` | tests |

Les autres dépendances directes sont à la dernière stable selon `dotnet list --outdated` au moment du contrôle. Les transitives obsolètes se concentrent dans les graphes Roslyn, HtmlSanitizer/bUnit et MAUI/Windows; une mise à jour transitive majeure ne doit pas être forcée sans compatibilité amont prouvée.

### Vulnérabilités et dépréciations

- Solution, 8 projets : 0 paquet vulnérable, 0 paquet déprécié.
- Hybrid smoke, 1 projet : 0 paquet vulnérable, 0 paquet déprécié.
- Source interrogée : `https://api.nuget.org/v3/index.json`.

## Licences

- Dépendances du package publié : aucune incompatibilité manifeste détectée; les expressions observées sont MIT ou Apache-2.0.
- Inventaire global : 94 expressions SPDX, 12 textes locaux hachés, 8 URL.
- Les textes Microsoft propriétaires appartiennent au graphe Hybrid/Windows, pas aux trois dépendances directes du package publié; leur présence dans le NuGet résulte du mélange de périmètres décrit par DEP-008.
- Cette vérification est technique et documentaire, pas un avis juridique.

## Limites et incidents d'exécution

- Les quatre premières tentatives `dotnet list` dans le sandbox restreint ont échoué exactement avec : `error: Failed to read NuGet.Config due to unauthorized access. Path: 'C:\Users\Woluwe\AppData\Roaming\NuGet\NuGet.Config'.` puis `error: Access to the path 'C:\Users\Woluwe\AppData\Roaming\NuGet\NuGet.Config' is denied.` Les commandes ont ensuite été relancées avec l'autorisation adaptée et ont toutes réussi contre NuGet.org; ce problème n'invalide donc pas les résultats.
- Aucun outil Python, aucune sonde Python et aucune modification de source n'ont été effectués.
- Le workload MAUI n'a pas été installé pendant ce passage en lecture seule. La preuve locale porte sur la configuration et l'état rapporté par le SDK, tandis que l'effet CI est déduit de la syntaxe officielle et du workflow.
- Les résultats `--outdated` comparent aux dernières versions publiées; ils ne prouvent pas qu'une mise à niveau transitive majeure est compatible avec le graphe MAUI actuel.
- Les paramètres externes du dépôt GitHub, notamment l'activation des alertes et du signalement privé, ne sont pas observables depuis les fichiers locaux. Seules les configurations `dependabot.yml` et workflows ont été auditées.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - la centralisation, les verrous, le SBOM, les notices et les gates sont proportionnés à un package NuGet public. L'alternative la plus simple reste de dériver les preuves depuis les manifestes et le graphe restauré existants; aucun framework de conformité supplémentaire n'est nécessaire pour corriger les findings ci-dessus.

## Résumé succinct

- Le SDK est exactement épinglé, mais le workload MAUI ne l'est pas à cause d'une clé `global.json` invalide et d'une gate qui reproduit cette erreur.
- Trois dépendances directes sont désormais obsolètes; le sanitizer de production et le graphe hybride demandent la priorité.
- Le SBOM embarqué porte une mauvaise version, mélange les dépendances produit, test et sample, et sa gate ne compare pas l'identité réelle des composants.
- Les actions sont sûres aujourd'hui, mais sept références sur huit échappent au contrôle automatique d'immutabilité.
- Les licences sont largement inventoriées, avec une URL Win2D devenue non probante et un périmètre de notices trop large pour le package publié.
