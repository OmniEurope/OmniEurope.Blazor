# Dépendances et chaîne d'outillage

Les versions directes sont centralisées dans `Directory.Packages.props`; les résolutions transitives sont figées dans les fichiers `packages.lock.json`. Le fichier `eng/dependency-policy.json` conserve la date et la source de la revue manuelle des versions sensibles. Dependabot propose chaque semaine les mises à jour NuGet et GitHub Actions sous forme de demandes contrôlées.

## Décisions vérifiées le 11 août 2026

- `bunit` reste en `2.8.6` : NuGet.org la publie comme dernière version stable. La version `2.9.0` indiquée par l'audit initial n'existe pas dans le catalogue officiel à cette date.
- `coverlet.collector` est épinglé en `10.0.1`, dernière version stable, pour produire la couverture Cobertura avec `XPlat Code Coverage`.
- les packages MAUI sont alignés sur `10.0.90`;
- le SDK et le workload set sont tous deux figés en `10.0.302`, sans roll-forward.

Toute modification doit mettre à jour les verrous, passer les tests et conserver les contrôles de couverture, de SBOM et de licences.
