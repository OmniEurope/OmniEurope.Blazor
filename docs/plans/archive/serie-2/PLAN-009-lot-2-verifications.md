<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009 : preuves du lot 2, 2026-10-30

> Lot 2 terminé le 2026-10-30. A360-001 et A360-002 clôturés par les preuves ci-dessous. Le test adjacent autorisé est aussi vérifié.

Base actuelle : `develop`, `55f1dd2cdc749ef36ac0e6eb697dcc8277c93a76`. La modification du menu d’apparence intervenue après le lot 1 est préservée. Périmètre : suppression de ligne terminale dans l’éditeur HTML et repli mémoire des préférences sous quota ; commit final autorisé, sans push. Version inchangée.

## Reproductions avant modification

| Constat | Parcours et résultat observé |
|---|---|
| A360-001 | Démo réellement lancée, `/components/html-editor`, article annoté : collage HTML riche d’un tableau contenant A puis une dernière ligne B avec `rowspan="2"`. Deux lignes et rowspan de B relus dans le DOM. Clic B puis Delete row : écran d’erreur de toute la page et `JSException: Cannot read properties of undefined (reading 'cells')`, pile `placeCell` puis `deleteRow`. Capture et console conservées. |
| A360-002 | Nupkg frais produit après restore verrouillé et build Release. Module extrait, SHA identique à la source. Sur une origine de test réservée, vraie saturation de localStorage, exception native `QuotaExceededError` et rejet d’une écriture de 4 096 caractères confirmés. `set` revient normalement, mais `get` et `entries` rendent OLD (3 caractères), au lieu de NEW (4 096). Aucun remplacement du stockage par un double dans cette preuve. |

Les sept tests unitaires de préférences ajoutés sont d’abord exécutés avant modification du script : trois réussissent, quatre échouent sur les anciennes lectures. Ils emploient des doubles de stockage et ne constituent pas la preuve du quota réel ci-dessus.

## Changements minimaux

- `html-editor/tables.js` : déplacer une cellule ancrée dans la ligne supprimée seulement si une ligne suivante existe. Les calculs de grille, les autres commandes et le traitement des fusions valides restent conservés.
- `omni-preferences.js` : `get` consulte d’abord la copie mémoire d’une écriture persistante échouée ; `entries` n’écrase pas cette copie par l’ancienne valeur du stockage. Le nettoyage mémoire après une écriture persistante réussie reste présent.
- Régression de tableau ajoutée à la sonde existante de l’éditeur : collage, suppression, ligne voisine et suite du document contrôlées. Sept régressions de préférences couvrent la priorité mémoire, le préfixe, les écritures successives, la réconciliation, les suppressions, la tombstone et le refus de lecture. Aucune API, dépendance, baseline ou exception modifiée ; les assertions existantes de ces parcours sont conservées. La correction d’assertion du test adjacent est décrite séparément.

## Preuves après modification

| Contrôle | Résultat |
|---|---|
| Build Release de la solution | Réussi, zéro avertissement et zéro erreur. |
| Tests JavaScript | 74 réussis, zéro échec et zéro ignoré ; les sept régressions de préférences réussissent. Preuve de logique avec doubles, distincte des parcours navigateur. |
| Même quota natif, module corrigé extrait du nupkg | `get` et `entries` rendent tous deux NEW (4 096 caractères), tandis que le stockage tient toujours OLD. Tombstone sous quota relue avec Value null. Après libération des seules clés de remplissage : écriture réellement persistée, copie mémoire libérée et écriture externe visible. Retrait puis vidage natifs du stockage de test : aucune valeur fantôme, zéro clé restante, console sans erreur ni avertissement. |
| Même collage et Delete row dans la démo | Réussi : une ligne A conservée, ligne B supprimée, suite du document présente. Saisie réelle ensuite relue, surface toujours modifiable et aucune erreur console. |
| Suppression ordinaire et cellules fusionnées valides | Réussies : suppression de A/B conserve C/D ; fusion ancrée sur la ligne supprimée déplacée dans la ligne suivante avec son contenu ; fusion traversant une ligne supprimée réduite de 3 à 2, voisins et contenu conservés. |
| Sonde Chromium de la publication fraîche, parties editor et preferences | Réussie en 41 secondes, zéro erreur console et zéro violation CSP. Inclut le cas terminal, les commandes ordinaires de tableau et les préférences entre onglets, stockage refusé et synchronisation. Les scénarios de base de démonstration utilisent un stockage de test dans le navigateur, pas PostgreSQL. |
| Consommateur C# ordinaire, vraie démo et API locale | Valeur de test enregistrée puis relue à l’identique par le service et directement par le stockage HTTP ; synchronisation et RemoveAsync effectués ; les deux affichages reviennent à leur état initial sans valeur, console propre. Distinct du scénario de quota du module extrait. |
| Suite complète de bibliothèque | Première passe : 10 907 réussis et un échec sur le nouveau test du menu. Après correction autorisée et dix passages consécutifs réussis : 10 908 réussis, zéro échec et zéro ignoré. Les nouveaux fichiers publics sont indexés et contrôlés par le garde inchangé. |
| API publique et CSP source | Gardes acceptés, 4 495 signatures et 1 056 fichiers source. |
| Nupkg et symboles | Gardes acceptés : 118 entrées, 60 assets, 23 cultures satellites et ressources neutres ; cinq entrées de symboles. |
| Budgets | Acceptés : assembly 2 548 224 octets, nupkg 2 179 310 octets, chacun sous 3 145 728. |

SHA-256 du module extrait et comparé à la source pour chaque phase :

```text
before 2025A52899629658C04EDB0BD2D09A143DA1592300093C4AA5F5C07F334306D8
after  507CEEA2F0DE73BA91D79FC3DCBB6DA501EEB63F8D3E763B12E722EAC6C04DB0
```

## Test adjacent du menu, extension explicitement autorisée

La première suite complète échoue dans `AppearanceStateTests.The_random_button_of_a_menu_that_follows_the_state_writes_the_whole_look_once`, ajouté par le commit de base. Il cherche un nom accentué en texte brut dans le JSON sérialisé. Après instrumentation, six tirages réussissent puis le septième échoue : nom attendu `Rétro`, champ stocké `"preset":"R\u00E9tro"`. Ce passage est une reproduction avant correction, pas une relance pour obtenir du vert.

La correction autorisée remplace la recherche de sous-chaîne par une égalité exacte du champ `preset` décodé avec `JsonDocument`. Le nombre d’écritures, le thème changé et le menu resté ouvert sont toujours contrôlés. Une trace reste attachée à la sortie du test. Aucun changement du produit, de l’aléatoire, du sérialiseur, des délais ou des règles.

Preuve après correction : dix exécutions consécutives réussies, un test réellement exécuté par passage et TRX vérifié ; les tirages comprennent `Néon` et `Nénuphar`, dont les JSON bruts restent échappés et les valeurs décodées correspondent exactement au thème. La même suite complète réussit ensuite avec 10 908 tests. Aucun retry ni échec ignoré. La recompilation a duré 25 minutes sous forte pression mémoire mesurée, sans désactivation d’analyseur ou autre protection.

## Limites et conservation

Le module JavaScript n’exporte ni remove ni clear. La suppression applicative passe par `OmniPreferences.RemoveAsync`, qui écrit une tombstone datée ; aucune nouvelle API de suppression n’est inventée. Les tests du retrait et du vidage ci-dessus utilisent les opérations natives du stockage après réconciliation. Le test de quota prouve le module extrait, le stockage natif et le contenu de la tombstone ; il ne prouve pas à lui seul un trajet C#/HTTP/PostgreSQL sous quota.

Les journaux, métadonnées du paquet, captures, consoles et fixtures sont conservés dans le dossier local de preuves du lot 2, daté du 2026-10-30. L’instance de fixture d’abord lancée dans le bac à sable était inaccessible au navigateur ; la preuve réelle utilise une origine locale joignable. Les clés de test sont nettoyées. Aucun seuil, garde ou délai assoupli. Les autres lots, dont la couverture A360-007, restent ouverts.
