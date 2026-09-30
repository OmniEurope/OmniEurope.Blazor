# Éditeurs : WYSIWYG, traitement de texte et code

La famille Editor couvre six composants :

| Composant | Rôle |
| --- | --- |
| `OmniHtmlEditor` | Éditeur WYSIWYG à face source HTML, barre de commandes extensible, sortie assainie ; sert aussi de traitement de texte léger. |
| `OmniCodeEditor` | Éditeur de code Monaco servi par l'hôte, avec repli en zone de texte brut. |
| `OmniDiffViewer` | Comparaison de deux versions d'un texte avec Monaco, repli en deux volets. |
| `OmniCodeViewer` | Code en lecture seule, numéroté, lignes surlignées et liens, bouton de copie. |
| `OmniCodeBlock` | Commande, extrait ou jeton à copier ; un secret reste masqué jusqu'à ce qu'on le révèle. |
| `OmniUnifiedDiff` | Diff unifié (`git diff`) dessiné sans Monaco, un bloc repliable par fichier. |

`OmniHtmlEditor` produit du HTML assaini ; `OmniCodeEditor` édite du texte brut ; les quatre autres affichent sans éditer.

## OmniHtmlEditor

### Deux faces

`Mode` choisit la face affichée, liée dans les deux sens par `ModeChanged` :

- `OmniHtmlEditorMode.Visual` (défaut) : le document mis en forme, édité sur place dans une surface
  `contenteditable` (`role="textbox"`, `aria-multiline="true"`) que pilote `omni-html-editor.js`.
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

Seule `Copy` a une icône par défaut ; les autres affichent leur libellé localisé, et `Icon`
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

## OmniCodeEditor

### Monaco chez l'hôte

`OmniCodeEditor` enveloppe Monaco (`monaco-editor`, construit AMD `min/vs`) sans l'embarquer : le
paquet ne contient que `omni-code-editor.js`. L'hôte sert le dossier `min/vs` depuis sa propre
origine, à l'adresse `MonacoPath` (défaut `lib/monaco-editor/min/vs`, relative à l'adresse de base de
la page) ; une adresse sur une autre origine est refusée. Deux raisons à ce partage :

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

- `Language` : identifiant Monaco (`yaml`, `json`, `csharp`, `javascript`, `html`, `css`, `sql`,
  `markdown`, `plaintext`...), changeable à chaud.
- `ReadOnly`, `ShowLineNumbers`, `Wrap` (retour à la ligne), `TabSize`, `Label` (`string?`), `AriaDescribedBy`.
- `Disabled` : Monaco passe en lecture seule, la zone de texte de repli est désactivée et l'éditeur est
  estompé ; la valeur ne change plus, que Monaco soit chargé ou non.
- `Height` : longueur CSS (`20rem`, `320px`, `50vh`, `%`), posée par le CSSOM ; toute autre valeur lève.
- `ShowStatusBar` : ligne, colonne et langage sous l'éditeur.
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
Ctrl+clic. Le lien n'ouvre rien : `LinkActivated` reçoit le nom du motif, le texte lié et la ligne.
C'est le fournisseur de liens YAML d'une application cliente (`pipeline: nom`) rendu générique :

```razor
<OmniCodeEditor @bind-Value="Yaml" Language="yaml"
                Links="@([new OmniCodeEditorLink("pipeline", "^\\s*(?:-\\s*)?pipeline:\\s*[\"']?([\\w-]+)")])"
                LinkActivated="OpenPipeline" />
```

## OmniCodeViewer : extrait numéroté

`OmniCodeViewer` affiche du code en lecture seule, numéros de ligne à côté du texte (jamais dedans,
une copie à la main ne les prend pas). `FirstLineNumber` (1 par défaut, ramené à 1 en dessous) numérote
un extrait comme dans son fichier : les lignes 631 à 640 autour d'un constat gardent leurs numéros.
`HighlightedLines` (`IReadOnlyList<int>`) et les numéros rapportés par `LinkActivated` comptent de la même façon.
`OnCopy` (`EventCallback<bool>`) est levé après une copie, avec l'acceptation du presse-papiers ;
`OmniCodeViewer` et `OmniCodeBlock` partagent le même en-tête (titre et bouton de copie).

```razor
<OmniCodeViewer Code="@Extrait" Title="src/Service.cs" FirstLineNumber="631" HighlightedLines="@([635])" />
```

## OmniDiffViewer

`OmniDiffViewer` compare deux versions d'un texte avec l'éditeur de différences de Monaco, côte à côte
ou en une colonne (`Inline`), les changements marqués ligne par ligne et dans la ligne. Il partage le
Monaco d'`OmniCodeEditor` et ses conditions : l'hôte sert `min/vs` à `MonacoPath` et autorise
`style-src 'unsafe-inline'`. Sous la politique stricte, `Engine="OmniCodeEditorEngine.PlainText"` montre
les deux textes dans deux volets nommés, sans les différences marquées ; c'est aussi ce qui s'affiche
pendant le chargement de Monaco et, avec un message d'état, s'il ne se charge pas.

- `Original`, `Modified` et `ModifiedChanged` : le texte modifié n'est éditable que si `ReadOnly` est
  faux (vrai par défaut) ; l'original ne l'est jamais. `Disabled` verrouille aussi le texte modifié,
  `ReadOnly` faux compris (Monaco en lecture seule, volet de repli désactivé), et estompe la comparaison. Une modification remonte comme dans
  `OmniCodeEditor`, sans écho.
- `Language`, `Inline`, `Height` (longueur CSS posée par le CSSOM, toute autre valeur lève), `Label`
  (« Comparaison »), `OriginalLabel` (« Avant ») et `ModifiedLabel` (« Après ») ; textes et options
  changent à chaud.
- Le composant est un `group` nommé ; chaque volet de repli est une `section` nommée dont le texte prend
  le focus pour défiler au clavier. Le thème suit l'`OmniThemeScope` englobant.
- La référence `DotNetObjectReference` du pont est libérée et l'éditeur détruit à la destruction du
  composant ; un composant détruit pendant le chargement du script libère aussitôt le module.
- Preuves : `DiffViewerComponentTests` (volets, saisie de repli, hauteur, chargement, échec, montage,
  mises à jour, passage au texte brut, libération). Monaco lui-même n'a pas été exercé dans un
  navigateur pour ce composant : la vitrine le montre en texte brut.

## OmniCodeBlock : commande ou secret à copier

`OmniCodeBlock` affiche en lecture seule un bloc de code, une commande ou un jeton, avec un bouton qui le
copie dans le presse-papiers ; le résultat est annoncé par une région live polie. Un secret est masqué à
l'écran (ses premiers et derniers caractères gardés, pour le reconnaître) jusqu'au bouton qui le révèle ;
la copie prend toujours la valeur entière. `Title` nomme le bloc (classe `omni-code-block__title`) et
`OnCopy` rapporte chaque copie. La copie passe par `omniInterop.js` : l'API asynchrone du
presse-papiers, ou une zone de texte cachée et la commande de copie quand elle est refusée.

## OmniUnifiedDiff : diff unifié sans Monaco

`OmniUnifiedDiff` dessine un diff unifié (texte brut d'un `git diff`, ou fichiers déjà analysés par
`OmniUnifiedDiffParser`) sans Monaco ni style en ligne : une section repliable par fichier avec son
chemin, ce qui lui est arrivé et ses nombres de lignes ajoutées et retirées, puis ses blocs avec les
numéros d'avant et d'après côte à côte et les lignes ajoutées et retirées teintées. Un en-tête de bloc
malformé (nombres hors d'un `int`) est ignoré. Il marche donc sous la politique CSP stricte.
`ExpandedByDefault` (vrai par défaut) ouvre chaque section de fichier ; `FileActionsTemplate` ajoute des
actions à l'en-tête de chaque fichier.

## Limites connues

- La surface visuelle s'appuie sur `document.execCommand`, déprécié mais sans remplaçant ; le
  comportement fin (listes imbriquées, fusion de blocs) suit le moteur du navigateur.
- La composition IME n'a pas été éprouvée sur un clavier asiatique réel.
- Le fonctionnement dans un navigateur réel est vérifié par une page d'essai hors dépôt ; la sonde CDP
  du catalogue ne couvre que la face source.
