# Stratégie de tests

La configuration exécutable canonique est `.claude/test-config.md`; la CI de référence est `.github/workflows/ci.yml`. Ce document explique leur portée sans dupliquer leurs commandes.

## Niveaux de preuve

- Unitaire : bUnit, contrats de rendu, localisation, accessibilité, API et gardes de conventions dans `tests/OmniEurope.Blazor.Tests`.
- Vitrine : gardes du site `site/OmniEurope.Blazor.Showcase`, exécutées dans la même suite unitaire. Elles exigent qu'un composant public ait une démonstration, que chaque valeur de chaque paramètre énuméré y apparaisse, que chaque démonstration se rende sans laisser fuir un littéral d'énumération dans le HTML, et que le personnalisateur, sa persistance et le contraste des palettes tiennent. Une exclusion de couverture n'est valide que si un test prouve qu'elle porte sur une chose réelle et qu'une démonstration ouvre l'interface par laquelle le visiteur atteint cette chose.
- Intégration : CSP source et fixtures, inventaires, budgets, paquet principal et symboles, empreintes de provenance.
- Bout en bout : catalogue Server, WebAssembly et Interactive Auto dans Chromium, pilotés par CDP; MAUI Hybrid dans WebView2 sous Windows, par auto-test interne. Le runner Windows n'expose jamais le port de débogage WebView2, quel que soit le moyen de le demander, donc l'hôte Hybrid se pilote lui-même quand `HYBRIDSMOKE_SELFTEST` est défini : `wwwroot/hybrid-smoke.js` clique le bouton, lit le compteur, la langue et le titre, capte les erreurs console, et l'hôte publie le résultat sur sa sortie standard, que `eng/Test-HybridHost.ps1` lit et vérifie.

Une compilation ne remplace pas une preuve navigateur. Une présence de fichier ne remplace pas un scénario comportemental. Les commandes lourdes .NET sont lancées via le runner gardé imposé par les instructions du dépôt.

## Critère de livraison

La livraison exige le build Release sans avertissement, la suite globale verte, les quatre hôtes vérifiés, les contrôles CSP/API/paquet/budgets, puis l'artefact NuGet exact accompagné de sa provenance. Le workflow de publication ne reconstruit jamais le paquet.
