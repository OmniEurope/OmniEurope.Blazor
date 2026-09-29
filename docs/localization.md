# Localisation

`OmniEurope.Blazor` fournit le marqueur public `AppStrings` et ses ressources dans les 24 langues officielles de l'Union européenne : le français en culture neutre (`AppStrings.resx`, sans suffixe), puis `en`, `bg`, `cs`, `da`, `de`, `el`, `es`, `et`, `fi`, `ga`, `hr`, `hu`, `it`, `lt`, `lv`, `mt`, `nl`, `pl`, `pt`, `ro`, `sk`, `sl` et `sv`, en cultures neutres (`pt` et non `pt-BR`) : une culture régionale comme `de-AT` retombe sur `de`. Chaque langue autre que le français est livrée en assembly satellite (`lib/net10.0/<langue>/OmniEurope.Blazor.resources.dll`), et `eng/Test-Package.ps1` échoue si le paquet en perd une. L'hôte active le contrat une seule fois :

```csharp
builder.Services.AddOmniEuropeBlazor();
```

Les composants utilisent `IStringLocalizer<AppStrings>`. Les noms de marque, identifiants, valeurs techniques et contenus fournis par le consommateur ne sont pas traduits automatiquement.

## Remplacer un texte du paquet

Un texte qui dépend du contexte métier se remplace de deux façons :

- par paramètre, quand le composant expose le texte (libellé, titre, texte d'action) : un paramètre `string?` dont la valeur `null` garde le texte localisé ([public-api-conventions.md](public-api-conventions.md)). Les textes de mécanique n'ont pas de paramètre par instance (pagination et filtres d'`OmniDataGrid`, boutons d'`OmniPager`) ;
- depuis les ressources de l'hôte, pour n'importe quel texte du paquet :

  ```csharp
  builder.Services.AddOmniEuropeBlazor();
  builder.Services.AddOmniEuropeTextOverrides<HostStrings>();
  ```

  `AddOmniEuropeTextOverrides<THostResource>` (à appeler après `AddOmniEuropeBlazor`) enregistre `OmniTextOverrideLocalizer` à la place du localiseur du paquet. Une clé de l'hôte nommée du préfixe (`Omni_` par défaut, second argument de la méthode) suivi de la clé du paquet l'emporte, dans chaque culture que l'hôte traduit : `Omni_ConnectionReconnectNow` remplace `ConnectionReconnectNow`. Une clé que l'hôte ne définit pas garde le texte du paquet, et un remplacement n'ajoute aucune clé que le paquet n'a pas.

## Garanties

Une clé absente constitue une régression : les tests doivent vérifier `ResourceNotFound == false` dans les cultures prises en charge. La ressource française sans suffixe reste le repli déterministe de la bibliothèque . `LibraryTranslationTests` exige un fichier pour chacune des 23 langues autres que le français, et, pour chacune, exactement les clés de la ressource neutre, les mêmes marqueurs `{n}` et aucune valeur vide là où le français en a une : une clé ajoutée sans ses traductions fait échouer la suite au lieu d'afficher du français dans une page allemande ou grecque.

## Vitrine

La vitrine propose les mêmes 24 langues dans un sélecteur (noms des langues dans leur propre langue). Le choix est gardé dans `localStorage` (`omnieurope.showcase.culture`) et la page se recharge dans la nouvelle culture, avec les données ICU complètes ; l'euro reste le symbole des montants. `ShowcaseText_EveryCulturePresentHasTheNeutralKeys` exige que chaque traduction de `ShowcaseStrings` ait exactement les clés de la ressource neutre.
