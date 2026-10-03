# OmniEurope.Blazor

**Accessible, themeable Blazor components that run under a strict Content Security Policy.**

[![NuGet](https://img.shields.io/nuget/v/OmniEurope.Blazor.svg)](https://www.nuget.org/packages/OmniEurope.Blazor)
[![License: EUPL-1.2](https://img.shields.io/badge/license-EUPL--1.2-blue.svg)](https://github.com/OmniEurope/OmniEurope.Blazor/blob/main/LICENSE)

More than a hundred components, one static stylesheet, no inline style, no `unsafe-eval`, and a theme you drive end to end with CSS variables. Built for teams that lock down their applications and refuse to trade security or accessibility for a nicer widget.

- **Demo site**: coming soon
- **Documentation**: coming soon
- **Package**: https://www.nuget.org/packages/OmniEurope.Blazor

## Why OmniEurope.Blazor

- **Strict CSP by design.** Components never emit `style` attributes, `<style>` tags, or inline event handlers. `default-src 'self'; script-src 'self'; style-src 'self'` is a tested target, not an aspiration.
- **Accessibility built in.** Keyboard navigation, focus containment and restoration, ARIA states, 44 px interactive targets, and reduced-motion support are part of every component, not an option you enable.
- **Fully tokenised theme.** Colours, type, spacing, radii, shadows, and the chart palette are CSS variables. Light and dark modes ship out of the box, with fifteen themes (shape) and fifteen palettes (colour) that combine freely and a three-step density; your own palette is a stylesheet away.
- **Localised from the start.** The texts ship in the 24 official languages of the European Union (French as the neutral culture, then English, Bulgarian, Croatian, Czech, Danish, Dutch, Estonian, Finnish, German, Greek, Hungarian, Irish, Italian, Latvian, Lithuanian, Maltese, Polish, Portuguese, Romanian, Slovak, Slovenian, Spanish and Swedish); override any text through standard .NET resources, or pass your own text to any component.
- **Every Blazor host.** Server, WebAssembly, Interactive Auto, and MAUI Blazor Hybrid, verified on each in continuous integration.
- **No JavaScript framework.** A single static stylesheet and a few small JavaScript modules, each loaded on demand by the component that needs it. Nothing to bundle, nothing to trust.

## What is in the box

| Family | Highlights |
| --- | --- |
| Actions | Buttons, split buttons, toggle buttons, overflow menus; one `OmniMenuItem` for every menu of the package |
| Layout | Application shell (layout, header, sidebar, body), rows and columns, stacks, cards, fieldsets, scoped themes |
| Theming | Appearance settings and a movable appearance window (mode, theme, palette, font, text size, density, control size) |
| Typography | Headings and text with consistent scale and tone |
| Forms | Text, multi-line, numeric and password inputs, checkboxes and switches (nullable too), labels, form fields, template forms with an unsaved-changes guard, form action rows, schema-driven forms, validators |
| Selection | Dropdowns, list boxes, multi-select, autocomplete, radio and checkbox lists, select bars, selectable cards and card groups, rating, sliders, date, time and date-time pickers, colour picker, upload |
| Data | Data grid with virtualisation, sorting, filtering, grouping, paging, frozen columns, editing and a loading bar; data lists, trees, pagers, spreadsheet, kanban board, log viewer, Markdown export |
| Navigation | Panel menus, profile menus, application menu, sidebars, tabs, steps, breadcrumbs, links |
| Overlays | Dialogs, notifications, tooltips, popovers, context and overflow menus |
| Feedback | Alerts, badges, status badges and strips, icons, images, progress and loading bars, logo loader, skeletons |
| Charts | Line, area, column and horizontal bar series (stackable), pie and donut, arc gauges, with axes, legends, markers, data labels and an accessible data table |
| Scheduling | Scheduler with day, week and month views, timelines, Gantt chart, step timeline |
| Editor | Extensible HTML editor with sanitised output, code editor, diff viewer, code viewer and code block, unified diff |
| Diagram | Mind map, layered graph layout, commit graph |
| Pages | Page header with breadcrumb service, detail shell, login shell, connection overlay, empty state, stat tile, wizard, description list, relative time |

## Getting started

Requires .NET 10.

Install the package:

```xml
<PackageReference Include="OmniEurope.Blazor" Version="1.5.0" />
```

Register the services:

```csharp
builder.Services.AddOmniEuropeBlazor();
```

Reference the stylesheet in your host page:

```html
<link rel="stylesheet" href="_content/OmniEurope.Blazor/omnieurope.blazor.css" />
```

Import the namespace in `_Imports.razor`:

```razor
@using OmniEurope.Blazor.Components
```

Use a component:

```razor
<OmniButton Variant="OmniButtonVariant.Primary" OnClick="SaveAsync">
    Save
</OmniButton>
```

## Unknown parameters

The components capture the attributes they do not declare and write them on their markup, so a parameter that was removed or misspelled would otherwise become an HTML attribute without a word. Two guards stop it:

- **At build time**, the package ships a Roslyn analyzer. `OE0001` is an error, reported at its line in the `.razor` file, for a PascalCase attribute that an OmniEurope.Blazor component has no parameter for: `OmniBadge has no parameter 'IconName'`. It also reports a lowercase `class` or `id` (write `Class` and `Id`) and any unknown attribute on a component that captures none. Other lowercase HTML attributes (`aria-*`, `data-*`) and `@attributes` splats are never reported.
- **At render time**, the component throws `InvalidOperationException` with the same message for such an attribute, including one that only exists at run time (a dictionary given to `@attributes`), and refuses `class` and `id` in any casing.

A `ProjectReference` to the library does not bring the analyzer packed in the NuGet package. A project that references the source adds one line beside that reference:

```xml
<ProjectReference Include="path/to/src/OmniEurope.Blazor.Analyzers/OmniEurope.Blazor.Analyzers.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

## Content Security Policy

The library targets, at minimum, a policy that grants neither `'unsafe-inline'` to `style-src` nor `'unsafe-eval'` to `script-src`. WebAssembly and Interactive Auto hosts add `'wasm-unsafe-eval'` to `script-src` for the .NET runtime itself; nothing broader is required by the rest of the library.

One opt-in exception: `OmniCodeEditor` and `OmniDiffViewer` with their default `Monaco` engine load a Monaco editor served by the host, and Monaco writes `style` elements at runtime, so that page needs `style-src 'unsafe-inline'`. Under the strict policy, `Engine="OmniCodeEditorEngine.PlainText"` keeps a plain text area without Monaco, which is also the fallback when Monaco fails to load. See [docs/contracts/csp-contract.md](docs/contracts/csp-contract.md).

## License

OmniEurope.Blazor is distributed under the [EUPL-1.2](LICENSE).
