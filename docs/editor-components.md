# Éditeurs : WYSIWYG, traitement de texte et code

La famille Editor couvre onze composants :

| Composant | Rôle |
| --- | --- |
| `OmniHtmlEditor` | Éditeur WYSIWYG à face source HTML, barre de commandes extensible, sortie assainie ; sert aussi de traitement de texte léger. |
| `OmniCodeEditor` | Éditeur de code Monaco servi par l'hôte, avec repli en zone de texte brut. |
| `OmniDiffViewer` | Comparaison de deux versions d'un texte avec Monaco, repli en deux volets. |
| `OmniCodeViewer` | Code en lecture seule, numéroté, lignes surlignées et liens, bouton de copie. |
| `OmniCodeBlock` | Commande, extrait ou jeton à copier ; un secret reste masqué jusqu'à ce qu'on le révèle. |
| `OmniUnifiedDiff` | Diff unifié (`git diff`) dessiné sans Monaco, un bloc repliable par fichier. |
| `OmniDocumentViewer` | Document converti par l'hôte, lu page par page : pages image ou HTML assaini, champs `data-oe-field` surlignés sur les pages HTML, zoom et ajustement à la largeur. |
| `OmniCharacterPalette` | Rangée de caractères fournis par l'hôte (lettres de la langue saisie) ; un clic rend le caractère sans retirer le focus d'un éditeur. |
| `OmniMailTemplateEditor` | Modèle de mail : objet, corps `OmniHtmlEditor`, variables insérées au curseur, aperçu du mail rendu ; `OmniMailRenderer` rend le modèle côté serveur pour les clients de messagerie. |
| `OmniInkCanvas` | Zone de dessin à main levée (souris, stylet avec pression, doigt) : traits en coordonnées relatives, couleur, épaisseur, Annuler, Effacer, export en PNG transparent. |
| `OmniDocumentEditor` | Éditeur structuré minimal d'un document converti par l'hôte (modèle neutre `OmniDocument`) : texte modifié sur place, champs remplis avec la saisie de leur type (texte, date, liste, nombre), dessin sous un paragraphe, jeu de changements rapporté à l'hôte. |

`OmniHtmlEditor` produit du HTML assaini ; `OmniMailTemplateEditor` en fait un modèle de mail ; `OmniCodeEditor` édite du texte brut ; `OmniCharacterPalette` fournit des caractères à insérer ; `OmniInkCanvas` recueille un dessin ; `OmniDocumentEditor` modifie un document converti par l'hôte ; les cinq autres affichent sans éditer.

## OmniHtmlEditor

### Deux faces

`Mode` choisit la face affichée, liée dans les deux sens par `ModeChanged` :

- `OmniHtmlEditorMode.Visual` (défaut) : le document mis en forme, édité sur place dans une surface
  `contenteditable` (`role="textbox"`, `aria-multiline="true"`) que pilote `omni-html-editor.js`. La surface
  n'est modifiable qu'une fois montée par ce script : rendue par Blazor avant son chargement, elle reste
  `contenteditable="false"` et `aria-busy="true"` jusqu'à la fin du montage, qui l'ouvre (sauf éditeur en
  lecture seule ou désactivé) et pose `data-omni-ready="true"`. Ce signal public est celui qu'une
  application ou ses tests de bout en bout attendent avant de taper (`.omni-html-editor__surface[data-omni-ready="true"]`),
  plutôt qu'une propriété interne de la surface.
- `OmniHtmlEditorMode.Source` : le HTML dans une zone de texte, avec l'aperçu assaini de `ShowPreview`.
  Les commandes y entourent le texte sélectionné des balises correspondantes.

Le bouton « Source HTML » de la barre bascule d'une face à l'autre ; ce qui venait d'être tapé est
repris avant le changement. Un parent qui ne lie pas `Mode` ne ramène pas la face en arrière en se
redessinant : le paramètre n'est suivi que lorsqu'il change.

`Label` (`string?`) nomme la surface ; sans lui, dans un `OmniFormField`, la surface (un élément modifiable
qu'un `label for` ne peut pas nommer) pose `aria-labelledby` sur le libellé du champ (`{For}-label`).
`ReadOnly` laisse lire, sélectionner et copier sans modifier : rien ne se tape, la barre est désactivée,
la surface est annoncée `aria-readonly` et la zone source est `readonly`, sans estomper l'éditeur ; les
éléments en ligne réagissent toujours au clic. `Disabled` l'emporte : rien ne se fait et l'éditeur est
estompé.

### Barre de commandes extensible

La barre est la liste `Commands`, rendue dans l'ordre. Sans valeur, c'est
`OmniHtmlEditorCommands.Default` : style de paragraphe (paragraphe, titres 1 à 4), gras, italique,
souligné, barré, indice, exposant, code en ligne, listes à puces et numérotée, retraits, citation, bloc
de code, lien et retrait du lien, quatre alignements, effacement de la mise en forme, annuler, rétablir
et bascule de source. `OmniHtmlEditorCommands.Document` est la barre du traitement de texte.

Une application ajoute, retire ou réordonne des commandes en construisant sa propre liste :

```csharp
private static readonly IReadOnlyList<OmniHtmlEditorCommand> Commands =
[
    .. OmniHtmlEditorCommands.Default,
    OmniHtmlEditorCommands.Separator,
    OmniHtmlEditorCommand.Create("signature", "Insérer la signature",
        context => context.InsertHtmlAsync("<p>Le service des dossiers</p>"), OmniIconName.Edit)
];
```

Chaque contrôle de la barre porte l'infobulle du paquet (son nom), sans le `title` natif du navigateur,
lent à paraître ; `Description` ajoute sous le nom à quoi sert la commande, aussi donnée aux
technologies d'assistance (`aria-description`). Une barre longue reste courte : `Overflow = true` range
une commande secondaire dans le menu « ⋮ » au bout de la barre, et `ToolbarRows` borne la barre à un
nombre de lignes, les boutons qui ouvriraient une ligne de plus passant, depuis la fin, dans ce menu
(ils reviennent quand la barre s'élargit ; les listes de style, de taille et de casse restent dans la
barre). `ToolbarLabels` affiche le nom à côté de l'icône tant que la barre fait au moins 64 rem.

Une commande intégrée n'a besoin que de son `OmniHtmlEditorAction` : libellé localisé et icône viennent
de l'éditeur, et `Label` ou `Icon` les remplacent. Une commande `Custom` reçoit un
`OmniHtmlEditorCommandContext` : `Html` (valeur courante, frappe récente comprise), `Mode`,
`InsertHtmlAsync` (au curseur, en remplaçant la sélection), `SetHtmlAsync` (toute la valeur) et
`ExecuteAsync` (une action intégrée). Tout ce qu'une commande écrit passe par l'assainissement et
l'historique. Une commande `Custom` sans `Execute` lève `InvalidOperationException`. Les séparateurs
en tête, en fin ou répétés ne sont pas dessinés. La barre passe à la ligne par groupes d'outils : un
séparateur ouvre le groupe qui le suit et reste avec lui, si bien qu'aucune ligne ne se termine par un
séparateur, et celui d'un groupe qui commence une ligne n'est pas dessiné.

Un hôte qui modifie lui-même le document par son propre script (structure, numérotation) passe
`context.SurfaceElement` (l'`ElementReference` de la surface, null en face source) à ce script, puis
appelle `context.CommitDomAsync()` : l'éditeur relit la surface, l'assainit avec la liste blanche et
la politique des extensions, en fait une étape d'historique et lève `ValueChanged` si la valeur change. Ce que
l'assainissement a retiré disparaît aussi de la surface, redessinée depuis la valeur. En face source,
`CommitDomAsync` ne fait rien. Hors d'une commande (clic sur une note en ligne, suggestion acceptée),
l'hôte appelle `OmniHtmlEditor.CommitDomAsync()` sur la référence du composant (`@ref`), avec le même
effet ; la méthode passe elle-même par le répartiteur du rendu et peut donc être appelée d'un rappel JS.
Le script de l'hôte trouve alors la surface par l'`Id` de l'éditeur.

Une commande sans icône affiche son libellé en texte ; `context.SetHtmlAsync(...)` réécrit toute la
valeur, ce qui couvre une transformation de l'ensemble du document.

Les boutons bascules portent `aria-pressed` selon la mise en forme au curseur (gras, italique,
souligné, barré, indice, exposant, code, listes, citation, bloc de code, lien, alignement), et les
listes de style et de taille montrent la valeur au curseur. Ils ne prennent pas le focus à la souris,
pour que la sélection reste dans le document.

### Presse-papiers, paragraphe et tableaux

Actions intégrées de la seule face visuelle (désactivées en face source), utilisables dans `Commands`
ou par `OmniHtmlEditorCommandContext.ExecuteAsync` :

- `Cut`, `Copy`, `Paste` (liste `OmniHtmlEditorCommands.Clipboard`). Couper et copier passent par le
  navigateur, avec repli sur le presse-papiers asynchrone (texte seul) s'il refuse. Coller lit le
  presse-papiers asynchrone : le navigateur peut demander l'autorisation, un refus ne colle rien, et le
  contenu passe par le même assainissement (politique comprise) que Ctrl+V.
- `InsertParagraph` : coupe le bloc au curseur en deux paragraphes, comme Entrée.
- Tableaux (liste `OmniHtmlEditorCommands.Table`, qui commence par `InsertTable`) : `AddRowAbove`,
  `AddRowBelow`, `DeleteRow`, `AddColumnBefore`, `AddColumnAfter`, `DeleteColumn`, `MergeCellRight`,
  `MergeCellDown`, `SplitCell` et, hors de cette liste, l'action `SetCellSpan` (argument
  `lignesxcolonnes`, `2x3` ; sans argument, la cellule revient à `1x1`). Elles agissent sur la cellule au curseur, tiennent compte des
  `rowspan`/`colspan` existants, et ne sont actives que le curseur dans une cellule. Une fusion qui
  couperait une autre cellule fusionnée ne change rien ; le contenu des cellules absorbées rejoint la
  cellule, séparé par un saut de ligne. Supprimer la dernière ligne ou colonne retire le tableau.

Seules `Copy` et `InsertTable` ont une icône par défaut ; les autres affichent leur libellé localisé, et `Icon`
en pose une.

### Position du curseur

`SelectionChanged` (`EventCallback<OmniHtmlEditorSelection>`) est levé dans la face visuelle quand le
curseur ou la sélection s'arrête ailleurs (après 120 ms de calme, et seulement si la réponse
change). `OmniHtmlEditorSelection` porte `IsCollapsed` (un simple curseur) et `Ancestors`, les
éléments qui contiennent le début de la sélection, du plus proche au plus lointain, la surface
exclue : chaque `OmniHtmlEditorSelectionNode` donne `TagName` (minuscules), `CssClasses` et
`DataAttributes` (clés complètes, `data-eid`). `Closest("aside")` et `ClosestWithClass("akn-note")`
cherchent dans cette chaîne. La surface ne calcule rien tant que personne n'écoute ; la face source ne
lève jamais l'événement. Une commande `Custom` lit la dernière position par
`OmniHtmlEditorCommandContext.Selection` (null en face source ou sans écouteur).

`OmniHtmlEditorCommand.Pressed` (`Func<OmniHtmlEditorSelection?, bool>?`) fait d'une commande une
bascule : son bouton porte `aria-pressed`, vrai quand la fonction répond vrai pour la position
courante (null en face source ou avant le premier rapport). Une telle commande suffit à faire
rapporter la position par la surface, même sans `SelectionChanged`, et remplace l'état enfoncé
intégré d'une action.

```csharp
OmniHtmlEditorCommand.Create("note", "Note", InsertNoteAsync) with
{
    Pressed = selection => selection?.ClosestWithClass("akn-authorial-note") is not null
};
```

### Clavier

Dans la surface : Ctrl+B, Ctrl+I, Ctrl+U (navigateur), Ctrl+Z pour annuler, Ctrl+Y ou Ctrl+Maj+Z pour
rétablir, Ctrl+K pour ouvrir le champ de lien, Tab et Maj+Tab pour passer d'une cellule de tableau à
l'autre (hors tableau, Tab quitte l'éditeur). Dans le champ de lien, Entrée applique et Échap ferme.
L'historique vit en .NET : une rafale de frappe (regroupée au quart de seconde) et une commande sont
chacune une étape, et l'annulation du menu contextuel du navigateur y est aussi renvoyée.

### Assainissement et CSP

La valeur est toujours passée par la liste blanche HtmlSanitizer : balises de texte, titres, listes,
citations, code, liens `http`, `https`, `mailto`, `tel` (avec `rel="noopener noreferrer"`), tableaux
(`colspan`, `rowspan`) et règle horizontale. Le seul attribut de présentation admis est `class`,
restreint à `omni-align-left|center|end|justify` et `omni-font-size-small|normal|large|xlarge`.
Les conteneurs de présentation d'un collage (`font`, `section`, `article`...) sont retirés en gardant
leur texte ; un script, une feuille de style, un document intégré ou un contrôle de formulaire part
avec son contenu.

Le collage et le dépôt ne passent jamais tels quels : le HTML du presse-papiers est assaini en .NET
avant d'être inséré, un texte brut devient des paragraphes échappés. L'alignement et la taille sont
posés en classes parce que les commandes natives écriraient un attribut `style`. Les jointures de
paragraphes les plus courantes (effacement en début ou en fin de bloc, suppression ou frappe sur une
sélection qui traverse des blocs) et le collage de blocs sont faits par le script lui-même, parce que
le moteur d'édition du navigateur y crée des `span` stylés qu'une CSP stricte refuse. Une jointure
dans une liste ou un tableau reste au navigateur ; il peut alors écrire un attribut `style`, refusé par
une CSP stricte (signalé dans la console, jamais appliqué) et retiré par l'éditeur à la normalisation
suivante.

Pour afficher ailleurs la valeur avec ses alignements et tailles, placez-la dans un conteneur de classe
`omni-rich-text` : la feuille du paquet y porte les règles des classes ci-dessus et des tableaux,
citations et blocs de code.

### Politique d'assainissement de l'hôte

La politique d'une extension (`OmniHtmlEditorExtension.SanitizerPolicy`, un `OmniHtmlSanitizerPolicy`)
élargit la liste blanche pour le balisage propre à l'hôte ; une extension peut ne porter que cela,
et les politiques de plusieurs extensions se fusionnent. Elle s'applique partout où l'éditeur assainit : valeur liée, frappe, collage et dépôt,
`InsertHtmlAsync`, `SetHtmlAsync`, résultat des commandes, face source et son aperçu.

```csharp
private static readonly OmniHtmlSanitizerPolicy Policy = new()
{
    AdditionalTags = ["aside", "img", "colgroup", "col"],
    AdditionalAttributes = ["contenteditable"],
    AdditionalTagAttributes = new Dictionary<string, IReadOnlyList<string>> { ["img"] = ["src", "alt"] },
    AdditionalCssClasses = ["akn-authorial-note"],   // ou AllowAnyClass = true
    AllowDataAttributes = true                       // tous les data-*
};


private sealed class PolicyExtension : OmniHtmlEditorExtension
{
    public override OmniHtmlSanitizerPolicy SanitizerPolicy => Policy;
}
// <OmniHtmlEditor Extensions="[new PolicyExtension()]" ... />
```

- `AdditionalAttributes` vaut pour tout élément admis ; `AdditionalTagAttributes` pour les seuls
  éléments nommés (ci-dessus, `src` reste retiré d'un paragraphe).
- Le script de la surface suit la même politique en rangeant le document : il garde les classes
  admises (toutes avec `AllowAnyClass`) et ne déballe plus un `span` porteur d'un autre attribut.
  Un élément de section admis (`aside`, `figure`...) n'est jamais laissé dans un paragraphe.
- La politique ne peut jamais rouvrir ce que la CSP et la sûreté interdisent : `script`, `style`,
  `iframe`, `frame`, `object`, `embed`, `base`, `link`, `meta`, `template`, `svg`, `math`, contrôles et
  formulaires, attributs `on*`, `style`, `srcdoc`, `action`, `formaction`, `http-equiv` et noms à
  espace de noms. Les nommer lève `ArgumentException` au rendu de l'éditeur plutôt que d'être ignoré.
- Les adresses de `href`, `src`, `cite`, `poster` et `longdesc` restent limitées à `http`, `https`,
  `mailto`, `tel` ou relatives : une adresse `javascript:` ou `data:` est retirée. Seule exception,
  `AllowImageDataUris` admet une image portée par son `src` en `data:` (PNG, JPEG, GIF ou WebP en
  base 64, sur `img` seulement, qui doit être admis) ; une image SVG reste refusée.
- L'éditeur garde le sanitiseur construit pour une instance de politique : une instance statique
  évite de le reconstruire.

### Casse, caractères spéciaux, import de tableau, blocs

Cinq commandes intégrées, à placer dans `Commands` comme les autres (aucune n'est dans la barre par
défaut) :

- `ChangeCase` : une liste (majuscules, minuscules, casse de titre) qui réécrit le texte sélectionné
  nœud de texte par nœud de texte, donc sans toucher au gras, aux liens ni aux éléments autour.
- `InsertSpecialCharacter` : un panneau de caractères par catégorie (courants, monnaies, flèches,
  mathématiques, grec, juridique), avec une recherche sur le nom localisé ; le caractère choisi est tapé
  au curseur (échappé en face source).
- `ImportTable` : un panneau de fichier qui lit un CSV ou un TSV (cellules entre guillemets, guillemets
  doublés, sauts de ligne dans une cellule), plus les formats des lecteurs d'extension, montre un aperçu,
  propose la première ligne en en-tête (cochée d'office quand elle ne contient aucun nombre) et insère le
  tableau au curseur. 100 lignes (plus l'en-tête) et 50 colonnes au plus, 10 Mo.
- `ShowBlocks` : une bascule qui dessine en pointillé le contour de chaque bloc du document.
- `Highlight` : surligne le texte sélectionné (élément `mark`, admis par la liste blanche) ; le curseur
  dans un surlignage, elle le retire.

`ChangeCase` et `ShowBlocks` sont désactivées en face source.

### Caractères de la langue saisie

`Characters` (`IReadOnlyList<string>?`, null par défaut, aucune palette) pose une `OmniCharacterPalette` sous la
surface d'édition : les lettres particulières de la langue que l'utilisateur tape (é è ê à ç œ « » en français,
ä ö ü ß en allemand). L'hôte fournit le jeu, et le change quand la langue change ; le paquet ne contient aucune
table de langue. Un clic tape le caractère au curseur par l'insertion de texte de la surface, comme une frappe :
une seule étape d'historique, `ValueChanged` levé. La palette n'apparaît qu'en face visuelle, liste non vide,
éditeur modifiable (ni `ReadOnly` ni `Disabled`) ; elle porte `aria-controls` vers la surface.

```razor
<OmniHtmlEditor @bind-Value="Text" Characters="@(["é", "è", "ê", "à", "ç", "œ", "«", "»"])" />
```

### Extensions

`Extensions` (`IReadOnlyList<OmniHtmlEditorExtension>`) : ce qu'une application ajoute à l'éditeur, sans
jamais toucher la surface elle-même. Une extension est une classe qui dérive
d'`OmniHtmlEditorExtension` et ne redéfinit que ce qu'il lui faut ; plusieurs s'appliquent dans l'ordre.
Les extensions sont comparées par instance : un parent peut repasser une nouvelle liste à chaque rendu.

| Membre | Rôle |
|---|---|
| `Commands`, `ArrangeToolbar(toolbar)` | Commandes apportées ; par défaut ajoutées après un séparateur, `ArrangeToolbar` peut réordonner toute la barre. |
| `SanitizerPolicy` | Fusionnée avec celles des autres extensions (`OmniHtmlSanitizerPolicy.Merge`) ; n'élargit que dans les limites de toute politique. |
| `Shortcuts` | `new OmniHtmlEditorShortcut("Ctrl+Shift+N", "nom-de-commande")` ; Ctrl vaut aussi Cmd. Ctrl+Z, Ctrl+Y, Ctrl+Maj+Z et Ctrl+K restent à l'éditeur ; une combinaison en double ou une commande introuvable lève une exception au rendu. |
| `InlineElements` | `new OmniHtmlEditorInlineElement(".note[data-marker]", context => ...)` : un clic sur l'élément (le plus proche qui correspond) appelle la fonction avec l'élément (`Element`, `Text`) ; `SetTextAsync` (texte brut, l'élément et ses attributs gardés), `ReplaceAsync` et `RemoveAsync` le changent en une étape d'historique. |
| `ContextMenu` | Commandes du menu ouvert au clic droit ou à la touche menu dans la face visuelle, à la place de celui du navigateur, servi par le moteur de menu commun du paquet (`omni-focus.js`). Un clic droit sur un élément en ligne le sélectionne, pour que les commandes agissent sur lui ; `Enabled` est évalué pour la sélection où le menu s'ouvre. |
| `TableReaders` | `new OmniHtmlEditorTableReader([".xlsx"], (nom, flux) => ...)` : lignes de cellules pour un format que `ImportTable` ne lit pas lui-même (une feuille lue par un serveur). |
| `TracksSelection`, `OnSelectionChangedAsync` | Reçoit la position du curseur, comme `SelectionChanged`. |
| `SuggestsText`, `SuggestAsync(texteAvant)` | Propose la suite après le curseur quand la frappe marque une pause (au moins cinq caractères avant lui dans son texte) : affichée en grisé, Tab la tape, Échap ou toute autre touche l'écarte ; elle n'entre jamais dans la valeur. La première extension qui propose l'emporte. |
| `Proofreader` | Relecteur d'orthographe ou de grammaire de l'hôte (`OmniHtmlEditorProofreader`), voir [Relecture](#relecture). |

#### Relecture

L'éditeur ne contient ni moteur ni dictionnaire : un `OmniHtmlEditorProofreader` de l'hôte (correcteur Hunspell,
liste de mots, vérification grammaticale) relit le texte de la face visuelle. Rien ne change tant qu'aucune
extension n'en fournit.

- Le texte part bloc par bloc (paragraphe, titre, élément de liste, cellule, citation), le balisage en ligne
  traversé (un mot en gras reste dans sa phrase), les éléments non éditables (`contenteditable="false"`) et le
  texte proposé après le curseur exclus. Chaque bloc porte sa langue : l'attribut `lang` de l'élément le plus
  proche qui en a un, dans la surface ou autour de l'éditeur, puis celle de la page.
- `CheckAsync(textes, jeton)` est appelé au montage, puis quand la frappe marque une pause (700 ms), pour les seuls
  blocs dont le texte a changé ; il rend des `OmniHtmlEditorProofreadingIssue` (indice du texte, début, longueur,
  `Kind`, `Message`). Une exception du relecteur écarte sa réponse sans toucher au document. Quand rien n'a été
  relu (éditeur verrouillé par `ReadOnly` ou `Disabled`, face source, tous les relecteurs en échec), aucun bloc
  n'est tenu pour juste : ils sont redemandés à la relecture suivante.
- Les passages sont soulignés par des surligneurs nommés de la CSS Custom Highlight API (`omni-proofreading-spelling`
  en couleur de danger, `omni-proofreading-grammar` en couleur d'information) : le document n'est jamais modifié et
  la valeur reste celle de l'utilisateur. Un navigateur sans cette API ne souligne rien.
- Un clic droit (ou la touche menu) sur un passage souligné ouvre le menu de l'éditeur, même sans `ContextMenu` :
  le `Message` du passage, puis les corrections de `SuggestAsync` (cinq au plus, demandées à l'ouverture, ce qui
  laisse les suggestions coûteuses hors de la frappe ; fermer le menu ou retirer l'éditeur annule le jeton d'une
  demande encore en cours), « Ignorer » (ce passage, tant que son bloc garde son texte),
  « Tout ignorer » (`CanIgnoreAll`, `IgnoreAllAsync`) et « Ajouter au dictionnaire » (`CanAddToDictionary`,
  `AddToDictionaryAsync`), suivis des commandes des extensions. Une correction remplace le passage comme une frappe :
  une étape d'historique et `ValueChanged`. Après « Tout ignorer » ou un ajout au dictionnaire, tout est relu.

Le contexte d'une commande (`OmniHtmlEditorCommandContext`) offre aussi, en face visuelle :
`ReplaceClosestAsync(selecteur, html)` (remplace l'élément le plus proche autour de la sélection, par
exemple une formule, et dit s'il l'a trouvé), `GetSelectedTextAsync()` et `InsertTextAsync(texte)` (texte
brut tapé sur la sélection). La sélection gardée pendant qu'un dialogue était ouvert compte. Pour une
cellule, `OmniHtmlEditorSelectionNode.ColumnSpan` et `RowSpan` donnent sa fusion.

```csharp
public sealed class NoteExtension : OmniHtmlEditorExtension
{
    private static readonly OmniHtmlEditorCommand AddNote = OmniHtmlEditorCommand.Create(
        "add-note", "Ajouter une note", context => context.InsertHtmlAsync("<span class=\"note\">Note</span>"));

    public override IReadOnlyList<OmniHtmlEditorCommand> Commands => [AddNote];
    public override OmniHtmlSanitizerPolicy SanitizerPolicy { get; } = new() { AdditionalCssClasses = ["note"], AllowDataAttributes = true };
    public override IReadOnlyList<OmniHtmlEditorShortcut> Shortcuts { get; } = [new("Ctrl+Shift+N", "add-note")];
    public override IReadOnlyList<OmniHtmlEditorInlineElement> InlineElements { get; } =
        [new(".note", context => context.ReplaceAsync($"<span class=\"note\">{WebUtility.HtmlEncode(context.Text)} (relue)</span>"))];
}
```

### Traitement de texte

`OmniHtmlEditor` sert de traitement de texte léger avec trois réglages, désactivés par défaut :
`Sheet` pose une page blanche centrée sur un fond gris, la barre d'outils collée en haut (classes
`omni-document-editor omni-document-editor--sheet` sur le cadre) ; `ShowStatusBar` ajoute sous
l'éditeur une barre d'état avec le nombre de mots et de caractères (espaces compris, `WordCount` et
`CharacterCount`) et deux boutons d'export ; `Commands="OmniHtmlEditorCommands.Document"` donne la barre
d'un traitement de texte (style du paragraphe, taille du texte, gras, italique, souligné, barré,
listes, retraits, alignements, tableau de trois lignes sur trois, lien, effacement, historique).
Sans `Sheet` ni `ShowStatusBar`, l'éditeur est rendu seul, sans cadre. Le cadre porte le nom
accessible de l'éditeur, « Traitement de texte » par défaut avec `Sheet`.

```razor
<OmniHtmlEditor @bind-Value="report" Label="Rapport" Sheet="true" ShowStatusBar="true"
                Commands="OmniHtmlEditorCommands.Document" FileName="rapport" Rows="20" />
```

- `ExportHtmlAsync()` rend un fichier HTML complet (`lang` de la culture courante, `title` tiré de
  `DocumentTitle`, sinon du premier titre de niveau 1). Les classes d'alignement et de taille y
  deviennent les déclarations équivalentes sur les éléments, pour que le fichier se lise de même dans
  un navigateur ou un traitement de texte ; ce fichier est remis à l'utilisateur, jamais rendu dans la
  page.
- `ExportTextAsync()` rend le texte brut : blocs séparés par une ligne vide, puces en `- `, listes
  numérotées en `1. `, cellules séparées par des tabulations.
- Les boutons de la barre d'état téléchargent `FileName.html` et `FileName.txt` par
  `omni-document-editor.js` (un `Blob` et un lien de téléchargement).
- `Disabled` verrouille la page, `Rows` en donne la hauteur minimale.

## OmniCharacterPalette : lettres de la langue saisie

Une rangée de boutons, un par caractère fourni par l'hôte ; un clic lève `OnSelect` avec le caractère.
`OmniHtmlEditor.Characters` en pose une sous l'éditeur ; seule, elle sert tout champ que l'hôte complète
lui-même.

| Paramètre | Rôle |
| --- | --- |
| `Characters` | Obligatoire, `IReadOnlyList<string>` : un caractère ou un graphème par élément (`"é"`, `"«"`, `"ß"`, une lettre suivie d'un accent combinant) ; les éléments vides sont ignorés. |
| `OnSelect` | `EventCallback<string>`, le caractère choisi dans la casse affichée. |
| `Label` | Nom accessible de la barre ; null donne « Caractères spéciaux » localisé. |
| `UppercaseLabel` | Nom accessible de la bascule majuscules ; null donne « Majuscules » localisé. |
| `ShowUppercaseToggle` | Vrai par défaut ; faux retire la bascule (Maj+clic reste actif). |

- Majuscules : la bascule (`aria-pressed`, glyphe ⇧) n'apparaît que si au moins un caractère a une forme
  majuscule distincte (`ToUpper` en culture invariante). Pressée, les boutons montrent et rendent les
  majuscules ; Maj+clic rend la majuscule d'un seul caractère. Un caractère sans majuscule (ponctuation, ß,
  que la culture invariante ne change pas) reste tel quel. `IsUppercase` lit l'état de la bascule.
- Focus : le `mousedown` de chaque bouton est neutralisé, si bien qu'un clic ne retire ni le focus ni la
  sélection d'un éditeur voisin. Au clavier : `role="toolbar"` et des boutons natifs atteints par Tab,
  actionnés par Entrée ou Espace, comme la barre d'`OmniHtmlEditor` (pas de focus itinérant).
- Chaque bouton est nommé « Insérer é » (`CharacterPaletteInsert`), dans les 24 langues. Chaque cible garde
  44 px de large et, par une couche transparente, 44 px de haut ; le bouton lui-même suit la densité.

```razor
<OmniCharacterPalette Label="Lettres allemandes" Characters="@(["ä", "ö", "ü", "ß"])" OnSelect="Append" />
```

## OmniMailTemplateEditor et OmniMailRenderer : modèles de mails

Un modèle de mail est du HTML de l'éditeur où des variables s'écrivent `{{nom}}` (une lettre, puis lettres,
chiffres, `_`, `.` ou `-`). `OmniMailTemplateEditor` l'écrit ; `OmniMailRenderer`, une classe statique sans
navigateur ni feuille de style, le rend côté serveur avec les vraies valeurs, au moment d'envoyer le mail.
Le paquet n'envoie rien : `OmniMailRenderResult` va à l'expéditeur de l'application.

### L'éditeur

| Paramètre | Rôle |
| --- | --- |
| `Value`, `ValueChanged` | Le corps du modèle (`@bind-Value`), HTML assaini avec ses `{{variables}}`. |
| `Subject`, `SubjectChanged` | L'objet du modèle (`@bind-Subject`), texte brut avec ses `{{variables}}`. |
| `ShowSubject` | Vrai par défaut ; faux retire le champ de l'objet et l'objet rendu au-dessus de l'aperçu. |
| `Variables` | `IReadOnlyList<OmniMailVariable>` : `Name`, `Label`, `SampleValue` (valeur de l'aperçu, le libellé entre crochets sinon), `Description` (ajoutée à l'infobulle du bouton). Un nom invalide lève `ArgumentException`. |
| `Theme` | `OmniMailTheme` de l'aperçu, celui avec lequel le mail partira ; `OmniMailTheme.Default` sinon. |
| `Label` | Nom des onglets ; « Modèle de mail » localisé sinon. |
| `Commands`, `Rows` | Barre et hauteur du corps, comme `OmniHtmlEditor`. |
| `Disabled`, `ReadOnly` | Verrouillent l'objet, le corps et les boutons de variables et de blocs ; l'aperçu reste lisible. |
| `OnSendTest` | Affiche « Envoyer un essai » : le mail rendu tel qu'il partira (attributs `style`, valeurs d'exemple, `OmniMailRenderResult`) est remis à l'hôte, qui l'envoie à qui le vérifie dans une vraie messagerie ; le paquet n'envoie rien. Le bouton reste occupé jusqu'à la fin de la tâche ; sans gestionnaire, pas de bouton. |
| `OnSendTestError` | Reçoit l'exception d'`OnSendTest` ; sans gestionnaire, elle remonte au moteur de rendu. |

- Variables : un bouton par variable (« Insérer une variable », icône `Tag`, infobulle `{{nom}}`) écrit le jeton
  au curseur du dernier champ qui a eu le focus, l'objet ou le corps (le curseur que l'éditeur mémorise, sa fin
  s'il n'en a jamais eu). Un clic laisse le focus où il est, si bien que le curseur ne bouge pas ; au clavier,
  le bouton atteint par Tab écrit au curseur mémorisé. Dans la face source du corps, le jeton se tape.
- Jetons : chaque `{{nom}}` du texte du corps est surligné (fond de la couleur d'accent atténuée) par la CSS Custom
  Highlight API, comme les passages de la relecture : le document n'est pas modifié et la valeur reste le modèle ;
  les marqueurs de blocs le sont aussi ;   le surlignage suit la frappe et revient avec la face visuelle. Un navigateur sans cette API ne surligne rien.
  L'adresse d'un lien n'est pas du texte et n'est donc pas surlignée.
- Onglets : « Modèle » (le corps) et « Aperçu », les deux panneaux restant rendus, si bien que l'éditeur garde
  son curseur et qu'une variable s'insère aussi depuis l'aperçu.
- Aperçu : le mail rendu par `OmniMailRenderer` avec les valeurs d'exemple, refait seulement quand le modèle,
  l'objet, les variables ou le thème changent, et seulement pendant que l'onglet est affiché. Il est dessiné
  par `omni-mail-preview.js` dans une racine fantôme (la feuille de la page ne l'atteint pas), ses déclarations
  écrites par le serveur dans `data-omni-mail-look` et rendues par le CSSOM (voir le contrat CSP) ; un lien de
  l'aperçu ne mène nulle part.

### Le rendu

`OmniMailRenderer.Render(modèle, valeurs, options)` rend un `OmniMailRenderResult` : `Html` (document complet),
`Text` (texte brut de repli, chaque lien suivi de son adresse), `Subject` (sur une ligne) et `MissingVariables`
(les noms sans valeur, absents ou nuls, chacun une fois, dans l'ordre d'apparition, remplacés par rien).
`FindVariables(modèle)` liste les variables d'un modèle.

- Sûreté : le modèle est assaini comme la valeur de l'éditeur ; une valeur est écrite comme texte, jamais lue
  comme balisage ; l'adresse d'un lien faite d'une variable n'est gardée que si elle est une adresse absolue
  `http`, `https` ou `mailto`, le lien perdant son adresse sinon.
- Mise en page : tableaux de présentation, chaque style écrit sur son élément (Gmail et Outlook ignorent les
  feuilles de style et les classes), classes d'alignement et de taille de l'éditeur changées en déclarations,
  carte de `Width` pixels (600 par défaut, de 320 à 1200), tenue pour Outlook par un tableau de largeur fixe
  dans ses commentaires conditionnels. Pas de mode sombre automatique, de police web ni de requête média : peu
  de clients les suivent, et le mail se lit pareil sans eux.
- Options (`OmniMailRenderOptions`) : `Theme`, `Subject` et `Preheader` (texte brut avec variables ; la ligne
  d'aperçu des listes de mails, masquée dans le mail), `Language` (`lang`, culture d'interface sinon), `Width`.
- Thème (`OmniMailTheme`) : couleurs, police et rayon littéraux, puisqu'un client de messagerie ne résout
  aucune variable CSS. `OmniMailTheme.From(OmniThemePreset, OmniAppearance)` et `From(OmniThemePalette,
  OmniAppearance)` les lisent dans un thème ou une palette du paquet, `FromTokens` dans tout jeu de jetons
  `--omni-*` ; un jeton absent ou non littéral (`var()`, `color-mix()`) garde la valeur par défaut. La police
  est suivie d'Arial, Helvetica, sans-serif. Un thème posé à la main dont une valeur n'est pas littérale fait
  lever `ArgumentException` au rendu.
  Les bandeaux prennent `Info`, `Success`, `Warning`, `Danger` (le filet, voir `BannerRule`) et leurs `…Subtle` (le fond), lus dans
  `--omni-color-info`, `--omni-color-success`, `--omni-color-warning`, `--omni-color-danger` et leurs jetons `-subtle`.

### Les blocs

Trois blocs complètent les variables de texte, tous en HTML statique de tableaux de présentation, chaque style
écrit sur son élément, sans script : Outlook (moteur Word), Gmail et les clients web les dessinent pareil.

| Bloc | Écriture dans le modèle | Rendu |
| --- | --- | --- |
| Bouton d'action | Un paragraphe qui contient `{{#button nom}}` ; son texte (variables écrites) est le libellé. | Tableau d'une cellule de fond `Accent`, lien `display:inline-block` rembourré de couleur `OnAccent`, rayon du thème ; alignement de l'éditeur (centré, à droite) repris par `align`. L'adresse est la valeur de `nom`, gardée seulement en `http`, `https` ou `mailto` absolue (le bouton perd son lien sinon, comme un lien). Libellé vide : aucun bouton. Texte brut : « libellé (adresse) ». |
| Bandeau | Un paragraphe qui contient `{{#banner ton}}`, `ton` valant `info` (défaut), `success`, `warning` ou `danger`. | Tableau pleine largeur de la carte, fond `…Subtle` du ton, filet de 4 px du ton au début (aucun avec `OmniMailTheme.BannerRule = false`), texte `Text` ; le contenu du paragraphe (mise en forme et liens) est gardé. Paragraphe vide : aucun bandeau. |
| Variable de bloc | `{{nom}}` seul dans son paragraphe (ou élément de liste, cellule, citation), `nom` étant une clé de `OmniMailRenderOptions.Blocks`. | Le fragment HTML de l'hôte, assaini, à la place du paragraphe (ou dans l'élément de liste, la cellule, la citation). Écrit au milieu d'une ligne, le jeton prend le texte brut du fragment. |

- Déclaration : l'hôte passe ses fragments dans `OmniMailRenderOptions.Blocks` (`IReadOnlyDictionary<string, string?>`) ;
  un nom présent là est une variable de bloc, un nom des valeurs une variable de texte, toujours échappée ; un
  nom présent des deux côtés fait lever `ArgumentException`. Un fragment nul ne rend rien et figure dans
  `MissingVariables`, comme l'adresse absente d'un bouton.
- Assainissement du fragment, par une liste blanche propre aux mails : éléments de texte, de liste et de
  tableau (`p`, `div`, `span`, `strong`, `em`, `ul`, `ol`, `li`, `table`, `thead`, `tbody`, `tr`, `th`, `td`,
  `caption`, `h1` à `h6`, `pre`, `code`, `hr`, `mark`, `small`…), liens `http`, `https`, `mailto` (avec
  `rel="noopener noreferrer"`), attributs `href`, `colspan`, `rowspan`, `title` et `style`, ce dernier réduit
  aux propriétés `color`, `background-color`, `font-weight`, `font-style`, `font-size`, `text-align`,
  `text-decoration`, `vertical-align`, `white-space`, `width`, `padding*`, `border*`. Sont retirés : scripts,
  `style`, `link`, `iframe`, `object`, formulaires, images, SVG, MathML, gestionnaires `on*`, classes,
  identifiants, attributs `data-*`, et tout `style` qui contient `url(`, `expression(`, `@import`, une barre
  oblique inverse ou `javascript:`. Les couleurs opaques sont réécrites en hexadécimal (Outlook ignore `rgba()`).
  Les déclarations de l'hôte suivent celles du thème sur chaque élément, si bien qu'elles l'emportent.
- Aucune injection par les valeurs : les marqueurs et les jetons de blocs sont lus dans le modèle avant que les
  valeurs soient écrites, et le texte d'un fragment n'est jamais relu pour des jetons ; une valeur qui contient
  `{{#banner danger}}` s'écrit comme texte.
- `FindVariables` liste aussi les variables de blocs et les adresses de boutons (pas les tons des bandeaux).
  `MissingVariables` suit l'ordre d'apparition dans l'objet, le préheader puis le corps.
- Bandeau sans filet : `OmniMailTheme.BannerRule` (`bool`, `true`) à `false` dessine chaque bandeau en simple bande de
  la couleur pâle de son ton, sans bordure colorée sur aucun côté ; placé dans le premier paragraphe, il fait l'en-tête
  du mail. Les fonds se règlent par ton avec `InfoSubtle`, `SuccessSubtle`, `WarningSubtle` et `DangerSubtle`
  (`OmniMailTheme.Default with { BannerRule = false, WarningSubtle = "#fff3e0" }`).
- Cadre des blocs : `OmniMailTheme.BlockBorder` (`string?`, nul par défaut, donc aucun cadre) est la couleur d'un
  trait de 1 px aux coins arrondis, avec une marge intérieure, autour de chaque bloc qui délimite une zone : le
  bandeau et le fragment d'une variable de bloc seule dans son paragraphe, alors posé dans un tableau de
  présentation pleine largeur. Le bouton d'action et le fragment écrit au milieu d'une ligne ne sont pas encadrés.
  Avec le filet, celui-ci remplace le cadre au début du bandeau. Une couleur non littérale fait lever
  `ArgumentException`. Les deux réglages passent par le thème, donc l'aperçu d'`OmniMailTemplateEditor` (`Theme`)
  les montre aussi.

```csharp
// Un mail aux couleurs de l'apparence de son destinataire (son OmniAppearanceState, ou ses préférences lues en base).
var preset = apparence.Preset ?? OmniThemePresets.All[0];
var theme = OmniMailTheme.From(apparence.Palette is { } palette ? preset.With(palette) : preset);
var mail = OmniMailRenderer.Render(modele.Corps,
    new Dictionary<string, string?> { ["client"] = client.Nom, ["file"] = dossier.Numero, ["run.url"] = lienDuRun },
    new OmniMailRenderOptions
    {
        Subject = modele.Objet,
        Theme = theme,
        // Le tableau des étapes en échec, rendu par l'application ; écrit {{steps}} seul dans son paragraphe.
        Blocks = new Dictionary<string, string?> { ["steps"] = etapesEnEchecHtml },
    });
await expediteur.EnvoyerAsync(destinataire, mail.Subject, mail.Html, mail.Text);
```

```razor
<OmniMailTemplateEditor @bind-Value="corps" @bind-Subject="objet" Variables="variables" Theme="theme" />

@code {
    // "run.url" est proposé pour les boutons d'action, "steps" s'insère comme bloc.
    private readonly OmniMailVariable[] variables =
    [
        new("client", "Client"),
        new("run.url", "Lien du run") { Kind = OmniMailVariableKind.Link },
        new("steps", "Étapes en échec") { Kind = OmniMailVariableKind.Block, SampleValue = "<table><tr><td>build</td></tr></table>" },
    ];
}
```

## OmniCodeEditor

### Monaco chez l'hôte

`OmniCodeEditor` enveloppe Monaco (`monaco-editor`, construit AMD `min/vs`) sans l'embarquer : le
paquet ne contient que `omni-code-editor.js`. L'hôte sert le dossier `min/vs` depuis sa propre
origine, à l'adresse `MonacoPath` (défaut `lib/monaco-editor/min/vs`, relative à l'adresse de base de
la page) ; une adresse sur une autre origine est refusée : un schéma (`https:`, `data:`, `blob:`, `javascript:`),
un début en `//` ou une barre oblique inverse, un espace ou un caractère de contrôle lèvent une `ArgumentException`
au rendu, et le script de chargement fait la même vérification. Un chemin relatif (`assets/monaco/vs`, `./vs`) ou
absolu sur l'hôte (`/_content/x/vs`) reste accepté. Deux raisons à ce partage :

- Monaco pèse environ 14 Mo, plusieurs fois le budget de 2 Mo du paquet NuGet ;
- il ne fonctionne pas sous la CSP stricte du paquet : il écrit des éléments `style` à l'exécution.

La politique de la page doit donc autoriser `style-src 'unsafe-inline'`. C'est la seule exception :
les workers sont lancés directement depuis `base/worker/workerMain.js` (et non par l'enveloppe `blob:`
que Monaco utilise par défaut), si bien que `script-src 'self'` et `worker-src 'self'` suffisent. Un
`window.MonacoEnvironment` défini par l'hôte est respecté. Vérifié dans Chromium avec Monaco 0.52.2 :
chargement, frappe, liens, thème et diagnostic JSON (worker) sans aucune violation sous
`default-src 'self'; script-src 'self'; worker-src 'self'; style-src 'self' 'unsafe-inline'` ; sous
`style-src 'self'`, l'éditeur se monte mais son rendu est cassé et la console relève les styles
refusés.

Sous la politique stricte, choisissez `Engine="OmniCodeEditorEngine.PlainText"` : une zone de texte,
sans script. Si les fichiers de Monaco ne se chargent pas (absents, réseau coupé, délai de 30 s), le
composant garde sa zone de texte et le dit dans un message d'état ; la valeur reste éditable et liée.

### Paramètres

- `Language` (`string?`) : identifiant Monaco (`yaml`, `json`, `csharp`, `javascript`, `html`, `css`, `sql`,
  `markdown`, `plaintext`...), changeable à chaud ; nul, l'éditeur est en `plaintext`.
- `ReadOnly`, `ShowLineNumbers`, `Wrap` (retour à la ligne), `TabSize`, `Label` (`string?`), `AriaDescribedBy`.
- `Disabled` : Monaco passe en lecture seule, la zone de texte de repli est désactivée et l'éditeur est
  estompé ; la valeur ne change plus, que Monaco soit chargé ou non.
- `Height` : longueur CSS (`20rem`, `320px`, `50vh`, `%`), posée par le CSSOM ; toute autre valeur lève.
- `ShowStatusBar` : ligne, colonne et langage sous l'éditeur.
- `InteropModulePath` : module JavaScript de même origine qui porte l'interop de l'éditeur. Par défaut c'est celui du paquet, qui utilise Monaco dans le document de l'hôte ; un hôte peut y mettre son propre module, servi de la même origine.
- Valeur liée dans les deux sens : la frappe remonte au quart de seconde (et à la perte du focus), une
  valeur changée par l'application entre dans l'historique d'annulation de Monaco au lieu de l'effacer,
  sans écho.
- Thème : suit le `data-omni-theme` de l'`OmniThemeScope` englobant (clair, sombre, ou système) et prend
  pour fond la couleur de surface du cadre, si bien qu'un thème prédéfini repeint aussi l'éditeur.
  Monaco n'a qu'un thème par page : avec plusieurs éditeurs sous des portées d'apparence différente, le
  dernier repeint l'emporte.
- Localisation de Monaco : la culture courante choisit le paquet `nls.messages.*` quand Monaco en
  fournit un (allemand, espagnol, français, italien, japonais, coréen, russe, chinois).

### Liens

`Links` reçoit des `OmniCodeEditorLink(Name, Pattern)` : chaque motif est une expression régulière
JavaScript appliquée ligne à ligne, dont le premier groupe (ou toute la correspondance) devient un lien
Ctrl+clic. Le lien n'ouvre rien : `OnLinkActivate` reçoit le nom du motif, le texte lié et la ligne.
C'est le fournisseur de liens YAML d'une application cliente (`pipeline: nom`) rendu générique :

```razor
<OmniCodeEditor @bind-Value="Yaml" Language="yaml"
                Links="@([new OmniCodeEditorLink("pipeline", "^\\s*(?:-\\s*)?pipeline:\\s*[\"']?([\\w-]+)")])"
                OnLinkActivate="OpenPipeline" />
```

## OmniCodeViewer : extrait numéroté

`OmniCodeViewer` affiche du code en lecture seule, numéros de ligne à côté du texte (jamais dedans,
une copie à la main ne les prend pas). `FirstLineNumber` (1 par défaut, ramené à 1 en dessous) numérote
un extrait comme dans son fichier : les lignes 631 à 640 autour d'un constat gardent leurs numéros.
`HighlightedLines` (`IReadOnlyList<int>?`) et les numéros rapportés par `OnLinkActivate` comptent de la même façon.
`OnCopy` (`EventCallback<bool>`) est levé après une copie, avec l'acceptation du presse-papiers ;
`OmniCodeViewer` et `OmniCodeBlock` partagent le même en-tête (titre et bouton de copie).

`ActionsContent` ajoute des boutons à l'en-tête, `SidePanelContent` pose un panneau à côté du code (nommé
par `SidePanelLabel`). `ShowCopy` (vrai par défaut) et `ShowWrapToggle` montrent ou cachent les boutons Copier et
Retour à la ligne. `Language` est, comme partout dans la famille, l'identifiant d'un langage (`string?`, `plaintext`
quand il est nul) ; `OmniCodeBlock.Code` est un `string?` (nul vaut vide) et `OmniCodeBlock.Inline` affiche le code
dans une ligne de texte.

`EmptyText` (`string?`) dit ce que montre un code vide ; nul, le texte localisé du paquet (« Rien à afficher. »). `WrapChanged` (`EventCallback<bool>`) est levé quand le bouton de retour à la ligne est pressé, et `Wrap` se lie par `@bind-Wrap`. `CopiedFeedbackDuration` (`TimeSpan`, 2 secondes par défaut) fixe combien de temps le bouton de copie montre son résultat.

```razor
<OmniCodeViewer Code="@Extrait" Title="src/Service.cs" FirstLineNumber="631" HighlightedLines="@([635])" />
```

## OmniDiffViewer

`OmniDiffViewer` compare deux versions d'un texte avec l'éditeur de différences de Monaco, côte à côte
ou en une colonne (`Unified`), les changements marqués ligne par ligne et dans la ligne. Il partage le
Monaco d'`OmniCodeEditor` et ses conditions : l'hôte sert `min/vs` à `MonacoPath` et autorise
`style-src 'unsafe-inline'`. Sous la politique stricte, `Engine="OmniCodeEditorEngine.PlainText"` montre
les deux textes dans deux volets nommés, sans les différences marquées ; c'est aussi ce qui s'affiche
pendant le chargement de Monaco et, avec un message d'état, s'il ne se charge pas.

- `Original`, `Modified` et `ModifiedChanged` : le texte modifié n'est éditable que si `ReadOnly` est
  faux (il est vrai par défaut : une comparaison est d'abord une vue, un écran d'édition le passe à faux) ; l'original ne l'est jamais. `Disabled` verrouille aussi le texte modifié,
  `ReadOnly` faux compris (Monaco en lecture seule, volet de repli désactivé), et estompe la comparaison. Une modification remonte comme dans
  `OmniCodeEditor`, sans écho.
- `Language` (`string?`, `plaintext` quand il est nul), `Unified`, `Height` (longueur CSS posée par le CSSOM, toute autre valeur lève), `Label`
  (« Comparaison »), `OriginalLabel` (« Avant ») et `ModifiedLabel` (« Après ») ; textes et options
  changent à chaud.
- Navigation entre les changements : `GoToNextChangeAsync()` et `GoToPreviousChangeAsync()` (demande
  d'une application cliente, recette R-100) déplacent l'éditeur de différences de Monaco sur le
  changement suivant ou précédent (`goToDiff('next' | 'previous')`) et l'affichent, pour qu'une page
  câble ses propres boutons dans sa propre barre. Quand Monaco n'est pas monté (chargement, moteur
  `PlainText`, échec), que le composant est détruit ou que le circuit est perdu, elles ne font rien et ne
  lèvent rien. `ShowNavigation` (`bool`, `false`) dessine au-dessus de la comparaison une petite barre de
  deux boutons à icône seule (`OmniButton` fantôme, petit, chevrons haut et bas), nommés et infobullés
  « Modification précédente » / « Modification suivante » (clés `DiffViewerPreviousChange` et
  `DiffViewerNextChange`, traduites dans toutes les langues livrées), désactivés tant que Monaco n'est
  pas monté. La bibliothèque n'ajoute aucun raccourci clavier : seules les touches de navigation que
  Monaco lie lui-même dans son éditeur de différences s'appliquent, une fois l'éditeur au focus.
- `FillAvailableHeight` (`bool`, `false`, nommé comme `OmniDataGrid.FillAvailableHeight` ; demande d'une
  application cliente, recette R-101) : le composant prend la hauteur libre de son parent (un bloc qui a
  une hauteur, ou un conteneur flex en colonne), en colonne elle-même : la barre `ShowNavigation` en haut,
  la comparaison (l'hôte de Monaco, ou les deux volets de repli) sur le reste. `Height` devient alors la
  hauteur minimale de la comparaison (20rem quand elle est nulle) : en dessous, le composant garde ce
  plancher et c'est le parent qui défile. La classe est `omni-diff-viewer--fill`, aucun style en ligne ;
  Monaco se remet en page quand sa boîte change de taille (`automaticLayout`, déjà actif au montage, aucun
  observateur ajouté).
- `InteropModulePath` (`string`, module du paquet par défaut ; même rôle que sur `OmniCodeEditor`) : le
  module JavaScript de même origine par lequel le composant charge, monte et pilote l'éditeur de
  différences. Sous une politique de styles stricte, l'hôte y met un module qui monte Monaco dans un cadre
  à politique propre. Le module exporte `setHeight(root, height)`, `load(basePath, culture, timeout)`
  (promesse : Monaco disponible ou non), `mountDiff(host, dotnet, options)` (monté ou non ; `options` porte
  `original`, `modified`, `language`, `readOnly`, `inline`, `originalLabel`, `modifiedLabel`),
  `setDiff(host, original, modified)`, `configureDiff(host, options)` (mêmes clés, textes nuls),
  `goToDiff(host, 'next' | 'previous')` et `disposeDiff(host)`, et remonte les modifications par
  `dotnet.invokeMethodAsync('OnModifiedChanged', value)`. Un chemin qui quitte l'origine de la page lève,
  avec la règle de `MonacoPath`.
- Le composant est un `group` nommé ; chaque volet de repli est une `section` nommée dont le texte prend
  le focus pour défiler au clavier. Le thème suit l'`OmniThemeScope` englobant.
- La référence `DotNetObjectReference` du pont est libérée et l'éditeur détruit à la destruction du
  composant ; un composant détruit pendant le chargement du script libère aussitôt le module.
- Preuves : `DiffViewerComponentTests` (volets, saisie de repli, hauteur, chargement, échec, montage,
  mises à jour, passage au texte brut, libération) et `DiffViewerNavigationTests` (méthodes de
  navigation, barre `ShowNavigation`, `FillAvailableHeight`, côté .NET avec un module simulé). Monaco
  lui-même n'a pas été exercé dans un navigateur pour ce composant : la vitrine le montre en texte brut
  (boutons de navigation désactivés).

## OmniCodeBlock : commande ou secret à copier

`OmniCodeBlock` affiche en lecture seule un bloc de code, une commande ou un jeton, avec un bouton qui le
copie dans le presse-papiers ; le résultat est annoncé par une région live polie. Un secret est masqué à
l'écran (ses premiers et derniers caractères gardés, pour le reconnaître) jusqu'au bouton qui le révèle ;
la copie prend toujours la valeur entière. `Title` nomme le bloc (classe `omni-code-block__title`) et
`OnCopy` rapporte chaque copie. La copie passe par `omniInterop.js` : l'API asynchrone du
presse-papiers, ou une zone de texte cachée et la commande de copie quand elle est refusée.

`Secret` (faux par défaut) masque la valeur à l'écran jusqu'au bouton qui la révèle, la copie prenant toujours la valeur entière ; `VisibleCharacters` (4 par défaut) dit combien de caractères restent visibles à chaque bout tant qu'elle est masquée ; `CopiedFeedbackDuration` (`TimeSpan`, 2 secondes par défaut) fixe combien de temps le bouton montre la coche après une copie.

`AnsiMode` (`OmniAnsiMode`, `Render` par défaut, comme sur `OmniLogViewer`) dit ce que le bloc fait des séquences ANSI d'une sortie de commande
(`systemctl`, `docker`) : `Render` dessine les couleurs et attributs, `Hide` retire les séquences sans rien dessiner, `Raw` montre le texte tel qu'écrit.
Un code sans séquence ne change dans aucun mode. Le bouton de copie prend le texte sans séquences en `Render` et en `Hide`, et le texte brut en
`Raw` ; un `Secret` reste masqué dans tous les modes, sans séquence visible, et sa copie suit la même règle. `Inline` et `Wrap` s'appliquent au
contenu ANSI comme à tout autre. Voir [data-components.md](data-components.md) pour `OmniAnsiText` et `OmniAnsi`.

## OmniUnifiedDiff : diff unifié sans Monaco

`OmniUnifiedDiff` dessine un diff unifié (texte brut d'un `git diff`, ou fichiers déjà analysés par
`OmniUnifiedDiffParser`) sans Monaco ni style en ligne : une section repliable par fichier avec son
chemin, ce qui lui est arrivé et ses nombres de lignes ajoutées et retirées, puis ses blocs avec les
numéros d'avant et d'après côte à côte et les lignes ajoutées et retirées teintées. Un en-tête de bloc
malformé (nombres hors d'un `int`) est ignoré. Il marche donc sous la politique CSP stricte.
`ExpandedByDefault` (vrai par défaut) ouvre chaque section de fichier ; `FileActionsTemplate` ajoute des
actions à l'en-tête de chaque fichier.

`Diff` (`string?`) reçoit le texte du diff ; il est ignoré quand `Files` (`IReadOnlyList<OmniDiffFile>?`) est donné, ce qui sert à passer des fichiers déjà lus par `OmniUnifiedDiffParser.Parse` ou construits par l'hôte. `Collapsible` (vrai par défaut) permet de replier chaque fichier par son en-tête. `EmptyText` (`string?`) dit ce que montre un diff vide ; nul, le texte localisé du paquet (« Aucune modification. »).

## OmniInkCanvas : dessin à main levée

`OmniInkCanvas` est une zone de dessin : la souris, le stylet (avec sa pression) ou le doigt tracent des
lignes sur un `<canvas>`, que le composant garde comme une liste de traits et sait exporter en PNG
transparent. Un petit module (`omni-ink-canvas.js`) lit les événements de pointeur (`touch-action: none`
dans la feuille de style, pointeur capturé pour que le trait continue hors du canevas) et dessine ;
.NET garde la valeur. Aucun style en ligne, aucun script en ligne : la taille de la zone passe par des
propriétés CSS personnalisées posées par le script (CSSOM), comme les autres composants à hauteur réglable.

### Le trait : `OmniInkStroke` et `OmniInkPoint`

`OmniInkStroke(Color, Width, Points)` est un enregistrement immuable ; `OmniInkPoint(X, Y, Pressure)` en est un
point. `X` et `Y` sont **relatifs** à la zone, de 0 (bord gauche, bord haut) à 1 (bord droit, bord bas) : un
trait ne dépend pas de la taille à laquelle il a été dessiné, et se redessine à l'identique après un
redimensionnement. `Pressure` (`double?`, de 0 à 1) est la pression du stylet ; `null` pour une souris ou un
doigt. Une pression de 0,5 trace la ligne à l'épaisseur du trait, moins de pression l'amincit, plus l'épaissit.
Un trait d'un seul point est un point (un clic).

`Width` est l'épaisseur en pixels CSS **pour une zone large de `OmniInkStroke.ReferenceWidth` (600 pixels)** et
suit la largeur de la zone : sur une zone de 300 pixels, `Width = 2` trace une ligne de 1 pixel ; sur une zone
de 1200 pixels, de 4 pixels. Le dessin garde ainsi son aspect à toute taille, comme une image. À l'export,
une image de `w` pixels de large reçoit des lignes de `Width × w / 600` pixels. `Color` est une couleur CSS
(`#1a73e8`, `rgb(0 0 0)`, `navy`) ou `OmniInkStroke.ThemeColor` (`currentColor`), la couleur du texte du thème,
qui suit un changement de thème (clair ou sombre) sans que le dessin soit refait.

### Paramètres

- `Value` (`IReadOnlyList<OmniInkStroke>`, vide par défaut, jamais `null`) et `ValueChanged` : le composant ne
  modifie jamais la liste reçue, il en lève une nouvelle à chaque trait, annulation et effacement, et dessine
  celle que l'hôte donne quand ce n'est pas celle qu'il a levée en dernier. Sans liaison, il garde son dessin.
- `Color` (`string?`) : la couleur des prochains traits ; `null` (défaut) pour la couleur du texte du thème.
  `StrokeWidth` (`double`, 2 par défaut, au-dessus de zéro) : leur épaisseur, à la largeur de référence. Le
  champ couleur et le curseur de la barre d'outils les remplacent jusqu'à ce que le paramètre change.
- `Width` et `Height` : la taille de la zone, des longueurs CSS validées comme celles de `OmniLogViewer.Height`
  (chiffres, unité en minuscules ; `Width` : `px`, `rem`, `em`, `ch`, `vw`, `%`, au-dessus de zéro ; `Height` : `px`,
  `rem`, `em`, `vh`, `%`) ; une autre valeur lève `ArgumentException`. Par défaut 100 % de large et 12rem de
  haut. `AspectRatio` (`double?`, largeur divisée par hauteur) remplace `Height` : la hauteur suit la largeur
  (3 pour un cadre de signature trois fois plus large que haut) ; une valeur qui n'est pas un nombre au-dessus de
  zéro lève `ArgumentOutOfRangeException`.
- `ShowToolbar` (vrai par défaut) : la barre d'outils au-dessus de la zone, avec les composants du paquet :
  couleur (`OmniColorPicker`), épaisseur (`OmniSlider`), Annuler et Effacer (`OmniButton`).
- `Disabled` : rien ne se dessine, la barre est désactivée et la zone sort de l'ordre de tabulation. `ReadOnly` :
  le dessin reste visible et focalisable, rien ne se trace ni ne s'annule ni ne s'efface, la barre est
  désactivée (même contrat que les autres composants de saisie).
- `Label` (`string?`) : le nom accessible de la zone ; `null` donne le texte localisé « Zone de dessin ».

### Méthodes

- `UndoAsync()` retire le dernier trait et `ClearAsync()` les retire tous, chacun levant `ValueChanged` ; sur un
  dessin vide, rien. Ce sont les méthodes de l'hôte : elles marchent aussi en `ReadOnly` ou `Disabled`.
- `ExportPngAsync(int width, int height)` dessine les traits sur fond **transparent** et rend les octets d'un
  PNG de `width` par `height` pixels (1 à 8192, sinon `ArgumentOutOfRangeException`). Les positions relatives
  sont étirées sur l'image : donner le rapport de la zone garde ses proportions. Un trait en couleur de thème
  est écrit **en noir** (une image n'a pas de thème). `ExportDataUrlAsync(int width, int height)` rend la même
  image sous forme d'adresse `data:image/png;base64,…`. L'export exige le script : appelé après la fin de vie du
  composant, il lève `ObjectDisposedException`.

### Clavier et accessibilité

La zone est un `role="img"` nommé par `Label` et décrit par une aide masquée ; elle est focalisable (Tab, et un
appui dessus lui donne le focus). Quand elle a le focus, **Ctrl+Z** (Cmd+Z) annule le dernier trait et
**Suppr** efface le dessin ; chaque action est annoncée dans une zone `role="status"`. Les boutons de la barre
sont dans l'ordre de tabulation et nommés (nom et infobulle). Les couleurs viennent des jetons du thème
(clair et sombre) ; le dessin se redessine quand le thème, le schéma de couleur du système ou la taille de la
zone change. Les textes (zone, aide, barre, couleur, épaisseur, Annuler, Effacer, annonces) sont localisés
dans les 24 langues du paquet.

### Limites

Le tracé est lissé par des courbes entre les points (souris, doigt) ou segment par segment avec la pression
(stylet) ; il n'y a ni gomme au trait, ni rétablissement, ni sélection d'un trait. Le dessin à plusieurs doigts
n'est pas pris en charge : seul le pointeur principal trace.

## OmniDocumentViewer : document page par page

`OmniDocumentViewer` montre, en lecture seule, un document que l'hôte a converti : le composant ne connaît aucun format de fichier. `Pages` (`IReadOnlyList<OmniDocumentPage>`) porte les pages dans l'ordre de lecture ; chaque `OmniDocumentPage(Number, ImageUrl, Image, Html, WidthPt, HeightPt)` a exactement une source : l'adresse d'une image (`ImageUrl`), les octets d'un PNG (`Image`, dessiné par une adresse `data:` : l'hôte autorise `data:` dans `img-src`) ou du HTML (`Html`). Une page sans source, avec deux sources, avec un numéro sous 1, avec des octets qui ne sont pas un PNG ou avec une taille qui n'est pas un nombre positif fait lever `ArgumentException`, qui nomme la page. La liste est relue quand le paramètre reçoit une autre liste : la remplacer, ne pas la modifier sur place. `WidthPt` et `HeightPt` (points, 595,3 par 841,9 pour une page A4) donnent la taille de la page ; sans eux, une image garde sa largeur propre et le HTML prend une largeur A4. `Label` (`string?`) nomme la zone des pages, « Document » quand il est nul ; chaque page est un groupe nommé « Page n sur N ».

**HTML assaini une seconde fois.** Le HTML de l'hôte passe par la liste d'autorisations du paquet (celle d'`OmniHtmlEditor`, voir `OmniHtmlSanitizerPolicy`) augmentée d'un seul attribut, `data-oe-field`. Un script, un attribut de gestionnaire (`on*`), un `style`, un cadre intégré ou une adresse `javascript:` sont retirés ; une balise non permise disparaît. `SanitizerPolicy` (`OmniHtmlSanitizerPolicy?`) élargit cette liste (images, classes, attributs) et se fusionne avec la liste par défaut ; elle ne peut jamais autoriser ce qu'aucune politique n'autorise (un nom interdit lève `ArgumentException`). Le HTML n'est pas écrit comme du balisage : il est dessiné élément par élément dans l'arbre de rendu, si bien qu'aucun texte du document n'est lu comme du balisage. Comme `style` est retiré, un convertisseur qui a besoin de classes les déclare dans `SanitizerPolicy.AdditionalCssClasses` et les fournit par la feuille de l'application.

**Champs.** Un élément de `data-oe-field="tag"` est un champ : surligné, focalisable (`tabindex="0"`), il affiche son libellé dans l'infobulle du paquet (`data-omni-tip`, le même moteur que le reste du paquet) au survol et au focus, et lève `OnFieldClick` (`EventCallback<string>`, avec le tag) au clic et sur Entrée. `FieldLabels` (`IReadOnlyDictionary<string, string>?`) donne le libellé de chaque tag ; un tag absent ou un libellé vide affiche le tag lui-même. `HighlightFields` (vrai par défaut) à faux dessine un document sans champs et ne lève jamais `OnFieldClick`. **Les champs n'existent que sur les pages HTML** : l'image d'une page n'a pas de coordonnées de champs, rien n'y est surligné ni cliquable (demande de l'application cliente, PLAN-010). Les champs imbriqués lèvent chacun leur propre tag ; un lien dans un champ lève aussi le champ.

**Zoom et ajustement.** `Zoom` (`double`, 1 = 100 %, de 0,25 à 4, 1 si ce n'est pas un nombre) et `ZoomChanged` s'appliquent aux images et au HTML de la même façon (la propriété CSS `zoom` de chaque page). `Fit` (`OmniDocumentFit`, `Width` par défaut) : avec `Width`, le zoom est calculé par `omni-document-viewer.js` pour que la page la plus large remplisse la zone, à chaque redimensionnement et à chaque image chargée, puis rapporté par `ZoomChanged` ; avec `None`, la page reste au `Zoom` du lecteur. `FitChanged` est levé quand la barre d'outils change l'ajustement : le bouton d'ajustement le passe à `Width`, un zoom avant ou arrière le passe à `None` (les pas tombent sur des multiples de 25 %). Un `Zoom` que l'hôte change met fin à l'ajustement et prend la valeur ; un `Fit` que l'hôte change est pris tel quel. `ShowToolbar` (vrai par défaut) montre zoom arrière, zoom avant, ajustement à la largeur (bouton à bascule, `aria-pressed`) et l'indicateur « Page n / N » ; la page en vue est la page qui remplit le plus la zone, rapportée par le même script au défilement.

La différence avec `OmniImage.Fit` est voulue : `OmniImageFit` dit comment une image remplit son cadre (`Contain`, `Cover`, `Natural`), `OmniDocumentFit` dit si le zoom suit la largeur de la visionneuse. Le script n'écrit que le CSSOM (`--omni-document-zoom` sur la zone, `--omni-document-page-width` et `--omni-document-page-height` sur chaque page), jamais un attribut `style` : la CSP stricte tient. Les couleurs sont des jetons (thèmes clair et sombre sans règle propre) ; `--omni-document-viewer-height` (40rem) plafonne la zone qui défile, `--omni-document-background` et `--omni-document-page-background` remplacent le bureau et le papier.

```razor
<OmniDocumentViewer Pages="@Pages" FieldLabels="@Labels" @bind-Zoom="Zoom" OnFieldClick="OpenField" />
```

Limites : l'infobulle d'un champ s'ouvre au clavier comme à la souris (focus visible), mais le tactile n'a pas été éprouvé sur un écran réel. Le calcul de l'ajustement et de la page en vue est vérifié par des tests du script (`tests/js/document-viewer.test.mjs`) ; la page de la vitrine les montre, mais aucune sonde de navigateur ne les mesure encore, et le script reste hors de la couverture JavaScript des sondes tant qu'une sonde ne l'exerce pas.

## OmniDocumentEditor : document structuré

`OmniDocumentEditor` est un éditeur structuré minimal pour un document que l'hôte a converti : le composant ne connaît aucun format de fichier et le paquet ne référence aucune bibliothèque de documents (l'hôte transforme son document, par exemple un fichier de traitement de texte, en `OmniDocument`, puis applique en retour le jeu de changements). Il n'a ni barre de mise en forme ni édition de paragraphe : on modifie le texte des passages, on remplit des champs, on dessine sous un paragraphe.

**Le modèle.** `OmniDocument(Blocks)` porte des `OmniDocumentBlock(Id, Inlines, Drawing?)` (un paragraphe). Un paragraphe est une suite d'`OmniDocumentInline` : `OmniDocumentRun(Id, Text, StyleKey?)`, un passage de texte, ou `OmniDocumentField(Id, Tag, Label, Kind, Value?, Items?, AllowFreeText)`, un champ. `Kind` est un `OmniDocumentFieldKind` (`Text`, `Date`, `List`, `Number`). Une valeur est toujours une chaîne : une date s'écrit `yyyy-MM-dd`, un nombre en culture invariante (`1250.5`), une valeur de liste est l'un des `Items` (ou n'importe quel texte quand `AllowFreeText` est vrai). `OmniDocumentDrawing(Strokes, WidthPt, HeightPt)` est un dessin d'`OmniInkStroke` sur une feuille dont la taille est en points. Les identifiants sont ceux de l'hôte, uniques dans le document (blocs entre eux, passages et champs ensemble) : le jeu de changements les reprend, jamais un index. Le modèle est vérifié quand le document arrive : un identifiant vide ou en double, un champ de liste sans élément qui n'accepte pas le texte libre, une valeur qui ne convient pas au type du champ, un dessin sans taille utilisable lèvent une `ArgumentException` qui dit quoi corriger, `null` une `ArgumentNullException`. Le composant ne modifie jamais le document reçu.

**Le texte.** Un passage se modifie sur place, en texte brut sur une seule ligne (`contenteditable="plaintext-only"`, `"true"` dans un navigateur qui ne connaît pas cette valeur) : Entrée ne fait rien, un collage ou un glisser-déposer insère le texte brut du presse-papiers, sauts de ligne remplacés par des espaces. `omni-structured-document.js` relit le texte à chaque saisie et à la sortie du passage ; .NET dessine le texte une seule fois par document, si bien qu'un rendu n'écrase jamais ce que le lecteur a tapé. `StyleClasses` (`Func<string, string?>`) donne la classe CSS d'un passage d'après sa `StyleKey` opaque : c'est du rendu, le jeu de changements n'en parle pas.

**Les champs.** Un champ est une pastille (libellé et valeur, « à renseigner » sans valeur) qui ouvre, au clic, à Entrée ou à Espace, une bulle `OmniPopover` avec la saisie de son type : `OmniTextBox` pour `Text`, `OmniDatePicker` pour `Date` (son calendrier se déplie dans la bulle), `OmniNumeric` pour `Number`, `OmniDropDown` sur les `Items` pour `List`, `OmniAutocomplete` sur les `Items` quand `AllowFreeText` est vrai. Un choix s'applique dès qu'il est fait (un texte ou un nombre à la frappe) et la bulle reste ouverte ; Échap ou un clic dehors la ferme, Échap rendant le focus à la pastille. `Culture` ne règle que l'affichage de la date et du nombre sur la pastille : le sélecteur de date et le champ numérique suivent la culture de la page, comme tout le paquet.

**Les dessins.** Avec `AllowDrawings` (vrai par défaut), chaque paragraphe a un bouton « Insérer un dessin » (puis « Remplacer le dessin ») qui ouvre un `OmniDialog` autour d'un `OmniInkCanvas` aux proportions de la feuille (400 par 150 points pour un dessin neuf). Valider range les traits et un PNG transparent (`ExportPngAsync`, à `points x 96 / 72` pixels) dans le jeu de changements et dessine les traits sous le paragraphe, en SVG en ligne (attributs seuls, aucune adresse `data:`) ; le paragraphe montre chaque trait d'une seule ligne de son épaisseur, la pression du stylet ne façonne que le PNG. `AllowDrawings` faux montre encore les dessins du document.

**Le jeu de changements.** `OnChange` (`EventCallback<OmniDocumentChangeSet>`) est levé après chaque modification avec ce qui a changé depuis le document reçu en dernier : `Runs` (`OmniRunTextChange(RunId, Text)`), `Fields` (`OmniFieldValueChange(FieldId, Tag, Value?)`), `Drawings` (`OmniDrawingChange(BlockId, Drawing, Png)`), chacun dans l'ordre du document, `IsEmpty` disant s'il n'y a rien. Remettre un passage ou un champ à ce que dit le document le retire du jeu, et lève quand même `OnChange`. `GetChangeSet()` rend le jeu courant à tout moment ; `ResetChanges()` le vide et redessine le document tel que reçu. Donner une autre instance de `Document` repart de zéro ; garder la même garde les modifications. `ReadOnly` coupe toute modification (texte, pastilles, boutons de dessin). `Label` nomme la région (« Document » par défaut) ; chaque paragraphe est un groupe nommé « Paragraphe n sur N ».

```razor
<OmniDocumentEditor Document="@Order" StyleClasses="@StyleOf" OnChange="@(changes => Changes = changes)" />
```

La feuille de style (`14-document-editor.css`) n'emploie que des jetons : les thèmes clair et sombre n'ont pas de règle propre. Le calendrier d'un champ date est posé dans le flux de la bulle, car le placement fixe du sélecteur n'y tient pas (la bulle crée son propre repère de positionnement, ce qui renvoyait le calendrier hors de la fenêtre, mesuré en navigateur).

Limites : la saisie est vérifiée dans Chromium par la sonde `structured` (voir [testing.md](testing.md)) ; la composition IME, le tactile et Firefox ou Safari n'ont pas été éprouvés. Le dessin affiché sous un paragraphe est celui du dernier dessin validé : il n'y a ni dessin par passage ni édition d'un trait existant. Le thème sombre n'a pas été contrôlé à l'écran (les couleurs sont des jetons).

## Limites connues

- La surface visuelle s'appuie sur `document.execCommand`, déprécié mais sans remplaçant ; le
  comportement fin (listes imbriquées, fusion de blocs) suit le moteur du navigateur.
- La composition IME n'a pas été éprouvée sur un clavier asiatique réel.
- Le fonctionnement dans un navigateur réel est vérifié par une page d'essai hors dépôt ; la sonde CDP
  du catalogue ne couvre que la face source.
