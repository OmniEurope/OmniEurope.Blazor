# Localisation

`OmniEurope.Blazor` fournit le marqueur public `AppStrings`, des ressources françaises par défaut et des ressources anglaises. L'hôte active le contrat une seule fois :

```csharp
builder.Services.AddOmniEuropeBlazor();
```

Les composants utilisent `IStringLocalizer<AppStrings>`. Une application peut ajouter ses cultures satellites et remplacer les libellés exposés par paramètres lorsqu'un texte dépend du contexte métier. Les noms de marque, identifiants, valeurs techniques et contenus fournis par le consommateur ne sont pas traduits automatiquement.

Une clé absente constitue une régression : les tests doivent vérifier `ResourceNotFound == false` dans les cultures prises en charge. La ressource française sans suffixe reste le repli déterministe de la bibliothèque.
