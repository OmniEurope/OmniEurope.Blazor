# Journal des erreurs

Erreurs réellement rencontrées sur ce dépôt, avec leur cause et le correctif retenu. Une entrée n'est ajoutée qu'après reproduction et vérification du correctif. Le but est d'éviter de rediagnostiquer deux fois le même piège.

## Chaîne d'outils

### `MSB4242` : le résolveur de workload plante avant tout restore

- **Symptôme** : `SDK Resolver Failure ... Workload version 10.0.302 ... was not found`, sur tous les projets, y compris ceux qui n'utilisent aucun workload.
- **Cause** : `global.json` déclarait `workloadVersion`. `actions/setup-dotnet` installe un SDK, jamais un *workload set*, donc la résolution échouait sur le runner.
- **Correctif** : `workloadVersion` retiré de `global.json`. Seul le job MAUI installe ce dont il a besoin, avec `dotnet workload install maui-windows`.
- **Garde** : `eng/Test-DependencyPolicy.ps1` compare le bloc `toolchain` de `eng/dependency-policy.json` à `global.json`.

### Dépôt inconstructible sur toute machine sans le patch SDK exact

- **Symptôme** : `A compatible .NET SDK was not found. Requested SDK version: 10.0.302`, alors que `10.0.303` est installé.
- **Cause** : `global.json` épinglait un patch exact avec `rollForward: disable`, et ce numéro était recopié à quatre endroits (`global.json`, deux gardes de `ci.yml`, un test de convention). Le contournement habituel consistait à éditer `global.json` puis à le restaurer, ce qui ne traitait jamais la cause.
- **Correctif** : pin de bande de fonctionnalités (`version: 10.0.300`, `rollForward: latestPatch`), qui accepte tout `10.0.3xx` et refuse une autre bande.
- **Garde** : `eng/Test-SdkBand.ps1` lit `global.json` et applique la règle dans les deux jobs. Le test de convention dérive lui aussi du fichier au lieu de contenir le numéro.

### `CS9057` : un analyseur ne peut pas devancer le compilateur

- **Symptôme** : `Analyzer assembly ... references version '5.9.0.0' of the compiler, which is newer than the currently running version '5.6.0.0'`.
- **Cause** : `Microsoft.CodeAnalysis.CSharp` monté à la dernière version stable. Un analyseur Roslyn doit rester au niveau du compilateur livré par le SDK épinglé.
- **Correctif** : version alignée sur le SDK, et statut `toolchain-bound` dans `eng/dependency-policy.json` avec sa raison, pour la sortir du suivi de dernière version stable.

### `xunit.v3` 4.0.0 supprime VSTest

- **Symptôme** : `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`.
- **Cause** : la 4.0.0 embarque Microsoft.Testing.Platform 2.3.3. Toute la chaîne de preuve de couverture du dépôt repose sur VSTest : `coverlet.collector`, `xunit.runner.visualstudio`, le `.trx` et `eng/Test-Coverage.ps1`.
- **Correctif** : maintien sur la ligne 3.x, statut `toolchain-bound` documenté. La montée est une migration de plateforme de test à mener séparément, pas une montée de version.

## Fins de ligne

### Les hashs de preuve divergent entre Windows et la CI

- **Symptôme** : `Licence text for vendored asset phosphor-icons does not match its declared hash`, et le registre de couverture régénéré modifiait le `targetSha256` de tous les composants sans qu'aucune source ait changé.
- **Cause** : aucun `.gitattributes` et `core.autocrlf=true`. Le dépôt hache des fichiers source et des textes de licence comme preuve ; un checkout Windows les récupérait en CRLF, donc les hashs ne correspondaient plus à ceux calculés en LF par la CI, pour un contenu strictement identique.
- **Correctif** : `.gitattributes` impose `text=auto eol=lf` à tout fichier suivi.
- **Piège associé** : `docs/third-party-licenses/**` est marqué `-text`, donc sans aucune conversion. `eng/Generate-Sbom.ps1` copie ces textes octet pour octet depuis le paquet NuGet puis les hache ; les normaliser ferait diverger le hash de ce que le paquet contient réellement.

## Intégration continue

### `NETSDK1112` : runtime pack absent d'un runner propre

- **Symptôme** : `The runtime pack for Microsoft.NETCore.App.Runtime.win-x64 was not downloaded`, sur le job MAUI seulement, et impossible à reproduire sur un poste de développement.
- **Cause** : `WindowsAppSDKSelfContained` impose un runtime pack, et un `packages.lock.json` ne sait pas exprimer ce téléchargement (aucune section `downloadDependencies`). L'enchaînement `dotnet restore --locked-mode` puis `dotnet build --no-restore` ne pouvait donc jamais le récupérer. Le poste de développement masquait le défaut parce que le pack traînait déjà dans son cache NuGet.
- **Correctif** : `--no-restore` retiré de l'étape de build MAUI. Le restore verrouillé valide toujours le graphe NuGet juste avant, donc la dérive reste détectée.
- **Reproduction** : déplacer `~/.nuget/packages/microsoft.netcore.app.runtime.win-x64` hors du cache, vider `obj` et `bin`, puis rejouer la séquence de la CI.

### Sondes navigateur dépendantes de la locale du runner

- **Symptôme** : `Résultat interactif inattendu : "Counter: 7" (attendu : "Compteur : 1")`. Le 7 vient de la sonde elle-même, qui reclique pendant dix secondes en attendant une chaîne qui n'arrivera jamais.
- **Cause** : les sondes attendent une interface en français, mais aucune locale n'était imposée au navigateur. Blazor WebAssembly suit la langue du navigateur, anglaise sur les runners GitHub, ce qui résolvait les ressources `.en.resx`. Le test passait sur un poste français et échouait sur la CI.
- **Correctif** : `--lang` et `--accept-lang` fixés au lancement du navigateur dans `eng/Test-WasmHost.ps1` et `eng/Test-AutoHost.ps1`, via un paramètre `-BrowserLanguage` qui vaut `fr` par défaut.
- **Vérification** : lancer la sonde avec `-BrowserLanguage 'en'` doit reproduire l'échec, et sans argument doit passer. Un correctif de locale sans ce contrôle négatif ne prouve rien.

### Tests dépendants de la culture de la machine

- **Symptôme** : `Assert.Contains() Failure: Sub-string not found. String: "Bob30.00Modifier". Not found: "30,00"`.
- **Cause** : les tests DataGrid attendent un séparateur décimal français, produit par `FormatString="{0:n2}"` sous `CurrentCulture`. Aucune culture n'était fixée, donc le résultat dépendait de la machine : virgule en France, point sur le runner. Même famille de cause que les sondes navigateur, sur un autre canal.
- **Correctif** : `tests/OmniEurope.Blazor.Tests/TestCulture.cs` fixe `fr-FR` via un `ModuleInitializer`, pour tout le banc de test. `LocalizationTests` continue de basculer explicitement de culture, donc le comportement multiculturel reste couvert.
- **Vérification** : basculer temporairement l'initialiseur sur `en-US` doit faire échouer les tests concernés, et le remettre sur `fr-FR` doit tout faire passer.
- **À savoir** : sur le runner, `CurrentUICulture` résolvait le `.resx` neutre, donc français, pendant que `CurrentCulture` restait invariante. Les chaînes d'interface passaient et seuls les nombres cassaient, ce qui rendait le diagnostic trompeur.

### La sonde MAUI n'attendait pas son hôte

- **Symptôme** : `Aucune cible CDP disponible sur http://127.0.0.1:9224`, uniquement sur la CI. En local le même script passe en 2,5 secondes.
- **Cause** : `eng/Test-HybridHost.ps1` lançait l'exécutable puis appelait immédiatement la sonde. Seule la boucle interne de 20 secondes de `Test-CdpProbe.mjs` absorbait le démarrage, ce qui suffit sur un poste tiède mais pas sur un runner froid où WebView2 s'initialise pour la première fois. Les autres scripts d'hôte ont une boucle d'attente ; celui-ci n'en avait aucune.
- **Correctif** : boucle d'attente sur `/json/list` avant d'appeler la sonde, avec `-ReadyTimeoutSeconds` à 90 par défaut, et sortie immédiate si le processus meurt.
- **Effet de bord voulu** : le message d'échec distingue désormais l'hôte mort (`L'hôte Hybrid s'est arrêté avant d'être prêt (code N)`, suivi de sa sortie) du délai dépassé. L'ancien message ne permettait pas de trancher entre les deux.

### La sonde MAUI échoue encore, et attendre plus longtemps ne prouve rien

- **Symptôme** : `Le host Hybrid n'a exposé aucune cible CDP sur le port 9224 en 90 secondes, alors que son processus est toujours vivant`. En local, sur session Windows interactive, le même script passe.
- **Ce que l'échec établit déjà** : l'attente de 90 secondes ajoutée par l'entrée précédente est atteinte sans succès. Le démarrage lent est donc exclu, et rallonger encore le délai serait un correctif de façade. L'application MAUI démarre et reste vivante, mais WebView2 n'ouvre jamais de cible CDP.
- **Trou de diagnostic** : le `catch` de la boucle d'attente avalait toutes les erreurs. Impossible de distinguer deux causes qui appellent des correctifs opposés, le port de débogage jamais ouvert d'un côté, le port ouvert mais sans cible de type `page` de l'autre.
- **État** : non corrigé, faute de cause établie. Seule l'instrumentation a été livrée : avant de lever l'exception, la sonde imprime la version du runtime WebView2 lue dans le registre EdgeUpdate, les arguments navigateur réellement transmis à l'enfant, puis soit la dernière erreur HTTP, soit la liste brute des cibles obtenues.
- **Contrôle négatif** : `./eng/Test-HybridHost.ps1 -ReadyTimeoutSeconds 0` doit imprimer les trois lignes de diagnostic avant l'exception, et le lancement par défaut doit rester vert.
- **Leçon** : quand un correctif de délai ne suffit pas, le réflexe utile n'est pas d'augmenter le délai mais de rendre l'échec bavard. Un `catch` vide dans une boucle de disponibilité transforme toutes les causes en un seul message inexploitable.
- **Ce que le diagnostic a répondu** : runtime WebView2 bien installé sur le runner (`151.0.4129.101`), arguments navigateur bien transmis, et le port de débogage n'a **jamais** répondu. Le problème n'est donc ni le runtime, ni la variable d'environnement : WebView2 n'ouvre pas son point de terminaison, ou n'est pas créé du tout.
- **Étape suivante** : la sonde ne masque plus la fenêtre (`WindowStyle Hidden` retiré, sur l'hypothèse qu'une fenêtre jamais réalisée ne déclenche pas la création de WebView2), elle compte les processus `msedgewebview2` vivants, et l'hôte trace sur sa sortie standard les marqueurs `HYBRID-SMOKE page-constructed`, `webview-initializing`, `page-loaded`, `webview-initialized`, `webview-url-loading`. Le marqueur manquant désignera l'étape qui échoue.
- **Contrôle de l'instrumentation** : lancer l'exécutable avec redirection de la sortie standard doit produire les cinq marqueurs et un compte de processus `msedgewebview2` non nul. Vérifié en local avant livraison, sinon la trace ne prouverait rien.

### Les preuves générées périment en silence

- **Symptôme** : `Public API baseline` en échec, et `Package registry count mismatch`, sur un commit qui ne touchait pourtant pas ces fichiers.
- **Cause** : `docs/public-api.txt`, `docs/component-coverage.json` et le SBOM sont des preuves générées. Ajouter un projet ou une API sans les régénérer les laisse périmées jusqu'au passage suivant de la garde.
- **Correctif** : après toute modification de la surface publique, de la liste des projets ou des dépendances, régénérer `eng/Test-PublicApi.ps1 -Update`, `eng/Generate-ComponentCoverage.ps1` et `eng/Generate-Sbom.ps1`, puis relancer les gardes correspondantes.

### Faux positif du scanner CSP

- **Symptôme** : `inline style attribute` signalé sur `src/OmniEurope.Blazor/wwwroot/omni-grid.js`, qui ne contient aucun attribut de style en dur.
- **Cause** : la règle cherche `\bstyle\s*=`, et un paramètre JavaScript nommé `style` comparé par `style === null` correspond au motif.
- **Correctif** : le paramètre a été renommé. La règle n'a pas été affaiblie, elle protège un contrat de sécurité ; c'est au code de ne pas la heurter.

### Le contrôle catalogue rougit tout seul avec le temps

- **Symptôme** : `NuGet catalog drift for ... reviewed X, latest stable Y`, sur un commit sans rapport.
- **Cause** : `eng/Test-DependencyPolicy.ps1` interroge nuget.org et exige que chaque paquet `latest-stable` soit exactement la dernière version publiée, et `reviewedAt` périme au bout de 30 jours. La CI dépend donc du calendrier de publication des éditeurs.
- **État** : assumé, non corrigé. Le contrôle force une revue régulière des dépendances. Il impose en contrepartie de traiter la dérive avant tout autre travail, et de marquer `toolchain-bound` ce qui ne peut structurellement pas suivre.

### Un script d'ingénierie qui ne peut tourner que sous Windows

- **Symptôme** : `Cannot bind argument to parameter 'Path' because it is null`, sur l'étape SBOM du job `validate`, sans aucune indication de la variable en cause.
- **Cause** : `eng/Generate-Sbom.ps1` cherchait le dossier NuGet global via `$env:NUGET_PACKAGES`, puis se rabattait sur `Join-Path $env:USERPROFILE '.nuget/packages'`. `USERPROFILE` n'existe que sous Windows. Le job `validate` tourne sur `ubuntu-latest`, la variable y valait `$null`, et `Join-Path` refusait de lier son paramètre. Le poste de développement masquait le défaut parce que la variable y est toujours définie.
- **Correctif** : repli par `[Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)`, qui résout `HOME` hors Windows, plus un `throw` nommé quand rien ne se résout, au lieu d'une erreur de liaison opaque.
- **Reproduction** : vider `USERPROFILE` dans une session PowerShell reproduit le message du runner mot pour mot, alors que `GetFolderPath` continue de résoudre le profil.
- **Leçon** : PowerShell est multiplateforme, pas les variables d'environnement Windows. Tout script `eng/*.ps1` susceptible de tourner sur un runner Linux doit passer par `GetFolderPath` ou `$HOME`, jamais par `USERPROFILE` seul.

### L'inventaire SBOM tournait là où ses paquets n'existent pas

- **Symptôme** : `NuGet metadata is missing for Microsoft.AspNetCore.Components.WebView 10.0.11: /home/runner/.nuget/packages/.../microsoft.aspnetcore.components.webview.nuspec`, uniquement sur le job `validate`.
- **Cause** : `eng/Generate-Sbom.ps1` balaie récursivement **tous** les `packages.lock.json` du dépôt, y compris celui de `samples/OmniEurope.Blazor.HybridSmoke`. Or ce projet MAUI est volontairement hors de `OmniEurope.Blazor.slnx`, donc le job `validate` ne le restaure jamais et ses nuspec ne descendent pas dans le cache du runner. Le poste de développement masquait le défaut parce qu'on y construit aussi le host Hybrid.
- **Fausse piste écartée** : restreindre l'inventaire aux paquets présents rendrait le SBOM dépendant du job qui le produit, et la garde `git diff --exit-code` deviendrait ininterprétable. L'inventaire doit rester déterministe et couvrir tous les verrous.
- **Correctif** : l'étape SBOM est passée sur le job Windows, qui restaure désormais à la fois le host Hybrid et la solution. C'est le seul job où tous les verrous du dépôt peuvent être satisfaits en même temps. Elle est placée **avant** la sonde Hybrid pour qu'un échec de celle-ci ne masque plus le résultat de l'inventaire.
- **Leçon** : une étape qui agrège tout le dépôt doit tourner là où tout le dépôt est restauré. Un projet sorti de la solution reste dans le périmètre des gardes qui balaient les fichiers, pas dans celui des jobs qui compilent la solution.

## Actions GitHub

### Avertissement de dépréciation Node 20

- **Symptôme** : `The following actions target Node.js 20 but are being forced to run on Node.js 24`.
- **Cause** : actions épinglées sur une version antérieure au passage à node24.
- **Correctif** : `actions/checkout` en v5 et `actions/upload-artifact` en v6. Attention, `upload-artifact` v5 est encore en node20 ; c'est bien la v6 qui est requise.
- **Méthode** : ne pas déduire le runtime du numéro de version. Lire le `runs.using` du fichier `action.yml` au SHA visé avant d'épingler.
