# Thème, navigation latérale et formulaires

Le deuxième lot complète les landmarks de page et installe un socle de formulaires fondé sur `EditContext`. Tous les états visuels proviennent de la feuille CSS statique ; aucun composant ne génère d'attribut `style`.

## Fondations complémentaires

- `OmniBody` structure la zone flexible située entre les landmarks de page.
- `OmniSidebar` rend un landmark `aside` contrôlé par les paramètres `Open` et `Position`.
- `OmniSidebarToggle` expose `aria-controls`, `aria-expanded` et `OpenChanged` ; menu ouvert, il garde les trois traits tant que la barre latérale tient à côté du contenu et ne devient une croix que sous le seuil mobile (39.99rem), où le menu couvre la page ; `OpenIcon` impose un glyphe à toutes les largeurs. Sans `Header` dans `OmniSidebar`, la barre d'application reste au-dessus du voile d'un menu superposé. Un menu superposé ouvert se ferme par un clic sur le voile, par le choix d'une entrée et par Échap, écouté sur tout le document tant qu'il est ouvert (`omni-focus.js`, `attachEscape`) puisque le focus reste souvent sur la poignée.
- `OmniThemeScope` applique les tokens `system`, `light` ou `dark` avec `data-omni-theme` ; `OmniAppearanceSettings` offre le choix de l'apparence dans un écran de réglages.

## Formulaires

- `OmniTextBox`, `OmniPassword`, `OmniTextArea` et `OmniNumeric<TValue>` héritent de `OmniInputBase<TValue>` et participent à `EditContext`.
- `OmniNumeric<TValue>` gagne `Clamp` (désactivé par défaut) : une valeur validée hors de `Minimum` ou `Maximum` est ramenée à la borne la plus proche, et c'est la borne que reçoit `ValueChanged` et qu'affiche ensuite le champ. Les bornes se lisent comme le navigateur lit `min` et `max`, en culture invariante ; une borne absente ou illisible pour `TValue` est ignorée. Valable pour `int`, `long`, `decimal`, `double` et leurs formes nullables ; un champ vidé reste `null`. Sans `Clamp`, la valeur est prise telle quelle, comme avant.
- `OmniTextBox` rend le type demandé par `Type` (`Text`, `Email`, `Tel`, `Url` ou `Search`), ce qui choisit le clavier mobile et le remplissage automatique du navigateur. `Icon` (facultatif) pose une icône décorative au début du champ, en couleur atténuée et transparente au clic, et décale le texte après elle : la loupe d'un champ de recherche (`<Icon><OmniIcon Name="OmniIconName.Search" /></Icon>`). Le champ garde son propre nom accessible ; sans `Icon`, il est rendu seul, comme avant. `DebounceMilliseconds` (0 par défaut) retarde la mise à jour de la valeur jusqu'à ce délai sans frappe : un champ de recherche qui recharge ses données ne le fait qu'une fois la frappe finie.
- `OmniCheckBox` et `OmniSwitch` lient un `bool` ou un `bool?` par `@bind-Value` (paramètre de type `TValue` déduit de la liaison ; tout autre type lève `InvalidOperationException`). Liés à un `bool`, la case est une case native et l'interrupteur a deux états. Liés à un `bool?`, ils ajoutent l'état non défini : la case devient un bouton `role="checkbox"` (`aria-checked="mixed"`), l'interrupteur place son curseur au milieu et se décrit par `IndeterminateDescription` ; un clic parcourt nul, vrai, faux, et `AllowIndeterminate="false"` interdit le retour à nul. `ChildContent` donne le texte du contrôle : sur une case liée à un `bool`, un `label.omni-checkbox-label` enveloppe la case et son texte.
- `OmniLabel` et `OmniFormField` associent libellé, description, contrôle et erreur sans masquer la sémantique HTML.
- `OmniTemplateForm<TModel>` accepte exactement un modèle ou un `EditContext` existant et place le focus sur le premier contrôle invalide avec le module statique `omniInterop.js`.
- `OmniRequiredValidator<TValue>` s'appuie sur un socle `ValidationMessageStore`, prend en charge la validation différée annulable (`ValidationDelayMilliseconds`) et annonce son message avec `role="alert"`. Les règles de longueur, d'adresse électronique et de comparaison s'expriment en attributs DataAnnotations (`StringLength`, `EmailAddress`, `Compare`) lus par `OmniDataAnnotationsValidator`.
- `OmniDataAnnotationsValidator` valide le modèle avec ses attributs DataAnnotations, comme `DataAnnotationsValidator`, mais écrit les messages des attributs standard (`Required`, `StringLength`, `MaxLength`, `MinLength`, `Range`, `EmailAddress`, `Url`, `Compare`, `RegularExpression`) dans les cultures de la bibliothèque. Le message est choisi d'après le type d'attribut, jamais déduit du texte anglais. Un modèle partagé avec une API garde donc ses attributs tels quels. Le nom du champ vient de `Display` ou `DisplayName`, sinon du nom de la propriété. Le paramètre facultatif `Localizer` (ressources de l'application) traduit ces noms et les messages personnalisés écrits comme clés de ressource. Un attribut avec `ErrorMessageResourceType`, déjà localisé par DataAnnotations, passe tel quel ; les résultats d'`IValidatableObject` aussi.

## Exemple

```razor
<OmniTemplateForm Model="model" OnValidSubmit="SaveAsync">
    <OmniFormField For="name" Label="@NameLabel" Required="true">
        <OmniTextBox Id="name" @bind-Value="model.Name" />
        <OmniRequiredValidator TValue="string" For="@(() => model.Name)" />
    </OmniFormField>
</OmniTemplateForm>

@code {
    private readonly PersonModel model = new();
    private RenderFragment NameLabel => builder => builder.AddContent(0, "Nom");
}
```
