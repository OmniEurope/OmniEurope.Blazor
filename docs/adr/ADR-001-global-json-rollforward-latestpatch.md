<!-- SPDX-License-Identifier: EUPL-1.2 -->
# ADR-001 : `global.json` garde `rollForward: latestPatch`

## Statut

Remplacé par [ADR-002](ADR-002-sdk-floor-latestfeature-pinned-implicit-packs.md) le 2026-09-25 : sa condition de sortie est remplie, les paquets implicites du SDK sont épinglés dans `Directory.Build.targets`.

## Date

2026-09-20

## Règle du kit dérogée

Kit `_Generic`, `docs/code-rules.md`, règle **`STD-SDKPIN`** (« One SDK floor, derived everywhere,
guarded not enforced »), recette de détection, point 8 : *une garde du dépôt qui impose un
épinglage exact, `rollForward: feature` ou `rollForward: latestPatch`, sans ADR du dépôt qui dise
pourquoi, est un constat*. La forme attendue par la règle est
`"rollForward": "latestFeature"` sur un plancher unique.

Mécanisme de dérogation : kit `docs/adr/ADR-003-override-by-adr.md` (toute règle du kit est
dérogeable, et seulement par un ADR du dépôt).

## Contexte

`global.json` du dépôt déclare `version: 10.0.401`, `rollForward: latestPatch`,
`allowPrerelease: false`. Quatre mécanismes du dépôt imposent cette forme, et non `latestFeature` :

- `tests/OmniEurope.Blazor.Tests/ConventionGuardTests.cs`,
  `GlobalJson_PinsTheFeatureBandWithoutAWorkloadVersion` : `Assert.Equal("latestPatch", sdk.rollForward)`
  et absence de `workloadVersion` ;
- `eng/Test-SdkBand.ps1` : échoue si le SDK résolu sort de la bande de fonctionnalités du plancher
  (`10.0.4xx`) ; appelé par les deux jobs de `.github/workflows/ci.yml` ;
- `eng/dependency-policy.json`, bloc `toolchain` : `sdk: 10.0.401`, `rollForward: latestPatch` ;
- `eng/Test-DependencyPolicy.ps1` : échoue quand `global.json` et cette politique divergent.

La raison technique est documentée dans `docs/dependencies.md` et `docs/reproducibility.md`, et
tracée dans le journal de [PLAN-004](../plans/PLAN-004-execution-log.md) (lot 3) : les hôtes
WebAssembly du dépôt (la vitrine et les échantillons) reçoivent du SDK des paquets implicites à la
version de son runtime (`Microsoft.NET.ILLink.Tasks`, `Microsoft.NET.Sdk.WebAssembly.Pack`,
`Microsoft.AspNetCore.App.Internal.Assets`). Leurs `packages.lock.json` ne se restaurent en
`--locked-mode` qu'avec la bande qui les a produits : sous `10.0.202` ou `10.0.303`, la restauration
verrouillée échoue en **NU1004**. `latestFeature` laisserait donc une machine à jour résoudre une
autre bande et casser la restauration verrouillée, ou obligerait à régénérer les fichiers de
verrouillage à chaque nouvelle bande.

La bibliothèque publiée et ses analyseurs, seuls chargés par un consommateur, ne dépendent pas de la
bande : `Microsoft.CodeAnalysis.CSharp` vise le compilateur du premier SDK .NET 10 (`5.0.0`, SDK
`10.0.100`), ce que garde `ConventionGuardTests.Analyzers_ReferenceTheCompilerOfTheFirstDotNet10Sdk`.
La contrainte porte uniquement sur la construction de la solution complète dans ce dépôt.

## Décision

`global.json` garde `rollForward: latestPatch` et les quatre gardes restent en place, inchangées.
L'écart à `STD-SDKPIN` est porté par cet ADR.

Le reste de `STD-SDKPIN` continue de s'appliquer sans aménagement : plancher unique dans
`global.json`, CI qui dérive par `global-json-file` sans version littérale, paquets implicites du SDK
épinglés explicitement, garde non bloquante qui signale une version plus récente.

## Conséquences

Acceptées :

- Une station qui ne possède que `10.0.2xx` ou `10.0.3xx` ne construit pas ce dépôt ; elle doit
  installer un SDK `10.0.4xx`. Le message est explicite (`eng/Test-SdkBand.ps1`).
- Le passage à la bande suivante est un commit délibéré : modifier `global.json` et
  `eng/dependency-policy.json`, régénérer les fichiers de verrouillage par une restauration Release,
  réaligner `Microsoft.CodeAnalysis` si le compilateur a bougé.

Positives :

- La restauration verrouillée est reproductible : pas de NU1004 silencieux ni de résolution
  différente entre deux machines.
- Une seule source de vérité de la bande SDK, vérifiée mécaniquement dans les deux jobs de CI.

## Condition de sortie

Cet ADR tombe le jour où les hôtes WebAssembly n'ont plus de paquet implicite lié au runtime du SDK,
ou si le dépôt cesse de restaurer en `--locked-mode`. `global.json` repasse alors à `latestFeature`
et les quatre gardes sont réécrites sur cette forme.

## Application mécanique

L'exclusion correspondante est déclarée dans `.config/verify-rules.json`
(règle `STD-SDKPIN`, chemin `global.json`), avec cet ADR pour raison.
