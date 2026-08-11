# Stratégie de tests

La configuration exécutable canonique est `.claude/test-config.md`; la CI de référence est `.github/workflows/ci.yml`. Ce document explique leur portée sans dupliquer leurs commandes.

## Niveaux de preuve

- Unitaire : bUnit, contrats de rendu, localisation, accessibilité, API et gardes de conventions dans `tests/OmniEurope.Blazor.Tests`.
- Intégration : CSP source et fixtures, inventaires, budgets, paquet principal et symboles, empreintes de provenance.
- Bout en bout : catalogue Server, WebAssembly et Interactive Auto dans Chromium; MAUI Hybrid dans WebView2 sous Windows.

Une compilation ne remplace pas une preuve navigateur. Une présence de fichier ne remplace pas un scénario comportemental. Les commandes lourdes .NET sont lancées via le runner gardé imposé par les instructions du dépôt.

## Critère de livraison

La livraison exige le build Release sans avertissement, la suite globale verte, les quatre hôtes vérifiés, les contrôles CSP/API/paquet/budgets, puis l'artefact NuGet exact accompagné de sa provenance. Le workflow de publication ne reconstruit jamais le paquet.
