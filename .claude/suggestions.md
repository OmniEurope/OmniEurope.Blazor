# Suggestions Backlog

> Deduplicated, effort-tagged, stable-ID backlog collected by `/next` and `/suggestions`.
> Tags: size 🟢 quick · 🟡 medium · 🟠 significant · 🔴 major - priority low / med / high.
> Only the 4 substantive categories are produced; no "Suite" / next-action section.
> Last updated: 2026-08-29

## Améliorations techniques
- [S-TECH-C4V8] [🟠 significant · high] Instaurer des fiches clean-room datées pour chaque évolution future sans fabriquer de preuves historiques rétroactives.
- [S-TECH-I7K2] [🟢 quick · med] Créer un `THIRD-PARTY-NOTICES.md` à la racine avant d'intégrer le moindre tracé d'un jeu d'icônes tiers, pour que l'obligation de conservation du texte de licence (ISC pour Lucide, MIT pour Phosphor et Heroicons) soit satisfaite dès le premier `<path>` copié dans `OmniIcon.razor`. _(2026-08-29)_
- [S-TECH-Q3M9] [🟡 medium · med] Étendre `OmniIconName` en une passe unique alignée sur les besoins réels de `OmniPanelMenu`, `OmniTabsItem`, `OmniContextMenu` et des menus de filtre, plutôt qu'icône par icône : chaque ajout étant une modification de `docs/public-api.txt`, les grouper limite le bruit sur la baseline. _(2026-08-29)_
