<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09, R1-24 archivée le 2026-11-12 ; nouvelle passe le 2026-11-12 (R1-30 à R1-32). Prochain numéro : **R1-33**.

## État

Le dépôt est le paquet lui-même : chaque point est d'origine paquet (OE). Les demandes viennent du
propriétaire ou de la recette d'une application cliente, dont le numéro est rappelé dans l'origine.
Traitement direct demandé par le propriétaire le 2026-10-09.

Nettoyage du 2026-10-29 : R1-1 à R1-16, faites et prouvées (livrées avec 1.10.0), sont archivées dans
`recette-oe-fait.md`. Les six lignes restantes ont été revérifiées dans le code le même jour : aucune
n'est encore faite (badge toujours dans `MainLayout.razor:23`, voile `.omni-busy` toujours pulsé,
aucun voile de page, `OmniMain` de la vitrine sans `Scrollable`, aucun commit sur l'espacement des champs
depuis la réserve de la ligne d'erreur).

### Lots du 2026-10-29 (R1-25 à R1-29)

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| Direct | R1-26, R1-27 | orchestrateur | 1 | Fait, archivé |
| A | R1-25 | sonnet high | 1 | Fait, archivé |
| B | R1-28 filtre du menu et de l'Overview | sonnet high | 1 | Fait, archivé |
| C | R1-28 recherche d'en-tête | sonnet high | 1 | Fait, archivé |
| D | R1-29 | sonnet high | 1 | Fait, archivé |

### Lots du 2026-11-12 (R1-30 à R1-32)

Branche `develop`, quatre agents au plus en parallèle.

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| A, réglage Fond | R1-31 (modèle du fond, défauts par thème, persistance, lignes Fond et Animation, Givre et Trou noir repris) | opus medium | 1 | En cours |
| B, fenêtre compacte | R1-30 | sonnet high | 2 | À faire |
| C1, fonds Ardoise à Octet | R1-32 (Ardoise, Galet, Halo, Néon, Papier, Rétro, Octet) | sonnet high | 2 | À faire |
| C2, fonds Nénuphar à Épure | R1-32 (Nénuphar, Velours, Relief, Aplat, Épure) | sonnet high | 2 | À faire |

## Légende

- Type : Bug, Moche, Erreur, Autre.
- État : Ouvert, Ouvert décidé, Question, Fait (prouvé), Écarté, En attente.

## Points

| N° | Page / composant | Type | Constat | Origine | Justification | État |
| --- | --- | --- | --- | --- | --- | --- |

| R1-30 | `OmniAppearanceWindow` (fenêtre d'apparence : thème, palette, mode, police, mouvement du fond, densité, tailles, largeur), `OmniAppearanceSettings` | Moche | La fenêtre a trop d'options et une de plus arrive (le fond, R1-31) : la remanier en « options de jeu vidéo », compacte, une ligne par réglage (libellé à gauche, réglage à droite, sans titre ni aide sur plusieurs lignes), lisible et utilisable sur téléphone. Preuve : fenêtre mesurée dans le navigateur à 375 px et en bureau ; l'application est lancée (`ylaunch.ps1 -s`) pour que le propriétaire regarde aussi. Bouton Annuler de la fenêtre en rouge Danger (`OmniAppearanceWindow.razor:116`) : hors standard, Annuler n'est pas destructif (Secondary). Déjà dit : non. Décisions du propriétaire du 2026-11-12 : maquette validée (sections Style, Texte, Disposition ; une ligne par réglage, libellé court à gauche, réglage à droite ; même structure en bureau et sur téléphone, réglage sur toute la largeur restante), avec le Mode en première ligne puis Thème, Palette, Fond, Animation (interrupteur), puis Police, Taille du texte, puis Densité, Taille des contrôles, Largeur ; pied : hasard, Tout réinitialiser, Annuler, Appliquer ; Annuler passe en Secondary (gris), accord du propriétaire. | OE | Propriétaire, 2026-11-12. Composant du paquet : `Components/Theming/OmniAppearanceWindow.razor` (lignes de `omni-appearance-settings__row`, `OmniDropDown`, `OmniSelectBar`, `OmniSwitch`), utilisé par la vitrine (`MainLayout.razor:83`, `ThemePage.razor`). | Ouvert décidé |
| R1-31 | Thèmes, nouveau réglage « Fond » (`ThemeCatalog.cs`, `AppearanceLook.cs`, `AppearanceChoices.cs`, `05-layout.css`) | Autre | Le fond animé devient un paramètre de l'apparence, comme la palette : chaque thème a une palette par défaut et un fond par défaut ; le thème par défaut (Essentiel, palette Essentiel) a « Aucun » par défaut ; le fond peut être activé, désactivé ou changé. Aujourd'hui seuls Givre (taches pastel en orbite, `--omni-scope-motion`) et Trou noir (disque d'accrétion, `omni-black-hole.js`) ont un fond qui bouge, figé par l'interrupteur « mouvement du fond » (`BackdropMotion`). Le choix est persisté comme les autres préférences (cache local et API de la vitrine). Déjà dit : non. Décisions du propriétaire du 2026-11-12 : « Fond » est l'option nouvelle annoncée ; la ligne Fond choisit le fond, une ligne Animation (interrupteur, l'actuel « mouvement du fond » gardé) l'anime ou le fige ; Essentiel n'a aucun fond, défaut « Aucun ». Pris sur recommandation, le propriétaire ayant dit de démarrer sans trancher : chaque autre thème s'affiche avec son propre fond par défaut ; la liste propose tous les fonds, celui du thème marqué « (défaut) », comme les palettes ; sous Essentiel la liste ne propose que « Aucun » (« pas de fond disponible »). | OE | Propriétaire, 2026-11-12. Le fond est porté par la portée de thème (`--omni-backdrop`, `--omni-scope-motion`) et la préférence `BackdropMotion` d'`AppearanceLook`. | Ouvert décidé |
| R1-32 | Thèmes, un fond animé pour chacun des 15 thèmes | Autre | Proposer un fond animé pour chaque thème (Givre et Trou noir en ont déjà un) ; présenter la proposition au propriétaire et attendre sa validation avant d'implémenter ; ensuite tout implémenter, le propriétaire teste chaque fond. Le propriétaire parle de 14 thèmes : le catalogue en compte 15 (Essentiel, Ardoise, Galet, Halo, Néon, Papier, Rétro, Octet, Nénuphar, Velours, Relief, Givre, Aplat, Épure, Trou noir). Déjà dit : non. Proposition validée par le propriétaire le 2026-11-12 (Essentiel : aucun fond) : Ardoise, grille fine d'architecte qui glisse en diagonale ; Galet, ondes concentriques de ricochet ; Halo, bulles de lumière floues qui respirent et dérivent ; Néon, grille synthwave en perspective vers l'horizon ; Papier, taches d'encre qui se diffusent (aquarelle) ; Rétro, rayons de soleil qui tournent lentement ; Octet, étoiles en pixels qui scintillent en parallaxe ; Nénuphar, reflets d'eau ondulants et feuilles qui dérivent ; Velours, reflet moiré qui glisse sur un drapé ; Relief, courbes de niveau qui ondulent ; Givre, existant (taches pastel en orbite) ; Aplat, formes géométriques pastel flottantes (Memphis) ; Épure, trame de points fins qui pulse ; Trou noir, existant (disque d'accrétion). Couleurs tirées de la palette, arrêt sous « réduire les animations », CSS seul quand c'est possible. | OE | Propriétaire, 2026-11-12. `Internal/Theming/ThemeCatalog.cs` (15 entrées). | Ouvert décidé |

## Critère de clôture

La recette se ferme quand chaque ligne est faite et prouvée par une observation (mesure, capture,
test qui échouait avant), archivée dans `recette-oe-fait.md`, et que la compilation, la suite de tests
et les gardes d'architecture passent réellement.
