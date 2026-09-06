# OmniEurope.Blazor

Bibliothèque de composants Blazor conçue selon une politique d'implémentation indépendante pour les applications OmniEurope. Son objectif est de fournir des primitives accessibles, thémables et compatibles avec une politique CSP stricte, afin de remplacer progressivement les usages de Radzen sans réutiliser son code.

## Principes

- politique clean-room : la copie ou la traduction de sources Radzen est interdite ; les limites des preuves de provenance actuellement conservées sont documentées dans [docs/reproducibility.md](docs/reproducibility.md) ;
- contrat CSP interdisant les attributs `style`, les balises `<style>` générées à l'exécution et `unsafe-eval` ; la portée actuelle des vérifications est documentée dans [docs/csp-contract.md](docs/csp-contract.md) ;
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

## Composants disponibles

Les 110 balises inventoriées disposent désormais d'une cible Razor OmniEurope : fondations, formulaires, sélecteurs, superpositions, navigation, collections, DataGrid, graphiques, scheduler et éditeur HTML. Ce total mesure la présence des cibles, pas une équivalence comportementale ni une validation complète. Le panorama et les limites actuelles sont décrits dans [docs/component-families.md](docs/component-families.md), avec les détails des [fondations](docs/foundation-components.md), [formulaires](docs/form-components.md), [sélecteurs](docs/selection-components.md) et [données](docs/data-components.md). Le [guide de migration](docs/migration-guide.md) prépare le remplacement sans l'exécuter.

La bibliothèque est construite et soumise aux gates documentées avant toute migration. Ces gates ne constituent pas encore une validation comportementale ou navigateur exhaustive, et aucun projet consommateur n'est modifié pendant cette étape.

## CSP de référence

La bibliothèque vise au minimum une politique qui n'accorde ni `unsafe-inline` à `style-src`, ni `unsafe-eval` à `script-src`. Le contrat complet et les responsabilités de l'application hôte sont décrits dans [docs/csp-contract.md](docs/csp-contract.md).

## Développement

Le dépôt requiert le SDK .NET `10.0.302`, verrouillé par `global.json`.

```powershell
dotnet restore OmniEurope.Blazor.slnx --locked-mode
dotnet test OmniEurope.Blazor.slnx --no-restore
dotnet pack src\OmniEurope.Blazor\OmniEurope.Blazor.csproj --no-restore -o artifacts\packages
```

Le catalogue des usages actuels et sa commande de régénération se trouvent dans [docs/component-inventory.md](docs/component-inventory.md). Il reflète un état local externe à ce dépôt et n'est reproductible qu'avec le même instantané de `C:\Dev`; les limites sont précisées dans [docs/reproducibility.md](docs/reproducibility.md). Leur regroupement par nature fonctionnelle et l'ordre de réalisation sont décrits dans [docs/component-roadmap.md](docs/component-roadmap.md). Le registre des 110 balises et de leurs cibles Razor se trouve dans [docs/component-coverage.md](docs/component-coverage.md).

Le catalogue local sous `samples/OmniEurope.Blazor.Catalog` expose les familles dans un hôte Interactive Server doté d'une CSP stricte et d'un endpoint `/csp-status`. Les conventions publiques, la compatibilité, l'accessibilité, les budgets, le versionnement et la reproductibilité sont documentés dans [docs/public-api-conventions.md](docs/public-api-conventions.md), [docs/compatibility.md](docs/compatibility.md), [docs/accessibility-contract.md](docs/accessibility-contract.md), [docs/performance-budgets.md](docs/performance-budgets.md), [docs/versioning.md](docs/versioning.md) et [docs/reproducibility.md](docs/reproducibility.md).

Trois sondes de compatibilité complètent le catalogue : `samples/OmniEurope.Blazor.WasmSmoke` publie une application autonome dont le clic et la progression sont exercés dans Chromium, `samples/OmniEurope.Blazor.AutoSmoke` vérifie le prérendu puis l'hydratation et le clic dans Chromium, et `samples/OmniEurope.Blazor.HybridSmoke` est une application MAUI Windows exécutable qui charge les composants dans `BlazorWebView`. La CI exerce WebAssembly, Auto et Hybrid par CDP et exige une console sans erreur; le catalogue possède en plus des contrôles navigateur de focus et de noms accessibles.

## Licence

OmniEurope.Blazor est distribué sous licence [EUPL-1.2](LICENSE).
