# OmniEurope.Blazor

Bibliothèque de composants Blazor développée indépendamment pour les applications OmniEurope. Son objectif est de fournir des primitives accessibles, thémables et compatibles avec une politique CSP stricte, afin de remplacer progressivement les usages de Radzen sans reprendre son code.

## Principes

- implémentation clean-room : aucune source Radzen n'est copiée ni traduite ;
- aucun attribut `style`, aucune balise `<style>` générée à l'exécution et aucun `unsafe-eval` ;
- styles livrés comme ressource statique versionnée ;
- API orientée capacités métier, sans promesse de compatibilité binaire avec Radzen ;
- accessibilité clavier et sémantique HTML intégrées aux critères d'acceptation ;
- migration incrémentale, composant par composant, en commençant par Aetheus.

## Installation locale

```xml
<PackageReference Include="OmniEurope.Blazor" Version="0.1.0-alpha.1" />
```

Charger la feuille statique dans l'hôte :

```html
<link rel="stylesheet" href="_content/OmniEurope.Blazor/omnieurope.blazor.css" />
```

Puis importer l'espace de noms :

```razor
@using OmniEurope.Blazor.Components
```

Exemple :

```razor
<OmniButton Variant="OmniButtonVariant.Primary" OnClick="SaveAsync">
    Enregistrer
</OmniButton>
```

## CSP de référence

La bibliothèque vise au minimum une politique qui n'accorde ni `unsafe-inline` à `style-src`, ni `unsafe-eval` à `script-src`. Le contrat complet et les responsabilités de l'application hôte sont décrits dans [docs/csp-contract.md](docs/csp-contract.md).

## Développement

```powershell
C:\Users\Woluwe\.codex\tools\invoke-dotnet-guarded.ps1 restore OmniEurope.Blazor.slnx
C:\Users\Woluwe\.codex\tools\invoke-dotnet-guarded.ps1 test OmniEurope.Blazor.slnx --no-restore
C:\Users\Woluwe\.codex\tools\invoke-dotnet-guarded.ps1 pack src\OmniEurope.Blazor\OmniEurope.Blazor.csproj --no-restore -o artifacts\packages
```

Le catalogue des usages actuels et sa commande de régénération se trouvent dans [docs/component-inventory.md](docs/component-inventory.md). Leur regroupement par nature fonctionnelle et l'ordre de réalisation sont décrits dans [docs/component-roadmap.md](docs/component-roadmap.md).

## Licence

OmniEurope.Blazor est distribué sous licence [EUPL-1.2](LICENSE).
