<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-009, lot 5 : couverture

État au 2026-10-30 : **lot 5 terminé**. La mesure complète finale compte 10 981 réussites ; les portes de couverture et CRAP acceptent le rapport frais sans changer seuil, baseline ni exception. Aucun code produit modifié.

## Reproduction et scénarios

La mesure fraîche `coverage-before-valid`, avec son unique Cobertura et son TRX de 10 924 réussites, reproduit une ligne et vingt branches manquantes dans les six fichiers désignés par A360-007. CRAP passe.

Les tests ajoutés vérifient le résumé masqué même avec une erreur et un slot masqué, la mise à jour sous le même EditContext, le démontage du parent avec son voile, les valeurs absentes ou vides de recherche, l’effacement sans navigation et les contrats de filtrage. Le renommage d’une entrée filtrée fait apparaître puis disparaître ses deux ancêtres et actualise la ligne de résultat, sans changer l’état de dépliage détenu par l’hôte. Les groupes conservent aussi leur état initial ou explicitement choisi quand seul leur ancêtre correspond.

Les contextes internes minimaux du menu vérifient ses dépendances optionnelles par leur DOM ; ils ne constituent pas un parcours navigateur. Le retrait répété d’un voile absent n’est pas accessible par les appelants publics normaux relus : leur inscription est retirée une fois avant effacement de la référence. Sur accord explicite, un test séparé vérifie le contrat interne d’idempotence, sans rendu supplémentaire ni retrait d’un autre voile. Cette preuve ne prétend pas reproduire ce chemin défensif par l’interface publique.

## Mesures observées

- `coverage-after-1` conserve l’échec d’une nouvelle fixture : son second rendu omettait ChildContent et supprimait l’entrée. La fixture conserve désormais le même fragment et la même instance. Aucun correctif produit.
- `coverage-after-2` : 10 937 réussites, aucune omission ; deux branches restent à couvrir, retrait absent et groupe initialement déplié.
- `coverage-after-3` : 10 980 réussites, aucune omission. Les six fichiers initiaux n’ont plus de ligne ni branche manquante. CRAP accepte 3 835 méthodes avec les treize exceptions existantes.

Entre ces deux dernières mesures, une fusion externe avance `develop` de `7dc4ca74` à `fd0eb60f`. Elle ajoute quarante cas de test et le nouveau composant `OmniPassTracker`. La porte complète restait alors en échec sur une seule branche de ce composant, ligne 76 : une marque future après celle en cours. Le test ciblé de la séquence « terminé, en cours, à faire » est ajouté sur accord explicite, sans modifier le composant ; quinze tests ciblés passent. Leur rapport ne présente aucune ligne ni branche manquante dans ce fichier, dont la ligne 76 à 100 % (4/4). Ce rapport ciblé ne remplace pas le rapport global unique exigé. Ces seules vérifications ciblées ne clôturaient pas le constat initial.

## Parcours réel du consommateur

Le consommateur Server isolé du nupkg local, sans ProjectReference, affiche un menu à trois niveaux filtré sur « audit ». Deux clics réels sur son bouton renomment « Janvier » en « Audit mensuel », puis « Archives ». Le navigateur vérifie les deux ancêtres visibles et ouverts au milieu, tous masqués avant et après, et le message de résultat correspondant. Le JSON et la capture ont été observés ; aucune erreur console ni violation CSP. Cela prouve le parcours public de renommage, séparément des tests de contrats internes.

Les journaux, TRX, Cobertura et captures sont conservés dans le dossier local de preuves du 2026-10-30. Commandes : suite Release avec `--coverlet --coverlet-output-format cobertura --coverlet-include "[OmniEurope.Blazor]*"`, puis `eng/Test-Coverage.ps1 -CoverageRoot <mesure>` et `eng/Test-Crap.ps1 -CoverageRoot <mesure>`. La première suite du lot 7 avait aussi vérifié l’assertion renforcée identifiant le voile conservé, mais comptait 10 979 réussites et une expiration de DiffViewer, avec la branche de OmniPassTracker encore manquante. Cette ancienne mesure reste conservée comme étape en échec. Voir les [limites de la recette finale](PLAN-009-lot-7-verifications.md).

## Clôture globale

Après les corrections de tests décrites au lot 7, dix suites complètes consécutives passent, 10 981 tests chacune. La dixième conserve son unique Cobertura et son TRX dans `artifacts/plan-009-diffviewer/verified-10`. `eng/Test-Coverage.ps1` accepte 27 680 lignes valides, 99,96 % des lignes et 99,61 % des branches, avec les seules admissions existantes ; les six fichiers initiaux et OmniPassTracker ne présentent plus de manque. CRAP accepte 3 835 méthodes avec ses treize exceptions inchangées. Les anciennes mesures rouges restent conservées et ne sont pas utilisées comme preuve de clôture.
