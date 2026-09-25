<!-- SPDX-License-Identifier: EUPL-1.2 -->
# ADR-002 : plancher SDK `10.0.100` en `latestFeature`, paquets implicites du SDK épinglés

## Statut

Accepté. Remplace [ADR-001](ADR-001-global-json-rollforward-latestpatch.md).

## Date

2026-09-25

## Contexte

ADR-001 dérogeait à la règle **`STD-SDKPIN`** du kit `_Generic` (plancher unique, `rollForward:
latestFeature`) en gardant `version: 10.0.401` et `rollForward: latestPatch`. Son motif était
constaté, pas théorique : les hôtes WebAssembly du dépôt (vitrine, Catalog, WasmSmoke, AutoSmoke et
son client) reçoivent du SDK trois références de paquets implicites à la version du runtime qu'il
embarque, lue dans `Microsoft.NETCoreSdk.BundledVersions.props` :

- `Microsoft.NET.ILLink.Tasks` (item `KnownILLinkPack`) ;
- `Microsoft.NET.Sdk.WebAssembly.Pack` (item `KnownWebAssemblySdkPack`) ;
- `Microsoft.AspNetCore.App.Internal.Assets` (item `KnownAspNetCorePack`).

Laissées implicites, ces versions suivent la bande du SDK (`10.0.6` sous `10.0.202`, `10.0.11` sous
`10.0.303`, `10.0.12` sous `10.0.401`), et chaque `packages.lock.json` ne se restaure en `--locked-mode`
que sous la bande qui l'a écrit.

Reproduit le 2026-09-25 avant cette décision, `global.json` passé à `10.0.100` en `latestFeature` et
une machine autorisée par ce plancher simulée en `10.0.303` puis `10.0.202` : la restauration
verrouillée échoue sur les cinq hôtes WebAssembly, par exemple
`error NU1004: The package reference Microsoft.NET.ILLink.Tasks version has changed from [10.0.12, ) to [10.0.11, )`
sous `10.0.303` et
`error NU1004: The package reference Microsoft.AspNetCore.App.Internal.Assets version has changed from [10.0.12, ) to [10.0.6, )`
sous `10.0.202`. ADR-001 était donc fondé. Sa condition de sortie prévoyait le cas présent : que les
hôtes WebAssembly n'aient plus de paquet implicite lié au runtime du SDK.

## Décision

1. La cause est levée plutôt que contournée : `Directory.Build.targets` met à jour les trois items
   `Known*Pack` par leur nom et fixe leur version à `OmniImplicitSdkPackVersion` (`10.0.12`), alignée
   sur les paquets `Microsoft.AspNetCore.*` de `Directory.Packages.props`. Les versions ne dépendent
   plus du SDK qui restaure.
2. `global.json` revient à la forme de la règle : `version: 10.0.100`, `rollForward: latestFeature`,
   `allowPrerelease: false`. `Microsoft.CodeAnalysis.CSharp` était déjà à `5.0.0`, le compilateur de
   ce plancher, et ne bouge pas.
3. Les quatre gardes d'ADR-001 sont réécrites sur cette forme : `ConventionGuardTests`
   (`GlobalJson_DeclaresTheSdkFloorWithoutAWorkloadVersion`), `eng/Test-SdkBand.ps1` (plancher et même
   ligne `10.0`, toute bande ultérieure acceptée), le bloc `toolchain` de `eng/dependency-policy.json`
   et `eng/Test-DependencyPolicy.ps1`, qui compare toujours les deux.
4. Deux gardes s'ajoutent : `CodeAnalysisPackages_MatchTheCompilerOfTheSdkFloor` (table `STD-SDKPIN`
   plancher vers Roslyn, pour les trois paquets `Microsoft.CodeAnalysis`) et
   `ImplicitSdkPacks_ArePinnedToTheCentralAspNetCoreVersion`, qui empêche le pin des paquets implicites
   de diverger des paquets ASP.NET Core centraux.
5. `.github/dependabot.yml` exclut des mises à jour les trois paquets `Microsoft.CodeAnalysis`, et
   seulement eux. L'exclusion `STD-SDKPIN` de `.config/verify-rules.json` est retirée.

## Preuves

Relevées le 2026-09-25 avec le pin en place :

- restauration `--locked-mode` en Release verte sous `10.0.202`, `10.0.303` et `10.0.401` ;
- sous `10.0.202`, la plus ancienne bande disponible sur le poste : compilation de la solution sans
  avertissement ni erreur, 1571 tests verts, publication puis sondes navigateur vertes des hôtes
  WebAssembly (`eng/Test-WasmHost.ps1`), Interactive Auto (`eng/Test-AutoHost.ps1`) et Catalog
  (`eng/Test-CatalogHost.ps1`) ;
- un dépôt consommateur par `ProjectReference` (Bellwether) compile sans `CS9057` ni erreur de
  restauration venue d'OE.

Aucun SDK `10.0.1xx` n'est installé sur le poste : le plancher exact n'a pas été exercé localement.
La CI le dérive par `global-json-file` et en fait foi.

## Conséquences

Positives :

- Toute station `10.0` à partir du plancher construit le dépôt, sans dérogation au kit.
- La restauration verrouillée reste reproductible, quelle que soit la bande résolue.

Acceptées :

- Monter le runtime cible est un commit délibéré : relever `OmniImplicitSdkPackVersion` avec les
  paquets `Microsoft.AspNetCore.*`, puis régénérer les verrous par une restauration Release. La garde
  d'alignement le rappelle.
- Un SDK plus ancien que `10.0.12` compile les hôtes WebAssembly avec des packs `10.0.12` ; prouvé
  sous `10.0.202` par les sondes navigateur ci-dessus, à revérifier si un écart apparaît.
