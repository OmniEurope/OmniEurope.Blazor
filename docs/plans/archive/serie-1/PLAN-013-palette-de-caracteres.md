<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-013 : Palette de caractères spéciaux sous l'éditeur HTML

> Statut : **fait**, à archiver après relecture du propriétaire. Établi le 2026-10-03 à la demande du propriétaire (besoin d'une application cliente et d'un outil de
> traduction). API additive ; OE ne contient aucune table de langue.

## Objectif

Sous l'éditeur, l'hôte montre les lettres particulières de la langue en cours de saisie (français é è ê à ç
œ « », allemand ä ö ü ß...) et un clic en insère une au curseur. L'hôte fournit le jeu de caractères ; le
paquet fournit un composant générique, `OmniCharacterPalette`, et un paramètre `Characters` d'`OmniHtmlEditor`
qui le pose sous la surface d'édition.

## Décisions

- `OmniCharacterPalette` : `Characters` (obligatoire, un caractère ou un graphème par élément), `OnSelect`
  (le caractère choisi, dans la casse affichée), `Label` (nom de la barre, « Caractères spéciaux » par défaut),
  `UppercaseLabel` (nom de la bascule), `ShowUppercaseToggle` (vrai par défaut).
- Bascule majuscules (`aria-pressed`) affichée seulement quand au moins un caractère a une forme majuscule
  distincte (`ToUpper` en culture invariante) ; un caractère sans majuscule (ponctuation, ß) reste tel quel.
  Maj+clic donne aussi la majuscule.
- Clavier : `role="toolbar"` et boutons natifs atteignables par Tab, comme la barre de l'éditeur (le paquet
  n'a pas de focus itinérant dans ses barres d'outils). Le `mousedown` des boutons est neutralisé : le clic
  ne retire ni le focus ni la sélection de la surface.
- `OmniHtmlEditor.Characters` (null par défaut, aucune palette) : palette rendue sous la surface en face
  visuelle quand la liste n'est pas vide et que l'éditeur est modifiable ; le caractère passe par l'insertion
  de texte interne (`InsertTextAsync`), une seule étape d'historique, comme une frappe.

## Lots

### Lot 1 - Composant et paramètre de l'éditeur
- [x] `OmniCharacterPalette` (famille Editor), styles dans `10-editors-settings.css` sur les jetons existants,
  cibles de 44 px.
- [x] `OmniHtmlEditor.Characters` et insertion au curseur.
- [x] Textes dans les 24 langues (`AppStrings`).
Contrôle : build Release sans avertissement ; `LibraryTranslationTests` vert.

### Lot 2 - Tests
- [x] bUnit : rendu des caractères, casse selon la bascule et Maj, bascule absente sans forme majuscule,
  `mousedown` neutralisé ; palette de l'éditeur seulement si `Characters` est donné et l'éditeur modifiable,
  clic inséré par `exec("inserttext")`.
Contrôle : nouveaux tests verts, gardes existantes vertes.

### Lot 3 - Vitrine, documentation, portes
- [x] Démonstration dans la page Éditeur de la vitrine (palette seule et éditeur avec `Characters`),
  `ShowcaseStrings` dans les 24 langues.
- [x] `docs/editor-components.md`, `CHANGELOG.md`, `docs/public-api.txt` régénéré par `OmniEurope.PublicApiGuard`.
- [x] Restauration verrouillée, build Release, suite complète, `Test-PublicApi.ps1`, `Test-Csp.ps1`,
  `Test-CspFixtures.ps1`.
Contrôle : suite complète verte avec compteur ; portes API et CSP vertes.

> Fait le 2026-10-03 : build Release 0 avertissement, 0 erreur ; suite complète 4133/4133 (dont
> `CharacterPaletteTests`, 8 tests) ; `Test-PublicApi.ps1` : 11 signatures ajoutées, aucune retirée, 3808 au
> total ; `Test-Csp.ps1` (790 fichiers), `Test-CspFixtures.ps1` et `Test-Budgets.ps1` verts ; le contrôle des règles : PASS. Vitrine vérifiée dans le navigateur (1400 x 900, le lanceur local -s`) : un clic sur « é »
> l'insère au curseur, le focus restant sur la surface ; bascule pressée, les boutons montrent « ÉÈÊ... » et le
> clic insère la majuscule ; Ctrl+Z retire le seul dernier caractère ; palette seule : Maj+clic sur « ä » rend
> « Ä », sur « ß » rend « ß » ; boutons de 44 x 36 px, couche transparente de 4 px en haut et en bas ; console
> sans erreur. Sondes manuelles de la vitrine (`Test-ShowcaseHost.ps1`, densité comprise) non rejouées.
