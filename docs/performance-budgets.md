# Budgets de performance

Les budgets sont des plafonds de régression, pas des objectifs à atteindre.

| Artefact ou scénario | Budget Release |
|---|---:|
| Feuille CSS statique | 96 Kio |
| Assembly principal | 1,5 Mio |
| Paquet NuGet `.nupkg` | 2 Mio |
| Rendu de 1 000 boutons bUnit | 5 s et 160 Mio alloués |
| DataGrid local, source de 10 000 lignes, page de 50 | 3 s et 160 Mio alloués |
| SVG de 1 000 points | 3 s et 80 Mio alloués |

La DataGrid locale ne rend que la page demandée, mais matérialise la projection filtrée et triée avant pagination. En mode `AllowVirtualization`, seule la fenêtre visible est rendue et les décalages verticaux sont tenus dans un arbre de Fenwick, donc en coût logarithmique par mesure ; la mémoire reste proportionnelle au nombre total de lignes, à raison de deux tableaux de hauteurs. Le chargement distant virtualisé borne son cache à vingt-quatre blocs. `OmniDataList` peut employer `Virtualize`, tandis que DropDown et les autres sélecteurs matérialisent encore toutes leurs options filtrées. Les chargements distants annulent la requête précédente et ignorent ses résultats obsolètes.

Les tests automatisés utilisent des plafonds suffisamment larges pour détecter une explosion d'ordre de grandeur. Chaque scénario effectue un échauffement, puis retient la médiane de cinq mesures. La collection xUnit est sérialisée et les allocations viennent du compteur global du processus, pas du seul thread courant. Cette gate reste volontairement un détecteur grossier de régression; toute décision d'optimisation exige un benchmark dédié sur machine contrôlée, avec version du SDK, profil matériel et résultats bruts archivés. Elle ne constitue pas une preuve de virtualisation réelle.
