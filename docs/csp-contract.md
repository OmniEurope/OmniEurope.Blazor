# Contrat CSP

## Garantie de la bibliothèque

Le code livré par `OmniEurope.Blazor` ne doit pas :

- émettre d'attribut HTML `style` ;
- injecter de balise `<style>` à l'exécution ;
- émettre de gestionnaire d'événement HTML sous forme de chaîne (`onclick="…"`, etc.) ;
- utiliser `eval`, `new Function` ou une API équivalente ;
- charger automatiquement une ressource depuis une origine distante.

Les variations visuelles dynamiques passent par un ensemble fini de classes CSS, d'attributs `data-*`, d'états ARIA et, pour le SVG, d'attributs géométriques ou de présentation autorisés ; aucun style inline n'est généré. La feuille `_content/OmniEurope.Blazor/omnieurope.blazor.css` est une ressource statique que l'application peut autoriser via `'self'`.

## Responsabilité de l'application hôte

L'hôte doit charger la feuille statique, définir ses propres en-têtes CSP et éviter de transmettre un attribut `style` ou un gestionnaire HTML inline. Les composants de base rejettent ces attributs lorsqu'ils arrivent par le dictionnaire d'attributs supplémentaires.

Politique de validation indicative :

```text
default-src 'self';
script-src 'self';
style-src 'self';
img-src 'self' data:;
font-src 'self';
object-src 'none';
base-uri 'self';
frame-ancestors 'none'
```

Cette politique est un objectif de test de la bibliothèque, pas un en-tête universel prêt à copier pour toutes les applications.

## État de la vérification

La CI scanne actuellement les sources Razor, C# et JavaScript pour les attributs `style`, les balises `style` créées à l'exécution et les appels `eval`/`new Function`, puis vérifie par HTTP l'en-tête et les assets du catalogue. Ce scanner ne couvre pas encore les gestionnaires HTML en chaîne ni tous les chargements distants. Le contrôle de `/csp-status` interroge le collecteur avant toute navigation interactive : son état vide ne prouve pas l'absence de violation à l'exécution. Une preuve complète exige encore un navigateur réel qui attend l'interactivité, exerce les composants et contrôle les rapports CSP ainsi que la console.

