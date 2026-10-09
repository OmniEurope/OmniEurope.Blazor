# Versionnement, dépréciation et ruptures

Le paquet suit SemVer.

- Avant `1.0`, une rupture est annoncée dans `CHANGELOG.md` et limitée à une version mineure.
- À partir de `1.0`, une suppression ou une modification incompatible exige une version majeure.
- Une API dépréciée reçoit `[Obsolete]`, une alternative documentée et reste disponible pendant au moins une version mineure complète.
- Les correctifs ne modifient pas la sémantique d'une liaison, d'un événement ou d'une valeur nullable.
- La baseline complète de l'API publique (`docs/public-api.txt`, vidage canonique de chaque type et membre public ou protégé de l'assembly compilé) et le contenu du paquet sont comparés en CI. Les limites de l'extraction API sont documentées dans [public-api-conventions.md](public-api-conventions.md).

Aucune promesse de compatibilité binaire avec une autre bibliothèque de composants n'est faite.

## Exception motivée : `1.0.1`

`1.0.1` porte une rupture dans un correctif : l'apparence livrée change (le thème Défaut avec la palette Défaut, aujourd'hui Essentiel, remplace l'aspect de `1.0.0` pour toute page, qu'elle pose un `OmniThemeScope` ou non), et quelques comportements par défaut changent avec elle (voir `CHANGELOG.md`). La règle ci-dessus demanderait une version `2.0.0`. Le passage de vingt thèmes à dix thèmes et dix palettes ne rompt, lui, rien de publié : le catalogue de vingt thèmes est arrivé après `1.0.0` et n'a jamais été livré.

L'exception est admise pour un seul motif : les deux versions publiées auparavant, `0.1.0-alpha.1` et `1.0.0`, sont délistées de NuGet.org, si bien que `1.0.1` devient la seule version installable et donc la version par défaut. Aucun consommateur connu ne dépend du paquet publié : les applications de l'organisation référencent la bibliothèque par `ProjectReference`.

Délister n'est pas supprimer : un projet qui épingle `1.0.0` continue de la restaurer, avec l'ancien aspect. Le numéro `1.0.1` ne se justifie qu'une fois les deux versions antérieures délistées ; tant que ce n'est pas fait, la règle générale s'applique. L'exception ne crée pas de précédent : toute autre rupture suit la règle de la version majeure.

## Rupture assumée : `1.1.0`

`1.1.0` porte une rupture dans une version mineure : `OmniDropDown` lève `InvalidOperationException` quand `ValueProperty` ou `TextProperty` désigne une propriété que l'élément ne possède pas, au lieu de se rabattre en silence sur l'élément. Décision du 2026-09-26 : aucune couche de compatibilité n'est maintenue pour l'ancien comportement. La rupture est listée sous « Breaking changes » dans `CHANGELOG.md`, avec la migration.

## Rupture assumée : `1.2.0`

`1.2.0` porte des ruptures dans une version mineure, sans passer à `2.0.0` : composants, paramètres, énumérations et types retirés, renommés ou fusionnés, valeurs par défaut changées, et un paramètre inconnu qui ne compile plus (`OE0001`) ni ne se rend (garde d'`OmniComponentBase`). Décision du propriétaire du 2026-09-29 (PLAN-007) : le propriétaire est le seul consommateur du paquet, ses applications suivent la bibliothèque, si bien qu'aucun alias `[Obsolete]` ni aucune couche de compatibilité n'est maintenu pour les anciens noms, par exception à la règle de dépréciation ci-dessus. Chaque rupture est listée sous « Breaking changes » dans `CHANGELOG.md`, avec sa migration.

## Rupture assumée : `1.3.0`

`1.3.0` porte des ruptures dans une version mineure : `OmniDataGridColumn.Filterable` devient `bool?` et suit la grille quand il n'est pas réglé, les textes du paquet qui suivent un nombre portent un bloc `plural` ICU (une lecture directe par `IStringLocalizer<AppStrings>` avec des arguments lève `FormatException`), et deux classes CSS changent (`OmniAppearanceWindow`, tableaux masqués d'`OmniChart`). Décision du propriétaire du 2026-09-30 (PLAN-009) : numéro `1.3.0` plutôt que `2.0.0`, pour le même motif qu'en `1.2.0` (le propriétaire est le seul consommateur du paquet). Chaque rupture est listée sous « Breaking changes » dans `CHANGELOG.md`, avec sa migration.

## Rupture assumée : `1.4.0`

`1.4.0` change une valeur par défaut dans une version mineure : sans `ExportFileName` (désormais `string?`, null par défaut au lieu de `"export"`), le fichier exporté prend le nom du titre ou de la légende de la grille, horodaté à la minute. Les autres changements visibles de cette version (pastille seule pour `OmniDialog.Intent`, menu `Push` superposé sur téléphone, fenêtre d'apparence qui rétablit son réglage d'ouverture quand on la ferme par la croix ou Échap) sont marqués **Aspect** ou **Comportement** dans le journal. La rupture n'a été consignée qu'après la publication : les notes de la release GitHub `1.4.0` la rangent encore parmi les ajouts (« Added »), et le message du commit de publication (`1ed6c26`) dit « No breaking change ».

## Rupture assumée : `1.5.0`

`1.5.0` change une valeur par défaut dans une version mineure : le filtre d'en-tête d'`OmniDataGrid` (`ShowHeaderFilterMenu`) est actif par défaut et la rangée de filtres en ligne n'est plus rendue que sur demande (`ShowHeaderFilterMenu="false"`). Décision du propriétaire du 2026-10-03 : numéro `1.5.0` plutôt que `2.0.0`, pour le même motif qu'en `1.2.0` (le propriétaire est le seul consommateur du paquet). La rupture est listée sous « Breaking changes » dans `CHANGELOG.md`, avec sa migration ; les autres changements de comportement par défaut, sans migration, sont sous « Changed » : « modifié » d'`OmniTemplateForm` comparé aux valeurs, boutons d'export en `Secondary` et désactivés tant que la grille n'a aucune ligne, infobulle du texte entier sur toute cellule coupée de toute grille, nom de l'utilisateur masqué dans le bouton d'`OmniAppMenu` sous 40rem, police d'`OmniAppearanceSettings` réglée dans la fenêtre seulement.

## Changements de comportement : `1.6.0`

`1.6.0` ne retire ni ne renomme rien. Elle change l'aspect de l'export d'`OmniDataGrid` (boutons en icône seule, libellé « Tout exporter » retiré, barre dans le cadre du tableau) et rend obsolètes `ExportFormats` et `ExportPosition` : toujours pris en charge, ils lèvent l'avertissement CS0618, qui devient une erreur dans un projet qui traite les avertissements en erreurs. Une barre affichée (`ShowHeaderBar`, `ShowFooterBar`) porte les boutons d'export à son début sauf `HeaderBarExport` ou `FooterBarExport` à `None` (décision du propriétaire du 2026-10-06). Un numéro `1.7.0` déclaré un temps sur `develop` avant sa publication y a été rabattu : la `1.6.0` porte l'ensemble. Chaque changement est sous « Changed » ou « Deprecated » dans `CHANGELOG.md`.

## Changements de comportement : `1.7.0`

`1.7.0` ne retire ni ne renomme rien. Elle change des valeurs par défaut visibles : la hauteur des contrôles dans chaque densité (28 px au lieu de 36 en `Comfortable`, la densité par défaut, 24 au lieu de 26 en `Compact`, 34 au lieu de 44 en `Spacious`), la largeur d'`OmniSidebar` ouvert (12,5 rem au lieu de 18), et la légende d'`OmniChart`, dont un clic masque désormais la série nommée (`OmniLegend.AllowToggle`, `true` par défaut, `false` rend l'ancienne légende). Un hôte qui veut l'ancien aspect pose les propriétés `--omni-control-height` et `--omni-sidebar-width`. Elle change aussi des comportements : les identifiants internes d'`OmniDataGrid`, d'`OmniPager` et d'`OmniSpreadsheet` sans `Id` prennent un préfixe propre à l'instance (un sélecteur d'hôte sur `#omni-grid-filter-…`, `#omni-pager-page-size` ou `#omni-spreadsheet-formula` ne les trouve plus : donner un `Id` au composant), les groupes radio de boutons ne sont plus qu'un arrêt de Tab parcouru aux flèches et la zone des notifications se pose en bas sur un écran étroit. Chaque changement est sous « Added », « Changed » ou « Fixed » dans `CHANGELOG.md`.
