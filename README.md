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
- **Fully tokenised theme.** Colours, type, spacing, radii, shadows, and the chart palette are CSS variables. Light and dark modes ship out of the box; your own palette is a stylesheet away.
- **Localised from the start.** French and English are included; add any culture through standard .NET resources, or pass your own text to any component.
- **Every Blazor host.** Server, WebAssembly, Interactive Auto, and MAUI Blazor Hybrid, verified on each in continuous integration.
- **No JavaScript framework.** A single static stylesheet and a small interop module. Nothing to bundle, nothing to trust.

## What is in the box

| Family | Highlights |
| --- | --- |
| Actions | Buttons, split buttons, toggle buttons, light/dark appearance toggle |
| Layout | Application shell (layout, header, sidebar, body), rows and columns, grid, stacks, cards, fieldsets, scoped themes |
| Typography | Headings and text with consistent scale and tone |
| Forms | Text, multi-line, numeric and password inputs, checkboxes and switches (nullable too), labels, form fields, template forms, validators |
| Selection | Dropdowns, list boxes, multi-select, autocomplete, radio and checkbox lists, select bars, sliders, date picker, colour picker, upload |
| Data | Data grid with virtualisation, sorting, filtering, grouping, paging, frozen columns and editing; data lists, trees, pagers |
| Navigation | Panel menus, profile menus, sidebars, tabs, steps, breadcrumbs, links |
| Overlays | Dialogs, notifications, tooltips, context menus |
| Feedback | Alerts, badges, icons, images, progress bars, skeletons |
| Charts | Line, area, bar, column, stacked series, pie, donut, arc gauges, with axes, legends, markers, data labels and tooltips |
| Scheduling | Scheduler with day, week and month views, timelines |
| Editor | HTML editor with sanitised output |

## Getting started

Requires .NET 10.

Install the package:

```xml
<PackageReference Include="OmniEurope.Blazor" Version="1.0.0" />
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

## Content Security Policy

The library targets, at minimum, a policy that grants neither `'unsafe-inline'` to `style-src` nor `'unsafe-eval'` to `script-src`. WebAssembly and Interactive Auto hosts add `'wasm-unsafe-eval'` to `script-src` for the .NET runtime itself; nothing broader is ever required.

## License

OmniEurope.Blazor is distributed under the [EUPL-1.2](LICENSE).
