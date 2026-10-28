<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-005 : galerie de la vitrine reprise de la démo d'une application cliente

Ouvert le 2026-10-27. Demande du propriétaire : rapatrier dans la vitrine
(`site/OmniEurope.Blazor.Showcase`) la démo OE d'une application cliente (71 pages, 15 familles,
écrite contre OE 1.5.0), avec pour chaque page trois onglets : aperçu normal, aperçu mobile
(écran de téléphone 360 × 780) et **Code** (sources réellement fonctionnelles, à la manière d'un
« View Source »). La démo est ensuite retirée de l'application cliente par un prompt séparé.

## Décisions du propriétaire (2026-10-27)

1. Langues de la vitrine : **anglais (neutre, par défaut) et français**, au choix dans l'en-tête.
   Les 22 autres langues sont retirées ; plus aucun test de couverture des langues.
2. Routes en anglais (`/components/buttons`, `/documentation`, `/customizer`).
3. Recoupements : quand une démo existe des deux côtés, la plus riche est gardée, l'autre supprimée.
4. Code : chaque page est découpée en une page de vitrine (titre, réglages, onglets) et un ou
   plusieurs composants « exemple » autonomes, compilés dans un projet qui ne référence qu'OE.
   Les textes des exemples sont en anglais en dur ; l'habillage de la page et les textes internes
   d'OE suivent la langue choisie.
5. Tout est fait, par lots vérifiés.
6. La vitrine est une **application déployée**, pas un exemple : front WebAssembly + API ASP.NET Core
   + PostgreSQL, sur le modèle web du kit de développement du propriétaire (Front, Back, Shared), sans authentification.
   Tous les kits s'appliquent : les règles d'hôte que le dépôt marque non applicables le deviennent
   pour la vitrine. Le déploiement suit les conventions de déploiement du propriétaire (Compose,
   `PORT_FRONT`/`PORT_BACK` sur `127.0.0.1`, health checks, SDK tiré de `global.json`, `.pipeline`).
   Le projet Examples n'est qu'une bibliothèque interne au site (le code de l'onglet Code).

## Architecture cible (contrat commun à tous les lots)

- `site/OmniEurope.Blazor.Showcase.Examples` : bibliothèque Razor, seule référence
  `src/OmniEurope.Blazor`. Un dossier par famille, un exemple = `<Name>Example.razor` +
  `.razor.cs` (+ données d'exemple `.cs`). Aucun `IStringLocalizer`, aucun type de la vitrine.
  Chaque fichier source est embarqué sous son chemin relatif : l'onglet Code affiche le fichier
  compilé, jamais une recopie.
- `site/OmniEurope.Blazor.Showcase/Components/Gallery/<Family>/<Name>Page.razor` : la page, son
  `@page "/components/<slug>"`, ses réglages, et `GalleryPageFrame` (onglets Preview, Mobile, Code).
- Catalogue : une famille par fichier, assemblées par `GalleryCatalog`. Chaque page déclare ses
  composants éprouvés et ses fichiers sources.
- Textes : une ressource par famille (neutre anglaise + `.fr`), l'habillage commun dans
  `ShowcaseStrings`.

## Lots

Preuve de chaque lot : build Release sans avertissement, tests `Showcase*` verts, chaque page du lot
ouverte dans le navigateur intégré sur ses trois onglets, en anglais et en français, sans erreur
console ; le code d'au moins un exemple vérifié compilable seul (il l'est par construction du projet
Examples).

| Lot | Contenu | Démos existantes absorbées | État |
|---|---|---|---|
| 0 | Fondation : projet Examples, cadre à trois onglets, vue Code, catalogue, EN/FR, routes anglaises, tests réécrits, page pilote Buttons | `boutons` | Fait |
| 1 | Split buttons, toggle buttons, alerts, progress, skeletons | `etats`, `alertes` (`chargement`, `retours` restent) | Fait |
| 2 | Dialogs, context menus, overflow menu, popover | `superpositions` en partie | Fait |
| 3 | Theme, icons, typography, badges, images | `themes`, `typographie`, `badges`, `medias` | Fait |
| 4 | Stacks, rows and columns, cards, fieldsets, settings sections | `mise-en-page`, `surfaces`, `accordeon` | Fait |
| 5 | Page shell, details, stat tiles, nav tiles | `pages`, `coquille`, `coquille-application` | Fait |
| 6 | Sidebar, window controls, loading bar, notifications, tooltips | `notifications` | Fait |
| 7 | Tabs, breadcrumbs, steps, profile menu, app menu | `navigation`, `reglages-onglets` (`parcours` passe au lot 16) | Fait |
| 8 | Text fields, numbers, toggles, validation, unsaved changes | `formulaires`, `formulaire-vertical`, `validation`, `saisie-avancee`, `reglages-onglets` | Fait |
| 9 | Drop-downs, autocomplete, multi-select, lists, radio buttons | `selection` | Fait |
| 10 | Select bar, selectable cards, dates, colors, slider, upload | `choix` | Fait |
| 11 | Data grid (colonnes, détail), data list | `grille`, `grilles-maquette`, `grille-avancee`, `listes` | À faire |
| 12 | Tree, spreadsheet, collections | `arborescence`, `tableur` | À faire |
| 13 | Column, line, pie charts, gauges | `graphiques`, `graphiques-avances` | À faire |
| 14 | Scheduler, timeline, Gantt, step timeline | `agenda`, `planification` | À faire |
| 15 | HTML editor, document editor, code editor, code reading, mind map | `editeur`, `diagramme` | À faire |
| 16 | Workflows, préférences (enregistrées par l API), accueil de la galerie, retrait de l'ancienne galerie et de `DemoCatalog` | (reste) | À faire |
| F | Fidélité (audit du 2026-10-28) : thème transmis à la portée racine, menu de galerie à la place du menu du site, accueil en cartes, sélecteur de thème avec bascule clair/sombre, espacement des cartes d'aperçu ; habillage perdu des démos (cadres pointillés, matrices, tuiles d'icônes, cadre des images, zones de clic droit, lignes à survol, grilles d'exemples adaptées au conteneur, largeurs plafonnées, curseur vertical, bascule du menu latéral à la fin, infobulle épinglée par défaut, tableau des fichiers) | | En cours |
| H1 | Hébergement : projets Back (API, EF Core, PostgreSQL, `/health/ready`) et Shared, préférences OE enregistrées en base sans compte (identifiant anonyme du navigateur, quotas), front servi en conteneur, le lanceur local démarre base, API et front | | Fait |
| H2 | Déploiement : Dockerfiles, Compose de production, `.pipeline`, overlay `docs/code-rules.md` (règles d'hôte actives pour la vitrine), CI ; preuve : contrôle de conformité du déploiement sans écart et pile Compose démarrée en local | | À faire |
| K | Règles du kit : corriger les constats du contrôle des règles mécanisées du kit (116 au 2026-10-27, dont STD-FILESIZE, STD-I18N, STD-OE, STD-STYLE, STD-TABS) sur la vitrine et ses nouveaux projets | | À faire |
| 17 | Vérification complète (suite, publication, toutes les pages), documentation, prompt de retrait pour l'application cliente | | À faire |

Vagues : lot 0 seul, puis H1 avec les lots 1 à 3, H2 avec les lots 4 à 6, lots 7 à 10, 11 à 14, 15 et 16 (4 en parallèle au plus, chacun dans son
worktree) ; aucune vague ne démarre avant que la précédente soit intégrée et prouvée.

## Journal

- 2026-10-27 : plan ouvert, inventaire fait (71 pages, environ 2 500 textes, 2 670 lignes de CSS).
- 2026-10-27 : lot 0 livré (`19e78c72`). Preuve : build Release sans avertissement, 228 tests
  `Showcase*` verts, `/components/buttons` vu dans le navigateur sur Aperçu, Mobile (cadre 360 × 780) et
  Code (fichiers réels), en anglais et en français, console sans erreur. Les infobulles restent dans la
  famille Shell (lot 6), le lot 2 a donc quatre pages. Vague 1 lancée : H1, lots 1 à 3.
- 2026-10-27 : lots 1 et 2 fusionnés. Preuve : build Release sans avertissement, 304 tests
  `Showcase*` + `ConventionGuardTests` verts, les 10 pages ouvertes dans le navigateur sur leurs trois
  vues sans erreur ni source manquante, dialogue ouvert et refermé par Échap, menu contextuel ouvert au
  clic droit. Restent dans l'ancienne galerie : `chargement` et `retours` (lot 6, badges au lot 3),
  `superpositions` (bouton scindé, menu de profil au lot 7, notifications au lot 6).
- 2026-10-27 : lot 3 fusionné (`7237e441`, conflits de suppression résolus dans `app.css`,
  `DemoCatalog.cs`, `Customizer.razor.cs`). Preuve : build Release sans avertissement, 324 tests
  `Showcase*`, `ConventionGuardTests`, `PublicContentGuardTests` verts ; les 5 pages ouvertes dans le
  navigateur sur leurs trois vues sans erreur console. Le plan nommait des outils privés (garde du
  contenu public) : formulation neutralisée. Le lanceur local signale 116 constats des règles du kit :
  lot K ajouté.
- 2026-10-27 : H1, lots 4, 8 et 9 fusionnés. Preuve : build Release sans avertissement, `restore
  --locked-mode` PASS, 431 tests `Showcase*`/conventions/contenu public et 64 tests de l'API (dont
  PostgreSQL réel par Testcontainers) verts ; pile complète lancée par le lanceur local (base, API,
  vitrine) : appels `OPTIONS`/`GET /api/preferences/omni.appearance` en 204 depuis la vitrine, sans
  erreur CORS ; les 18 pages des lots 4, 8 et 9 ouvertes sur leurs trois vues sans erreur ;
  autocomplétion « Par » → « Paris, France ». Défaut trouvé et corrigé : la vitrine n'avait pas
  d'`OmniComponentsHost`, la garde de modifications retombait sur `window.confirm` ; le dialogue OE
  s'ouvre maintenant et « Quitter sans enregistrer » navigue. Workflows et préférences passent au lot 16
  (même famille que le lot 6).
- 2026-10-28 : lots 5, 6, 7 et 10 repris des WIP interrompus et fusionnés dans `develop` (`1771a0c8`,
  `c619450d`, `4ea876ec`, `148e6ed3`). Démos absorbées : `pages`, `coquille`, `coquille-application`,
  `notifications`, `navigation`, `reglages-onglets`, `choix` ; `parcours` (assistant, connexion) reste
  pour le lot 16. Preuve : build Release sans avertissement, suite complète 10633 tests verts ; les
  22 pages (page-shell, details, stat-tiles, nav-tiles, sidebar, window-controls, loading-bar,
  notifications, tooltips, tabs, breadcrumbs-links, steps, profile-menu, app-menu, select-bar,
  selectable-cards, dates, colors, slider, upload, autocomplete, radio-buttons) ouvertes sur leurs
  trois vues dans le navigateur, 66 vues sans erreur console. Non vérifiés : le français de ces pages,
  les interactions page par page et les sondes repointées (`chevrons`, `focus`, `overlays`, `keys`,
  `leaks`, `requests`, modules, contraste), à passer avant la release.
