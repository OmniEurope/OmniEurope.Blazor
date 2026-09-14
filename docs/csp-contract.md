# Contrat CSP

## Garantie de la bibliothèque

Le code livré par `OmniEurope.Blazor` ne doit pas :

- émettre d'attribut HTML `style` ;
- injecter de balise `<style>` à l'exécution ;
- émettre de gestionnaire d'événement HTML sous forme de chaîne (`onclick="…"`, etc.) ;
- utiliser `eval`, `new Function` ou une API équivalente ;
- charger automatiquement une ressource depuis une origine distante.

Les variations visuelles dynamiques passent par un ensemble fini de classes CSS, d'attributs `data-*`, d'états ARIA et, pour le SVG, d'attributs géométriques ou de présentation autorisés ; aucun style inline n'est généré. Les longueurs que seul le navigateur peut connaître, hauteur du tableau virtualisé, hauteur des lignes d'espacement, largeur et décalage des colonnes gelées, sont posées par `omni-grid.js` en propriétés personnalisées CSS via `setProperty`. `omni-mindmap.js` ne passe même pas par le CSSOM : pendant un geste, il n'écrit que des attributs SVG (`transform`, `d`, géométrie du lasso) et des classes. Le CSSOM n'est pas soumis à `style-src` et aucun attribut `style` ni aucune balise `<style>` n'est produit ; le scan CSP couvre ces fichiers. La feuille `_content/OmniEurope.Blazor/omnieurope.blazor.css` est une ressource statique que l'application peut autoriser via `'self'`.

## Exception : OmniCodeEditor avec Monaco

`OmniCodeEditor` est le seul composant qui puisse sortir de cette garantie, et seulement si l'hôte le demande : son moteur `Monaco` (par défaut) charge l'éditeur Monaco que l'hôte sert depuis sa propre origine (`MonacoPath`), et Monaco écrit des éléments `style` à l'exécution. La page doit alors autoriser `style-src 'unsafe-inline'` ; `script-src 'self'` et `worker-src 'self'` suffisent, les workers étant lancés depuis le fichier servi plutôt que par une URL `blob:`. Le paquet n'embarque pas Monaco et refuse une adresse sur une autre origine. Sous la politique stricte, `Engine="OmniCodeEditorEngine.PlainText"` garde une zone de texte sans script, et c'est aussi le repli quand Monaco ne se charge pas. Les fichiers du paquet, `omni-code-editor.js` compris, restent couverts par le scan CSP sans exclusion. Détails dans [editor-components.md](editor-components.md).

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

