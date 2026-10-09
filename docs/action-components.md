# Actions (famille Actions)

Les actions déclenchent quelque chose : un bouton natif (`button type="button"` par défaut), aux trois mêmes tailles dans toute la famille ; `OmniButton` et `OmniSplitButton` partagent les variantes d'emphase (`Variant`), que `OmniToggleButton` n'a pas. Un bouton à icône seule est carré et doit recevoir un nom accessible (`Label`) ; il mesure la hauteur de contrôle de la densité, sans zone transparente de 44 px (décision R-025, voir [ui-conventions.md](ui-conventions.md)).

| Composant | Rôle |
| --- | --- |
| `OmniButton` | Bouton d'action, avec variantes d'emphase et d'intention (principal, secondaire, discret, danger...), icône avant le texte ou seule, état occupé et pastille décorative d'attente (notifications non lues). |
| `OmniToggleButton` | Bouton à deux états (`aria-pressed`) : une option qu'on active ou désactive sans quitter la page, comme le gras d'une barre d'outils. |
| `OmniSplitButton` | Bouton scindé : la partie principale lance l'action principale, le chevron ouvre un menu des autres actions. Au clavier, les flèches ouvrent le menu sur sa première ou sa dernière entrée et le parcourent, Échap le ferme et rend le focus au chevron ; choisir une entrée ferme le menu avant de lancer son action. Entrée sur la partie principale lance l'action sans ouvrir le menu ; `OnClick` reçoit un `MouseEventArgs`, `Open`/`OpenChanged` pilotent le menu. |

Les entrées d'un menu de bouton scindé sont les `OmniMenuItem` des autres menus du paquet (voir [overlay-components.md](overlay-components.md)).

Le mode clair, sombre ou système se choisit dans `OmniAppMenu` ([navigation-components.md](navigation-components.md)), dans les réglages d'apparence ([foundation-components.md](foundation-components.md), « Réglages d'apparence réutilisables »), ou avec un `OmniButton` nommé qui change `OmniThemeScope.Appearance` ; un thème `DarkOnly` reste sombre et un thème `LightOnly` reste clair quel que soit ce choix.

Démontré dans la vitrine (page Boutons) ; les conventions de bouton de dialogue sont dans [ui-conventions.md](ui-conventions.md).
