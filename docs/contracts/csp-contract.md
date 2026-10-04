# Contrat CSP

## Garantie de la bibliothèque

Le code livré par `OmniEurope.Blazor` ne doit pas :

- émettre d'attribut HTML `style` ;
- injecter de balise `<style>` à l'exécution ;
- émettre de gestionnaire d'événement HTML sous forme de chaîne (`onclick="…"`, etc.) ;
- utiliser `eval`, `new Function` ou une API équivalente ;
- charger automatiquement une ressource depuis une origine distante.

Les variations visuelles dynamiques passent par un ensemble fini de classes CSS, d'attributs `data-*`, d'états ARIA et, pour le SVG, d'attributs géométriques ou de présentation autorisés ; aucun style inline n'est généré. Les valeurs que seul le navigateur peut connaître passent par le CSSOM (`element.style`), jamais par un attribut écrit dans le balisage : propriétés personnalisées (`setProperty('--omni-…')`), propriétés standard (`style.left`, `style.top`, `style.inlineSize`, `style.insetInlineStart`…) et, pour une sonde de mesure, `style.cssText`. Les modules concernés :

- `omni-grid.js` : hauteur du tableau virtualisé et des lignes d'espacement, largeur et décalage des colonnes gelées, largeurs de colonne et sonde de mesure de l'ajustement au contenu (`cssText`), position des panneaux de filtre et de la liste de suggestions du filtre `Combo` (`--omni-popover-x`, `--omni-popover-y`, `--omni-popover-min`, `--omni-anchor-x`, `--omni-anchor-y`, `--omni-anchor-w`), espaceurs d'`OmniDataList` (`--omni-data-list-spacer`) ;
- `omni-dialog.js` : déplacement d'un dialogue glissé (`style.left`, `style.top` ; jamais `transform`, qui ferait du dialogue l'origine des surfaces fixes qu'il contient) et, avec `FreezeScale`, dimensions calculées figées en pixels sur le dialogue et ses descendants (marges, espacements, bordures, par `setProperty` sur des propriétés standard), la hauteur figée étant rendue (`removeProperty`) aux conteneurs d'une ligne qui arrive ou repart ; largeur libre d'`OmniDialog.Width` (`--omni-dialog-width`, valeur vérifiée côté serveur : un nombre et une unité seulement) et marque `data-omni-dialog-width-ready` qui met fin à l'attente transparente ;
- `omni-tooltip.js` : position d'une bulle suivie ou recadrée (`--omni-tooltip-x`, `--omni-tooltip-y`…) ;
- `omni-theme.js` : jetons d'un préréglage appliqués à une portée (propriétés personnalisées, posées et retirées), attribut `data-omni-theme-resolved` en mode système ;
- `omni-boot.js` : avant Blazor, jetons de l'instantané de la portée (`OmniThemeScope.SnapshotKey`) posés sur la racine et sur la portée, puis retirés à la passation ;
- `omni-code-editor.js` : hauteur de l'éditeur (`--omni-code-editor-height`) ;
- `omni-html-editor.js` : nombre de lignes de la surface (`--omni-html-editor-rows`) ;
- `omni-focus.js` : position de tout menu du moteur commun, sous son déclencheur ou au pointeur (`--omni-menu-x`, `--omni-menu-y`), décalage d'un popover recadré dans la fenêtre (`--omni-popover-shift-x`) et position du panneau d'un sélecteur de date ou d'heure sous son champ (`--omni-picker-x`, `--omni-picker-y`, posées sur le panneau, qui quitte la page avec elles) ;
- `omni-black-hole.js` : couleur d'un jeton du thème lue par une sonde masquée (`style.color`), retirée aussitôt lue ; le fond de Trou noir est dessiné par WebGL sur le `canvas` d'`OmniThemeScope`, sans style.

`omni-page-header.js` (défilement du titre, repli des badges et actions de l'en-tête de page quand la ligne 1 déborde, à toute largeur) mesure et bascule des attributs seulement ; il n'écrit aucun style.

`omni-mindmap.js` ne passe même pas par le CSSOM : pendant un geste, il n'écrit que des attributs SVG (`transform`, `d`, géométrie du lasso) et des classes. Les écritures du CSSOM ne sont pas soumises à `style-src` et aucun attribut `style` ni aucune balise `<style>` n'est produit ; le scan CSP couvre ces fichiers. La feuille `_content/OmniEurope.Blazor/omnieurope.blazor.css` est une ressource statique que l'application peut autoriser via `'self'`.

## Exception : OmniCodeEditor et OmniDiffViewer avec Monaco

`OmniCodeEditor` et `OmniDiffViewer`, qui partage son Monaco et son script, sont les seuls composants qui puissent sortir de cette garantie, et seulement si l'hôte le demande : son moteur `Monaco` (par défaut) charge l'éditeur Monaco que l'hôte sert depuis sa propre origine (`MonacoPath`), et Monaco écrit des éléments `style` à l'exécution. La page doit alors autoriser `style-src 'unsafe-inline'` ; `script-src 'self'` et `worker-src 'self'` suffisent, les workers étant lancés depuis le fichier servi plutôt que par une URL `blob:`. Le paquet n'embarque pas Monaco et refuse une adresse sur une autre origine. Sous la politique stricte, `Engine="OmniCodeEditorEngine.PlainText"` garde une zone de texte (ou, pour la comparaison, deux volets) sans Monaco, et c'est aussi le repli quand Monaco ne se charge pas. Les fichiers du paquet, `omni-code-editor.js` compris, restent couverts par le scan CSP sans exclusion. Détails dans [editor-components.md](../editor-components.md).

## Responsabilité de l'application hôte

L'hôte doit charger la feuille statique et, s'il utilise l'apparence enregistrée ou l'écran de démarrage, `_content/OmniEurope.Blazor/omni-boot.js` en script classique dans le `<head>`, avant Blazor (fichier servi depuis l'origine, aucun script en ligne, donc compatible `script-src 'self'`). Il doit aussi définir ses propres en-têtes CSP et éviter de transmettre un attribut `style` ou un gestionnaire HTML inline. Les composants de base rejettent ces attributs lorsqu'ils arrivent par le dictionnaire d'attributs supplémentaires ; la même garde (`CspAttributeGuard`) refuse aussi `class` et `id` en toute casse, qui passent par les paramètres `Class` et `Id`.

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

