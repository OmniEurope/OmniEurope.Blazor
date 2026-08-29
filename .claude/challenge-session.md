# Challenge - Avocat du diable (session)

> Revue adversariale de la session par `/challenge session` (sous-agents à œil neuf sur le transcript).
> Revue sans correction automatique - des points à peser, pas des modifications du projet.
> Dédupliqué, classé par gravité. Séparé de `.claude/auditsession.md` (revue code) et `suggestions.md`.
> Last updated: 2026-08-29

## Findings

- [🟠 Élevé] **DECISION REQUIRED** - Le pin `global.json` (SDK `10.0.302`, `rollForward: disable`) ne
  correspond à aucun SDK installé, et il est contourné à chaque fois au lieu d'être résolu - preuve :
  les deux critiques l'ont relevé indépendamment, et le panéliste tests de l'audit a rendu
  « TESTS_RUN: NOT RUN (SDK 10.0.302 requis par global.json absent ; installés : 10.0.202, 10.0.303) » ;
  vérifié sur disque, `dotnet --list-sdks` ne liste que `3.1.426`, `10.0.202`, `10.0.303` - risque :
  toute machine ou CI sans ce SDK exact voit `dotnet build` et `dotnet test` échouer immédiatement ;
  dans cette session même, un des trois panélistes d'audit n'a pu exécuter aucune gate, ramenant de fait
  la vérification « 3/3 » d'un changement à rupture d'API publique à deux avis réellement outillés. Le
  contournement (édition temporaire de `global.json` puis restauration) a été répété sans jamais traiter
  la cause - vérif : aligner `global.json` sur un SDK réellement disponible (`10.0.303` avec
  `rollForward: latestFeature`) ou scripter l'installation de `10.0.302` sous `eng/`, puis relancer la
  chaîne sans aucune édition manuelle. _(lentilles : pre-mortem, authenticité)_
  **Décision requise avant exécution** : changer le SDK épinglé ou son `rollForward` touche la politique
  de version du dépôt et la reproductibilité de la CI. Hors du périmètre qu'une correction automatique
  peut trancher seule.

## Corrigé dans ce run

- [🟡 Moyen] Les quatre chaînes de ressources `MultiSelect*` n'étaient exercées par aucun test.
  Couvert par `CompactMultiSelect_ResolvesItsOwnResourcesInBothCultures` en `fr-FR` et `en-US`.

## Notifications

- [Sur-ingénierie] aucune. Les deux critiques ont comparé la surface livrée à l'alternative la plus
  simple et n'ont relevé aucune flexibilité non justifiée.
