using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public abstract class OmniInputBase<TValue> : InputBase<TValue>
{
    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Class { get; set; }

    protected string? AriaInvalid => EditContext is not null && EditContext.GetValidationMessages(FieldIdentifier).Any()
        ? "true"
        : null;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        CspAttributeGuard.EnsureSafe(AdditionalAttributes);
    }

    protected string InputCss(params string?[] values) =>
        CssClassBuilder.Combine(values.Append(CssClass).Append(Class));
}
