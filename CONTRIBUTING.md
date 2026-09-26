# Contributing

Thank you for considering a contribution. This library exists to give Blazor applications accessible, themeable components that run under a strict Content Security Policy; every change is measured against that promise.

## Toolchain

The repository declares a .NET SDK floor in `global.json` (`10.0.100`, `rollForward: latestFeature`): any `10.0` SDK from `10.0.100` works, because the implicit SDK packs are pinned in `Directory.Build.targets` (ADR-002). Restore with the lock files, then build and test in Release:

```powershell
dotnet restore OmniEurope.Blazor.slnx --locked-mode -p:Configuration=Release
dotnet build OmniEurope.Blazor.slnx --configuration Release --no-restore
dotnet test OmniEurope.Blazor.slnx --configuration Release --no-build
```

## What a component pull request includes

- An observable need, described independently of any other library's API.
- A documented public API following `docs/public-api-conventions.md`.
- Rendering, interaction and accessibility tests proportionate to the change.
- Proof that no inline style and no inline script handler is emitted; the CSP contract is in `docs/csp-contract.md`.
- A demonstration in `site/OmniEurope.Blazor.Showcase/Components/Demos`, registered in `Demos/DemoCatalog.cs`, covering the component and every value of its enumerated parameters; the `ShowcaseCoverageTests` and `ShowcaseVariantCoverageTests` guards fail the build otherwise.
- A `CHANGELOG.md` entry whenever public behaviour changes.

## Ground rules

- Nullable reference types, warnings as errors, deterministic Release builds and locked restores stay on. Do not weaken a gate to obtain a pass.
- Razor markup stays declarative; behaviour lives in a same-name `.razor.cs` code-behind.
- Every human-facing string and accessible label is localised through resources or an explicit public text parameter. French and English resources ship together.
- Visual changes are verified in a real browser or WebView2 with a clean console.
