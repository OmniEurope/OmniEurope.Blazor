using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

public abstract class OmniComponentBase : ComponentBase
{
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
}

