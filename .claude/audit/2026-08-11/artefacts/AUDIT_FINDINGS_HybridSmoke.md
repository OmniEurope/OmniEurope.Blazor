# Findings d'audit 360 - HybridSmoke

> Audit: 2026-08-11
> Les blocs sont ajoutés fichier par fichier. Une absence de finding est consignée explicitement par `RAS`.

<a id="samples-omnieurope-blazor-hybridsmoke-hybridsmoke-razor"></a>
## `samples/OmniEurope.Blazor.HybridSmoke/HybridSmoke.razor`

- [Élevé] [Style] `STD-I18N` : le titre de la sonde et le libellé du bouton sont codés en dur sans `IStringLocalizer<AppStrings>` - lignes 4-5 - source : registre `_Generic` et `AUDIT_KIT.md` (infrastructure de localisation absente) - recommandation : Codex peut ajouter un contrat de ressources adapté au smoke host, conserver `OmniEurope.Blazor` comme nom de marque et localiser les termes descriptifs et l'action.
- [Élevé] [Fiabilité] `STD-UIVERIFY` : le composant visuel Hybrid ne possède aucune preuve d'exécution dans WebView2 ni de console sans erreur ; le projet est une bibliothèque de compilation sans entrée MAUI exécutable - lignes 3-6, preuves croisées `OmniEurope.Blazor.HybridSmoke.csproj:3-8` et `docs/compatibility.md` - source : registre `_Generic` et matrice de compatibilité - recommandation : Codex peut ajouter une sonde MAUI Windows exécutable minimale, charger ce composant comme racine de `BlazorWebView`, l'inspecter par CDP et conserver la preuve d'une console sans erreur avant de fermer la gate graphique Hybrid.
- [Moyen] [Style] `STD-BTN` : le bouton d'action possède un texte mais aucune icône, contrairement à la convention texte + icône hors grille - ligne 5 - source : registre `_Generic` - recommandation : Codex peut ajouter une icône décorative adaptée à `OmniButton` tout en conservant un libellé localisé, sans réintroduire Radzen.

Sécurité : contenu statique sans entrée non fiable, secret, injection, ressource ou état persistant ; aucun autre défaut de performance ou de fiabilité, aucun défaut d'authenticité ou de clean-room et aucun tiret cadratin U+2014 détecté. L'absence de SAST et de scan de secrets outillé limite cette conclusion à une revue best-effort, non outillée. La sonde reste une preuve de compilation ; l'exécution graphique Hybrid est explicitement hors de sa preuve actuelle selon `docs/compatibility.md`.

<a id="samples-omnieurope-blazor-hybridsmoke-hybridsmoketypes-cs"></a>
## `samples/OmniEurope.Blazor.HybridSmoke/HybridSmokeTypes.cs`

- [Faible] [Architecture] `HybridSmokeTypes` et `RequiredTypes` sont publics alors qu'aucun appelant n'existe dans le dépôt et que l'architecture réserve ce module à un adaptateur terminal sans surface réutilisée - lignes 6-12 - source : recherche `HybridSmokeTypes|RequiredTypes` limitée au dépôt, sans autre résultat, et `AUDIT_ARCHITECTURE.md` - recommandation : Codex peut conserver cette preuve de résolution de types tout en rendant le conteneur et son membre internes, afin de ne pas exposer une API de sample inutile.

La liste de types matérialise bien des références de compilation à `BlazorWebView` et `OmniButton` ; elle n'est donc pas classée comme stub ou faux test. La solution la plus simple qui préserve cette preuve est le même conteneur à visibilité interne, sans abstraction supplémentaire. Aucun secret, entrée, injection, blocage asynchrone, ressource non libérée, `partial`, suppression de contrôle, code Radzen, défaut de performance notable ou tiret cadratin U+2014 n'est détecté. Revue de sécurité best-effort, non outillée ; l'absence de MCP Roslyn limite la preuve d'usage à la recherche textuelle vérifiée.

<a id="samples-omnieurope-blazor-hybridsmoke-csproj"></a>
## `samples/OmniEurope.Blazor.HybridSmoke/OmniEurope.Blazor.HybridSmoke.csproj`

- [Moyen] [Fiabilité] `D-001` : les références directes `Microsoft.Maui.Controls` et `Microsoft.AspNetCore.Components.WebView.Maui` héritent des versions centralisées 10.0.20, alors que le scan NuGet les donne en 10.0.90 et relève un sous-graphe `Microsoft.Extensions.*` partagé entre 10.0.0 et 10.0.10 - lignes 13-14 - source : `AUDIT_DEPENDENCIES.md` et `Directory.Packages.props` - recommandation : Codex peut mettre à niveau les deux packages MAUI de façon coordonnée avec le SDK et le workload retenus, régénérer le verrou, puis vérifier la restauration verrouillée et la compilation Hybrid Windows.

Le SDK Razor avec `UseMaui`, `SingleProject`, une cible Windows explicite, `OutputType=Library`, un verrou activé et la seule référence projet vers la RCL forme une sonde de compilation proportionnée. Une application MAUI exécutable avec `Platforms/`, démarrage DI et cycle de vie WebView2 offrirait une preuve runtime plus forte, mais élargirait la mission actuelle ; `docs/compatibility.md` qualifie correctement cette limite et ne prétend pas à une validation graphique. Aucun package Radzen, version flottante dans ce manifeste, `NoWarn`, suppression d'analyse, référence inversée, secret, chemin non fiable ou tiret cadratin U+2014 n'est présent. Sécurité supply-chain fiable pour les avis NuGet de la révision ; SAST et scan historique de secrets non fiables.

<a id="samples-omnieurope-blazor-hybridsmoke-lock"></a>
## `samples/OmniEurope.Blazor.HybridSmoke/packages.lock.json`

- [Moyen] [Fiabilité] `D-001` : le verrou matérialise une pile Hybrid en retard et désalignée : packages directs MAUI 10.0.20 aux lignes 5-26, `Microsoft.AspNetCore.Components.WebView` 10.0.0 aux lignes 62-73, plusieurs transitifs `Microsoft.Extensions.*` 10.0.0 aux lignes 106-220 et composants Blazor/RCL 10.0.10 aux lignes 386-403 - source : fichier intégral de 407 lignes et `AUDIT_DEPENDENCIES.md` - recommandation : Codex peut mettre à niveau les deux parents MAUI de façon coordonnée, régénérer ce verrou et confirmer la disparition du sous-graphe 10.0.0 non nécessaire par restauration verrouillée et compilation Hybrid Windows.

Le verrou NuGet v2 contient 45 entrées, deux dépendances directes, une dépendance central-transitive, une référence projet et un `contentHash` pour chaque package résolu. Le scan NuGet de la révision établit zéro vulnérabilité connue et zéro package déprécié. Les packages Windows lourds et aux licences spécifiques restent cantonnés au sample Hybrid ; `D-005` couvre déjà au niveau global l'absence de SBOM et de registre de notices, sans défaut supplémentaire propre à ce JSON. Aucun package Radzen, secret, URL de dépôt non fiable, suppression de contrôle, preview explicite, conflit diagnostiqué ou tiret cadratin U+2014 n'est présent. Sécurité supply-chain fiable pour les avis NuGet et les versions verrouillées ; SAST et historique des secrets non fiables. Complexité, couverture et CRAP non applicables à ce verrou généré.

`PROPORTIONALITY: NONE` - le graphe verrouillé correspond aux deux briques MAUI requises, à WebView2, aux packs Windows et à la RCL ; aucune dépendance directe superflue n'est prouvée. La sonde de compilation reste l'alternative minimale pour la compatibilité de build, tandis qu'une sonde exécutable est requise uniquement pour satisfaire la gate graphique distincte `STD-UIVERIFY`.

## Revue du module dans son ensemble

Le module est cohérent, terminal et faiblement couplé : un composant Razor, une preuve de types, un manifeste MAUI Windows et son verrou. La dépendance de projet pointe uniquement vers la RCL, aucun modèle ou service du module n'est consommé ailleurs, aucun mécanisme Radzen n'est réintroduit et la procédure clean-room reste respectée. Les responsabilités de compilation et de preuve graphique sont clairement séparées dans la documentation ; la seconde demeure ouverte et est consignée ci-dessus. Les dimensions authentification, autorisation, persistance, sérialisation, I/O et concurrence ne sont pas applicables à ce module statique. Findings actionnables propres ou rattachés au module : 6 occurrences, dont 2 élevées, 3 moyennes et 1 faible ; `D-001` est décrit dans deux blocs pour assurer la preuve par fichier mais constitue un seul écart sémantique lors de la consolidation, soit 5 écarts distincts.
