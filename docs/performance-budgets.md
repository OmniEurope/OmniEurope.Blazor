# Budgets de performance

Les budgets sont des plafonds de régression, pas des objectifs à atteindre.

La feuille CSS n'a pas de budget de taille, retiré à la demande du propriétaire le 2026-09-18. `eng/Test-Budgets.ps1` vérifie seulement que la copie livrée est la copie minifiée produite par le build Release (`eng/OmniEurope.Stylesheet.targets`) : avec `-PackagePath`, il la lit dans le paquet lui-même et échoue si elle contient encore un commentaire.

Le budget de l'assembly est passé de 1,5 à 2 Mio le 2026-09-18, sur décision du propriétaire : la DLL atteignait 1 557 504 octets (99 %) après l'arrivée des composants d'Aetheus, du catalogue de thèmes et palettes et des sélecteurs de date et d'heure, et les composants de la barre d'application restaient à ajouter.

| Artefact ou scénario | Budget Release |
|---|---:|
| Assembly principal | 2 Mio |
| Paquet NuGet `.nupkg` | 2 Mio |
| Rendu de 1 000 boutons bUnit | 5 s et 160 Mio alloués |
| DataGrid local, source de 10 000 lignes, page de 50 | 3 s et 160 Mio alloués |
| SVG de 1 000 points | 3 s et 80 Mio alloués |

La DataGrid locale ne rend que la page demandée, mais matérialise la projection filtrée et triée avant pagination. En mode `AllowVirtualization`, seule la fenêtre visible est rendue et les décalages verticaux sont tenus dans un arbre de Fenwick, donc en coût logarithmique par mesure ; la mémoire reste proportionnelle au nombre total de lignes, à raison de deux tableaux de hauteurs. Le chargement distant virtualisé borne son cache à vingt-quatre blocs. `OmniDataList` avec `Virtualize` ne rend de même que les éléments proches de la zone visible, avec le même index de décalages, tandis que DropDown et les autres sélecteurs matérialisent encore toutes leurs options filtrées. Les chargements distants annulent la requête précédente et ignorent ses résultats obsolètes.

Les tests automatisés utilisent des plafonds suffisamment larges pour détecter une explosion d'ordre de grandeur. Chaque scénario effectue un échauffement, puis retient la médiane de cinq mesures. La collection xUnit est sérialisée et les allocations viennent du compteur global du processus, pas du seul thread courant. Cette gate reste volontairement un détecteur grossier de régression; toute décision d'optimisation exige un benchmark dédié sur machine contrôlée, avec version du SDK, profil matériel et résultats bruts archivés. Elle ne constitue pas une preuve de virtualisation réelle.
