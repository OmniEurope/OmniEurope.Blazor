# Thème, navigation latérale et formulaires

Le deuxième lot complète les landmarks de page et installe un socle de formulaires fondé sur `EditContext`. Tous les états visuels proviennent de la feuille CSS statique ; aucun composant ne génère d'attribut `style`.

## Fondations complémentaires

- `OmniBody` structure la zone flexible située entre les landmarks de page.
- `OmniSidebar` rend un landmark `aside` contrôlé par les paramètres `Open` et `Position`.
- `OmniSidebarToggle` expose `aria-controls`, `aria-expanded` et `OpenChanged`.
- `OmniThemeScope` applique les tokens `system`, `light` ou `dark` avec `data-omni-theme`.
- `OmniAppearanceToggle` parcourt ces trois apparences par un événement contrôlé.

## Formulaires

- `OmniTextBox`, `OmniPassword`, `OmniTextArea` et `OmniNumeric<TValue>` héritent de `OmniInputBase<TValue>` et participent à `EditContext`.
- `OmniCheckBox` et `OmniSwitch` lient des valeurs booléennes ; `OmniNullableCheckBox` et `OmniNullableSwitch` ajoutent un cycle contrôlé pour l'état non défini.
- `OmniLabel` et `OmniFormField` associent libellé, description, contrôle et erreur sans masquer la sémantique HTML.
- `OmniTemplateForm<TModel>` accepte exactement un modèle ou un `EditContext` existant et place le focus sur le premier contrôle invalide avec le module statique `omniInterop.js`.
- `OmniRequiredValidator<TValue>`, `OmniLengthValidator`, `OmniEmailValidator` et `OmniCompareValidator<TValue>` partagent un socle `ValidationMessageStore`, prennent en charge la validation différée annulable et annoncent leur message avec `role="alert"`.

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
