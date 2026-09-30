using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The base of every component of the package that draws an element of its own: it carries the
/// <see cref="Id"/>, the <see cref="Class"/>, the host's additional attributes and the preset, refuses
/// what the strict CSP forbids, and resolves the package's localized texts.
/// </summary>
/// <remarks>
/// Rules every derived component follows: <see cref="Class"/> goes on the outermost element it draws,
/// <see cref="Id"/> on its focusable control (the outermost element when there is none), and the
/// additional attributes are written first, so an attribute the component owns always wins over one
/// of the same name passed by the host. A lowercase <c>class</c> or <c>id</c> passed as an attribute
/// is refused: <see cref="Class"/> and <see cref="Id"/> are the parameters.
/// </remarks>
public abstract class OmniComponentBase : ComponentBase
{
    [Inject]
    private IStringLocalizer<AppStrings> StringLocalizer { get; set; } = default!;

    [Inject]
    private IServiceProvider PresetServices { get; set; } = default!;

    /// <summary>
    /// The clock a time-aware component reads: the host's registered <see cref="TimeProvider"/>, the
    /// system clock when it registers none. Resolved on each read, so a test swaps it in the container.
    /// </summary>
    private protected TimeProvider Clock => PresetServices.GetService(typeof(TimeProvider)) as TimeProvider ?? TimeProvider.System;

    /// <summary>
    /// The preset (named parameter set registered by the host) this component takes: its type's default
    /// when unset, none with <c>"none"</c>. Parameters written explicitly win over the preset.
    /// </summary>
    [Parameter]
    public string? PresetName { get; set; }

    /// <summary>
    /// The id of the component's focusable control, or of its outermost element when it has none. Null,
    /// the default, writes no id (a component that needs one for its own wiring generates it).
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>CSS classes added to the component's outermost element, after its own.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Attributes the component has no parameter for (<c>data-*</c>, <c>aria-*</c>, <c>title</c>...),
    /// written on its element before the ones it owns, which win. Inline styles, inline event handlers,
    /// a lowercase <c>class</c> or <c>id</c> and a PascalCase name (a parameter that does not exist) are
    /// refused with an <see cref="InvalidOperationException"/>.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Applies the preset, then the parameters written explicitly.</summary>
    /// <param name="parameters">The parameters given by the renderer.</param>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        parameters.TryGetValue<string?>(nameof(PresetName), out var presetName);
        if (PresetServices.GetService(typeof(OmniPresetRegistry)) is OmniPresetRegistry presets)
            presets.Apply(this, parameters, presetName);
        else if (presetName is not null)
            OmniPresetRegistry.ThrowUnregistered(GetType(), presetName);
        return base.SetParametersAsync(parameters);
    }

    /// <summary>Refuses the additional attributes the CSP contract forbids. A derived component calls it first.</summary>
    protected override void OnParametersSet()
    {
        CspAttributeGuard.EnsureSafe(AdditionalAttributes, GetType());
    }

    /// <summary>The component's classes followed by <see cref="Class"/>, nulls and blanks left out.</summary>
    /// <param name="values">The component's own classes.</param>
    protected string Css(params string?[] values) => CssClassBuilder.Combine(values.Append(Class));

    /// <summary>A text of the package's resources in the current UI culture.</summary>
    /// <param name="name">The resource key.</param>
    /// <param name="arguments">The values of the key's <c>{n}</c> placeholders.</param>
    protected string Localize(string name, params object[] arguments) =>
        PluralMessage.Localize(StringLocalizer, name, arguments);

    /// <summary>
    /// A text the host may override: <paramref name="value"/> unless it is null, empty or white space, else
    /// the package's localized text <paramref name="key"/>.
    /// </summary>
    /// <param name="value">The host's text, usually an optional parameter.</param>
    /// <param name="key">The resource key of the default text.</param>
    protected string LocalizeOr(string? value, string key) => string.IsNullOrWhiteSpace(value) ? Localize(key) : value;
}
