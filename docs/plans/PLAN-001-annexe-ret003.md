<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-001 : annexe, classement des retours UI récurrents (D22 à D35)

> Annexe de [PLAN-001](PLAN-001-reliquats-audit-et-retours-ui.md). Établie le 2026-10-12 par lecture
> des citations (projet, date, contexte) et du code du paquet ; chaque groupe classé A a été reproduit
> dans la vitrine, les autres ne le sont pas. Notation : messages / projets ; identifiants `COR-NNNN`
> du rapport source. Les applications ne sont pas nommées.

## Classes

- **A** : défaut ou capacité manquante du paquet, reproduit. Repris au lot 5.
- **Décision** : comportement voulu aujourd'hui, dont la valeur par défaut était à trancher (D2 à D6, tranchées le 2026-10-12 : voir PLAN-001).
- **B** : le paquet fait déjà bien ; l'écart vient de l'application, ou d'une version du paquet
  antérieure au correctif (« A clos » : corrigé dans OE après la plainte, l'application n'a pas suivi).
- **C** : indéterminé sans reproduction ; « à reproduire » renvoie au lot 6.

Totaux : 715 citations, 341 hors périmètre, 374 en 82 groupes : **6 A**, **5 décisions**, **37 B**,
**34 C** (dont 8 au lot 6). Après le lot 6 du 2026-10-23, G-D28-02, G-D28-01 et G-D30-01 sont reproduits et passent de C en A : **9 A**, **31 C** (dont 5 au lot 6) ; puis G-D28-04 et G-D34-02 : **11 A**, **29 C** (dont 3 au lot 6).

## A, reproduits (lot 5)

| Groupe | Composant | Msg/proj | Reproduction (vitrine, 2026-10-12) | Correction proposée | Défaut visible changé |
|---|---|---|---|---|---|
| G-D23-03 | `OmniStatTile` | 7/2 | `/composants/pages`, 1440 px : 84 et 68 px dans une même rangée | `block-size: 100%` | oui |
| G-D35-02 | `OmniChart` | 4/2 | `/composants/graphiques`, 1920 × 1080 : svg 1126 × 1126 px | plafond de hauteur, `MaxHeight` | oui |
| G-D25-03 | `OmniButton` | 3/2 | `/composants/boutons` : icône seule sans `aria-label` 34 × 20 px, avec 20 × 20 px | carré selon le contenu, `title` en repli | oui |
| G-D29-03 | `OmniDataGrid` | 2/1 | `demo-grid-edit` : Entrée et Échap (touches réelles) laissent la ligne en édition | Entrée enregistre, Échap annule | oui |
| G-D26-04 | `OmniDataGrid` | 1/1 | `demo-grid-edit` : 40 px puis 45 px en édition | hauteur d'édition contenue partout | oui |
| G-D26-05 | `OmniLogViewer` | 1/1 | `/composants/listes`, 375 px : numéro décalé de 16 px au défilement | numéro et heure collants | oui (léger) |

## Décisions de valeurs par défaut

| Groupe | Composant | Msg/proj | Constat | Option |
|---|---|---|---|---|
| G-D27-03 | `OmniDataGrid` | 6/2 | `ScrollMode = Paged` par défaut, pagination non voulue | `Virtual` par défaut |
| G-D27-05 | `OmniDataGrid` | 2/2 | `AllowAlternatingRows` en option | actif par défaut |
| G-D27-06 | `OmniDataGrid` | 2/1 | en-tête 12 px, cellules 14 px (mesuré) | jeton `--omni-grid-header-font-size` |
| G-D27-07 | `OmniDataGrid` | 1/1 | en-tête de colonne numérique aligné à la fin | `HeaderTextAlign`, début par défaut |
| G-D23-06 | `OmniDataGrid` | 6/2 | un `int Id` s'aligne à la fin comme un montant ; citations contradictoires | exclure les identifiants ou documenter `TextAlign` |

## C à reproduire (lot 6)

| Groupe | Composant | Msg/proj | Preuve dans le code |
|---|---|---|---|
| G-D28-02 | `OmniPanelMenuItem` | 3/3 | libellé d'un groupe avec `Href` = lien sans `Toggle` ; `IsWithin` faux sur la route du groupe. **Reproduit et corrigé (2026-10-23), classe A** : le clic sur le libellé déplie |
| G-D28-01 | `OmniPanelMenuItem` | 4/2 | `IsCurrent` arbitre groupe et enfants, pas deux frères en `Prefix`. **Reproduit et corrigé (2026-10-23), classe A** : l'adresse la plus longue l'emporte entre frères |
| G-D23-09 | `OmniNotification` | 3/2 | cartes à 16,25rem ou 22,5rem selon la longueur ; aucune démo d'émission |
| G-D30-01 | `OmniDataAnnotationsValidator` | 2/2 | messages `IValidatableObject` ajoutés sans `Lookup()` ; reproduit et corrigé le 2026-10-23 (lot 6) |
| G-D28-04 | menus | 4/2 | placement abandonné après 10 essais sans prévenir .NET, `IsOpen` reste vrai. **Reproduit et corrigé (2026-10-23), classe A** : le script lâche le menu et prévient .NET (test node `tests/js/menus.test.mjs`) |
| G-D34-02 | `OmniTooltip` | 1/1 | `title` d'un descendant non retiré pendant le survol. **Reproduit et corrigé (2026-10-23), classe A** : mis de côté pendant le survol et le focus, rendu ensuite, `aria-label` prêté (test node `tests/js/tooltip.test.mjs`) |
| G-D24-04 | `OmniDataGrid` | 9/2 | alternance à 4 % du texte, peu visible en sombre |
| G-D22-04 | `OmniDataGrid` | 5/2 | plancher de 8rem par colonne sans largeur, défilement horizontal |

## Autres C

| Groupe | Composant | Msg/proj | Raison |
|---|---|---|---|
| G-D24-01 | jetons de thème | 25/4 | matrice de contraste déjà testée ; cause probable dans le CSS des applications, non prouvée |
| G-D23-01 | coquille (menu latéral, profil, en-tête) | 19/4 | reprise plusieurs fois depuis ; aucun défaut lisible statiquement |
| G-D22-01 | défilement de coquille | 8/4 | dépend de la hauteur fournie par l'application |
| G-D22-02 | `OmniDataGrid` cellules à gabarit | 8/2 | non reproduit à 1024 et 1280 px dans la vitrine |
| G-D26-01 | animations et décalages | 11/4 | tuiles du paquet sans `transform` ; reste non attribuable |
| G-D28-03 | `OmniSidebar` flottant | 5/2 | voile non refermé après une entrée action, à mesurer |
| G-D33-01 | `OmniTabs` | 5/2 | contextes recréés à chaque rendu, coût non mesuré |
| G-D32-02 | densité et tailles | 3/3 | choix de densité de l'application probable |
| G-D32-03 | contraste en-tête et alertes | 3/3 | à mesurer par palette |
| G-D23-04 | divers (pulseur, assistant, envoi) | 7/2 | un cas par composant, contexte insuffisant |
| G-D35-03 | réglages d'apparence | 4/2 | reprises du 27/09 et du 02/10 |
| G-D25-02 | icônes de boutons | 4/2 | aucune règle du paquet ne masque l'icône |
| G-D34-01 | `OmniTooltip` intermittent | 7/1 | défilement en capture qui relâche la bulle, hypothèse |
| G-D27-11 | `OmniDataGrid` rendu | 7/3 | plusieurs points déjà couverts ; « espace » non attribué |
| G-D27-01 | ajustement au double clic | 7/1 | mesure réécrite le 27/09, après les plaintes |
| G-D27-09 | grille virtuelle | 6/1 | correctifs du 27 et 29/09, après les plaintes |
| G-D27-04 | double ascenseur | 5/1 | correctif du 21/09, après les plaintes |
| G-D30-03 | `OmniDatePicker` en sombre | 3/2 | cause non établie |
| G-D30-08 | curseur renvoyé en fin de champ | 2/2 | composant exact inconnu |
| G-D28-07 | onglet à deux clics | 4/1 | exception de l'application probable |
| G-D30-06, G-D30-07, G-D29-04 | champs, liste, menu | 1/1 chacun | non reproduits ou antérieurs au composant |
| singletons D32/D33 | textes longs, groupes de notifications, défilement saccadé | 1/1 chacun | à tester au cas par cas |

## B

| Groupe | Composant | Msg/proj | Ce qui fait déjà bien |
|---|---|---|---|
| G-D25-01 | `OmniButton` tailles | 14/4 | trois tailles sur `--omni-control-height` ; tailles mélangées côté application (rappeler une seule taille par barre d'actions dans la règle des boutons) |
| G-D24-02 | thèmes et palettes | 22/2 | A clos (rayon, secondaire, désactivé, `color-scheme`) |
| G-D24-03 | Trou noir | 6/5 | A clos en 1.7.0 (résolution, filaments, moitié claire) |
| G-D26-02 | indicateurs de chargement | 6/3 | A clos (barre, cercle, épaisseur) |
| G-D26-03 | choix d'indicateur | 8/2 | barre dans la grille par défaut ; rotations côté application |
| G-D24-05 | liserés de notification | 4/4 | A clos |
| G-D22-03 | `OmniTabs` | 5/2 | A clos (barre masquée, compteur borné) |
| G-D22-05 | grille virtuelle, dernière ligne | 7/1 | A clos le 27/09 |
| G-D22-06 | menus hors écran | 3/2 | A clos le 29/09 |
| G-D23-02 | ligne de filtres | 7/4 | non reproduit : champs, listes et boutons à 28 px |
| G-D23-05 | `OmniBadge` | 6/2 | A clos ; surcharges côté application |
| G-D23-07 | boutons et en-têtes de grille | 4/2 | A clos |
| G-D23-08 | recette du paquet | 7/1 | A clos le 18/09 |
| G-D23-10 | calendrier dans un dialogue | 2/2 | A clos le 30/09 |
| G-D35-01 | marges de densité | 4/3 | A clos le 18/09 |
| G-D24-06 | `OmniBadge` ternes | 4/1 | A clos (`Solid` par défaut) |
| G-D25-04 | pied de dialogue | 4/1 | ordre et croix du paquet conformes |
| G-D25-05 | `OmniPassword` | 2/2 | A clos le 02/10 |
| G-D26-06 | bouton occupé | 1/1 | `Busy` garde taille et contenu |
| G-D27-02 | filtres d'en-tête | 6/3 | menu de filtre par défaut ; désactivé côté application |
| G-D27-10 | cycle de tri | 2/2 | cycle complet, `SortProperty` |
| G-D27-08 | colonne d'identifiant large | 2/1 | `Width` et `MinWidth` |
| G-D27-13 | export Markdown | 2/1 | visible avec `ShowHeaderBar` ou `ShowFooterBar` |
| G-D27-12 | filtre de date | 1/1 | filtre de plage fourni |
| G-D29-01 | focus initial du dialogue | 2/1 | A clos en 1.7.0 (`InitialFocus`) |
| G-D29-02 | fermer et valider | 2/2 | la fermeture émet `OpenChanged(false)` ; libellés de l'application |
| G-D30-04 | saisie effacée dans un dialogue | 2/2 | modèle réinitialisé dans `OnParametersSet` côté application ; à documenter |
| G-D30-05 | gestionnaire de mots de passe | 1/1 | `IgnorePasswordManagers` |
| G-D32-04 | langues mélangées | 4/2 | ressources du paquet complètes ; textes des applications |
| G-D28-06 | forme et survol du menu | 3/2 | survol sur toute la ligne |
| G-D28-05 | fermeture au clic extérieur | 2/2 | écoute en capture ; `Open` sans `OpenChanged` côté application |
| G-D33-02 | chargement de grille | 4/1 | `ShowLoadingBar` et `Busy` à documenter |
| G-D32-01 | titres | 3/1 | `Level` choisi par l'application |
| G-D32-05 | en-tête de page | 2/1 | A clos |
| G-D31-01 | reconnexion | 2/1 | superposition de présentation ; pilote de reconnexion = capacité nouvelle |
| G-D28-08 | rechargement complet | 2/1 | le paquet ne force aucun rechargement |
| singleton D28 | largeur d'onglet | 1/1 | l'onglet suit déjà son texte |
