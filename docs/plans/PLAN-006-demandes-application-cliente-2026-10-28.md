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

## Journal

- 2026-10-28 : plan ouvert, lots A à D délégués en worktrees, lot E traité par l'orchestrateur.
