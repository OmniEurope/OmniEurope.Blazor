using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

public abstract class OmniComponentBase : ComponentBase
{
    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    [Inject]
    private IServiceProvider PresetServices { get; set; } = default!;

    /// <summary>
    /// The preset (named parameter set registered by the host) this component takes: its type's default
    /// when unset, none with <c>"none"</c>. Parameters written explicitly win over the preset.
    /// </summary>
    [Parameter]
    public string? PresetName { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<string?>(nameof(PresetName), out var presetName);
        if (PresetServices.GetService(typeof(OmniPresetRegistry)) is OmniPresetRegistry presets)
            presets.Apply(this, parameters, presetName);
        else if (presetName is not null)
            OmniPresetRegistry.ThrowUnregistered(GetType(), presetName);
        return base.SetParametersAsync(parameters);
    }

    protected override void OnParametersSet()
    {
        CspAttributeGuard.EnsureSafe(AdditionalAttributes);
    }

    protected string Css(params string?[] values) => CssClassBuilder.Combine(values.Append(Class));

    protected string Localize(string name, params object[] arguments) =>
        (arguments.Length == 0 ? StringLocalizer[name] : StringLocalizer[name, arguments]).Value;
}

