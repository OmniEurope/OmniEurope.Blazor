# Versionnement, dépréciation et ruptures

Le paquet suit SemVer.

- Avant `1.0`, une rupture est annoncée dans `CHANGELOG.md` et limitée à une version mineure.
- À partir de `1.0`, une suppression ou une modification incompatible exige une version majeure.
- Une API dépréciée reçoit `[Obsolete]`, une alternative documentée et reste disponible pendant au moins une version mineure complète.
- Les correctifs ne modifient pas la sémantique d'une liaison, d'un événement ou d'une valeur nullable.
- Une baseline partielle des composants publics et le contenu du paquet sont comparés en CI. Les limites de l'extraction API sont documentées dans [public-api-conventions.md](public-api-conventions.md).

Aucune promesse de compatibilité binaire avec une autre bibliothèque de composants n'est faite.

## Exception motivée : `1.0.1`

`1.0.1` porte une rupture dans un correctif : l'apparence livrée change (le thème Défaut avec la palette Défaut remplace l'aspect de `1.0.0` pour toute page, qu'elle pose un `OmniThemeScope` ou non), et quelques comportements par défaut changent avec elle (voir `CHANGELOG.md`). La règle ci-dessus demanderait une version `2.0.0`. Le passage de vingt thèmes à dix thèmes et dix palettes ne rompt, lui, rien de publié : le catalogue de vingt thèmes est arrivé après `1.0.0` et n'a jamais été livré.

L'exception est admise pour un seul motif : les deux versions publiées auparavant, `0.1.0-alpha.1` et `1.0.0`, sont délistées de NuGet.org, si bien que `1.0.1` devient la seule version installable et donc la version par défaut. Aucun consommateur connu ne dépend du paquet publié : les applications de l'organisation référencent la bibliothèque par `ProjectReference`.

Délister n'est pas supprimer : un projet qui épingle `1.0.0` continue de la restaurer, avec l'ancien aspect. Le numéro `1.0.1` ne se justifie qu'une fois les deux versions antérieures délistées ; tant que ce n'est pas fait, la règle générale s'applique. L'exception ne crée pas de précédent : toute autre rupture suit la règle de la version majeure.
