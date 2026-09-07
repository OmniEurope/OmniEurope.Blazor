using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public abstract class OmniInputBase<TValue> : InputBase<TValue>
{
    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

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

    protected string Localize(string name, params object[] arguments) =>
        (arguments.Length == 0 ? StringLocalizer[name] : StringLocalizer[name, arguments]).Value;
}
