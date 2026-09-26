# Éditeurs : WYSIWYG, traitement de texte et code

Ce lot couvre `OmniHtmlEditor`, `OmniDocumentEditor` et `OmniCodeEditor`. Les deux premiers produisent
le même HTML assaini ; le troisième édite du texte brut.

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
en tête, en fin ou répétés ne sont pas dessinés.

Les interrupteurs `EnableBold`, `EnableItalic`, `EnableSubscript`, `EnableSuperscript`, `EnableIndent`
et `EnableOutdent` masquent toujours leur commande, quelle que soit la barre, et `CustomTools`
(transformations de toute la valeur) s'affiche toujours après elle.

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
  `MergeCellDown`, `SplitCell` et `SetCellSpan` (argument `lignesxcolonnes`, `2x3` ; sans argument,
  la cellule revient à `1x1`). Elles agissent sur la cellule au curseur, tiennent compte des
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
restreint à `omni-align-left|center|right|justify` et `omni-font-size-small|normal|large|xlarge`.
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

`SanitizerPolicy` (`OmniHtmlSanitizerPolicy`, null par défaut) élargit la liste blanche pour le
balisage propre à l'hôte, partout où l'éditeur assainit : valeur liée, frappe, collage et dépôt,
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
  `mailto`, `tel` ou relatives : une adresse `javascript:` ou `data:` est retirée.
- L'éditeur garde le sanitiseur construit pour une instance de politique : une instance statique
  évite de le reconstruire.

## OmniDocumentEditor

Un traitement de texte léger : une page blanche centrée sur un fond gris, la barre
`OmniHtmlEditorCommands.Document` collée en haut (style du paragraphe, taille du texte, gras,
italique, souligné, barré, listes, retraits, alignements, tableau de trois lignes sur trois, lien,
effacement, historique), et une barre d'état avec le nombre de mots et de caractères (espaces compris)
et deux boutons d'export.

- `Value`, `ValueChanged` et `ValueExpression` sont transmis à l'éditeur interne : `@bind-Value` fait
  participer le traitement de texte à un formulaire comme `OmniHtmlEditor`.
- `ExportHtmlAsync()` rend un fichier HTML complet (`lang` de la culture courante, `title` tiré de
  `DocumentTitle`, sinon du premier titre de niveau 1). Les classes d'alignement et de taille y
  deviennent les déclarations équivalentes sur les éléments, pour que le fichier se lise de même dans
  un navigateur ou un traitement de texte ; ce fichier est remis à l'utilisateur, jamais rendu dans la
  page.
- `ExportTextAsync()` rend le texte brut : blocs séparés par une ligne vide, puces en `- `, listes
  numérotées en `1. `, cellules séparées par des tabulations.
- Les boutons de la barre d'état téléchargent `FileName.html` et `FileName.txt` par
  `omni-document-editor.js` (un `Blob` et un lien de téléchargement).
- `ReadOnly`, `Rows` (hauteur minimale de la page), `ShowStatusBar`, `ShowExport`, `Commands`.

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
- `ReadOnly`, `ShowLineNumbers`, `WordWrap`, `TabSize`, `Label`, `AriaDescribedBy`.
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
C'est le fournisseur de liens YAML d'Aetheus (`pipeline: nom`) rendu générique :

```razor
<OmniCodeEditor @bind-Value="Yaml" Language="yaml"
                Links="@([new OmniCodeEditorLink("pipeline", "^\\s*(?:-\\s*)?pipeline:\\s*[\"']?([\\w-]+)")])"
                LinkActivated="OpenPipeline" />
```

## OmniDiffViewer

`OmniDiffViewer` compare deux versions d'un texte avec l'éditeur de différences de Monaco, côte à côte
ou en une colonne (`Inline`), les changements marqués ligne par ligne et dans la ligne. Il partage le
Monaco d'`OmniCodeEditor` et ses conditions : l'hôte sert `min/vs` à `MonacoPath` et autorise
`style-src 'unsafe-inline'`. Sous la politique stricte, `Engine="OmniCodeEditorEngine.PlainText"` montre
les deux textes dans deux volets nommés, sans les différences marquées ; c'est aussi ce qui s'affiche
pendant le chargement de Monaco et, avec un message d'état, s'il ne se charge pas.

- `Original`, `Modified` et `ModifiedChanged` : le texte modifié n'est éditable que si `ReadOnly` est
  faux (vrai par défaut) ; l'original ne l'est jamais. Une modification remonte comme dans
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

## Limites connues

- La surface visuelle s'appuie sur `document.execCommand`, déprécié mais sans remplaçant ; le
  comportement fin (listes imbriquées, fusion de blocs) suit le moteur du navigateur.
- La composition IME n'a pas été éprouvée sur un clavier asiatique réel.
- Le fonctionnement dans un navigateur réel est vérifié par une page d'essai hors dépôt ; la sonde CDP
  du catalogue ne couvre que la face source.
