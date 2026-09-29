# Localisation

`OmniEurope.Blazor` fournit le marqueur public `AppStrings` et ses ressources dans les 24 langues officielles de l'Union européenne : le français en culture neutre (`AppStrings.resx`, sans suffixe), puis `en`, `bg`, `cs`, `da`, `de`, `el`, `es`, `et`, `fi`, `ga`, `hr`, `hu`, `it`, `lt`, `lv`, `mt`, `nl`, `pl`, `pt`, `ro`, `sk`, `sl` et `sv`, en cultures neutres (`pt` et non `pt-BR`) : une culture régionale comme `de-AT` retombe sur `de`. L'ajout des 22 langues autres que le français et l'anglais est en cours (PLAN-007, lot 2). L'hôte active le contrat une seule fois :

```csharp
builder.Services.AddOmniEuropeBlazor();
```

Les composants utilisent `IStringLocalizer<AppStrings>`. Les noms de marque, identifiants, valeurs techniques et contenus fournis par le consommateur ne sont pas traduits automatiquement.

## Remplacer un texte du paquet

Un texte qui dépend du contexte métier se remplace de deux façons :

- par paramètre, quand le composant expose le texte (libellé, titre, texte d'action) ;
- depuis les ressources de l'hôte, pour n'importe quel texte du paquet :

  ```csharp
  builder.Services.AddOmniEuropeBlazor();
  builder.Services.AddOmniEuropeTextOverrides<HostStrings>();
  ```

  `AddOmniEuropeTextOverrides<THostResource>` (à appeler après `AddOmniEuropeBlazor`) enregistre `OmniTextOverrideLocalizer` à la place du localiseur du paquet. Une clé de l'hôte nommée du préfixe (`Omni_` par défaut, second argument de la méthode) suivi de la clé du paquet l'emporte, dans chaque culture que l'hôte traduit : `Omni_ConnectionReconnectNow` remplace `ConnectionReconnectNow`. Une clé que l'hôte ne définit pas garde le texte du paquet, et un remplacement n'ajoute aucune clé que le paquet n'a pas.

## Garanties

Une clé absente constitue une régression : les tests doivent vérifier `ResourceNotFound == false` dans les cultures prises en charge. La ressource française sans suffixe reste le repli déterministe de la bibliothèque.
