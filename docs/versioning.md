# Versionnement, dépréciation et ruptures

Le paquet suit SemVer.

- Avant `1.0`, une rupture est annoncée dans `CHANGELOG.md` et limitée à une version mineure.
- À partir de `1.0`, une suppression ou une modification incompatible exige une version majeure.
- Une API dépréciée reçoit `[Obsolete]`, une alternative documentée et reste disponible pendant au moins une version mineure complète.
- Les correctifs ne modifient pas la sémantique d'une liaison, d'un événement ou d'une valeur nullable.
- Une baseline partielle des composants publics et le contenu du paquet sont comparés en CI. Les limites de l'extraction API sont documentées dans [public-api-conventions.md](public-api-conventions.md).

Les guides de migration décrivent la capacité remplacée et les différences volontaires. Aucune promesse de compatibilité binaire avec Radzen n'est faite.
