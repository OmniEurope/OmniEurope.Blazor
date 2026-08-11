# Findings d’audit 360 - OmniEurope.PublicApiGuard

> Audit frais : 2026-08-11
> Périmètre : 3 fichiers, mode Full, lecture intégrale.
> Les constats globaux Architecture/Kit/Dépendances ne sont pas dupliqués.

## Synthèse

| Critique | Élevé | Moyen | Faible | INFO |
|---:|---:|---:|---:|---:|
| 0 | 1 | 1 | 1 | 1 |

<a id="engomnieuropepublicapiguardomnieuropepublicapiguardcsproj"></a>
## `eng/OmniEurope.PublicApiGuard/OmniEurope.PublicApiGuard.csproj`

RAS.

<a id="engomnieuropepublicapiguardpackageslockjson"></a>
## `eng/OmniEurope.PublicApiGuard/packages.lock.json`

RAS. La dépendance transitive `HtmlSanitizer` obsolète est couverte globalement par `DEP-002`; aucun constat supplémentaire propre à PublicApiGuard.

<a id="engomnieuropepublicapiguardprogramcs"></a>
## `eng/OmniEurope.PublicApiGuard/Program.cs`

- [PUBAPI-001] [Élevé] [Authenticité] L’extraction dite exhaustive omet des éléments SemVer significatifs : `PublicDeclared` exclut tous les membres `protected`, les signatures ne distinguent ni nullabilité référence, `init`/`set`, `required`, classes `abstract`/`sealed`, membres `static`/`virtual`, ni valeurs de constantes/enums, et les tableaux multidimensionnels sont aplatis en `[]`. Des ruptures publiques peuvent donc franchir la gate sans diff - lignes 42, 55-71, 78-109 et 152-163 - preuve : les propriétés nullable `OmniSchedulerAppointment.Description` et init-only `OmniDialogRequest.Footer` sont sérialisées comme non nullable et `{get;set}`; les valeurs des 102 champs const sont absentes - recommandation : Codex peut remplacer la sérialisation ad hoc par un modèle couvrant accessibilité, modificateurs, nullabilité, custom modifiers, valeurs et formes de types, avec snapshots négatifs par mutation.
- [PUBAPI-002] [Moyen] [Tests] L’auto-test vérifie seulement la présence de dix fragments dans un ensemble global; il omet notamment enums, champs, paramètres/ref/in/params, valeurs par défaut, attributs `Parameter`/`EditorRequired`, nullabilité, init/required, visibilité protégée et modificateurs. Une régression peut être masquée dès qu’un autre fixture conserve le même marqueur - lignes 167-177 - preuve : aucun test de l’extracteur dans les 181 tests et aucune classe PublicApiGuard dans Cobertura - recommandation : Codex peut créer des fixtures exactes par catégorie et des mutations devant produire un diff, puis inclure l’outil dans la couverture.
- [PUBAPI-003] [Faible] [Fiabilité] La comparaison utilise `Except` dans les deux sens et accepte donc une baseline désordonnée ou contenant des doublons, malgré la promesse d’une sérialisation canonique; le mode update l’écrase en outre directement sans remplacement atomique - lignes 13-17 et 27-35 - preuve : comparaison ensembliste sans validation de `SequenceEqual` - recommandation : Codex peut comparer la séquence exacte à la sortie triée/unique et écrire la mise à jour dans un fichier sibling validé avant remplacement atomique.

## Notification de proportionnalité (INFO)

- [PUBAPI-INFO-001] Un exécutable de réflexion unique reste l’architecture la plus simple pour ce dépôt; ajouter un service, un plugin ou une couche de stockage serait excessif. La correction proportionnée consiste à enrichir le modèle de signature et ses fixtures exactes, ou à adopter un outil API-compat éprouvé sans construire un framework maison.

