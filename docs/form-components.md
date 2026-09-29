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
- `OmniCheckBox` et `OmniSwitch` lient un `bool` ou un `bool?` par `@bind-Value` (paramètre de type `TValue` déduit de la liaison ; tout autre type lève `InvalidOperationException`). Liés à un `bool`, la case est une case native et l'interrupteur a deux états. Liés à un `bool?`, ils ajoutent l'état non défini : la case devient un bouton `role="checkbox"` (`aria-checked="mixed"`), l'interrupteur place son curseur au milieu et se décrit par `IndeterminateDescription` ; un clic parcourt nul, vrai, faux, et `AllowIndeterminate="false"` interdit le retour à nul. `ChildContent` donne le texte du contrôle : liée à un `bool`, la case est alors enveloppée avec son texte d'un `label.omni-checkbox-label`, qui porte `Class` (la case garde sa classe et l'état de validation), et le milieu de la case tombe sur le milieu des lettres : le texte est rogné à la hauteur des capitales et à la ligne de base (`text-box: trim-both cap alphabetic`), la ligne garde sa hauteur. `TextFirst` (case et interrupteur, avec un texte) place le texte avant le contrôle, sans changer l'ordre de lecture ni de focus.
- `OmniLabel` et `OmniFormField` associent libellé, description, contrôle et erreur sans masquer la sémantique HTML.
- `OmniTemplateForm<TModel>` accepte exactement un modèle ou un `EditContext` existant et place le focus sur le premier contrôle invalide avec le module statique `omniInterop.js`. Il avertit aussi avant de quitter une page dont un champ a changé (`GuardUnsavedChanges`, actif par défaut) : une navigation dans l'application attend la réponse au dialogue de confirmation du paquet (« Quitter sans enregistrer » en `Danger`, « Rester sur la page »), fermer ou recharger l'onglet lève la question du navigateur (`beforeunload`). Un envoi valide vaut enregistrement : la question cesse jusqu'au prochain changement, et une navigation lancée par le gestionnaire d'envoi n'est jamais retenue par son propre formulaire. `GuardUnsavedChanges="false"` le retire (une page de connexion, un filtre).
- `OmniUnsavedChangesGuard` porte seul ce garde (`HasChanges`, `Title`, `Message`, `LeaveText`, `StayText`) pour des modifications qu'aucun formulaire ne voit : une liste éditée sur place, un dessin. Il repose sur `NavigationLock` ; hors d'un `OmniComponentsHost`, la question interne est celle du navigateur (`confirm`).
- `OmniRequiredValidator<TValue>` s'appuie sur un socle `ValidationMessageStore`, prend en charge la validation différée annulable (`ValidationDelayMilliseconds`) et annonce son message avec `role="alert"`. Les règles de longueur, d'adresse électronique et de comparaison s'expriment en attributs DataAnnotations (`StringLength`, `EmailAddress`, `Compare`) lus par `OmniDataAnnotationsValidator`.
- `OmniValidatorBase<TValue>` est la base publique des validateurs de champ du paquet (celle d'`OmniRequiredValidator<TValue>`) : magasin de messages, validation différée annulable et annonce du message, pour écrire un validateur propre à l'hôte.
- `OmniDynamicForm` dessine un formulaire depuis un schéma : un champ par `OmniDynamicField`, chacun édité par le contrôle de son genre (texte, plusieurs lignes, nombre, oui ou non, choix), avec son libellé et sa marque d'obligation, son aide et son message de validation. Les valeurs entrent et sortent en un seul dictionnaire de chaînes, écrites comme un formulaire les enverrait (`true`/`false`, nombre au point décimal, valeur de l'option) ; un champ vide n'a pas d'entrée, et un oui ou non obligatoire sans défaut reste sans réponse tant qu'on n'y a pas touché. Dans un `EditForm`, sa validation suit celle du formulaire ; hors d'un formulaire, il tient son propre contexte d'édition. Des messages trouvés par l'hôte (un refus du serveur) s'ajoutent aux siens.
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
