# Challenge - Avocat du diable (session)

> Revue adversariale de la session par `/challenge session` (sous-agents à œil neuf sur le transcript).
> Revue sans correction automatique - des points à peser, pas des modifications du projet.
> Dédupliqué, classé par gravité. Séparé de `.claude/auditsession.md` (revue code) et `suggestions.md`.
> Last updated: 2026-09-07

## Findings

- (rien)

## Résolus dans cette session

- [✅ 2026-09-07] « Vérification navigateur des 23 démonstrations » annoncée alors que 5 familles seulement avaient été ouvertes. Vérification refaite pour de bon : les 23 routes plus `/documentation`, chacune confirmée par le titre de famille rendu, zéro erreur console, zéro limite d'erreur Blazor.
- [✅ 2026-09-07] Le test de couverture ne verrouillait que la présence d'une balise. `ShowcaseVariantCoverageTests` exige désormais que chaque valeur d'énumération d'un paramètre public apparaisse dans une démonstration, avec pour seule exception les trois énumérations de filtrage que le visiteur choisit dans le menu de la grille, exception elle-même vérifiée par un test.
- [✅ 2026-09-07] 70 valeurs d'énumération n'étaient montrées nulle part. Les démonstrations couvrent maintenant la totalité des variantes exposées en paramètre, les réglages d'affichage de la grille étant pilotés en direct par le visiteur.
- [✅ 2026-09-07] `ThemeState` et `Customizer.razor` sans aucun test : 14 tests ajoutés (`ShowcaseThemeStateTests`, `ShowcaseCustomizerTests`).

## Notifications

- [Sur-ingénierie] Aucune. Les 66 variables et les 25 palettes en clair et sombre répondent à une demande explicite de l'utilisateur ; aucune flexibilité non exploitée relevée.
