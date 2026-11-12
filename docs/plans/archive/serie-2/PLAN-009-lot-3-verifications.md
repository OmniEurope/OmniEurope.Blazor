<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009 : preuves du lot 3, 2026-10-30

> Lot 3 terminé le 2026-10-30. A360-003, A360-006 et A360-005 clôturés par les preuves ci-dessous.

Base : `develop`, `188dcf43e90f73d37998a4012dd91a56aa61b2c5`. Périmètre : verrouillage des sélecteurs ouverts, fermeture explicite du dialogue Information et insertion de variables dans la source du courriel. Commit autorisé, sans push. API et version inchangées.

## Reproductions avant modification

| Constat | Observation réelle |
|---|---|
| A360-003 | Démo locale, calendrier ouvert, activation de ReadOnly par Espace sans clic extérieur : choisir le 31 octobre change la valeur liée du 30 au 31. Avec Disabled, choisir le 30 change aussi la valeur. Le même parcours DateTimePicker change la date dans les deux états. Le panneau reste accessible malgré le déclencheur désactivé. |
| A360-003, heure | Une instrumentation temporaire ajoute deux interrupteurs et un compteur de ValueChanged à l’exemple de l’heure. Sur la publication antérieure, le panneau ouvert change `09:30` en `10:30` après ReadOnly, compteur 1, puis en `11:30` après Disabled, compteur 2. Ce défaut analogue est donc exécuté avant de modifier TimePicker. |
| A360-006 | Dismissible désactivé, Information ouvert : aucune action de fermeture, seulement les deux sentinelles de focus. Échap et appui sur le fond laissent le dialogue affiché. |
| A360-005 | Source du courriel, caret à la fin, position 139 : le bouton Client name ne change ni le texte ni sa longueur. Le test ciblé confirme l’absence de l’appel d’insertion à la source. |

Les six nouveaux cas de verrouillage échouent avant correction sur le panneau encore présent, puis passent après correction. Le nouveau cas du courriel échoue avant correction et passe après. La nouvelle sonde Pickers échoue aussi sur la publication antérieure : « Le verrouillage Lecture seule de dates-date laisse le panneau ouvert ou change la valeur liée. » Captures, états accessibles, journaux et TRX sont conservés dans le dossier local de preuves du lot 3.

## Changements bornés

- Les trois sélecteurs ferment leur panneau existant lorsque ReadOnly ou Disabled devient vrai, sans redonner le focus au déclencheur désormais désactivé.
- La composition Information fournit toujours un pied avec une action Close. La primitive et son refus d’Échap et du fond lorsque Dismissible est faux ne changent pas.
- Le courriel réutilise `insertAtCaret` pour la face source active. Ce chemin accepte aussi une textarea, remplace sa sélection et déclenche l’entrée habituelle du champ. La face visuelle, le sujet et les variables Block gardent leurs chemins.

## Vérifications après modification

| Contrôle | Résultat observé |
|---|---|
| Build Release de la solution | Réussi, zéro avertissement et zéro erreur. |
| Tests ciblés | PickerEdgeTests : 22 réussites ; MailTemplateEditorTests : 21 ; ShowcaseDialogsExampleTests : 2. Doubles d’interop : ces tests prouvent les appels, les notifications du formulaire et les rendus, pas le moteur du navigateur. |
| API publique | 4 495 signatures acceptées, baseline inchangée. |
| CSP | 1 056 fichiers acceptés ; fixture sûre acceptée et fixture dangereuse refusée. |
| Compteurs navigateur | Six cas réels, les trois sélecteurs avec ReadOnly puis Disabled : panneau absent, valeur et compteur de ValueChanged inchangés pendant le verrouillage. Réactivation puis sélection : valeur changée et exactement un callback supplémentaire dans chaque cas. Aucune nouvelle erreur ni aucun avertissement console. |
| Dialogue dans le navigateur | Close ferme Information et rend le focus à son déclencheur avec Dismissible vrai puis faux. Faux : rôle alertdialog, aucune croix, Échap et fond ne ferment pas, action explicite visible et utilisable. Capture inspectée. |
| Source du courriel dans le navigateur | L’insertion auparavant sans effet à la fin fonctionne et persiste après aller-retour visuel/source. Sur `<p>Before TARGET after.</p>`, remplacer TARGET donne exactement `<p>Before {{client}} after.</p>`, token unique, sélection réduite au caret 20. Capture inspectée. |
| Tests JavaScript | 74 réussites, aucun échec ni test ignoré. |
| Paquet local 1.10.0 | 118 entrées, 60 assets, 23 satellites et 5 entrées de symboles acceptés. Assembly : 2 548 736 octets ; nupkg : 2 179 480, chacun sous 3 145 728. Le module extrait, les sources et la publication finale ont le même SHA-256 : `106BD3A848204DF230C15804767D7D95BFD520972D57349F025C465E5F47F63D`. |
| Recette navigateur finale | Publication Release sans instrumentation réussie. Pickers et Scripts, parties overlays et editor seulement : réussis en 166 s, aucune erreur console ni violation CSP. Le courriel vérifie aussi le caret source, le remplacement, le retour visuel/source, le sujet et la variable Block Expected documents. Le dialogue vérifie ses deux états, le focus et le refus des fermetures implicites. |
| Suite complète de bibliothèque | Lanceur officiel, Debug : 10 917 réussites, zéro échec et zéro test ignoré. TRX `library-tests-20261030-140151752.trx` relu : total 10 917, réussis 10 917, échecs 0. Les gardes d’architecture et de conventions restent actifs. |
| Remise en route | Lanceur silencieux : état Ready observé. Catalogue local, vitrine locale et endpoint de disponibilité de l’API répondent réellement HTTP 200. La dernière précision XML limite la remarque de remplacement aux variables textuelles ; compilation Release et paquet revérifiés, sans changement fonctionnel après la suite. |

Les deux fichiers instrumentés de l’exemple horaire sont restaurés à leur contenu initial : diff Git vide, puis nouvelle publication finale. Leurs copies et les compteurs JSON restent uniquement dans le dossier local de preuves. Aucun paramètre de démonstration temporaire ne fait partie du lot livré.

## Limites et incidents du dispositif de preuve

La première compilation de la fixture refuse un paramètre inexistant du commutateur ; l’instrumentation utilise ensuite son attribut accessible existant. La publication avec `--no-build` est refusée par les projets d’analyseurs, donc la publication normale est utilisée. Le premier passage instrumenté après correction échoue sur une expression convertie en nullable dans la fixture, et sur un ciblage confondant le réglage radio Information avec le bouton d’ouverture. La fixture emploie ensuite des propriétés directement nullables, et le ciblage exclut les boutons radio, avec les noms accessibles des deux langues. Les compteurs navigateur sont rejoués avec succès. Les passages en échec sont conservés comme tels ; ils ne valent pas validation du produit.

La preuve reste locale, sans production ni publication. Les hôtes Server, Auto et Hybrid ne sont pas rejoués en exécution dans ce lot. La consommation isolée du nupkg, la recette transversale, la porte de couverture et les autres constats restent dans les lots suivants. Des tests verts ne clôturent pas ces contrôles différés.

Le précontrôle signale huit références NuGet dépassées, déjà présentes, sans modification de version. Son attente est localisée par lecture de la pile PowerShell à la ligne 256 du scanner partagé, sur l’énumération récursive avec Include. Une mesure indépendante retrouve 58 chemins ; deux filtres explicites retrouvent exactement ces mêmes chemins en 1,84 s. L’extension est autorisée séparément : commit du kit `45da69db695dfd7b5cdadcc44100848fc6ab5664`, sur main, sans push. Six cas du harnais passent avant et après, dont les inclusions docs/archive et les exclusions. Le même scan réel complet termine ensuite en 108,81 s avec `RESULT: WARN` et 389 motifs mécaniques ; ce compte n’est pas une liste de défauts validés. La première invocation bloquée est interrompue avant tout test global, son journal est conservé, puis le lanceur est relancé normalement. Aucun contrôle, seuil ou filtre d’exclusion n’est assoupli. Les modifications préexistantes du kit sont laissées hors du commit.

Le serveur d’index des composants est indisponible ; les composants, conventions et sources existants sont consultés directement.
