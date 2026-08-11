# Conventions de l'API publique

## Nommage et liaison

- Les composants publics portent le préfixe `Omni` et décrivent une capacité, pas un composant Radzen.
- Une valeur contrôlée utilise `Value`, `ValueChanged` et, lorsqu'elle participe à un formulaire, `ValueExpression` via `InputBase<TValue>`.
- Les collections utilisent `IReadOnlyList<T>` ; une absence de sélection multiple est une liste vide, jamais `null`.
- Une valeur réellement optionnelle utilise un type nullable (`DateOnly?`, `bool?`). Les composants non nullables ne donnent pas de sens implicite à `default`.
- Les opérations distantes reçoivent un `CancellationToken`. `OmniDataList`, `OmniDataGrid` et `OmniScheduler` rendent chargement et erreur observables et proposent une reprise. `OmniAutocomplete` ne possède pas encore ces états ; cette limitation reste ouverte.
- Les templates sont des `RenderFragment` ou `RenderFragment<T>`. Les événements asynchrones sont des `EventCallback` ou des délégués retournant `Task`.

## HTML, attributs et CSS

- Les composants fondés sur `OmniComponentBase` partagent `Id`, `Class` et `AdditionalAttributes`. Les contrôles de formulaire héritent de `OmniInputBase<TValue>`, qui déclare `Id` et `Class` et garde les `AdditionalAttributes` fournis par `InputBase<TValue>`.
- `AdditionalAttributes` refuse les gestionnaires HTML `on*` et l'attribut `style` afin de préserver le contrat CSP.
- Les états visuels sont des classes CSS finies. Les données SVG emploient des attributs géométriques, jamais un style inline.
- Les chaînes affichées par défaut sont en français et peuvent être remplacées par paramètres lorsque le contexte l'exige.

## Compatibilité et évolution

La surface n'est ni binaire ni syntaxiquement compatible avec Radzen. Toute correspondance est explicite dans les guides de famille. Une API publique publiée suit la politique décrite dans [versioning.md](versioning.md).

## État de la garde API

La baseline CI actuelle est extraite par expressions régulières. Elle couvre les fichiers de composants, une partie des paramètres `[Parameter]` et les déclarations publiques `enum`, `class` et `record`, mais pas exhaustivement les paramètres requis, méthodes, propriétés, constructeurs, contraintes génériques ni toutes les formes de types. Elle détecte donc certaines dérives ; elle ne prouve pas encore la stabilité de toute la surface publique.
