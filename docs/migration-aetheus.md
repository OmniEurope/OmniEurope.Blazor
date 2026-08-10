# Migration progressive d'Aetheus

Aetheus est le premier consommateur cible parce qu'il concentre le plus grand volume Radzen observé. La migration reste incrémentale : Radzen et OmniEurope.Blazor peuvent coexister pendant la transition.

## Ordre recommandé

1. Primitives sans état : boutons, cartes, badges, piles et séparateurs.
2. Retours utilisateur : alertes, notifications, indicateurs de progression.
3. Navigation et superpositions : menus, onglets, dialogues et info-bulles.
4. Formulaires : champs, validation, listes et sélecteurs.
5. Données complexes : grilles, arbres, graphiques et éditeur riche.

Chaque lot doit retirer les imports et usages Radzen correspondants, exécuter les tests Aetheus, puis vérifier l'application avec sa CSP stricte avant de passer au lot suivant.

