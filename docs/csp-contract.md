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

Cette politique est un objectif de test de la bibliothèque, pas un en-tête universel prêt à copier pour toutes les applications. Un hôte WebAssembly, y compris un hôte Interactive Auto qui télécharge le runtime client, doit ajouter la source CSP ciblée `'wasm-unsafe-eval'` à `script-src`. Elle autorise la compilation WebAssembly sans autoriser la source plus large et interdite `'unsafe-eval'`.

## État de la vérification

La CI scanne les sources Razor, C#, JavaScript et HTML pour les styles inline, les balises de style créées à l'exécution, les gestionnaires HTML `on*=`, les URI `javascript:`, les ressources statiques distantes, les imports distants et les évaluations JavaScript dynamiques. Des fixtures prouvent que les constructions Razor sûres restent acceptées et que les formes dangereuses sont rejetées. Le scanner reste une défense statique et non une preuve d'exécution.

La sonde WebAssembly publie un manifeste `_headers` qui impose notamment `frame-ancestors 'none'`; cette directive a été retirée de la balise `meta`, où les navigateurs l'ignorent. Chaque hébergeur statique doit appliquer ce manifeste ou le traduire vers sa configuration native. La CI vérifie sa présence dans l'artefact publié.

Le contrôle de `/csp-status` interroge un collecteur borné et n'expose que le compteur. Son état vide avant toute navigation interactive ne prouve toujours pas l'absence de violation à l'exécution. Une preuve complète exige un navigateur réel qui attend l'interactivité, exerce les composants et contrôle les rapports CSP ainsi que la console.

