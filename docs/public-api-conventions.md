# Conventions de l'API publique

## Nommage et liaison

- Les composants publics portent le préfixe `Omni` et décrivent une capacité, pas une implémentation.
- Une valeur contrôlée utilise `Value`, `ValueChanged` et, lorsqu'elle participe à un formulaire, `ValueExpression` via `InputBase<TValue>`.
- Les collections utilisent `IReadOnlyList<T>` ; une absence de sélection multiple est une liste vide, jamais `null`.
- Une valeur réellement optionnelle utilise un type nullable (`DateOnly?`, `bool?`). Les composants non nullables ne donnent pas de sens implicite à `default`.
- Les opérations distantes reçoivent un `CancellationToken`. `OmniDataList`, `OmniDataGrid`, `OmniScheduler` et `OmniAutocomplete` rendent chargement et erreur observables et proposent une reprise. Pour l'autocomplete, `SearchFailed` reçoit l'exception sans l'afficher et l'état récupérable reste localisé.
- Les templates sont des `RenderFragment` ou `RenderFragment<T>`. Les événements asynchrones sont des `EventCallback` ou des délégués retournant `Task`.
- Un fragment sans contexte qui remplace une zone porte le suffixe `Content` (`EmptyContent`, `LoadingContent`, `ErrorContent`, `TitleContent`) ; un fragment répété avec son élément (`RenderFragment<T>`) porte le suffixe `Template` (`ItemTemplate`, `OptionTemplate`).
- L'icône d'un composant est un fragment nommé `Icon`, en général un `OmniIcon`, rendu décoratif par le composant. Le composant dimensionne une `OmniIcon` sans `Size` par la propriété `--omni-icon-size` (badge, bouton partagé, petit bouton) ; une `Size` posée sur l'icône, ou une classe du consommateur qui la dimensionne, l'emporte.
- Le nom accessible d'un bouton sans texte visible se termine par `Label` (`CloseLabel`, `BackLabel`) ; le texte visible d'un bouton se termine par `Text` (`ActionText`, `BackText`).
- Une sévérité est un `OmniSeverity` (`Info`, `Success`, `Warning`, `Danger`, les noms des intentions d'`OmniButtonVariant`), partagé par les alertes et les notifications.

## HTML, attributs et CSS

- Les composants fondés sur `OmniComponentBase` partagent `Id`, `Class` et `AdditionalAttributes`. Les contrôles de formulaire héritent de `OmniInputBase<TValue>`, qui déclare `Id` et `Class` et garde les `AdditionalAttributes` fournis par `InputBase<TValue>`.
- `AdditionalAttributes` refuse les gestionnaires HTML `on*` et l'attribut `style` afin de préserver le contrat CSP.
- Les états visuels sont des classes CSS finies. Les données SVG emploient des attributs géométriques, jamais un style inline.
- Les chaînes affichées par défaut sont en français et peuvent être remplacées par paramètres lorsque le contexte l'exige.

## Compatibilité et évolution

La surface ne promet aucune compatibilité binaire ou syntaxique avec une autre bibliothèque de composants. Une API publique publiée suit la politique décrite dans [versioning.md](versioning.md).

## État de la garde API

La baseline CI est extraite depuis l'assembly compilé par `OmniEurope.PublicApiGuard`. Elle sérialise de façon canonique les types et membres publics ou protégés, la nullabilité référence, `init`/`required`, les modificateurs, constantes, enums, rangs de tableaux, paramètres et contraintes génériques. Des fixtures exactes couvrent ces catégories avant la comparaison séquentielle, triée et sans doublon avec `docs/public-api.txt`; la mise à jour utilise un remplacement atomique.
