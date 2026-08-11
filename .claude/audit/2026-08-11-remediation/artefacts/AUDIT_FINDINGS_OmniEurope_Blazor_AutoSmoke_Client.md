# Findings d'audit - OmniEurope.Blazor.AutoSmoke.Client

Date : 2026-08-11  
Mode : Full  
Périmètre : 9 fichiers du client Interactive Auto  
Verdict du module : **0 finding actionnable** - 0 Critique, 0 Élevé, 0 Moyen, 0 Faible. **INFO : 1**.

## Méthode et preuves transversales

Les 9 fichiers ont été lus intégralement après vérification des artefacts globaux Architecture, Kit, Dépendances et métriques. Le préflight prouve la compilation, la publication et le runtime Interactive Auto, y compris l'hydratation et l'interaction navigateur. Le module respecte le code-behind Razor, utilise des ressources fr/en aux clés identiques, compose un bouton texte et icône, ne contient ni style inline, ni HTML brut, ni ressource distante, ni secret ou marqueur de copie. Aucune référence, dépendance ou mention Radzen n'est présente. La CSP relève de l'hôte Server et le client ne tente aucun contournement. Semgrep et gitleaks n'étant pas disponibles, la sécurité reste best-effort sur ces axes.

Les écarts globaux de la RCL et des dépendances ne sont pas dupliqués.

## Findings actionnables

Aucun.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - La séparation du client WebAssembly est requise par Interactive Auto. Le composant, son code-behind, deux ressources et le bootstrap constituent un adaptateur minimal.

<a id="auto-client-i01"></a>
### AUTO-CLIENT-I01 - [INFO] [Sur-ingénierie] Le gestionnaire synchrone expose inutilement un contrat Task

`AutoProbe.razor.cs:15-19` incrémente un entier puis retourne `Task.CompletedTask` sans opération asynchrone. Un gestionnaire `void` exprimerait plus directement le comportement actuel. **Notification consultative, non actionnable, exclue des findings et du verdict.**

## Contrôles fichier par fichier

<a id="samples-omnieurope-blazor-autosmoke-client-imports-razor"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/_Imports.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-autosmoke-client-autoprobe-razor"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/AutoProbe.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Référence inter-module sans duplication : [OE-BLAZOR-008](AUDIT_FINDINGS_OmniEurope_Blazor.md#oe-blazor-008) couvre la cible tactile du bouton Medium dans la feuille RCL.

<a id="samples-omnieurope-blazor-autosmoke-client-autoprobe-razor-cs"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/AutoProbe.razor.cs`

Référence consultative : [AUTO-CLIENT-I01](#auto-client-i01).

<a id="samples-omnieurope-blazor-autosmoke-client-omnieurope-blazor-autosmoke-client-csproj"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/OmniEurope.Blazor.AutoSmoke.Client.csproj`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-autosmoke-client-packages-lock-json"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/packages.lock.json`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Référence inter-passes sans duplication : [DEP-002](AUDIT_DEPENDENCIES.md#dep-002---moyen-dépendances--sécurité-le-sanitizer-de-production-est-obsolète-et-absent-de-la-politique).

<a id="samples-omnieurope-blazor-autosmoke-client-program-cs"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/Program.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-autosmoke-client-resources-autosmokestrings-cs"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/Resources/AutoSmokeStrings.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-autosmoke-client-resources-autosmokestrings-en-resx"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/Resources/AutoSmokeStrings.en.resx`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-autosmoke-client-resources-autosmokestrings-resx"></a>
### `samples/OmniEurope.Blazor.AutoSmoke.Client/Resources/AutoSmokeStrings.resx`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

## Totaux

- Critique : 0
- Élevé : 0
- Moyen : 0
- Faible : 0
- INFO, consultatif et exclu du verdict : 1
- Fichiers audités : 9/9
