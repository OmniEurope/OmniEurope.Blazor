# Findings d'audit - OmniEurope.Blazor.HybridSmoke

Date : 2026-08-11  
Mode : Full  
Périmètre : 14 fichiers de l'hôte MAUI Windows / WebView2  
Verdict du module : **2 findings actionnables** - 0 Critique, 2 Élevé, 0 Moyen, 0 Faible. **INFO : 2**.

## Méthode et preuves transversales

Les 14 fichiers ont été lus intégralement après vérification des artefacts globaux inchangés. Le préflight prouve le build Windows, le lancement MAUI, le chargement WebView2, le clic CDP, le compteur à 1 et une console sans erreur. Le graphe converge uniquement vers MAUI, WebView2 et la RCL; les dépendances obsolètes ou de servicing sont déjà consignées globalement et ne sont pas dupliquées. Les deux ressources Razor ont des clés fr/en identiques. Aucun style inline, HTML brut, secret, référence distante applicative, dépendance ou marqueur Radzen n'est présent. Les objets MAUI et le `BlazorWebView` suivent le cycle de vie de la fenêtre et de la page; aucune fuite propre supplémentaire n'est observée. Semgrep et gitleaks étant indisponibles, la sécurité reste best-effort sur ces axes.

## Findings actionnables

<a id="hyb-001"></a>
### HYB-001 - [Élevé] [Sécurité / CSP] Le WebView2 hybride n'applique aucune politique CSP

**Preuves :** `wwwroot/index.html` ne contient ni meta `Content-Security-Policy` ni mécanisme équivalent. Contrairement aux hôtes HTTP Catalog, WASM et Auto, un BlazorWebView local ne reçoit pas leurs en-têtes serveur. `eng/Test-HybridHost.ps1` vérifie le clic et la console, mais ne vérifie aucune politique effective.

**Impact :** le smoke Hybrid peut rester vert alors que le document autorise par défaut scripts, styles, images, frames et connexions sans la restriction attendue du contrat CSP. Une régression d'injection spécifique à WebView2 ne serait pas bloquée ni détectée.

**Remédiation :** définir dans l'hôte HTML une meta CSP minimale compatible avec `blazor.webview.js` et les assets locaux, sans `unsafe-inline` ni `unsafe-eval`; étendre la sonde WebView2 pour lire la politique effective et prouver le blocage d'une ressource interdite.

<a id="hyb-002"></a>
### HYB-002 - [Élevé] [Internationalisation / Accessibilité] La coque native et le document WebView restent figés dans une seule langue

**Preuves :** `wwwroot/index.html:2` fixe `lang="fr"`, la ligne 6 contient le titre anglais `OmniEurope.Blazor Hybrid Smoke` et la ligne 11 le texte français `Chargement...`. `OmniEurope.Blazor.HybridSmoke.csproj:11` fixe également l'`ApplicationTitle` anglais, alors que le contenu Razor dispose de ressources fr/en.

**Impact :** sur un Windows anglophone, le document est annoncé comme français et montre un fallback français; sur un système francophone, le titre de fenêtre reste anglais. La localisation du composant ne suffit donc pas à rendre la surface Hybrid cohérente.

**Remédiation :** localiser le titre natif par les mécanismes MAUI/Windows, synchroniser `lang` avec la culture UI et remplacer le fallback statique par un contenu neutre ou localisé; tester les deux cultures dans WebView2.

## Proportionnalité et sur-ingénierie

`PROPORTIONALITY: NONE` - Une application MAUI Windows minimale et un contrôle CDP sont proportionnés à la preuve Hybrid. Les remédiations CSP et langue restent dans la coque existante.

<a id="hyb-i01"></a>
### HYB-I01 - [INFO] [Sur-ingénierie] Le gestionnaire synchrone expose inutilement un contrat Task

`HybridSmoke.razor.cs:15-19` incrémente un entier puis retourne `Task.CompletedTask`. Un gestionnaire `void` suffit au comportement actuel. **Notification consultative, non actionnable, exclue des findings et du verdict.**

<a id="hyb-i02"></a>
### HYB-I02 - [INFO] [Sur-ingénierie] HybridSmokeTypes duplique des références déjà exercées

`HybridSmokeTypes.cs` matérialise `BlazorWebView` et `OmniButton`, mais `MainPage.cs` et `HybridSmoke.razor` référencent déjà directement ces types et le smoke runtime les exerce. Le conteneur n'a aucun lecteur. Sa suppression est l'alternative la plus simple si aucune gate externe documentée ne le consomme. **Notification consultative, non actionnable, exclue des findings et du verdict.**

## Contrôles fichier par fichier

<a id="samples-omnieurope-blazor-hybridsmoke-app-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/App.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-hybridsmoke-razor"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/HybridSmoke.razor`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Référence inter-module sans duplication : [OE-BLAZOR-008](AUDIT_FINDINGS_OmniEurope_Blazor.md#oe-blazor-008) couvre la cible tactile du bouton Medium dans la feuille RCL.

<a id="samples-omnieurope-blazor-hybridsmoke-hybridsmoke-razor-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/HybridSmoke.razor.cs`

Finding(s) : [HYB-I01](#hyb-i01).

<a id="samples-omnieurope-blazor-hybridsmoke-hybridsmoketypes-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/HybridSmokeTypes.cs`

Finding(s) : [HYB-I02](#hyb-i02).

<a id="samples-omnieurope-blazor-hybridsmoke-mainpage-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/MainPage.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-mauiprogram-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/MauiProgram.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-omnieurope-blazor-hybridsmoke-csproj"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/OmniEurope.Blazor.HybridSmoke.csproj`

Finding(s) : [HYB-002](#hyb-002).

<a id="samples-omnieurope-blazor-hybridsmoke-packages-lock-json"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

Références inter-passes sans duplication : [DEP-003](AUDIT_DEPENDENCIES.md#dep-003---moyen-dépendances--fiabilité-le-smoke-hybride-conserve-un-socle-runtime-transitive-non-servi) et [DEP-010](AUDIT_DEPENDENCIES.md#dep-010---faible-licences--conformité-une-url-de-licence-ne-fournit-plus-le-texte-déclaré).

<a id="samples-omnieurope-blazor-hybridsmoke-platforms-windows-app-xaml"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/Platforms/Windows/App.xaml`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-platforms-windows-app-xaml-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/Platforms/Windows/App.xaml.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-resources-hybridsmokestrings-cs"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/Resources/HybridSmokeStrings.cs`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-resources-hybridsmokestrings-en-resx"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/Resources/HybridSmokeStrings.en.resx`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-resources-hybridsmokestrings-resx"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/Resources/HybridSmokeStrings.resx`

RAS - Aucun écart additionnel relevé après lecture Full et contrôle transversal.

<a id="samples-omnieurope-blazor-hybridsmoke-wwwroot-index-html"></a>
### `samples/OmniEurope.Blazor.HybridSmoke/wwwroot/index.html`

Finding(s) : [HYB-001](#hyb-001), [HYB-002](#hyb-002).

## Totaux

- Critique : 0
- Élevé : 2
- Moyen : 0
- Faible : 0
- INFO, consultatif et exclu du verdict : 2
- Fichiers audités : 14/14
