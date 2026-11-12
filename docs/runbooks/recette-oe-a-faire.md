<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09, R1-24 archivée le 2026-11-12 ; nouvelle passe le 2026-11-12, numérotation repartie à 1 après l'archive ; R1-1 à R1-4 de cette passe faites, prouvées et archivées le 2026-11-12 dans `recette-oe-fait.md`. Nouvelle série de retours le 2026-11-12, numérotée à partir de 1 (R1-1 à R1-6). Prochain numéro : **R1-7**.

## État

Le dépôt est le paquet lui-même : chaque point est d'origine paquet (OE). Les demandes viennent du
propriétaire ou de la recette d'une application cliente, dont le numéro est rappelé dans l'origine.
Traitement direct demandé par le propriétaire le 2026-10-09.

Historique de la passe précédente (anciens R1-1 à R1-29, nettoyage du 2026-10-29 et lots du 2026-10-29) : voir `recette-oe-fait.md` et l'historique Git de ce fichier.

### Lots du 2026-11-12 (R1-1 à R1-3)

Branche `develop`, quatre agents au plus en parallèle.

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| A, réglage Fond | R1-2 (modèle du fond, défauts par thème, persistance, lignes Fond et Animation, Givre et Trou noir repris) | opus medium | 1 | Fait, prouvé (8430a58e) |
| B, fenêtre compacte | R1-1 | sonnet high | 2 | Fait, prouvé (da0b14e0, 5cbe4cec) |
| C1, fonds Ardoise à Octet | R1-3 (Ardoise, Galet, Halo, Néon, Papier, Rétro, Octet) | sonnet high | 2 | Fait, prouvé (5006db7a, 39f89d03) |
| C2, fonds Nénuphar à Épure | R1-3 (Nénuphar, Velours, Relief, Aplat, Épure) | sonnet high | 2 | Fait, prouvé (6ca60e28) |

### Lots du 2026-11-12, retours sur la fenêtre (nouveaux R1-1 à R1-6)

Branche `develop`, deux agents en parallèle, chacun dans son worktree.

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| D, fenêtre d'apparence du paquet | R1-1, R1-3, R1-4, R1-5, R1-6 | sonnet high | 1 | En cours |
| E, fenêtre principale de la vitrine | R1-2 | sonnet medium | 1 | En cours |

## Légende

- Type : Bug, Moche, Erreur, Autre.
- État : Ouvert, Ouvert décidé, Question, Fait (prouvé), Écarté, En attente.

## Points

| N° | Page / composant | Type | Constat | Origine | Justification | État |
| --- | --- | --- | --- | --- | --- | --- |
| R1-1 | `OmniAppearanceWindow`, `OmniAppearanceSettings`, lignes Taille du texte et Taille des contrôles | Moche | Remplacer les boutons [-] 100 % [+] par un curseur encadré des boutons moins et plus (le propriétaire préfère garder un curseur). Déjà dit : oui (retour sur l'ancienne R1-1 du 2026-11-12, archivée). Décisions du propriétaire du 2026-11-12 : icône de remise à zéro gardée, bouton d'annulation « Restaurer » avant « Appliquer » gardé. | OE | Propriétaire, 2026-11-12. Lignes d'échelle de `Components/Theming/OmniAppearanceSettings.razor` (lot B, da0b14e0). | Ouvert décidé |
| R1-2 | Vitrine, fenêtre d'apparence principale (`site/OmniEurope.Blazor.Showcase/Components/Layout/MainLayout.razor:83`) | Bug | Il manque des réglages dans la fenêtre : la police, la taille du texte, la taille des contrôles, la largeur (la maquette les montrait). Déjà dit : non. | Vitrine | Propriétaire, 2026-11-12, capture à l'appui (sections Style et Disposition seulement). La fenêtre ne montre une ligne que si l'hôte lie son changement ; `MainLayout.razor:83-89` ne lie ni `FontChanged`, ni `TextSizeLevelChanged`, ni `ControlSizeLevelChanged`, ni `FullWidthChanged`, et `Theming/ThemeState.cs` ne garde pas ces réglages (seule la page Thème les lie, `ThemePage.razor:54`). | Ouvert |
| R1-3 | Thème Essentiel, liste Fond | Autre | Sous Essentiel la liste Fond ne propose rien : on doit pouvoir choisir un fond sous Essentiel aussi, « Aucun » restant le défaut. Déjà dit : non. Décision du propriétaire du 2026-11-12, qui remplace celle de l'ancienne R1-2 (« Essentiel sans fond proposé »). | OE | Propriétaire, 2026-11-12. `OmniThemePresets.BackdropsFor` ne rend que « Aucun » sous Essentiel ; la garde de contraste thème x fond (`ThemeContrastMatrixTests`) mesurera Essentiel sur chaque fond. | Ouvert décidé |
| R1-4 | `OmniAppearanceWindow`, interrupteur Animation | Autre | L'interrupteur reste toujours affiché ; quand le fond dessiné ne bouge pas (« Aucun », par exemple sous Essentiel), il est désactivé et montré à faux. Déjà dit : non. Décision du propriétaire du 2026-11-12. | OE | Propriétaire, 2026-11-12. Aujourd'hui la ligne Animation n'apparaît que sous un fond qui bouge (`OmniAppearanceSettings`). | Ouvert décidé |
| R1-5 | `OmniAppearanceWindow`, bouton Aléatoire | Bug | « Aléatoire » doit tout tirer au hasard, le fond compris. Déjà dit : non. Décision du propriétaire du 2026-11-12, qui remplace le choix de l'orchestrateur (« l'apparence au hasard garde le fond par défaut du thème »). Pris sur recommandation : le hasard tire le thème, la palette, le fond et la police (les choix de style), pas le mode, la densité, les tailles ni la largeur. | OE | Propriétaire, 2026-11-12. `AppearanceChoices.RandomLookAsync`. | Ouvert décidé |
| R1-6 | `OmniAppearanceWindow` sous le thème Octet | Bug | La fenêtre casse sous Octet : les boutons du pied (Random, Reset all, Restore, Apply) passent sur deux lignes et sortent du cadre. Agrandir un peu la fenêtre ou réduire les boutons : elle ne doit casser sous aucun thème. Déjà dit : non. | OE | Propriétaire, 2026-11-12, capture à l'appui (Octet, palette Océan, fond Relief). Octet élargit ses boutons (capitales, cadres en escalier) ; la sonde `eng/scripts-probe/overlays.mjs` (balayage de la fenêtre sous chaque thème) doit le couvrir. | Ouvert |

## Critère de clôture

La recette se ferme quand chaque ligne est faite et prouvée par une observation (mesure, capture,
test qui échouait avant), archivée dans `recette-oe-fait.md`, et que la compilation, la suite de tests
et les gardes d'architecture passent réellement.
