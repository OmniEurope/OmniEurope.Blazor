<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-011 : Demandes OE de la recette

> Statut : **terminé** le 2026-10-03, à archiver. Établi le 2026-10-02 à partir de neuf demandes de la recette décidées par le propriétaire ; lot 1 fait (R-052 mesuré : liste et bouton `Small` à 28 px, `Medium` 36 px, `Large` 44 px) ; lot 2 fait (à 620 px, `ToolbarRows="1"` tient la barre du document sur une ligne et passe 12 boutons dans « ⋮ », ouvert au clavier, une commande lancée depuis le menu s'applique ; infobulle « nom, puis description » vérifiée). Lot 3 fait : burger sur l'axe du rail (615 px et 615 px), tuiles de même hauteur (149 px) à 1280 px et empilées à 375 px, paires côte à côte à 1920 px (1088 px puis 2 x 538 px) et empilées à 375 px.

## Objectif

Les neuf demandes de la recette du 2026-10-02 passent dans OE, pour tous les sites : une
tuile de navigation, un burger qu'un projet ne peut plus désaligner, le filtre d'en-tête par défaut,
une barre d'outils d'éditeur HTML qui tient en peu de lignes avec de vraies infobulles, une icône de
document Word, une taille sur les champs d'une ligne, des réglages en paires et un formulaire qui
redevient propre quand on efface sa saisie. Une application cliente lit OE par `ProjectReference` pendant sa recette :
un commit sur develop suffit, la réponse donne les empreintes.

## État des lieux (2026-10-02)

Lignes de recette dans le worktree d'une application cliente `(chemin local)`,
fichier `docs/guides/recette-ui-a-faire.md`.

- R-036 : OE n'a qu'`OmniStatTile` (chiffre, bouton `OnClick`, pas de `Href`). Modèle d'une application cliente `(fichier d'une application cliente)` et `wwwroot/css/admin.css:1-33`.
- R-037 : `05-layout.css:105` n'annule que la marge intérieure d'`OmniHeader` (-0.75rem) ; un enveloppeur de projet avec sa propre marge (une application cliente `layout.css:52-58`, une application cliente `app.css:6099-6115`, leur R-001) décale le burger de l'axe des icônes du rail.
- R-041 : `ShowHeaderFilterMenu` vaut `false` par défaut (`OmniDataGrid.razor.cs:347`) ; la rangée de filtres en ligne est affichée par défaut.
- R-042 : `HtmlEditorToolbar.cs:30-53` regroupe et passe à la ligne ; l'extension AKN d'une application cliente montre 50 boutons sur 4 lignes.
- R-043 : boutons de la barre d'outils en `title` natif et `aria-label` (`OmniHtmlEditor.razor:65,77-78`).
- R-051 : `OmniIconName` a `FilePdf`, `FileMd`, `FileCsv`, `FileXls`, pas de document Word.
- R-052 : ni `OmniDropDown` ni `OmniInputBase` n'ont de `Size` ; un select fait 2.25rem (`06-forms.css:43`), un bouton `Small` 1.75rem (`04-actions-content.css:53-55`).
- R-060 : `OmniSettingsSection` et `OmniSettingsTile` existent, pas l'appariement en deux colonnes (une application cliente le fait en CSS local, `.settings-pair`, `app.css` vers 6476-6492, leur R-448).
- R-061 : `_dirty` d'`OmniTemplateForm` ne redescend jamais (`OmniTemplateForm.razor.cs:103-110`) ; `OmniTabsItem` garde monté un panneau visité (`OmniTabsItem.razor.cs:68`), donc la garde d'un formulaire caché bloque encore la navigation.

## Décisions (validées le 2026-10-02 dans la session d'une application cliente)

- R-036 : nouveau composant, toute la tuile est un vrai lien (`Href`), un ton par tuile, au survol une ombre seulement (pas de translation).
- R-041 : option 1, le filtre d'en-tête devient le défaut de toutes les grilles ; un projet qui veut la rangée la redemande. Changement de comportement consigné au CHANGELOG.
- R-051 : option 1, seule l'icône prend la couleur de son format ; le bouton reste `Secondary` (`STD-BTN`).
- R-060 : sections à 100 % de la page, tuiles à 50 % (deux par ligne, empilées sous environ 24rem) ou 100 %, sans plafond de largeur.

## Lots

### Lot 1 - Correctifs et options
- [x] R-061 : `OmniTemplateForm` modifié = valeurs courantes différentes de l'instantané initial, instantané repris après enregistrement ; garde d'un panneau d'onglet non affiché ignorée, ou documentée.
- [x] R-041 : `ShowHeaderFilterMenu` à `true` par défaut, rangée de filtres en ligne sur demande ; CHANGELOG (comportement), vitrine.
- [x] R-051 : icône `FileDoc` (document Word) et teinte de format optionnelle sur l'icône d'un fichier.
- [x] R-052 : `Size` (`OmniControlSize`) sur `OmniDropDown` et les autres champs d'une ligne, hauteur alignée sur celle des boutons.
Controle : un test par point ; R-052 mesuré en navigateur (select `Small` et bouton `Small` de même hauteur).

### Lot 2 - Éditeur HTML
- [x] R-042 : menu « ⋮ » (`OmniOverflowMenu`) en bout de barre pour les commandes secondaires ou qui ne tiennent pas, nombre de lignes borné, libellés visibles en option sur grand écran ; une commande dit si elle est principale.
- [x] R-043 : infobulle OE sur chaque contrôle de la barre ; description optionnelle par commande (`OmniHtmlEditorCommand`) montrée dans l'infobulle.
Controle : extension de 50 commandes en vitrine tenue en deux lignes au plus à 1280 px, débordement atteignable au clavier ; infobulle visible en navigateur.

### Lot 3 - Mise en page
- [x] R-036 : tuile de navigation (icône sur carré teinté, titre, description optionnelle, `Href`, ton) et sa démonstration.
- [x] R-060 : appariement des tuiles de réglages (option de section ou composant de paire), deux colonnes empilées sous environ 24rem, sans plafond.
- [x] R-037 : `OmniHeader` porte la marque et le burger, alignés sur l'axe des icônes du rail quel que soit l'enveloppeur ; doc : ne pas les envelopper.
Controle : burger sur l'axe du rail en navigateur avec un enveloppeur à marge intérieure ; tuiles et paires mesurées à 1280 et 375 px.

## Ordre et dépendances

Les lots sont indépendants. Le lot 1 passe d'abord (petits, débloquent une application cliente), R-037 en dernier :
il touche la coquille de tous les sites.

## Critère de clôture

Chaque case a son test et, pour le visuel, sa mesure en navigateur ; une application cliente a reçu les empreintes
de commit ; CHANGELOG et documentation des composants à jour.
