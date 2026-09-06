# Composants de fondation

Ce lot fournit 15 composants clean-room supplémentaires. Ils produisent du HTML sémantique, refusent les attributs `style` et les gestionnaires HTML sous forme de chaîne, et utilisent uniquement la feuille CSS statique de la bibliothèque.

## Correspondances de capacité

| Usage observé | Capacité OmniEurope |
|---|---|
| `RadzenText`, titres et contenu | `OmniText`, `OmniHeading` |
| `RadzenIcon`, `RadzenBadge` | `OmniIcon`, `OmniBadge` |
| `RadzenRow`, `RadzenColumn` | `OmniRow`, `OmniColumn`, `OmniGrid` |
| `RadzenLayout`, `RadzenBody`, `RadzenHeader` | `OmniLayout`, `OmniBody`, `OmniMain`, `OmniHeader` |
| `RadzenSidebar`, `RadzenSidebarToggle` | `OmniSidebar`, `OmniSidebarToggle` |
| `RadzenProgressBar`, `RadzenProgressBarCircular` | `OmniProgressBar` avec `Shape` |
| `RadzenTheme` | `OmniThemeScope` et `OmniAppearanceToggle` |

Ces correspondances décrivent un résultat, pas une compatibilité paramètre par paramètre.

| Composant | Rôle |
| --- | --- |
| `OmniText` | Texte rendu en `span`, `p`, `strong`, `em` ou `small`, avec tons et troncature statiques. |
| `OmniHeading` | Titres `h1` à `h6` déterminés par `OmniHeadingLevel`. |
| `OmniIcon` | Tracés Phosphor `regular` intégrés pour les usages du paquet, décoratifs par défaut ou nommés avec `AriaLabel` ; `Glyph` accepte n'importe quel autre tracé sans alourdir le paquet. |
| `OmniBadge` | Étiquette courte avec variantes neutre, accent, succès, avertissement et danger. |
| `OmniLink` | Lien natif ; un nouvel onglet ajoute automatiquement `noopener noreferrer`. |
| `OmniImage` | Image responsive avec texte alternatif, chargement différé et dimensions natives optionnelles. |
| `OmniSkeleton` | État de chargement décoratif ou région `status` nommée, avec une à dix lignes. |
| `OmniRow` | Rangée flex avec espacement, alignement, justification et retour à la ligne typés. |
| `OmniColumn` | Colonne sur douze unités avec variantes responsive `SmallSpan`, `MediumSpan` et `LargeSpan`. |
| `OmniGrid` | Grille CSS de une à douze colonnes avec espacement typé. |
| `OmniLayout` | Conteneur de page pleine largeur, large ou centré sur le contenu. |
| `OmniMain` | Landmark `main`, ciblable par un lien d'évitement grâce à `FocusTarget`. |
| `OmniHeader` | Landmark `header`, avec position collante optionnelle définie dans la feuille statique. |
| `OmniFieldset` | Groupe de champs natif avec `legend` obligatoire et état désactivé. |
| `OmniProgressBar` | Progression linéaire ou circulaire, déterminée ou indéterminée, avec valeurs ARIA. |

## Icônes

Les tracés intégrés proviennent du poids `regular` de [Phosphor Icons](https://github.com/phosphor-icons/core) `2.0.8` (MIT), repris tels quels. Le paquet n'embarque que les onze tracés qu'il dessine lui-même : le catalogue complet n'est jamais distribué.

Pour toute autre icône, Phosphor ou non, passez le tracé au paramètre `Glyph`. Il ne coûte rien au paquet puisque la donnée vit chez le consommateur.

```razor
@* Un des tracés intégrés. *@
<OmniIcon Name="OmniIconName.Filter" AriaLabel="Filtrer" />

@* N'importe quel autre tracé, ici une icône Phosphor absente du jeu intégré. *@
<OmniIcon Glyph="@Download" AriaLabel="Télécharger" />

@code {
    private static readonly OmniIconGlyph Download = OmniIconGlyph.Phosphor(
        "M224,152v56a16,16,0,0,1-16,16H48a16,16,0,0,1-16-16V152a8,8,0,0,1,16,0v56H208V152a8,8,0,0,1,16,0Zm-101.66,5.66a8,8,0,0,0,11.32,0l40-40a8,8,0,0,0-11.32-11.32L136,132.69V40a8,8,0,0,0-16,0v92.69L93.66,106.34a8,8,0,0,0-11.32,11.32Z");
}
```

`OmniIconGlyph` n'accepte que de la donnée de tracé SVG et rejette tout le reste : un glyphe porte un contour, pas du balisage. `OmniIconGlyph.Phosphor` fixe la grille de 256 unités du jeu ; le constructeur accepte une autre grille carrée.

Mesure du surcoût des onze tracés Phosphor, publication WebAssembly identique avant et après : **+6656 octets bruts, +1392 octets une fois compressés en brotli**, soit +0,9 % de l'assembly livré. Une garde de test échoue si le nombre de tracés intégrés dépasse celui des valeurs d'`OmniIconName`, afin qu'un import massif du catalogue ne passe pas inaperçu.

## Exemple

```razor
<OmniMain Id="content" AriaLabelledBy="page-title">
    <OmniHeading Id="page-title" Level="OmniHeadingLevel.H1">
        Importation
    </OmniHeading>

    <OmniProgressBar Label="Importation des données"
                     Value="42"
                     ShowValue="true" />
</OmniMain>
```

## Validation

Les tests du lot vérifient le rendu des 15 composants, leur sémantique principale, les classes responsive, les états ARIA, les bornes numériques et l'absence de style inline. Le scanner CSP inspecte l'ensemble des sources Razor, C# et JavaScript de la bibliothèque.
