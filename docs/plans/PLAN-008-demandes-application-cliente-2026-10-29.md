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
- 2026-10-29 : les neuf points livrés sur `develop` ; suite de 10 796 tests, API publique 4 431 signatures,
  index des composants, aucun échec. Preuves dans la vitrine :
  - W-1 `OmniButtonVariant.GhostDanger` : fond transparent, texte danger 5,69:1 sur la page, 4,61:1 au survol
    (teinte danger) ; sonde de contraste complète validée, registre vide (124 760 mesures sur 450
    combinaisons ; acceptées par les décisions existantes : voile 4 893 contre 4 443, Givre 614 contre 554).
  - W-2 `OmniSettingsSection.Flat` : tuiles sans fond ni cadre, filet de 1 px entre deux tuiles.
  - W-3 `OmniSettingsDangerAction.ConfirmDetails`, `CanConfirm`, `OnCancel` : champ focalisé à l'ouverture,
    action inactive à 3 caractères, active à 8 ; après Échap, champ vidé et rien d'exécuté.
  - W-4 `OmniDataList.Density` : lignes de 36 px en compact, 44 hérité, 52 spacieux.
  - W-5 `OmniProgressBar.Segments`, `ShowLegend` : segments à 40, 25 et 10 % de la piste, couleurs égales
    à la légende, `aria-valuenow` 23 et `aria-valuetext` « Discovered 12, Known 8, Mastered 3 ».
  - W-6 `OmniStatTile.WrapTitle` : en six colonnes (121 px), libellé entier sur quatre lignes au lieu
    d'être réduit puis coupé ; trois tuiles à 116 px. Renommage `Label` vers `Title` déjà au CHANGELOG (1.9.0).
  - W-7 `OmniAppMenu.UserDisplay`, `Initials` : à 375 px, « CD » avec l'icône ; nom accessible avec l'adresse.
  - W-8 `OmniNavTile.Disabled` : pas de `href`, `aria-disabled`, opacité 0,55, taille 682 × 133 inchangée,
    non focalisable.
  - W-9 : `--omni-card-border-width` documenté comme prise publique ; à 0 sur une classe hôte, cartes
    sans liseré (1 px vers 0). Il touche aussi grilles et éditeurs ; un `OmniCard.Bordered` reste à décider.
