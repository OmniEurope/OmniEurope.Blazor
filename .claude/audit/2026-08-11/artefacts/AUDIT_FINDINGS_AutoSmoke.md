# Findings d'audit 360 - AutoSmoke

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="samples-omnieurope-blazor-autosmokecomponents_importsrazor"></a>
## `samples/OmniEurope.Blazor.AutoSmoke/Components/_Imports.razor`

RAS.

Contrôle de proportionnalité: l'import unique du namespace Blazor Web est la solution minimale requise pour cet hôte et n'introduit aucune abstraction ou dépendance superflue.

<a id="samples-omnieurope-blazor-autosmokecomponentsapprazor"></a>
## `samples/OmniEurope.Blazor.AutoSmoke/Components/App.razor`

RAS.

Contrôle de proportionnalité: la page hôte se limite au document HTML, aux assets Blazor et au composant de sonde en rendu `InteractiveAuto`; une couche de layout, un routeur ou un framework UI ajouterait du coût sans besoin observé. La règle `STD-RADZEN` n'entraîne aucune réintroduction de Radzen dans ce produit clean-room.

<a id="samples-omnieurope-blazor-autosmokeomnieuropeblazorautosmokecsproj"></a>
## `samples/OmniEurope.Blazor.AutoSmoke/OmniEurope.Blazor.AutoSmoke.csproj`

RAS.

Contrôle de proportionnalité: le manifeste ne déclare que le package serveur nécessaire à Interactive Auto et la référence vers le client associé; cette dépendance terminale respecte le graphe cible et aucune dépendance Radzen ou infrastructure superflue n'est présente. Le scan NuGet signale zéro vulnérabilité et zéro dépréciation pour ce projet.

<a id="samples-omnieurope-blazor-autosmokepackageslockjson"></a>
## `samples/OmniEurope.Blazor.AutoSmoke/packages.lock.json`

RAS.

Contrôle de proportionnalité: le verrou v2 contient uniquement les six entrées attendues, leurs versions résolues et leurs empreintes de contenu; il fournit la reproductibilité requise sans duplication ou package non justifié. Le rapport de dépendances confirme l'absence de vulnérabilité et de dépréciation connues dans ce graphe.

<a id="samples-omnieurope-blazor-autosmokeprogramcs"></a>
## `samples/OmniEurope.Blazor.AutoSmoke/Program.cs`

- [Moyen] [Sécurité] Le trousseau ASP.NET Core Data Protection est persisté dans le chemin temporaire prévisible `OmniEurope.Blazor.AutoSmoke.Keys` sans mécanisme explicite de chiffrement au repos. La documentation ASP.NET Core 10 confirme qu'un emplacement explicite désactive le chiffrement automatique des clés; une lecture ou altération locale de ce répertoire peut donc compromettre les charges protégées et le partage du même `ApplicationName` étend l'impact aux instances du smoke host - lignes 8-11 - revue best-effort, non outillée; source: `metrics/SECURITY_SCAN.md` et [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0) - recommandation: Codex peut supprimer la persistance temporaire explicite et conserver le fournisseur par défaut pour ce smoke host jetable, ou configurer un emplacement non temporaire avec protection de clés adaptée à chaque système d'exploitation si une persistance inter-redémarrage est réellement requise.
- [Faible] [Sécurité] Le middleware CSP apporte une base stricte, mais l'hôte Web n'ajoute pas `X-Content-Type-Options: nosniff`, `Referrer-Policy` ni `Permissions-Policy`, pourtant requis par les standards Web du kit; la défense en profondeur reste donc incomplète si le smoke host est exposé au-delà d'un poste local - lignes 17-24 - revue best-effort, non outillée; source: `C:\Dev\_Generic\docs\coding-standards.md` - recommandation: Codex peut compléter le même middleware avec ces trois en-têtes, puis vérifier leurs valeurs sur une réponse de l'hôte sans relâcher la CSP existante.

Contrôle de proportionnalité: le pipeline minimal Razor Components et la CSP dédiée sont justifiés par la sonde Interactive Auto. Aucun framework de sécurité supplémentaire n'est requis; les corrections proposées se limitent au stockage sûr du trousseau et à trois en-têtes standard.

<a id="samples-omnieurope-blazor-autosmokewwwrootappcss"></a>
## `samples/OmniEurope.Blazor.AutoSmoke/wwwroot/app.css`

- [Faible] [Qualité] La seule règle du fichier, `color-scheme: light dark`, n'est jamais chargée: aucune référence à `app.css` n'existe dans le module, et `App.razor` ne lie que la feuille de style de la bibliothèque. L'asset publié est donc mort et son intention de thème n'a aucun effet observable - lignes 1-3; source: recherche `rg` vérifiée dans le module - recommandation: Codex peut ajouter le lien vers `app.css` dans le `<head>` de `App.razor`, après la feuille de style de la bibliothèque, puis vérifier que le document charge bien l'asset; si ce comportement n'est plus souhaité, Codex pourra supprimer le fichier au lieu de conserver un asset inerte.

Contrôle de proportionnalité: une feuille globale de trois lignes est plus simple qu'un système de thème propre au smoke host; seule son absence de raccordement doit être corrigée.

## Revue du module dans son ensemble

Le module est cohérent avec sa responsabilité d'adaptateur terminal Interactive Auto, ne duplique aucune logique de la bibliothèque et respecte la direction `AutoSmoke -> AutoSmoke.Client -> OmniEurope.Blazor`. Aucun package, import, composant ou asset Radzen n'est présent; aucune recommandation ne réintroduit Radzen dans le produit clean-room. Aucun stub, suppression de contrôle, test factice ou fonctionnalité silencieusement désactivée n'a été détecté. Les limites sécurité, secrets, couverture, CRAP et complexité restent celles des rapports de métriques: revue best-effort, non outillée pour SAST et secrets, sans score fabriqué.

`PROPORTIONALITY: NONE` - le découpage Server/Client est imposé par le mode Interactive Auto et constitue la solution minimale qui prouve cette compatibilité; ajouter DI métier, routage, services, abstraction de stockage ou framework UI n'aurait aucun besoin ni second usage dans ce module.

## Compteurs du module

| Mesure | Valeur |
| --- | ---: |
| Fichiers inventoriés | 6 |
| Fichiers audités en mode `Full` | 6 |
| Fichiers non audités | 0 |
| Findings Critique | 0 |
| Findings Élevé | 0 |
| Findings Moyen | 1 |
| Findings Faible | 2 |
| Total actionnable | 3 |
