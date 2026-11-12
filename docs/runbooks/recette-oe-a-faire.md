<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Recette d'OmniEurope.Blazor, points à faire

> Statut : ouverte le 2026-10-09, R1-24 archivée le 2026-11-12 ; nouvelle passe le 2026-11-12, numérotation repartie à 1 après l'archive (R1-1 à R1-3 ; les anciens R1-1 à R1-29 sont dans `recette-oe-fait.md`). Prochain numéro : **R1-5**.

## État

Le dépôt est le paquet lui-même : chaque point est d'origine paquet (OE). Les demandes viennent du
propriétaire ou de la recette d'une application cliente, dont le numéro est rappelé dans l'origine.
Traitement direct demandé par le propriétaire le 2026-10-09.

Historique de la passe précédente (anciens R1-1 à R1-29, nettoyage du 2026-10-29 et lots du 2026-10-29) : voir `recette-oe-fait.md` et l'historique Git de ce fichier.

### Lots du 2026-11-12 (R1-1 à R1-3)

Branche `develop`, quatre agents au plus en parallèle.

| Lot | Lignes | Modèle | Vague | État |
| --- | --- | --- | --- | --- |
| A, réglage Fond | R1-2 (modèle du fond, défauts par thème, persistance, lignes Fond et Animation, Givre et Trou noir repris) | opus medium | 1 | En cours |
| B, fenêtre compacte | R1-1 | sonnet high | 2 | À faire |
| C1, fonds Ardoise à Octet | R1-3 (Ardoise, Galet, Halo, Néon, Papier, Rétro, Octet) | sonnet high | 2 | À faire |
| C2, fonds Nénuphar à Épure | R1-3 (Nénuphar, Velours, Relief, Aplat, Épure) | sonnet high | 2 | À faire |

## Légende

- Type : Bug, Moche, Erreur, Autre.
- État : Ouvert, Ouvert décidé, Question, Fait (prouvé), Écarté, En attente.

## Points

| N° | Page / composant | Type | Constat | Origine | Justification | État |
| --- | --- | --- | --- | --- | --- | --- |

| R1-1 | `OmniAppearanceWindow` (fenêtre d'apparence : thème, palette, mode, police, mouvement du fond, densité, tailles, largeur), `OmniAppearanceSettings` | Moche | La fenêtre a trop d'options et une de plus arrive (le fond, R1-2) : la remanier en « options de jeu vidéo », compacte, une ligne par réglage (libellé à gauche, réglage à droite, sans titre ni aide sur plusieurs lignes), lisible et utilisable sur téléphone. Preuve : fenêtre mesurée dans le navigateur à 375 px et en bureau ; l'application est lancée en local pour que le propriétaire regarde aussi. Bouton Annuler de la fenêtre en rouge Danger (`OmniAppearanceWindow.razor:116`) : hors standard, Annuler n'est pas destructif (Secondary). Déjà dit : non. Décisions du propriétaire du 2026-11-12 : maquette validée (sections Style, Texte, Disposition ; une ligne par réglage, libellé court à gauche, réglage à droite ; même structure en bureau et sur téléphone, réglage sur toute la largeur restante), avec le Mode en première ligne puis Thème, Palette, Fond, Animation (interrupteur), puis Police, Taille du texte, puis Densité, Taille des contrôles, Largeur ; pied : hasard, Tout réinitialiser, Annuler, Appliquer ; Annuler passe en Secondary (gris), accord du propriétaire. | OE | Propriétaire, 2026-11-12. Composant du paquet : `Components/Theming/OmniAppearanceWindow.razor` (lignes de `omni-appearance-settings__row`, `OmniDropDown`, `OmniSelectBar`, `OmniSwitch`), utilisé par la vitrine (`MainLayout.razor:83`, `ThemePage.razor`). | Ouvert décidé |
| R1-2 | Thèmes, nouveau réglage « Fond » (`ThemeCatalog.cs`, `AppearanceLook.cs`, `AppearanceChoices.cs`, `05-layout.css`) | Autre | Le fond animé devient un paramètre de l'apparence, comme la palette : chaque thème a une palette par défaut et un fond par défaut ; le thème par défaut (Essentiel, palette Essentiel) a « Aucun » par défaut ; le fond peut être activé, désactivé ou changé. Aujourd'hui seuls Givre (taches pastel en orbite, `--omni-scope-motion`) et Trou noir (disque d'accrétion, `omni-black-hole.js`) ont un fond qui bouge, figé par l'interrupteur « mouvement du fond » (`BackdropMotion`). Le choix est persisté comme les autres préférences (cache local et API de la vitrine). Déjà dit : non. Décisions du propriétaire du 2026-11-12 : « Fond » est l'option nouvelle annoncée ; la ligne Fond choisit le fond, une ligne Animation (interrupteur, l'actuel « mouvement du fond » gardé) l'anime ou le fige ; Essentiel n'a aucun fond, défaut « Aucun ». Pris sur recommandation, le propriétaire ayant dit de démarrer sans trancher : chaque autre thème s'affiche avec son propre fond par défaut ; la liste propose tous les fonds, celui du thème marqué « (défaut) », comme les palettes ; sous Essentiel la liste ne propose que « Aucun » (« pas de fond disponible »). | OE | Propriétaire, 2026-11-12. Le fond est porté par la portée de thème (`--omni-backdrop`, `--omni-scope-motion`) et la préférence `BackdropMotion` d'`AppearanceLook`. | Ouvert décidé |
| R1-3 | Thèmes, un fond animé pour chacun des 15 thèmes | Autre | Proposer un fond animé pour chaque thème (Givre et Trou noir en ont déjà un) ; présenter la proposition au propriétaire et attendre sa validation avant d'implémenter ; ensuite tout implémenter, le propriétaire teste chaque fond. Le propriétaire parle de 14 thèmes : le catalogue en compte 15 (Essentiel, Ardoise, Galet, Halo, Néon, Papier, Rétro, Octet, Nénuphar, Velours, Relief, Givre, Aplat, Épure, Trou noir). Déjà dit : non. Proposition validée par le propriétaire le 2026-11-12 (Essentiel : aucun fond) : Ardoise, grille fine d'architecte qui glisse en diagonale ; Galet, ondes concentriques de ricochet ; Halo, bulles de lumière floues qui respirent et dérivent ; Néon, grille synthwave en perspective vers l'horizon ; Papier, taches d'encre qui se diffusent (aquarelle) ; Rétro, rayons de soleil qui tournent lentement ; Octet, étoiles en pixels qui scintillent en parallaxe ; Nénuphar, reflets d'eau ondulants et feuilles qui dérivent ; Velours, reflet moiré qui glisse sur un drapé ; Relief, courbes de niveau qui ondulent ; Givre, existant (taches pastel en orbite) ; Aplat, formes géométriques pastel flottantes (Memphis) ; Épure, trame de points fins qui pulse ; Trou noir, existant (disque d'accrétion). Couleurs tirées de la palette, arrêt sous « réduire les animations », CSS seul quand c'est possible. | OE | Propriétaire, 2026-11-12. `Internal/Theming/ThemeCatalog.cs` (15 entrées). | Ouvert décidé |
| R1-4 | `OmniFactTile` (`Styles/10-editors-settings.css`, colonnes et police de la valeur) | Moche | Dans une tuile large, libellé et valeur à parts égales (50/50) par défaut pour toutes les tuiles, libellés usuels non coupés (« Release déployée », « Durée moyenne des succès »), police de la valeur un peu plus petite ; mobile inchangé ou lisible. Déjà dit : non. | OE | Demande d'une application cliente (sa recette, dictée par le propriétaire le 2026-11-12) : page d'exécution de cette application, tuile « Exécution du pipeline », libellés coupés alors que les valeurs ont de la place. Colonnes `minmax(0, min(6.5rem, 40%))` et valeur en `--omni-font-size-base` (`10-editors-settings.css:284`, `:289`). | Ouvert décidé |

## Critère de clôture

La recette se ferme quand chaque ligne est faite et prouvée par une observation (mesure, capture,
test qui échouait avant), archivée dans `recette-oe-fait.md`, et que la compilation, la suite de tests
et les gardes d'architecture passent réellement.
