# Dépendances et chaîne d'outillage

Les versions directes sont centralisées dans `Directory.Packages.props`; les résolutions transitives sont figées dans les fichiers `packages.lock.json`. Le fichier `eng/dependency-policy.json` conserve la date et la source de la revue manuelle des versions sensibles. Dependabot propose chaque semaine les mises à jour NuGet et GitHub Actions sous forme de demandes contrôlées.

## Décisions vérifiées le 25 septembre 2026

- les packages ASP.NET Core et `Microsoft.Extensions.Localization` sont en `10.0.12` et `bunit` en `2.11.3`, dernières versions stables publiées par NuGet.org.
- la chaîne de tests tourne sur Microsoft.Testing.Platform, déclaré par le bloc `test` de `global.json`. `xunit.v3` `4.0.1` embarque son propre exécuteur, donc ni `Microsoft.NET.Test.Sdk` ni `xunit.runner.visualstudio` ne sont nécessaires. `coverlet.MTP` `10.0.1` produit le Cobertura (`--coverlet --coverlet-output-format cobertura`, restreint à `[OmniEurope.Blazor]*`) et `Microsoft.Testing.Extensions.TrxReport` `2.4.1` le TRX (`--report-trx`).
- les packages MAUI sont alignés sur `10.0.101`;
- `Microsoft.CodeAnalysis.CSharp` est en `5.0.0`, le compilateur du premier SDK .NET 10 (`10.0.100`). Un analyseur ne peut pas référencer un compilateur plus récent que celui du SDK qui le charge (`CS9057`), et un projet qui référence ce dépôt par `ProjectReference` charge les analyseurs avec son propre SDK : en visant le plus ancien compilateur .NET 10, tout SDK `10.0` les charge. Vérifié le 2026-09-18 sous `10.0.202`, `10.0.303` et `10.0.401` ; avec `5.9.0`, `10.0.202` échoue en `CS9057`. Un test de convention fige cette version.
- le SDK est épinglé sur la bande de fonctionnalités `10.0.4xx` via `global.json` (`version: 10.0.401`, `rollForward: latestPatch`). Aucun workload set n'est figé : seul le job MAUI installe `maui-windows`, et `eng/Test-SdkBand.ps1` vérifie la bande dans les deux jobs plutôt que de recopier un numéro dans les workflows. Cet épinglage reste nécessaire pour la solution complète : les hôtes WebAssembly (vitrine, échantillons) reçoivent du SDK des paquets implicites à la version de son runtime (`Microsoft.NET.ILLink.Tasks`, `Microsoft.NET.Sdk.WebAssembly.Pack`, `Microsoft.AspNetCore.App.Internal.Assets`), et leurs fichiers de verrouillage ne se restaurent en `--locked-mode` qu'avec la bande qui les a produits (`NU1004` sous `10.0.202` et `10.0.303`). La bibliothèque et ses analyseurs, seuls chargés par un consommateur, n'en dépendent pas.

## Ressources tierces hors NuGet

Les ressources recopiées dans le dépôt sont déclarées dans `eng/vendored-assets.json`, qui pilote leur section dans `NOTICE.md`, leur composant CycloneDX et la conservation de leur texte de licence. Y figure aujourd'hui Phosphor Icons `2.1.1` (MIT), source des tracés intégrés d'`OmniIcon`. Toute mise à jour doit rejouer `eng/Generate-Sbom.ps1` puis `eng/Test-Sbom.ps1`.

Toute modification doit mettre à jour les verrous, passer les tests et conserver les contrôles de couverture, de SBOM et de licences.
