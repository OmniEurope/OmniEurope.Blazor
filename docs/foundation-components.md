| `OmniFieldset` | Groupe de champs natif avec `legend` obligatoire et état désactivé ; `Collapsible` le replie avec l'élément natif `details`. |# Composants de fondation

Ce lot fournit 15 composants. Ils produisent du HTML sémantique, refusent les attributs `style` et les gestionnaires HTML sous forme de chaîne, et utilisent uniquement la feuille CSS statique de la bibliothèque.

## Capacités

| Besoin | Capacité OmniEurope |
|---|---|
| Titres et contenu textuel | `OmniText`, `OmniHeading` |
| Icônes et badges | `OmniIcon`, `OmniBadge` |
| Rangées, colonnes et grille | `OmniRow`, `OmniColumn`, `OmniGrid` |
| Coque applicative | `OmniLayout`, `OmniBody`, `OmniMain`, `OmniHeader` |
| Barre latérale et sa bascule | `OmniSidebar`, `OmniSidebarToggle` |
| Progression linéaire ou circulaire | `OmniProgressBar` avec `Shape` |
| Thème et apparence | `OmniThemeScope` (avec `Preset`, un thème de `OmniThemePresets`) et `OmniAppearanceToggle` |

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

Les tracés intégrés proviennent du poids `regular` de [Phosphor Icons](https://github.com/phosphor-icons/core) `2.1.1` (MIT), repris tels quels. Le paquet embarque un catalogue choisi : les tracés qu'il dessine lui-même et chaque icône que ses applications affichent, un tracé par valeur d'`OmniIconName`. Le catalogue complet n'est jamais distribué ; une application à qui il manque une icône l'ajoute au catalogue, par son nom.

Pour un tracé ponctuel, Phosphor ou non, le paramètre `Glyph` reste disponible.

```razor
@* Une icône du catalogue. *@
<OmniIcon Name="OmniIconName.Save" AriaLabel="Enregistrer" />

@* Un tracé ponctuel, ici une icône Phosphor absente du catalogue. *@
<OmniIcon Glyph="@Compass" AriaLabel="Boussole" />

@code {
    private static readonly OmniIconGlyph Compass = OmniIconGlyph.Phosphor(
        "M128,24A104,104,0,1,0,232,128,104.11,104.11,0,0,0,128,24Zm0,192a88,88,0,1,1,88-88A88.1,88.1,0,0,1,128,216Zm50.34-138.34a8,8,0,0,0-8.68-1.73l-56,24a8,8,0,0,0-4.2,4.2l-24,56a8,8,0,0,0,10.41,10.51l56-24a8,8,0,0,0,4.2-4.2l24-56A8,8,0,0,0,178.34,77.66ZM140,140l-33.42,14.32L120.9,120.9,154.32,106.6Z");
}
```

`OmniIconGlyph` n'accepte que de la donnée de tracé SVG et rejette tout le reste : un glyphe porte un contour, pas du balisage. `OmniIconGlyph.Phosphor` fixe la grille de 256 unités du jeu ; le constructeur accepte une autre grille carrée.

Mesure du surcoût des onze tracés Phosphor, publication WebAssembly identique avant et après : **+6656 octets bruts, +1392 octets une fois compressés en brotli**, soit +0,9 % de l'assembly livré. Passage du catalogue à 86 icônes, `OmniEurope.Blazor.dll` en Release avant et après (2026-09-13) : **+57 856 octets bruts, +11 318 octets en brotli** (niveau maximal), soit +5,9 % de l'assembly compressé. Une garde de test échoue si le nombre de tracés intégrés diffère de celui des valeurs d'`OmniIconName`, afin qu'un import massif du catalogue ne passe pas inaperçu.

## Thèmes

`OmniThemePresets.All` livre vingt thèmes : Ardoise, Galet, Néon, Papier, Terracotta, Rétro, Forêt, Lavande, Océan, Mono, Bonbon, Gravure, Affiche, Cahier, Nénuphar, Velours, Sable, Béton, Givre et Octet. Chacun a une moitié claire et une moitié sombre, et change la forme autant que les couleurs : arrondis, épaisseur des bordures, ombres, police (piles système seulement), allure des boutons, des cartes et des titres.

Les couleurs de chaque moitié sont dérivées puis déplacées jusqu'aux ratios WCAG : 4,5 pour tout texte que la feuille écrit (texte courant et discret, accent fort, sévérités sur la page et sur leur teinte pâle, texte posé sur un aplat, chaque aplat ayant sa propre couleur de texte `--omni-color-on-*`), 3 pour l'accent contre la page. Une bordure teintée ne descend jamais sous la bordure dérivée, et une ombre qui doit rester visible en sombre porte un filet ou vient d'une couleur du mode : les tests de `ShowcaseThemeTests` tiennent ces lignes pour les quarante moitiés.

```razor
<OmniThemeScope Appearance="OmniAppearance.System" Preset="@OmniThemePresets.All[0]">
    ...
</OmniThemeScope>
```

Le thème ne repeint que sa portée. Les valeurs sont des surcharges des variables de la feuille livrée, posées par le CSSOM ; les variables de forme (`--omni-button-*`, `--omni-card-*`, `--omni-heading-*`, `--omni-border-width`) valent par défaut le rendu livré, si bien qu'une application peut aussi les redéfinir elle-même.

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
