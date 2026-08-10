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

La DataGrid locale ne rend que la page demandée, mais matérialise la projection filtrée et triée avant pagination. Elle ne fournit pas de virtualisation pilotée par le viewport. `OmniDataList` peut employer `Virtualize`, tandis que DropDown et les autres sélecteurs matérialisent encore toutes leurs options filtrées. Les chargements distants annulent la requête précédente et ignorent ses résultats obsolètes.

Les tests automatisés utilisent des plafonds suffisamment larges pour détecter une explosion d'ordre de grandeur. Ils reposent toutefois sur des mesures murales et d'allocation sans échauffement ni isolation ; ils ne constituent ni un benchmark stable, ni une preuve de virtualisation réelle.
