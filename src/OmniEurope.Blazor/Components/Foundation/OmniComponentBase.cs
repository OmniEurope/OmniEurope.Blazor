using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public abstract class OmniComponentBase : ComponentBase
{
    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnParametersSet()
    {
        CspAttributeGuard.EnsureSafe(AdditionalAttributes);
    }

    protected string Css(params string?[] values) => CssClassBuilder.Combine(values.Append(Class));

    protected string Localize(string name, params object[] arguments) => StringLocalizer[name, arguments].Value;
}

