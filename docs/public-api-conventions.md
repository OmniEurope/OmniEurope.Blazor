# Conventions de l'API publique

Ces règles sont celles de la passe de cohérence 1.2.0 ([PLAN-007](plans/archive/PLAN-007-coherence-et-24-langues.md), « Décisions », validées le 2026-09-29). Un nouveau paramètre les suit ; un écart n'est admis que s'il figure dans la liste des exceptions en fin de document, avec sa raison.

## Nommage et liaison

- Les composants publics portent le préfixe `Omni` et décrivent une capacité, pas une implémentation.
- Nom accessible : `Label` (`string?`, `null` donne le texte localisé par défaut du composant). Aucun paramètre ne s'appelle `AriaLabel`. Un composant à déclencheur nomme le déclencheur par `Label` et la surface qu'il ouvre par `MenuLabel` (menus) ou `PopupLabel` (`OmniPopover`). Les autres textes qui nomment un bouton sans texte visible gardent le suffixe `Label` (`CloseLabel`, `BackLabel`, `RevealLabel`) ; un texte visible porte le suffixe `Text` (`ActionText`, `ConfirmText`, `CancelText`).
- Texte optionnel : `string?`, jamais `string = ""` comme sentinelle. Le composant remplace `null` par son texte localisé (`LocalizeOr` d'`OmniComponentBase` et d'`OmniInputBase<TValue>`).
- Liaison : `Value` / `ValueChanged` pour toute valeur ou sélection, y compris multiple (`OmniDataGrid`, `OmniTree`, `OmniSelectableCard`), avec `ValueExpression` quand le composant participe à un formulaire (`InputBase<TValue>`). Un autre état lié suit le motif `X` / `XChanged` (`Open` / `OpenChanged`, `Expanded` / `ExpandedChanged`, `Density` / `DensityChanged`).
- Événement d'action : `On` + sujet + verbe au présent (`OnClick`, `OnRowClick`, `OnRowEdit`, `OnItemMove`, `OnAppointmentMove`, `OnTaskClick`, `OnCopy`, `OnExport`). Échec : `On…Error` (`OnLoadError`, `OnSearchError`, `OnExportError`), qui reçoit l'exception. `OnClick` est partout un `EventCallback<MouseEventArgs>`.
- Délégués : la clé d'un élément s'obtient par `KeyOf`, le texte d'une valeur par `FormatValue`.
- Recherche dans une liste d'options : `Filterable` ; son texte indicatif : `FilterPlaceholder`.
- Retour à la ligne : `Wrap`. Durée : `TimeSpan`, sans unité dans le nom (`Debounce`, `SlotDuration`, `Step`). Occupation : `Busy`. Repli : `Expanded` (vrai = déplié), jamais `Collapsed`.
- Fragments : un fragment sans contexte qui remplace une zone porte le suffixe `Content` (`EmptyContent`, `LoadingContent`, `ErrorContent`, `TitleContent`, `HeaderContent`, `FooterContent`, `MenuContent`) ; un fragment répété avec son élément (`RenderFragment<T>`) porte le suffixe `Template` (`ItemTemplate`, `OptionTemplate`, `OptionIconTemplate`, `FileActionsTemplate`).
- Collections en paramètre : `IReadOnlyList<T>` ; une absence de sélection multiple est une liste vide, jamais `null`.
- Une valeur réellement optionnelle utilise un type nullable (`DateOnly?`, `bool?`, `double?`, `CultureInfo?`). Les composants non nullables ne donnent pas de sens implicite à `default`.
- L'icône d'un composant est un fragment nommé `Icon`, en général un `OmniIcon`, rendu décoratif par le composant. Le composant dimensionne une `OmniIcon` sans `Size` par la propriété `--omni-icon-size` (badge, bouton partagé, petit bouton) ; une `Size` posée sur l'icône, ou une classe du consommateur qui la dimensionne, l'emporte.
- Les opérations distantes reçoivent un `CancellationToken`. `OmniDataList`, `OmniDataGrid`, `OmniScheduler` et `OmniAutocomplete` rendent chargement et erreur observables et proposent une reprise : `LoadingContent`, `ErrorContent` et `OnLoadError` ; l'autocomplétion n'a pas de `LoadingContent` et propose `ErrorContent`, `OnSearchError` (qui reçoit l'exception) et `SearchErrorMessage`.
- Les événements asynchrones sont des `EventCallback` ou des délégués retournant `Task`.

## Ton, remplissage, sévérité et emphase

- Ton : `OmniTone { Neutral, Accent, Info, Success, Warning, Danger }`, pour `OmniBadge.Tone`, `OmniProgressBar.Tone`, `OmniStatus`, l'intention d'un dialogue (`OmniDialog.Intent`) et le ton d'un item de menu (`OmniMenuItem.Tone`). `Neutral` est l'absence de ton.
- Remplissage : `OmniFill { Tonal, Outline, Solid }`, pour `OmniAlert.Fill`, `OmniBadge.Fill` et `OmniStatus`.
- `OmniSeverity` (`Info`, `Success`, `Warning`, `Danger`) reste la sévérité d'un message : alertes, notifications, `OmniOverlayService.Notify`. La même sévérité donne le même glyphe dans une alerte, une notification et un dialogue.
- `OmniButtonVariant` reste l'emphase d'un bouton (`Primary`, `Secondary`, `Ghost`…) et n'est pas un ton.

## HTML, attributs et CSS

- Les composants fondés sur `OmniComponentBase` partagent `Id`, `Class`, `PresetName` et `AdditionalAttributes`. Les contrôles de formulaire héritent d'`OmniInputBase<TValue>`, qui déclare `Id` et `Class` et garde les `AdditionalAttributes` fournis par `InputBase<TValue>`. Un composant qui ne rend aucun élément propre (`OmniComponentsHost`, `OmniBootSplash`, validateurs, `OmniUnsavedChangesGuard`) dérive de `ComponentBase` et n'accepte aucun attribut.
- `@attributes` d'abord, attributs propres au composant ensuite : le composant gagne. Un `aria-label`, `aria-describedby` ou `target` passé en attribut supplémentaire est remplacé par la valeur du composant ; passer par le paramètre (`Label`, `AriaDescribedBy`, `NewTab`).
- `class` et `id` en minuscules sont refusés (en toute casse) : la garde d'exécution (`CspAttributeGuard`, par `OmniComponentBase`) lève `InvalidOperationException` et l'analyseur `OE0001` le signale dès la compilation ; utiliser `Class` et `Id`.
- `Class` va sur l'élément le plus externe du composant (le cadre d'un champ à icône, d'un mot de passe, d'un sélecteur de date) ; `Id` va sur le contrôle focusable (celui qu'un `<label for>` vise), avec les attributs supplémentaires. Un composant à déclencheur (`OmniOverflowMenu`, `OmniProfileMenu`, `OmniPopover`) pose `Id` sur son déclencheur.
- `AdditionalAttributes` refuse aussi les gestionnaires HTML `on*`, l'attribut `style` (contrat CSP) et tout nom qui commence par une majuscule ASCII : un attribut HTML est en minuscules, un nom en PascalCase est un paramètre que le composant n'a pas (`OmniBadge has no parameter 'IconName'.`). `OE0001` signale aussi tout attribut inconnu sur un composant qui ne capture pas d'attributs ([analyzers.md](analyzers.md)).
- Les états visuels sont des classes CSS finies. Les données SVG emploient des attributs géométriques, jamais un style inline.
- Les textes affichés par défaut viennent des ressources du paquet, dans les 24 langues de l'Union européenne ([localization.md](localization.md)). Un texte qui dépend du contexte est un paramètre `string?` du composant ; les textes de mécanique (pagination, filtres de grille) n'ont pas de paramètre par instance et se remplacent par `AddOmniEuropeTextOverrides`.

## Exceptions documentées

- `ErrorContent` est un `RenderFragment<Exception>` (grille, liste, agenda, autocomplétion) : il porte l'exception à afficher, mais garde le suffixe `Content` des zones de remplacement qu'il partage avec `LoadingContent` et `EmptyContent`.
- `OmniUpload.FilesSelected`, `OmniUpload.FileRemoved`, `OmniCodeEditor.LinkActivated`, `OmniCodeViewer.LinkActivated` et `OmniValidatorBase<TValue>.ValidationFailed` : nommés avant la règle `On` + sujet + verbe, gardés pour ne pas rompre l'API publique.
- `OmniLogViewer.ShowSearch` et `OmniLogViewer.SearchPlaceholder` : la recherche du journal marque les occurrences et les parcourt sans masquer de ligne ; ce n'est pas un filtre d'options (`Filterable`), et le nom suit les autres bascules de sa barre (`ShowLevelFilter`).
- `OmniDataGridColumn.FilterSearchable` et `OmniDataGridColumn.FormatFilterValue` : `Filterable` et `FormatValue` y désignent déjà le filtre de la colonne et l'affichage de ses cellules ; le préfixe `Filter` situe ces deux réglages dans la liste de choix du filtre (`FilterSearchable` alimente le `Filterable` de cette liste, `FormatFilterValue` le texte de chaque valeur candidate).
- `OmniListBox<TValue, TSelection>` : le second paramètre générique est ce que lie `@bind-Value`, une valeur d'option ou une collection avec `Multiple` ; l'inférence les déduit tous deux.
- `OmniSelectableCardGroup<TValue, TSelection>` : même raison, `TValue` est la valeur d'une carte, `TSelection` la valeur liée (une seule valeur, ou une collection avec `Multiple`).

## Compatibilité et évolution

La surface ne promet aucune compatibilité binaire ou syntaxique avec une autre bibliothèque de composants. Une API publique publiée suit la politique décrite dans [versioning.md](versioning.md).

## État de la garde API

La baseline CI est extraite depuis l'assembly compilé par `OmniEurope.PublicApiGuard`. Elle sérialise de façon canonique les types et membres publics ou protégés, la nullabilité référence, `init`/`required`, les modificateurs, constantes, enums, rangs de tableaux, paramètres et contraintes génériques. Des fixtures exactes couvrent ces catégories avant la comparaison séquentielle, triée et sans doublon avec `docs/public-api.txt`; la mise à jour utilise un remplacement atomique.
