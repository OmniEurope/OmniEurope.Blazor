# Contrat CSP

## Garantie de la bibliothèque

Le code livré par `OmniEurope.Blazor` ne doit pas :

- émettre d'attribut HTML `style` ;
- injecter de balise `<style>` à l'exécution ;
- émettre de gestionnaire d'événement HTML sous forme de chaîne (`onclick="…"`, etc.) ;
- utiliser `eval`, `new Function` ou une API équivalente ;
- charger automatiquement une ressource depuis une origine distante.

Les variations visuelles dynamiques passent par un ensemble fini de classes CSS, d'attributs `data-*` et d'états ARIA. La feuille `_content/OmniEurope.Blazor/omnieurope.blazor.css` est une ressource statique que l'application peut autoriser via `'self'`.

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

