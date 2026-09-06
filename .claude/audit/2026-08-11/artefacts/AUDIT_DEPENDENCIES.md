# Audit des dépendances externes

> Date: 2026-08-11  
> Révision inspectée: `717af586cc40f3d87572e8e76b0b452ef4766b04`  
> Portée: tous les manifestes .NET/NuGet, les fichiers de verrouillage, le SDK, la solution et les dépendances GitHub Actions  
> Mode: lecture seule du code et de Git; aucun outil Python sondé ou exécuté

## Verdict

- Vulnérabilités NuGet connues: **aucune détectée** dans les 7 projets, dépendances transitives incluses, depuis `https://api.nuget.org/v3/index.json`.
- Paquets NuGet dépréciés: **aucun signalé** dans les 7 projets, dépendances transitives incluses.
- Versions: **3 findings moyens et 3 faibles**. Les priorités sont la pile Hybrid MAUI obsolète et désalignée, l'absence du collecteur de couverture demandé par le préflight et les GitHub Actions référencées par tags majeurs mutables.
- Verrouillage: les 7 `packages.lock.json` sont suivis, en schéma v2 avec hachages de contenu; les restaurations CI utilisent `--locked-mode` pour la solution et séparément pour HybridSmoke.
- Conflits: aucun downgrade ou conflit NuGet diagnostiqué par le build Release (0 avertissement). Six packages `Microsoft.Extensions.*` existent néanmoins en `10.0.0` dans HybridSmoke et en `10.0.10` ailleurs.
- Licences: les 87 couples package/version résolus (81 noms uniques) ont une métadonnée de licence locale. Le package publié `OmniEurope.Blazor` ne dépend directement que d'un package MIT. Les dépendances propriétaires ou sans expression SPDX sont cantonnées au sample Hybrid.

## Sources et fiabilité

### Rapports croisés

- `metrics/PREFLIGHT.md`: build Release réussi, 0 avertissement et 0 erreur; 57 tests réussis; collecte `XPlat Code Coverage` en échec car le collecteur est absent.
- `metrics/SECURITY_SCAN.md`: SAST Semgrep non fiable car désactivé par le Python opt-out; Gitleaks absent; aucun avertissement `CA****` observé, sans preuve d'un jeu de règles sécurité exhaustif.
- La conclusion CVE ci-dessous est fiable pour les versions verrouillées et les avis exposés par NuGet.org au moment du scan. Elle ne remplace pas le SAST, le scan de secrets ou la revue manuelle.

### Commandes exécutées

- `dotnet list OmniEurope.Blazor.slnx package --vulnerable --include-transitive --no-restore`: succès; 6/6 projets sans package vulnérable.
- `dotnet list samples/OmniEurope.Blazor.HybridSmoke/OmniEurope.Blazor.HybridSmoke.csproj package --vulnerable --include-transitive --no-restore`: succès; HybridSmoke sans package vulnérable.
- Les deux variantes `--outdated --include-transitive --no-restore`: succès; résultats détaillés plus bas.
- Les deux variantes `--deprecated --include-transitive --no-restore`: succès; aucun package déprécié.
- Le premier essai sandboxé a échoué sur l'accès à `C:\Users\Woluwe\AppData\Roaming\NuGet\NuGet.Config`; la relance autorisée a réussi avec accès à NuGet.org. La portée versions/CVE/dépréciations n'est donc pas dégradée.

## Manifestes inventoriés

| Manifeste | Rôle | Dépendances ou réglages directs |
|---|---|---|
| `global.json` | SDK | SDK `10.0.302`, `rollForward: latestPatch`, prerelease interdit |
| `Directory.Build.props` | Cible commune | `net10.0`, sauf surcharge Hybrid Windows |
| `Directory.Packages.props` | Gestion centralisée | `ManagePackageVersionsCentrally=true`; 10 versions exactes |
| `NuGet.Config` | Source | source unique `nuget.org` en HTTPS après `<clear />` |
| `OmniEurope.Blazor.slnx` | Graphe principal | 6 projets; HybridSmoke est construit dans un job CI Windows séparé |
| `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj` | Bibliothèque publiée | `Microsoft.AspNetCore.Components.Web@10.0.10`; package EUPL-1.2 |
| `tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj` | Tests | `bunit@2.8.6`, `Microsoft.NET.Test.Sdk@18.8.1`, `xunit.v3@3.2.2`, `xunit.runner.visualstudio@3.1.5` |
| `samples/OmniEurope.Blazor.AutoSmoke.Client/OmniEurope.Blazor.AutoSmoke.Client.csproj` | Client WASM | `Microsoft.AspNetCore.Components.WebAssembly@10.0.10` |
| `samples/OmniEurope.Blazor.AutoSmoke/OmniEurope.Blazor.AutoSmoke.csproj` | Hôte Auto | `Microsoft.AspNetCore.Components.WebAssembly.Server@10.0.10` |
| `samples/OmniEurope.Blazor.Catalog/OmniEurope.Blazor.Catalog.csproj` | Catalogue serveur | aucune dépendance NuGet explicite |
| `samples/OmniEurope.Blazor.WasmSmoke/OmniEurope.Blazor.WasmSmoke.csproj` | Smoke WASM | `Microsoft.AspNetCore.Components.WebAssembly@10.0.10`, DevServer `10.0.10` en `PrivateAssets=all` |
| `samples/OmniEurope.Blazor.HybridSmoke/OmniEurope.Blazor.HybridSmoke.csproj` | Smoke MAUI Windows | `Microsoft.Maui.Controls@10.0.20`, `Microsoft.AspNetCore.Components.WebView.Maui@10.0.20` |
| 7 fichiers `packages.lock.json` | Résolution transitive | 186 occurrences résolues, 81 noms uniques, 87 couples nom/version, hachages de contenu |
| `.github/workflows/ci.yml` | Dépendances CI | `actions/checkout@v4`, `actions/setup-dotnet@v5`, `actions/upload-artifact@v4`; workload `maui-windows` |
| `.github/workflows/publish-nuget.yml` | Publication | `actions/checkout@v4`, `actions/setup-dotnet@v5`, `NuGet/login@v1` |

Aucun `package.json`, lock npm/yarn/pnpm, manifeste Python, `go.mod`, `Cargo.toml`, `Paket` ou autre gestionnaire de packages n'a été détecté. Il n'existe donc pas de portée `npm audit`, `govulncheck`, `cargo audit` ou `pip-audit` à exécuter. Aucun outil Python n'a été sondé.

## Versions directes centralisées

| Package | Version | Projets consommateurs |
|---|---:|---|
| `bunit` | 2.8.6 | Tests |
| `Microsoft.AspNetCore.Components.Web` | 10.0.10 | Bibliothèque; résolution central-transitive dans WASM, Hybrid et tests |
| `Microsoft.AspNetCore.Components.WebView.Maui` | 10.0.20 | HybridSmoke |
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.10 | AutoSmoke.Client, WasmSmoke; central-transitive dans AutoSmoke et tests |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | 10.0.10 | WasmSmoke, privé |
| `Microsoft.AspNetCore.Components.WebAssembly.Server` | 10.0.10 | AutoSmoke |
| `Microsoft.NET.Test.Sdk` | 18.8.1 | Tests |
| `Microsoft.Maui.Controls` | 10.0.20 | HybridSmoke |
| `xunit.v3` | 3.2.2 | Tests |
| `xunit.runner.visualstudio` | 3.1.5 | Tests, privé |

Toutes les versions de `Directory.Packages.props` sont numériques et non flottantes. Les `PackageReference` n'emploient ni wildcard, ni `VersionOverride`, ni intervalle explicite. Les références de projet sont:

- `OmniEurope.Blazor` depuis tous les samples et les tests.
- `OmniEurope.Blazor.AutoSmoke.Client` depuis `OmniEurope.Blazor.AutoSmoke`.

## Inventaire résolu exhaustif par verrou

Les entrées `Direct` ci-dessous incluent les dépendances injectées par le SDK, pas uniquement les `PackageReference` écrits dans les `.csproj`. `CentralTransitive` désigne une dépendance transitive épinglée par la gestion centralisée.

### `src/OmniEurope.Blazor/packages.lock.json`

- TFM `net10.0`: 19 packages, dont 1 direct et 18 transitifs.
- Direct: `Microsoft.AspNetCore.Components.Web@10.0.10`.
- Transitifs: `Microsoft.AspNetCore.Authorization@10.0.10`; `Microsoft.AspNetCore.Components@10.0.10`; `Microsoft.AspNetCore.Components.Analyzers@10.0.10`; `Microsoft.AspNetCore.Components.Forms@10.0.10`; `Microsoft.AspNetCore.Metadata@10.0.10`; `Microsoft.Extensions.Configuration@10.0.10`; `Microsoft.Extensions.Configuration.Abstractions@10.0.10`; `Microsoft.Extensions.Configuration.Binder@10.0.10`; `Microsoft.Extensions.DependencyInjection@10.0.10`; `Microsoft.Extensions.DependencyInjection.Abstractions@10.0.10`; `Microsoft.Extensions.Diagnostics@10.0.10`; `Microsoft.Extensions.Diagnostics.Abstractions@10.0.10`; `Microsoft.Extensions.Logging.Abstractions@10.0.10`; `Microsoft.Extensions.Options@10.0.10`; `Microsoft.Extensions.Options.ConfigurationExtensions@10.0.10`; `Microsoft.Extensions.Primitives@10.0.10`; `Microsoft.Extensions.Validation@10.0.10`; `Microsoft.JSInterop@10.0.10`.

### `samples/OmniEurope.Blazor.AutoSmoke.Client/packages.lock.json`

- TFM `net10.0`: 31 entrées, dont 4 directes, 1 central-transitive, 25 transitives et 1 projet. La cible secondaire `net10.0/browser-wasm` ne contient aucune entrée additionnelle.
- Directes: `Microsoft.AspNetCore.App.Internal.Assets@10.0.10`; `Microsoft.AspNetCore.Components.WebAssembly@10.0.10`; `Microsoft.NET.ILLink.Tasks@10.0.10`; `Microsoft.NET.Sdk.WebAssembly.Pack@10.0.10`.
- Central-transitive: `Microsoft.AspNetCore.Components.Web@10.0.10`.
- Projet: `OmniEurope.Blazor`.
- Transitives: `Microsoft.AspNetCore.Authorization@10.0.10`; `Microsoft.AspNetCore.Components@10.0.10`; `Microsoft.AspNetCore.Components.Analyzers@10.0.10`; `Microsoft.AspNetCore.Components.Forms@10.0.10`; `Microsoft.AspNetCore.Metadata@10.0.10`; `Microsoft.Extensions.Configuration@10.0.10`; `Microsoft.Extensions.Configuration.Abstractions@10.0.10`; `Microsoft.Extensions.Configuration.Binder@10.0.10`; `Microsoft.Extensions.Configuration.FileExtensions@10.0.10`; `Microsoft.Extensions.Configuration.Json@10.0.10`; `Microsoft.Extensions.DependencyInjection@10.0.10`; `Microsoft.Extensions.DependencyInjection.Abstractions@10.0.10`; `Microsoft.Extensions.Diagnostics@10.0.10`; `Microsoft.Extensions.Diagnostics.Abstractions@10.0.10`; `Microsoft.Extensions.FileProviders.Abstractions@10.0.10`; `Microsoft.Extensions.FileProviders.Physical@10.0.10`; `Microsoft.Extensions.FileSystemGlobbing@10.0.10`; `Microsoft.Extensions.Logging@10.0.10`; `Microsoft.Extensions.Logging.Abstractions@10.0.10`; `Microsoft.Extensions.Options@10.0.10`; `Microsoft.Extensions.Options.ConfigurationExtensions@10.0.10`; `Microsoft.Extensions.Primitives@10.0.10`; `Microsoft.Extensions.Validation@10.0.10`; `Microsoft.JSInterop@10.0.10`; `Microsoft.JSInterop.WebAssembly@10.0.10`.

### `samples/OmniEurope.Blazor.AutoSmoke/packages.lock.json`

- TFM `net10.0`: 6 entrées, dont 2 directes, 1 central-transitive, 1 transitive et 2 projets.
- Directes: `Microsoft.AspNetCore.App.Internal.Assets@10.0.10`; `Microsoft.AspNetCore.Components.WebAssembly.Server@10.0.10`.
- Central-transitive: `Microsoft.AspNetCore.Components.WebAssembly@10.0.10`.
- Transitive: `Microsoft.JSInterop.WebAssembly@10.0.10`.
- Projets: `OmniEurope.Blazor`; `OmniEurope.Blazor.AutoSmoke.Client`.

### `samples/OmniEurope.Blazor.Catalog/packages.lock.json`

- TFM `net10.0`: 2 entrées.
- Directe SDK: `Microsoft.AspNetCore.App.Internal.Assets@10.0.10`.
- Projet: `OmniEurope.Blazor`.

### `samples/OmniEurope.Blazor.WasmSmoke/packages.lock.json`

- TFM `net10.0`: 32 entrées, dont 5 directes, 1 central-transitive, 25 transitives et 1 projet. La cible secondaire `net10.0/browser-wasm` ne contient aucune entrée additionnelle.
- Directes: `Microsoft.AspNetCore.App.Internal.Assets@10.0.10`; `Microsoft.AspNetCore.Components.WebAssembly@10.0.10`; `Microsoft.AspNetCore.Components.WebAssembly.DevServer@10.0.10`; `Microsoft.NET.ILLink.Tasks@10.0.10`; `Microsoft.NET.Sdk.WebAssembly.Pack@10.0.10`.
- Central-transitive: `Microsoft.AspNetCore.Components.Web@10.0.10`.
- Projet: `OmniEurope.Blazor`.
- Transitives: même socle de 25 packages `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*` et `Microsoft.JSInterop*` que AutoSmoke.Client, tous en `10.0.10`.

### `samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json`

- TFM `net10.0-windows10.0.19041`: 45 entrées, dont 2 directes, 1 central-transitive, 41 transitives et 1 projet.
- Directes: `Microsoft.AspNetCore.Components.WebView.Maui@10.0.20`; `Microsoft.Maui.Controls@10.0.20`.
- Central-transitive: `Microsoft.AspNetCore.Components.Web@10.0.10`.
- Projet: `OmniEurope.Blazor`.
- Transitives: `Microsoft.AspNetCore.Authorization@10.0.10`; `Microsoft.AspNetCore.Components@10.0.10`; `Microsoft.AspNetCore.Components.Analyzers@10.0.10`; `Microsoft.AspNetCore.Components.Forms@10.0.10`; `Microsoft.AspNetCore.Components.WebView@10.0.0`; `Microsoft.AspNetCore.Metadata@10.0.10`; `Microsoft.Extensions.Configuration@10.0.10`; `Microsoft.Extensions.Configuration.Abstractions@10.0.10`; `Microsoft.Extensions.Configuration.Binder@10.0.10`; `Microsoft.Extensions.Configuration.FileExtensions@10.0.0`; `Microsoft.Extensions.Configuration.Json@10.0.0`; `Microsoft.Extensions.DependencyInjection@10.0.10`; `Microsoft.Extensions.DependencyInjection.Abstractions@10.0.10`; `Microsoft.Extensions.Diagnostics@10.0.10`; `Microsoft.Extensions.Diagnostics.Abstractions@10.0.10`; `Microsoft.Extensions.FileProviders.Abstractions@10.0.0`; `Microsoft.Extensions.FileProviders.Composite@10.0.0`; `Microsoft.Extensions.FileProviders.Embedded@10.0.0`; `Microsoft.Extensions.FileProviders.Physical@10.0.0`; `Microsoft.Extensions.FileSystemGlobbing@10.0.0`; `Microsoft.Extensions.Hosting.Abstractions@10.0.0`; `Microsoft.Extensions.Logging@10.0.0`; `Microsoft.Extensions.Logging.Abstractions@10.0.10`; `Microsoft.Extensions.Options@10.0.10`; `Microsoft.Extensions.Options.ConfigurationExtensions@10.0.10`; `Microsoft.Extensions.Primitives@10.0.10`; `Microsoft.Extensions.Validation@10.0.10`; `Microsoft.Graphics.Win2D@1.3.2`; `Microsoft.IO.RecyclableMemoryStream@3.0.1`; `Microsoft.JSInterop@10.0.10`; `Microsoft.Maui.Controls.Build.Tasks@10.0.20`; `Microsoft.Maui.Controls.Core@10.0.20`; `Microsoft.Maui.Controls.Xaml@10.0.20`; `Microsoft.Maui.Core@10.0.20`; `Microsoft.Maui.Essentials@10.0.20`; `Microsoft.Maui.Graphics@10.0.20`; `Microsoft.Maui.Graphics.Win2D.WinUI.Desktop@10.0.20`; `Microsoft.Maui.Resizetizer@10.0.20`; `Microsoft.Web.WebView2@1.0.3179.45`; `Microsoft.Windows.SDK.BuildTools@10.0.22621.756`; `Microsoft.WindowsAppSDK@1.7.250909003`.

### `tests/OmniEurope.Blazor.Tests/packages.lock.json`

- TFM `net10.0`: 58 entrées, dont 4 directes, 2 central-transitives, 51 transitives et 1 projet.
- Directes: `bunit@2.8.6`; `Microsoft.NET.Test.Sdk@18.8.1`; `xunit.runner.visualstudio@3.1.5`; `xunit.v3@3.2.2`.
- Central-transitives: `Microsoft.AspNetCore.Components.Web@10.0.10`; `Microsoft.AspNetCore.Components.WebAssembly@10.0.10`.
- Projet: `OmniEurope.Blazor`.
- Transitives: `AngleSharp@1.5.2`; `AngleSharp.Css@1.0.0-beta.224`; `AngleSharp.Diffing@1.1.1`; `Microsoft.ApplicationInsights@2.23.0`; `Microsoft.AspNetCore.Authorization@10.0.10`; `Microsoft.AspNetCore.Components@10.0.10`; `Microsoft.AspNetCore.Components.Analyzers@10.0.10`; `Microsoft.AspNetCore.Components.Authorization@10.0.10`; `Microsoft.AspNetCore.Components.Forms@10.0.10`; `Microsoft.AspNetCore.Components.WebAssembly.Authentication@10.0.10`; `Microsoft.AspNetCore.Metadata@10.0.10`; `Microsoft.Bcl.AsyncInterfaces@6.0.0`; `Microsoft.CodeCoverage@18.8.1`; `Microsoft.Extensions.Caching.Abstractions@10.0.10`; `Microsoft.Extensions.Caching.Memory@10.0.10`; `Microsoft.Extensions.Configuration@10.0.10`; `Microsoft.Extensions.Configuration.Abstractions@10.0.10`; `Microsoft.Extensions.Configuration.Binder@10.0.10`; `Microsoft.Extensions.Configuration.FileExtensions@10.0.10`; `Microsoft.Extensions.Configuration.Json@10.0.10`; `Microsoft.Extensions.DependencyInjection@10.0.10`; `Microsoft.Extensions.DependencyInjection.Abstractions@10.0.10`; `Microsoft.Extensions.Diagnostics@10.0.10`; `Microsoft.Extensions.Diagnostics.Abstractions@10.0.10`; `Microsoft.Extensions.FileProviders.Abstractions@10.0.10`; `Microsoft.Extensions.FileProviders.Physical@10.0.10`; `Microsoft.Extensions.FileSystemGlobbing@10.0.10`; `Microsoft.Extensions.Localization.Abstractions@10.0.10`; `Microsoft.Extensions.Logging@10.0.10`; `Microsoft.Extensions.Logging.Abstractions@10.0.10`; `Microsoft.Extensions.Options@10.0.10`; `Microsoft.Extensions.Options.ConfigurationExtensions@10.0.10`; `Microsoft.Extensions.Primitives@10.0.10`; `Microsoft.Extensions.Validation@10.0.10`; `Microsoft.JSInterop@10.0.10`; `Microsoft.JSInterop.WebAssembly@10.0.10`; `Microsoft.Testing.Extensions.Telemetry@1.9.1`; `Microsoft.Testing.Extensions.TrxReport.Abstractions@1.9.1`; `Microsoft.Testing.Platform@1.9.1`; `Microsoft.Testing.Platform.MSBuild@1.9.1`; `Microsoft.TestPlatform.ObjectModel@18.8.1`; `Microsoft.TestPlatform.TestHost@18.8.1`; `Microsoft.Win32.Registry@5.0.0`; `xunit.analyzers@1.27.0`; `xunit.v3.assert@3.2.2`; `xunit.v3.common@3.2.2`; `xunit.v3.core.mtp-v1@3.2.2`; `xunit.v3.extensibility.core@3.2.2`; `xunit.v3.mtp-v1@3.2.2`; `xunit.v3.runner.common@3.2.2`; `xunit.v3.runner.inproc.console@3.2.2`.

## Obsolescence et maintenance

### Packages directs obsolètes

| Projet | Package | Résolu | Dernier NuGet | Écart |
|---|---|---:|---:|---|
| Tests | `bunit` | 2.8.6 | 2.9.0 | mineur |
| HybridSmoke | `Microsoft.AspNetCore.Components.WebView.Maui` | 10.0.20 | 10.0.90 | servicing MAUI |
| HybridSmoke | `Microsoft.Maui.Controls` | 10.0.20 | 10.0.90 | servicing MAUI |

### Transitifs obsolètes signalés

- Tests: `AngleSharp 1.5.2 -> 1.7.1`; `AngleSharp.Css 1.0.0-beta.224 -> 1.0.1`; `Microsoft.ApplicationInsights 2.23.0 -> 3.1.2`; `Microsoft.Bcl.AsyncInterfaces 6.0.0 -> 10.0.10`; `Microsoft.Testing.Extensions.Telemetry`, `Microsoft.Testing.Extensions.TrxReport.Abstractions`, `Microsoft.Testing.Platform` et `Microsoft.Testing.Platform.MSBuild`, tous `1.9.1 -> 2.3.3`.
- HybridSmoke: `Microsoft.AspNetCore.Components.WebView 10.0.0 -> 10.0.10`; sept packages `Microsoft.Extensions.* 10.0.0 -> 10.0.10`; `Microsoft.Graphics.Win2D 1.3.2 -> 1.4.0`; huit packages MAUI `10.0.20 -> 10.0.90`; `Microsoft.Web.WebView2 1.0.3179.45 -> 1.0.4129.50`; `Microsoft.Windows.SDK.BuildTools 10.0.22621.756 -> 10.0.28000.2526`; `Microsoft.WindowsAppSDK 1.7.250909003 -> 2.3.1`.

Les nombres `Latest` comparent la version absolue la plus récente, y compris des changements majeurs potentiellement incompatibles. Les transitifs doivent être mis à niveau par leurs packages parents, pas épinglés individuellement sans nécessité. Aucun package n'est déclaré déprécié par NuGet.org; cela constitue un signal de maintenance positif, sans prouver à lui seul l'activité de chaque dépôt source.

## Conflits, duplications et verrouillage

### Versions multiples entre verrous

| Package | Versions | Cause observée |
|---|---|---|
| `Microsoft.Extensions.Configuration.FileExtensions` | 10.0.0, 10.0.10 | Hybrid WebView.Maui 10.0.20 contre pile .NET 10.0.10 ailleurs |
| `Microsoft.Extensions.Configuration.Json` | 10.0.0, 10.0.10 | même cause |
| `Microsoft.Extensions.FileProviders.Abstractions` | 10.0.0, 10.0.10 | même cause |
| `Microsoft.Extensions.FileProviders.Physical` | 10.0.0, 10.0.10 | même cause |
| `Microsoft.Extensions.FileSystemGlobbing` | 10.0.0, 10.0.10 | même cause |
| `Microsoft.Extensions.Logging` | 10.0.0, 10.0.10 | même cause |

Il ne s'agit pas d'un conflit dans un même graphe résolu: chaque projet dispose de son verrou et le build ne remonte ni `NU1605`, ni autre avertissement NuGet. Le décalage complique cependant la validation Hybrid et disparaîtrait vraisemblablement avec la mise à niveau coordonnée des deux packages MAUI directs.

### Reproductibilité

- Les 7 verrous sont suivis par Git et contiennent des `contentHash`.
- `ci.yml` restaure `OmniEurope.Blazor.slnx --locked-mode`, puis restaure aussi HybridSmoke `--locked-mode` dans un job Windows dédié.
- `publish-nuget.yml` restaure la bibliothèque publiée `--locked-mode` avant le pack.
- `RestoreLockedMode` n'est pas globalement activé dans MSBuild. Ce n'est pas un finding pour la CI ou la publication, qui passent explicitement le mode verrouillé; les restaurations locales ordinaires peuvent mettre à jour un verrou et laisser Git rendre la dérive visible.
- `global.json` autorise `latestPatch` et le job Hybrid exécute `dotnet workload install maui-windows` sans workload set explicite: cette partie du toolchain reste partiellement flottante.

## Poids et utilisation potentielle

Les tailles suivantes sont les tailles compressées des `.nupkg` du cache local. Elles donnent un ordre de grandeur de restauration, pas la taille finale publiée ou déployée.

| Package | Version | Taille `.nupkg` | Portée |
|---|---:|---:|---|
| `Microsoft.WindowsAppSDK` | 1.7.250909003 | 114,76 Mio | Hybrid sample uniquement |
| `Microsoft.Maui.Resizetizer` | 10.0.20 | 40,93 Mio | Hybrid sample uniquement |
| `Microsoft.Windows.SDK.BuildTools` | 10.0.22621.756 | 21,11 Mio | Hybrid sample uniquement |
| `Microsoft.Maui.Controls.Core` | 10.0.20 | 12,14 Mio | Hybrid sample uniquement |
| `Microsoft.CodeCoverage` | 18.8.1 | 10,50 Mio | tests uniquement |
| `Microsoft.Web.WebView2` | 1.0.3179.45 | 8,39 Mio | Hybrid sample uniquement |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | 10.0.10 | 8,29 Mio | WasmSmoke, privé |
| `Microsoft.TestPlatform.TestHost` | 18.8.1 | 5,22 Mio | tests uniquement |
| `Microsoft.NET.Sdk.WebAssembly.Pack` | 10.0.10 | 4,34 Mio | samples WASM |
| `Microsoft.Testing.Platform` | 1.9.1 | 4,21 Mio | tests uniquement |

Aucun package direct n'est prouvé inutilisé. Les usages source confirment Blazor Web, WebAssembly, MAUI, bUnit et xUnit; `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` et DevServer sont des dépendances d'infrastructure. Le graphe de tests contient à la fois VSTest/TestHost et Microsoft Testing Platform via xUnit v3; il est potentiellement plus lourd que nécessaire, mais leur suppression ne peut pas être recommandée sans une expérience isolée couvrant `dotnet test`, la découverte IDE et la CI. Les piles lourdes restent hors du package NuGet publié.

## Licences

### Résumé

| Métadonnée | Couples nom/version | Noms uniques | Conclusion |
|---|---:|---:|---|
| `MIT` | 73 | 67 | permissive; inclut toute la pile runtime de la bibliothèque publiée |
| `Apache-2.0` | 10 | 10 | pile xUnit uniquement |
| fichier de licence embarqué | 2 | 2 | WebView2 permissif de type trois clauses; WindowsAppSDK sous termes Microsoft propriétaires |
| URL de licence Win2D | 1 | 1 | métadonnée historique HTTP, sans expression SPDX |
| URL de licence Windows SDK | 1 | 1 | termes Microsoft Windows SDK, sans expression SPDX |
| absente ou illisible | 0 | 0 | aucune |

### Inventaire par licence

- `Apache-2.0`: `xunit.analyzers@1.27.0`; `xunit.runner.visualstudio@3.1.5`; `xunit.v3@3.2.2`; `xunit.v3.assert@3.2.2`; `xunit.v3.common@3.2.2`; `xunit.v3.core.mtp-v1@3.2.2`; `xunit.v3.extensibility.core@3.2.2`; `xunit.v3.mtp-v1@3.2.2`; `xunit.v3.runner.common@3.2.2`; `xunit.v3.runner.inproc.console@3.2.2`.
- Fichier de licence: `Microsoft.Web.WebView2@1.0.3179.45`; `Microsoft.WindowsAppSDK@1.7.250909003`.
- URL de licence: `Microsoft.Graphics.Win2D@1.3.2` vers `http://www.microsoft.com/web/webpi/eula/eula_win2d_10012014.htm`; `Microsoft.Windows.SDK.BuildTools@10.0.22621.756` vers `https://aka.ms/WinSDKLicenseURL`.
- `MIT`: `AngleSharp@1.5.2`; `AngleSharp.Css@1.0.0-beta.224`; `AngleSharp.Diffing@1.1.1`; `bunit@2.8.6`; `Microsoft.ApplicationInsights@2.23.0`; tous les `Microsoft.AspNetCore.*` inventoriés; `Microsoft.Bcl.AsyncInterfaces@6.0.0`; `Microsoft.CodeCoverage@18.8.1`; tous les `Microsoft.Extensions.*` inventoriés; `Microsoft.IO.RecyclableMemoryStream@3.0.1`; `Microsoft.JSInterop*`; tous les `Microsoft.Maui.*` inventoriés; `Microsoft.NET.ILLink.Tasks@10.0.10`; `Microsoft.NET.Sdk.WebAssembly.Pack@10.0.10`; `Microsoft.NET.Test.Sdk@18.8.1`; tous les `Microsoft.Testing.*`; `Microsoft.TestPlatform.*`; `Microsoft.Win32.Registry@5.0.0`.

Le `NOTICE.md` du dépôt décrit seulement l'indépendance vis-à-vis de Radzen et ne contient pas d'inventaire des notices tierces. Aucun conflit de licence n'est établi pour le package EUPL-1.2: sa seule dépendance directe, `Microsoft.AspNetCore.Components.Web`, est MIT. Les termes propriétaires Windows concernent le sample Hybrid et nécessitent une traçabilité si ses binaires sont redistribués.

## Findings priorisés

### Critique

RAS.

### Élevé

RAS.

### Moyen

- **D-001 [Moyen] [Dépendances]** La pile HybridSmoke est en retard (`Microsoft.Maui.Controls` et `Microsoft.AspNetCore.Components.WebView.Maui` 10.0.20 contre 10.0.90) et résout plusieurs composants .NET en 10.0.0 à côté du socle 10.0.10 - `Directory.Packages.props:8,13`, `samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json` - source: `dotnet list ... --outdated --include-transitive` et comparaison exhaustive des verrous - **remédiation Codex-exécutable:** Codex mettra à jour les deux packages MAUI de manière coordonnée vers une version compatible avec le SDK/workload retenu, régénérera le verrou, puis exécutera restore verrouillé, build Hybrid Windows et smoke tests avant de conserver le changement.
- **D-002 [Moyen] [Fiabilité]** Le graphe de tests ne fournit pas le collecteur demandé par `--collect:"XPlat Code Coverage"`; le préflight échoue malgré `Microsoft.CodeCoverage@18.8.1` transitive, rendant couverture et CRAP non fiables - `tests/OmniEurope.Blazor.Tests/OmniEurope.Blazor.Tests.csproj:8-11`, `metrics/PREFLIGHT.md` - **remédiation Codex-exécutable:** Codex choisira une seule chaîne de couverture compatible, ajoutera et épinglera centralement le collecteur requis, ou alignera la commande sur Microsoft Code Coverage, puis prouvera la génération d'un fichier de couverture exploitable sur 57 tests ou plus.
- **D-003 [Moyen] [Sécurité supply-chain]** Les actions CI et publication sont référencées par tags majeurs mutables, y compris `NuGet/login@v1` dans le job disposant de `id-token: write` - `.github/workflows/ci.yml:16-17,61,69-70`, `.github/workflows/publish-nuget.yml:16-17,26` - **remédiation Codex-exécutable:** Codex résoudra les révisions officielles approuvées, remplacera chaque tag par un SHA complet avec commentaire de version, puis configurera une mise à jour automatisée contrôlée et validera les workflows.

### Faible

- **D-004 [Faible] [Dépendances]** `bunit@2.8.6` est derrière `2.9.0`; son graphe conserve notamment AngleSharp CSS beta et Microsoft Testing Platform 1.9.1 - `Directory.Packages.props:6`, `tests/OmniEurope.Blazor.Tests/packages.lock.json` - source: analyse outdated NuGet.org - **remédiation Codex-exécutable:** Codex mettra à niveau bUnit dans une branche isolée, régénérera le verrou en mode contrôlé et exécutera les 57 tests, les tests CSP et les budgets avant d'accepter la résolution transitive.
- **D-005 [Faible] [Licences]** Aucun SBOM ou inventaire versionné des licences tierces ne couvre les 81 packages uniques; quatre packages Hybrid utilisent une licence par fichier ou URL au lieu d'une expression SPDX et WindowsAppSDK porte des termes propriétaires - `NOTICE.md`, `samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json` - **remédiation Codex-exécutable:** Codex générera un SBOM et un registre de notices à partir des verrous, conservera les textes requis avec les artefacts redistribués et ajoutera un contrôle CI qui échoue sur toute licence absente ou sur tout changement inattendu de classification.
- **D-006 [Faible] [Reproductibilité]** Le SDK autorise `latestPatch` et le workload MAUI Windows est installé sans workload set explicite, ce qui laisse varier des packs hors NuGet malgré les verrous - `global.json:4`, `.github/workflows/ci.yml:75-76` - **remédiation Codex-exécutable:** Codex épinglera un workload set compatible avec le SDK choisi, vérifiera qu'une restauration propre reproduit les mêmes packs sur le runner Windows, puis ajoutera un contrôle de dérive du toolchain.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - la gestion centralisée de 10 versions, les verrous par projet et le `--locked-mode` CI constituent une solution plus simple et plus vérifiable que des versions répétées dans 7 projets ou un framework de dépendances personnalisé. Les graphes lourds sont justifiés par les samples MAUI/WASM et les tests, et ne contaminent pas le package publié. Aucun finding de sur-ingénierie n'est émis.

## Compteurs

- Manifestes/réglages inspectés: 21, dont 7 `.csproj`, 7 verrous NuGet, 1 manifeste central, 1 configuration NuGet, 1 SDK, 1 props de build, 1 solution et 2 workflows.
- Dépendances explicites: 11 occurrences `PackageReference`, 10 noms centralisés.
- Résolution: 186 occurrences dans les verrous, 81 noms uniques, 87 couples package/version.
- Vulnérabilités: 0.
- Dépréciations: 0.
- Findings actionnables: 6, soit Critique 0, Élevé 0, Moyen 3, Faible 3.
