<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-008 - Demandes d'une application cliente du 2026-10-29

Neuf besoins génériques relevés par une application cliente (consommatrice de la 1.8.2) pendant une passe
d'ergonomie, relus contre `develop`. Chaque point est d'abord confirmé manquant ; un point n'est « fait »
qu'avec sa preuve observée dans la vitrine.

| Lot | Points | Contenu | Contrôle |
|---|---|---|---|
| A | W-1, W-5, W-6 | `OmniButton` discret en teinte danger ; `OmniProgressBar` à plusieurs segments empilés ; libellé long d'`OmniStatTile` à la ligne | couleur du texte danger sans fond ; largeurs des segments proportionnelles ; libellé entier sur deux lignes à 375 px |
| B | W-2, W-3, W-9 | Présentation plate d'`OmniSettingsSection` ; contenu de saisie dans `OmniSettingsDangerAction` qui conditionne la confirmation ; jeton public de liseré des cartes | tuiles sans fond ni cadre ; confirmation inactive tant que la saisie est invalide ; carte sans liseré par le jeton documenté |
| C | W-4, W-7, W-8 | `Density` sur `OmniDataList` ; nom court ou initiales dans le bouton d'`OmniAppMenu` ; `Disabled` sur `OmniNavTile` | hauteur de ligne compacte mesurée ; adresse électronique non coupée sur téléphone ; tuile sans lien, `aria-disabled`, atténuée |

Constats de départ :

- W-6 : le renommage `OmniStatTile.Label` vers `Title` est déjà au CHANGELOG (1.9.0, rubrique de migration).
- W-9 : `--omni-card-border-color` et `--omni-card-border-width` sont des jetons de thème non documentés ;
  la couleur sert aussi à l'en-tête, aux champs et aux fichiers déposés, la largeur aux seules surfaces de carte.

## Journal

- 2026-10-29 : plan ouvert, lots A, B et C délégués en parallèle en worktrees.
