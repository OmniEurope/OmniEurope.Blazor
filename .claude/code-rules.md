# OmniEurope.Blazor code rules overlay

This repository is a reusable Razor Class Library (RCL), not a hosted business application. These rules adapt the `_Generic` kit without weakening its security, accessibility, testing, or authenticity requirements.

This file is the canonical, machine-discoverable `STD-*` registry for this repository. Every inherited kit ID appears exactly once below with an explicit applicability and enforcement layer; the detailed sections that follow explain only project-specific boundaries.

## Registry

| ID | Project rule | Layer | Severity | Applicability |
| --- | --- | --- | --- | --- |
| `STD-PARTIAL` | No partial production type except verified Razor/XAML code-behind, migrations or a known source-generator contract | `analyzer:GEN008` + audit | high | Active |
| `STD-RADZEN` | Keep the product independent and clean-room: no Radzen dependency, copied source, generated output, CSS, JavaScript, tests, comments or assets | audit + corpus gates | high | Adapted, replaces Radzen-first |
| `STD-STYLE` | No inline/local component styles; package UI CSS lives in `omnieurope.blazor.css`, host CSS in each host's global stylesheet | CSP gate + audit | medium | Active |
| `STD-I18N` | Localize every visible string and accessible label through resources or an explicit public text parameter | tests + audit | high | Active |
| `STD-UIVERIFY` | Verify every visual change in Chromium or WebView2 with a clean console | runtime gates + audit | high | Active |
| `STD-FOCUS` | No implicit startup/navigation focus; explicit interaction focus must restore predictably | browser tests + audit | medium | Adapted |
| `STD-BTN` | Explicit native type, localized accessible name, semantic role styling and a 44 px target | split tests + audit | medium | Adapted |
| `STD-DIALOG` | Dialog primitive provides close, Escape, focus containment/restoration; consumers own business confirmation actions | split tests + audit | medium | Adapted |
| `STD-GRID` | `OmniDataGrid` sorting/filtering/paging/actions must remain functional and accessible | component tests + audit | medium | Adapted |
| `STD-FORM` | Inputs update predictably and localized validation/business failures remain contained | component tests + audit | high | Active |
| `STD-NAV` | Route-to-menu completeness belongs to routed hosts; the route-free RCL proves only reusable active-state behavior | host runtime + audit | medium | Host-only |
| `STD-GRIDTITLE` | Grid headings remain readable without wrapping regressions | CSS/browser audit | low | Active |
| `STD-TABS` | Tabs expose controlled state and keyboard behavior; URL synchronization is host-owned | component/browser tests | medium | Adapted |

## Inherited without change

- Keep nullable reference types, warnings as errors, deterministic Release builds, locked restores, and no weakened gates.
- Keep Razor markup declarative. Component behavior belongs in a same-name `.razor.cs` code-behind unless the file is markup-only.
- Do not emit inline `style`, raw HTML event handlers, dynamic JavaScript evaluation, or unapproved remote resources.
- Localize human-facing text and accessible labels through `IStringLocalizer<AppStrings>` or an explicit public text parameter.
- Preserve keyboard, focus, ARIA, contrast, CSP, and browser verification requirements.
- Implement from public requirements and clean-room observations only. Never copy Radzen source, generated code, CSS, JavaScript, tests, comments, or assets.

## Adapted for this RCL

- The kit's normal Radzen UI baseline is not applicable: the product exists to provide an independent clean-room replacement. This exception authorizes no Radzen dependency or source reuse.
- Keep one RCL package and one public component namespace. Do not introduce Domain/Application/Infrastructure projects without a proven second responsibility.
- Public component APIs require semantic baseline and package verification. Internal render contexts should remain internal unless a consumer requirement proves otherwise.
- Host-specific behavior belongs in samples or host extensions. The RCL must remain compatible with Server, WebAssembly, Interactive Auto, and MAUI Blazor Hybrid.
- Resource defaults live in the RCL; hosts may add cultures and override context-specific public text parameters.
- Razor files compiled into shipped projects must pass GEN004 with code-behind enforcement. Single-file Razor fixtures under the test project may retain local inline state when that state exists only to arrange a render test; they are not shipped components and remain covered by the test suite itself.

## Not applicable

- Database migrations, repositories, persistence entities, authentication flows, HTTP API controllers, and remote deployment rules are not RCL concerns unless a future scoped feature introduces them.
- A repository interface, message bus, or application layer per component is prohibited without a measured need.

## STD boundary overrides for the RCL

- `STD-NAV` route completeness is host-owned and is not applicable to this route-free RCL. The library gate named `PanelMenuItemLocationSubscriptionGuard` proves only URI normalization and subscription cleanup for the reusable menu item; Catalog route coverage is verified by its runtime probe.
- `STD-BTN` is enforced by separate, accurately named proofs: `NativeButtonTypeAttributeGuard` covers explicit native button types, component tests cover icon/text and accessible names, and the CSS/budget gates cover the 44 px target and role colors. Cross-component visual coherence remains a browser/audit concern rather than a single over-claimed unit test.
- `STD-DIALOG` is split at the package boundary. `DialogPrimitiveAccessibilityGuard` covers the reusable primitive's dialog semantics, Escape handling and focus interop. Business confirmation wording, destructive actions and opt-outs belong to consumers; the Catalog runtime proves nested closing and an explicit harmless footer action.

## Required gates

- Build and test through the guarded .NET runner documented in the global instructions.
- Run `eng/Test-Csp.ps1`, `eng/Test-CspFixtures.ps1`, public API, package, inventory, and budget gates for affected surfaces.
- Use browser or WebView runtime evidence for behavior that compilation and bUnit cannot prove.
- Record audit remediation against `plans/PLAN-003-correction-findings-audit.md`.
