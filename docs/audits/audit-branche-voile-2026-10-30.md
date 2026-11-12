<!-- SPDX-License-Identifier: EUPL-1.2 -->
# Audit de la branche du voile, 2026-10-30

Branche `work/veil-k3m9`, commit `80ddc8c8c98048949eabe4bd14e6c9bf6d321f7d`, comparée à `76daf06203bc285a4431823360b113633abd3a18`. Un commit du 30 octobre à 08:58:55 +01:00, 14 fichiers, 148 lignes ajoutées et 42 retirées. Cette synthèse est destinée au dépôt public ; le rapport nominatif et les journaux bruts restent dans les preuves locales de la session.

## Périmètre et résultat

Diff intégral et appelants immédiats relus selon trois volets : correction/sécurité/authenticité, architecture/performance/qualité et tests/conventions/documentation. Le paramètre `OmniPageVeil.Opacity` est un entier, défaut 85 %, arrondi au pas de 5 et borné de 0 à 100. `Opacity="100"` restitue le fond opaque précédent. La démo présente les deux portées existantes en premier et ajoute le curseur.

Le fond seul est mélangé au transparent, le logo garde son opacité. Les 21 classes finies préservent la CSP sans attribut style ; la notification au conteneur, le délai, les attributs ARIA et les assertions existantes restent présents. Aucune nouvelle couche ou dépendance. La baseline API reçoit le paramètre ; les baselines de couverture, seuils et exceptions restent inchangés.

Aucun défaut fonctionnel supplémentaire retenu dans ce périmètre ; deux écarts documentaires faibles. Cette revue ne clôt aucun des quinze constats de l’audit initial du 30 octobre et ne remplace pas un nouvel audit 360.

## Constats

| Identifiant | Gravité | Source et preuve | Correction proposée |
|---|---|---|---|
| VEIL-DOC-001 | Faible | `eng/scripts-probe/veil.mjs:108` annonce « opaque » alors que la vraie sonde contrôle et relève `color(srgb 1 1 1 / 0.85)`. Les assertions restent correctes. | Décrire le fond à 85 % et conserver le relevé après nouvelle exécution. |
| VEIL-DOC-002 | Faible | `CHANGELOG.md:13` classe le défaut passé de 100 à 85 sous Added ; `docs/versioning.md:6` impose Changed ou Breaking changes. La migration `Opacity="100"` est bien présente. | Classer le changement de défaut et sa migration sous Changed, sans choisir de nouvelle version. |

Ces deux constats sont suivis au lot 6 du [PLAN-009](../plans/archive/serie-2/PLAN-009-remediation-audit-360-2026-10-30.md). Ils n’ont pas été corrigés pendant l’audit.

## Preuves sur le commit de la branche

| Contrôle exécuté | Résultat |
|---|---|
| Restore verrouillé et build Release de la solution | Réussis, zéro avertissement et zéro erreur. |
| Suite Release de la bibliothèque avec TRX et couverture | 10 905 réussis, zéro échec et zéro ignoré. |
| Garde de couverture | Échec déjà présent : six fichiers, une ligne et vingt branches manquantes ; A360-007 reste ouvert. |
| Garde CRAP | Accepté, 3 825 méthodes, 13 exceptions existantes conservées. |
| Gardes API et CSP | Acceptés, respectivement 4 490 signatures et 1 056 fichiers source. |
| Nupkg local 1.10.0 et symboles | Gardes acceptés : 118 entrées, 60 assets, 23 cultures satellites et ressources neutres ; cinq entrées de symboles. |
| Budgets | Acceptés : assembly 2 546 176 octets, nupkg 2 177 274 octets, chacun sous 3 145 728. |
| Publication Release fraîche, partie veil de Scripts, vrai Chromium sous CSP | Réussie en 27 secondes, zéro erreur console et zéro violation CSP. |
| Tests analyseurs par le lanceur local | Dix tests d’analyseurs réussis, zéro échec et zéro ignoré. |
| Lancement de develop après fusion avec le lanceur local en mode silencieux | Build Debug sans avertissement ni erreur, hôtes prêts ; vrai clic Content : fond à 85 %, puis fin d’attente et boutons réactivés, console propre. |

La sonde Release mesure les fonds 85 %, 100 % et 50 %, les deux portées, le centrage des logos en balisage, par image chargée, animé ou absent, le délai d’environ 100 ms et le mode sombre. L’attente de 50 ms n’a aucune image visible. La propriété CSS opacity reste à 1. Le réglage du curseur de cette sonde utilise des événements synthétiques ; une vérification distincte du build Debug utilise la vraie touche Home, observe la valeur zéro et le fond transparent après clic Content, puis rétablit 85 par End et trois flèches gauche.

Fusion explicitement autorisée après audit et exécutée par `git merge --ff-only work/veil-k3m9` : develop passe de `76daf062` à `80ddc8c8`, sans nouveau commit de fusion ni push. L’arbre produit est comparé au commit audité, sans différence. Les corrections ultérieures du PLAN-009 sont tracées séparément.

## Limites

Les preuves sont attachées au commit ci-dessus et conservées localement, sans chemins personnels dans le dépôt public. Les tests bUnit ne prouvent pas des pixels ; les fixtures ne prouvent pas un navigateur. Aucun nouveau consommateur externe du nupkg, parcours interactif complet Hybrid/Auto/Server, audit global de dépendances, SAST, pentest ou certification d’accessibilité n’est revendiqué. La couverture en échec n’est pas présentée comme réussie. Le contrôle générique du kit et les alertes de versions du lanceur restent des signaux à qualifier, pas de nouveaux constats validés par simple comptage.
