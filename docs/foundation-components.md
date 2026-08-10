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
| `OmniIcon` | Jeu initial d'icônes SVG propres, décoratives par défaut ou nommées avec `AriaLabel`. |
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
