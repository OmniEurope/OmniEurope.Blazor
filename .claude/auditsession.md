# Session Audit - Follow-ups

> Deferred follow-ups raised by `/audit session` (the self-review run as `/next`'s first step).
> Each item is work Codex can execute end-to-end in a later scoped run; no item delegates manual work to the user.
> Deduplicated and effort-tagged; resolved items are pruned on later runs.
> Separate from `.claude/suggestions.md` (kit improvement backlog).
> Tags: size 🟢 quick · 🟡 medium · 🟠 significant · 🔴 major - priority low / med / high.
> Last updated: 2026-09-06

## Follow-ups

- [🟡 medium · high] Couvrir `ThemeState` et la page `Customizer` par des tests - toute la logique du personnalisateur (`SetAsync`, `ApplyAsync`, `SetModeAsync`, `ExportCss`, `Restore` avec son `try/catch` JSON) n'a aucun test unitaire, et `Customizer.razor` n'a aucun test de rendu, alors que PLAN-005 déclare cette phase livrée et vérifiée. Une régression sur l'export CSS, la bascule de mode ou la restauration du stockage passerait inaperçue. Remède : un test unitaire de `ThemeState` avec un `IJSRuntime` simulé couvrant les cinq opérations, plus un test bUnit de rendu de `Customizer.razor`. _(raised 2026-09-06 · `site/OmniEurope.Blazor.Showcase/Theming/ThemeState.cs:1`)_
