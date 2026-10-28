<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-006 - Demandes d'une application cliente du 2026-10-28

Neuf demandes génériques issues de la recette d'une application cliente. Chaque lot se termine par un
contrôle mesurable ; un point n'est « fait » qu'avec sa preuve observée (navigateur ou test réel).

| Lot | Points | Contenu | Contrôle |
|---|---|---|---|
| A | R-5, R-3, R-67 | `OmniBadge` : jamais plus étroit que haut ; distinct d'un bouton ; `OnClick`/`Href` en bouton accessible, ton Accent | largeur >= hauteur mesurée sur « 1 », « F » ; badge cliquable activé au clavier (Entrée, Espace) |
| B | R-12, R-57 | `OmniFormField` : hauteur de l'erreur réservée ; option de tuile à texte ajusté sur une ligne, aligné à gauche | position du champ suivant inchangée à l'apparition de l'erreur ; « Environnements » sur une ligne entière dans 6 colonnes |
| C | R-52, R-4 | `OmniDataGrid` : filtre de date souple en ligne (jj/mm/aaaa, mm/aaaa, aaaa, une borne), année à 4 chiffres, filtre avancé Du/Au dans le menu, option par colonne ; en-têtes visibles et pas de défilement pendant la mesure | tests de l'analyseur de dates ; en-têtes visibles au premier rendu |
| D | R-44, R-73 | Trou noir : halo comparé à la version précédente ; étoiles nettes | rendu comparé dans le navigateur |
| E | R-16 | Infobulles décalées (défilement, dialogue, redimensionnement), points de suspension sans infobulle à 375 px | instrumentation puis position mesurée collée à son ancre |
| F | R-26 | `OmniPageHeader` sur téléphone : titre, fil d'Ariane, bouton principal (`PinnedActionsContent`), contenu | ordre mesuré à 375 px |

## Journal

- 2026-10-28 : plan ouvert, lots A à D délégués en worktrees, lot E traité par l'orchestrateur.
- 2026-10-28 : livrés et prouvés sur `develop` (release prévue : 1.10.0).
  - R-5 : badges « 1 » et « F » mesurés 25 × 22 px dans la vitrine, jamais plus étroits que hauts.
  - R-67 (`91e60b87`, `796dc286`, nom accessible `Label`) : Entrée puis Espace sur le badge bouton
    font passer le compteur de 0 à 2, anneau de focus visible.
  - R-12 (`e26978a2`, `745e392a`) : décalage du champ suivant mesuré à 0 px sur les deux formulaires
    de la page de validation (avant : 20 px avec `OmniValidationMessage` ou `ValidationMessage`).
  - R-57 (`OmniNavTile.FitTitle`) : à 1440 px, six colonnes de 139 px, titres sur une ligne sans
    coupe, alignés à gauche ; démonstration déplacée dans l'exemple `NavTilesFitTitleExample`.
  - R-44, R-73 (`2ce9025f`) : halo retiré, étoiles comparées à l'ancienne version sur un banc
    (pixels au-dessus de 10 % du pic 12,1 % → 4,4 %, pic 101 → 187 en sombre).
  - R-16 (`235a4341`) : décalage reproduit (bouton remonté de 100 px, infobulle restée 88 px sous
    lui) ; avec le correctif l'infobulle se ferme. Texte coupé à 375 px : infobulle observée au survol
    d'un libellé de `OmniStatTile`. Cause exacte dans l'application cliente non reproduite.
  - R-26 (`c3cc9479`) : à 375 px, titre (3 px) → fil d'Ariane (47) → bouton épinglé (71).
  - R-52 (`53734fdf`, `c68bcfe4`) : dans la vitrine, `09/03/2026` garde une ligne, `03/2026` et
    `2026` gardent mars, `2025` aucune ; `20000` et `31/02/2026` marquent `aria-invalid` sans filtrer ;
    une seule borne Du (`2026-03-10`) garde les lignes à partir du 10 et la saisie affiche
    `10/03/2026 -` ; sélecteurs bornés `0001-01-01` à `9999-12-31`. Non vu en navigateur : le bouton
    calendrier de la rangée en ligne (couvert par bUnit). Défaut : une colonne de date reçoit `Date`.
  - R-4 (`b7edc1b0`) : sur la grille chargée par `Load`, séquence observée : `aria-busy` vrai, en-têtes
    visibles (`data-omni-headed`), lignes masquées, viewport `overflow: hidden`, puis lignes
    visibles et `overflow: auto` une fois `data-omni-sized` posé. Durées non mesurables (panneau
    navigateur masqué, minuteries ralenties).
  - Gardes rattrapées à l'intégration : couverture de la vitrine (filtres de date démontrés,
    `ce51ad50`), appel de `base.OnParametersSet` (`c68bcfe4`). Suite complète : 10633 tests, 0 échec.
- En attente du propriétaire : R-3 (badge distinct d'un bouton, la garde de rayon `--omni-radius-sm`
  interdit la pastille sans sa décision).
- Validé côté client (OE en sources locales) : R-26 sur ses 3 pages ; R-16 mesuré (infobulle centrée à
  1 px sur son bouton, chevauchement de 3 px voulu car placée sur le pointeur) ; R-4, catégorie E2E
  tableau de bord 28/28 verte, test réactivé des quatre barres de chargement sous les en-têtes passé.
- R-8 (`f146a1df`) : `OmniFormField.EndContent` sur la ligne du contrôle (`omni-form-field__control--end`),
  mesuré dans la vitrine à 943 et 375 px ; validé côté client (champ et trois boutons sur une ligne),
  qui a retiré son contournement local.
