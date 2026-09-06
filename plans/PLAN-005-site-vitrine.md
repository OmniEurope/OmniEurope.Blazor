# PLAN-005 - Site vitrine, documentation et personnalisation de theme

> Precedent : `plans/PLAN-004-grille-complete.md`, qui reste ouvert derriere celui-ci.
> Cree le : 2026-08-29, clos le : 2026-09-06
> Portee : un site vitrine statique, sans base de donnees ni backend, servant de vitrine produit,
> de documentation, d'explorateur de composants facon Kendo UI et de personnalisateur de theme.

## Decisions actees

- Technologie : Blazor WebAssembly statique, hebergeable sans serveur applicatif.
- Emplacement : nouveau projet dedie, `site/OmniEurope.Blazor.Showcase`.
- Personnalisateur : apercu en direct, export du CSS de theme, persistance cote navigateur.
- Prealable : passe de variabilisation des tokens avant toute ligne du site.
- Themes : reprise des 25 palettes Bootswatch (licence MIT), chacune declinee en clair et en sombre.
- Typographie : police systeme uniquement, aucune police web, aucune famille portee par les palettes.

## Contraintes connues

Aucune palette Bootswatch ne fournit les deux modes : la moitie des 50 palettes cibles est une
derivation maison, etiquetee comme telle dans le selecteur. Certaines palettes amont ne passent pas
les ratios WCAG : les valeurs sont alors deplacees jusqu'a les franchir, la lisibilite primant sur la
fidelite, et les tests tiennent cette ligne.

## Phase 1 - Variabilisation complete [done]

66 tokens dans `:root`, contre 25 avant la passe. Zero couleur, taille de police ou arrondi codes en
dur hors definitions de token.

## Phase 2 - Socle du site [done]

- [x] Projet WebAssembly statique, ajoute a la solution sous `/site/`.
- [x] Mise en page a quatre menus : vitrine, documentation, composants, personnalisation.
- [x] Publication statique verifiee : `wwwroot` autonome, `_headers` de politique de securite,
      variantes precompressees, aucune dependance serveur.

## Phase 3 - Explorateur de composants [done]

- [x] 23 familles couvrant **109 composants sur 109**, seul `OmniComponentsHost` etant exclu parce
      qu'il se monte une fois a la racine de l'application.
- [x] Rendu vivant, capacites listees et code source affiche.
- [x] Le code affiche est le fichier compile lui-meme, embarque comme ressource et relu par le type
      du composant, jamais une transcription.
- [x] Tests : couverture exhaustive verrouillee, chaque demonstration rendue, chaque demonstration
      verifiee conforme a la politique de securite.

## Phase 4 - Personnalisateur et catalogue de themes [done]

- [x] Les 66 tokens sont editables, la liste etant lue dans la feuille de style livree.
- [x] Application en direct par le CSSOM, seule voie compatible avec `style-src 'self'`.
- [x] Export du CSS des seules variables deplacees, copie presse-papiers et telechargement.
- [x] Persistance du reglage dans le navigateur, tolerante au stockage indisponible.
- [x] 25 palettes en clair et en sombre, moitie derivee et etiquetee.
- [x] Controle de contraste automatise sur les 50 palettes-modes.

## Phase 5 - Typographie [done]

- [x] `--omni-font-family` porte la pile systeme et est appliquee par `.omni-theme-scope`.
- [x] Aucune palette ne porte de famille de police, aucune police web n'est embarquee ni chargee.
- [x] Verifie en navigateur : zero requete de fichier de police.

## Etat de cloture

Toutes les phases sont livrees et verifiees. Le plan est clos.
