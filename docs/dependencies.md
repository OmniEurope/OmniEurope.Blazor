# Dépendances et chaîne d'outillage

Les versions directes sont centralisées dans `Directory.Packages.props`; les résolutions transitives sont figées dans les fichiers `packages.lock.json`. Le fichier `eng/dependency-policy.json` conserve la date et la source de la revue manuelle des versions sensibles. Dependabot propose chaque semaine les mises à jour NuGet et GitHub Actions sous forme de demandes contrôlées.

## Décisions vérifiées le 13 septembre 2026

- les packages ASP.NET Core et `Microsoft.Extensions.Localization` sont en `10.0.12`, `bunit` en `2.11.3` et `Microsoft.NET.Test.Sdk` en `18.10.0`, dernières versions stables publiées par NuGet.org.
- `coverlet.collector` est épinglé en `10.0.1`, dernière version stable, pour produire la couverture Cobertura avec `XPlat Code Coverage`.
- les packages MAUI sont alignés sur `10.0.101`;
- `Microsoft.CodeAnalysis.CSharp` reste en `5.6.0`, compilateur du SDK `10.0.3xx` : la `5.9.0` publiée exige un compilateur plus récent que le dernier SDK stable et déclencherait `CS9057`.
- `xunit.v3` `3.2.2` et `xunit.runner.visualstudio` `3.1.5` restent en place : la ligne 4 impose de migrer la chaîne de tests et de couverture vers Microsoft.Testing.Platform (voir `eng/dependency-policy.json`).
- le SDK est épinglé sur la bande de fonctionnalités `10.0.3xx` via `global.json` (`version: 10.0.300`, `rollForward: latestPatch`). Aucun workload set n'est figé : seul le job MAUI installe `maui-windows`, et `eng/Test-SdkBand.ps1` vérifie la bande dans les deux jobs plutôt que de recopier un numéro dans les workflows.

## Ressources tierces hors NuGet

Les ressources recopiées dans le dépôt sont déclarées dans `eng/vendored-assets.json`, qui pilote leur section dans `NOTICE.md`, leur composant CycloneDX et la conservation de leur texte de licence. Y figure aujourd'hui Phosphor Icons `2.1.1` (MIT), source des tracés intégrés d'`OmniIcon`. Toute mise à jour doit rejouer `eng/Generate-Sbom.ps1` puis `eng/Test-Sbom.ps1`.

Toute modification doit mettre à jour les verrous, passer les tests et conserver les contrôles de couverture, de SBOM et de licences.
