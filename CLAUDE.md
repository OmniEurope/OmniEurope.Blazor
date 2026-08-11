# Project instructions

## Product

`OmniEurope.Blazor` is a clean-room Razor Class Library that provides accessible Blazor components under a strict CSP. It must not depend on or copy Radzen implementation material.

## Stack and structure

- `src/OmniEurope.Blazor`: the single production RCL and NuGet package.
- `tests/OmniEurope.Blazor.Tests`: bUnit and contract tests.
- `samples`: Server catalog plus WebAssembly, Interactive Auto, and Hybrid smoke hosts.
- `eng`: reproducible validation, inventory, package, CSP, API, and budget gates.
- `docs`: component contracts, compatibility, migration, security, localization, and clean-room evidence.
- `plans`: canonical numbered plans. The active plan is indexed by `.claude/plan.md`.

## Repository overlay

Read `.claude/code-rules.md` before implementation. It is the authority for rules inherited from `_Generic`, RCL-specific adaptations, and non-applicable application rules.

## Validation

Use the guarded .NET runner for restore, build, test, publish, and pack. Never weaken warnings, locked restore, security checks, tests, package checks, or budgets to obtain a pass. Browser and WebView behavior require runtime evidence.

Core commands, passed as arguments to `C:\Users\Woluwe\.codex\tools\invoke-dotnet-guarded.ps1` on Windows:

```text
dotnet restore OmniEurope.Blazor.slnx --locked-mode
dotnet build OmniEurope.Blazor.slnx --configuration Release --no-restore
dotnet test OmniEurope.Blazor.slnx --configuration Release --no-build
dotnet pack src/OmniEurope.Blazor/OmniEurope.Blazor.csproj --configuration Release --no-build --output artifacts/packages
```

Host, CSP, API, package, inventory, and budget commands are catalogued in `.claude/test-config.md` and enforced by `.github/workflows/ci.yml`.

## Documentation map

| Canonical kit responsibility | Project authority | Scope |
| --- | --- | --- |
| `agent-principles.md` | `AGENTS.md`, `docs/agents.md`, this file | Entry points, authority, evidence and prohibited shortcuts |
| `code-rules.md` | `.claude/code-rules.md` | Single `STD-*` registry with every inherited rule marked active, adapted or host-only |
| `roslyn-analyzers.md` | `docs/analyzers.md` | `GEN001` to `GEN008`, wiring and dedicated positive/negative tests |
| `architecture.md` | `docs/architecture.md`, `docs/component-families.md` | RCL boundaries, physical families and host relationships |
| `tech-stack.md` | `docs/dependencies.md`, `Directory.Packages.props`, `global.json` | Toolchain, direct dependencies, servicing and review policy |
| `code-patterns.md` | `docs/public-api-conventions.md`, `docs/ui-conventions.md` | Public component patterns and implementation conventions |
| `coding-standards.md` | `.claude/code-rules.md`, `.editorconfig`, `docs/testing.md` | Enforced code, test and quality standards |
| `testing.md` | `.claude/test-config.md`, `docs/testing.md`, `docs/browser-scenarios.json` | Unit, integration, browser and WebView evidence |
| Clean-room evidence | `docs/clean-room.md`, `docs/clean-room-component-sheet.md`, `docs/reproducibility.md` | Prohibited inputs, corpus provenance and reproducibility |
| Components and scenarios | `docs/component-coverage.md`, `docs/component-contracts.md`, `docs/catalog-scenarios.json`, family guides | Component surface, contracts and executable examples |
| Runtime quality | `docs/compatibility.md`, `docs/csp-contract.md`, `docs/accessibility-contract.md`, `docs/localization.md`, `docs/performance-budgets.md` | Hosts, CSP, accessibility, localization and budgets |
| Delivery and evolution | `docs/versioning.md`, `docs/migration-guide.md`, `docs/migration-aetheus.md`, `plans/` | Versioning, migrations and canonical numbered plans |

The kit's application deployment and database pattern documents are not applicable to this package-only RCL; host runtime evidence is local and no production deployment target is defined here.

The RCL intentionally has no Radzen dependency and no compatibility promise with Radzen internals. Public observations may inform requirements, but Radzen source, CSS, JavaScript, tests, comments, generated code, and assets are prohibited inputs. Architectural decisions already traced in the documents above do not require duplicate ADRs; create an ADR only for a new explicit decision with durable alternatives and consequences.

## Session resume

On startup, if `.claude/handoff.md` exists, read it to restore context from the previous session.
