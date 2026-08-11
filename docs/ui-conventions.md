# Conventions d'interface

- Les actions hors grille associent un libellé localisé et une icône Omni décorative lorsque la convention d'action l'exige.
- Les cibles interactives auditées mesurent au moins 44 par 44 px, sans imposer cette taille au glyphe visible.
- Les noms accessibles, états, erreurs et pluriels proviennent des ressources ou d'un paramètre public explicite.
- Le clavier, le focus, les rôles ARIA et les annonces live font partie du contrat fonctionnel.
- Les styles sont statiques; aucun attribut `style`, gestionnaire JavaScript HTML ou `unsafe-eval` n'est autorisé.
- Les composants utilisent exclusivement les primitives OmniEurope. Aucune dépendance, copie, traduction ou adaptation de code, CSS, JavaScript, tests, commentaires ou assets Radzen n'est permise.

La preuve visuelle et interactive vient des sondes navigateur décrites dans `docs/testing.md`; le catalogue reste une illustration partielle et ne constitue pas une preuve exhaustive.
