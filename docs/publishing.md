# Guide de publication NuGet

Procédure opérationnelle pour publier `OmniEurope.Blazor` sur NuGet.org. La politique de versionnement est dans [versioning.md](versioning.md); ce guide ne décrit que les gestes et les liens.

Dépôt : `OmniEurope/OmniEurope.Blazor`. Version déclarée dans `src/OmniEurope.Blazor/OmniEurope.Blazor.csproj` : `1.1.0`.

## Principe

La publication n'est jamais manuelle. Le workflow `.github/workflows/publish-nuget.yml` se déclenche sur l'événement `release: published`, retrouve le run CI vert du commit ciblé, télécharge l'artefact `nuget-package` déjà validé, vérifie sa provenance, sa version, son contenu et ses symboles, puis pousse ce paquet exact. Il ne reconstruit rien. Aucune clé API n'est stockée : l'authentification passe par le trusted publishing NuGet (OIDC).

Conséquence : un paquet ne peut être publié que s'il provient d'un run CI réussi sur le commit de la release.

## Prérequis, à vérifier une fois

| Réglage | Où | Attendu |
| --- | --- | --- |
| Environnement `release` | https://github.com/OmniEurope/OmniEurope.Blazor/settings/environments | Existe, avec les protections voulues |
| Secret `NUGET_USER` | https://github.com/OmniEurope/OmniEurope.Blazor/settings/secrets/actions | Nom de compte NuGet.org, pas une clé API |
| Trusted publishing | https://www.nuget.org/account/trustedpublishing | Déclare le dépôt, le workflow `publish-nuget.yml` et l'environnement `release` |

## Étapes

1. **Publier `main`.** Le push de `main` est une action humaine, jamais déléguée à un agent.

   ```bash
   git push origin main
   ```

2. **Attendre la CI verte sur ce commit.** C'est ce run qui produit l'artefact publié.

   https://github.com/OmniEurope/OmniEurope.Blazor/actions/workflows/ci.yml?query=branch%3Amain

3. **Créer la Release.** Elle déclenche la publication.

   https://github.com/OmniEurope/OmniEurope.Blazor/releases/new

   Tag : la version exacte du `.csproj`, sans préfixe (`1.1.0`). Cible : `main`. Le workflow compare le tag à la version du paquet et échoue en cas d'écart.

4. **Suivre la publication.**

   https://github.com/OmniEurope/OmniEurope.Blazor/actions/workflows/publish-nuget.yml

5. **Vérifier le paquet.** L'indexation prend quelques minutes.

   https://www.nuget.org/packages/OmniEurope.Blazor

Les symboles (`.snupkg`) partent dans le même appel `dotnet nuget push`; NuGet.org les route vers son serveur de symboles.

## Dépendances

Les mises à jour automatiques sont hebdomadaires : `.github/dependabot.yml` a été retiré après que des montées de version fusionnées aient cassé `main`, puis réintroduit le 2026-09-19 avec le modèle du kit. Les paquets gelés et leurs motifs sont dans `eng/dependency-policy.json`, et `eng/Test-DependencyPolicy.ps1` en fait une garde CI. Ceux marqués `toolchain-bound` sont exclus par le bloc `ignore` de la configuration : une proposition sur l'un d'eux ne se fusionne pas, elle se ferme.
