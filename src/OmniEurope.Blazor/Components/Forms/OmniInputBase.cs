using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The base of the package's form controls: Blazor's <see cref="InputBase{TValue}"/> (binding, edit
/// context, validation classes) with the <see cref="Id"/>, <see cref="Class"/>, preset, attribute guard
/// and localized texts of <see cref="OmniComponentBase"/>, under the same rules.
/// </summary>
/// <typeparam name="TValue">The type of the bound value.</typeparam>
public abstract class OmniInputBase<TValue> : InputBase<TValue>
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

    /// <summary>The id of the focusable control, which a label's <c>for</c> points at.</summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>CSS classes added to the control's outermost element, after its own.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// The height of a one-line field, matched to the button of the same size so that a toolbar mixing
    /// them lines up (recette R-052): <see cref="OmniControlSize.Small"/> is the control height
    /// less 0.5rem with the small text, <see cref="OmniControlSize.Large"/> the control height plus
    /// 0.5rem with the large text. Null or <see cref="OmniControlSize.Medium"/> keeps the control height.
    /// Honoured by the one-line fields: <see cref="OmniTextBox"/>, <see cref="OmniPassword"/>,
    /// <see cref="OmniNumeric{TValue}"/>, <see cref="OmniDropDown{TValue}"/>, the date and time pickers and
    /// <see cref="OmniAutocomplete{TItem}"/>; the other controls ignore it.
    /// </summary>
    [Parameter]
    public OmniControlSize? Size { get; set; }

    /// <summary>The class of <see cref="Size"/> on the field element, or null at the control height.</summary>
    private protected string? SizeClass => Size switch
    {
        OmniControlSize.Small => "omni-input--small",
        OmniControlSize.Large => "omni-input--large",
        _ => null
    };

    /// <summary>What an enclosing <see cref="OmniFormField"/> cascades: the id its label names and the label's id.</summary>
    [CascadingParameter]
    private OmniFormFieldLabel? EnclosingFormFieldLabel { get; set; }

    /// <summary>
    /// The id of the label of the enclosing <see cref="OmniFormField"/> when that label names this control
    /// (its <c>For</c> is <see cref="Id"/>), else null. A control that a <c>label for</c> cannot name
    /// writes it as <c>aria-labelledby</c> when it has no label of its own.
    /// </summary>
    private protected string? FormFieldLabelId => OmniFormFieldLabel.For(EnclosingFormFieldLabel, Id);

    /// <summary><c>"true"</c> while the edit context holds a message for the bound field, else null.</summary>
    protected string? AriaInvalid => EditContext is not null && EditContext.GetValidationMessages(FieldIdentifier).Any()
        ? "true"
        : null;

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

    /// <summary>Refuses the additional attributes the CSP contract forbids.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        CspAttributeGuard.EnsureSafe(AdditionalAttributes, GetType());
    }

    /// <summary>
    /// The control's classes, then the validation classes of the edit context, then <see cref="Class"/>:
    /// for a control whose outermost element is the input itself.
    /// </summary>
    /// <param name="values">The control's own classes.</param>
    protected string InputCss(params string?[] values) =>
        CssClassBuilder.Combine(values.Append(SizeClass).Append(CssClass).Append(Class));

    /// <summary>
    /// The control's classes, then the validation classes of the edit context, without
    /// <see cref="Class"/>: for the input of a control drawn inside a wrapper, which takes
    /// <see cref="Class"/> through <see cref="WrapperCss"/>.
    /// </summary>
    /// <param name="values">The input's own classes.</param>
    protected string InnerInputCss(params string?[] values) =>
        CssClassBuilder.Combine(values.Append(SizeClass).Append(CssClass));

    /// <summary>The wrapper's classes followed by <see cref="Class"/>: the outermost element of a wrapped control.</summary>
    /// <param name="values">The wrapper's own classes.</param>
    protected string WrapperCss(params string?[] values) =>
        CssClassBuilder.Combine(values.Append(Class));

    /// <summary>A text of the package's resources in the current UI culture.</summary>
    /// <param name="name">The resource key.</param>
    /// <param name="arguments">The values of the key's <c>{n}</c> placeholders.</param>
    protected string Localize(string name, params object[] arguments) =>
        PluralMessage.Localize(StringLocalizer, name, arguments);

    /// <summary>
    /// A text the host may override: <paramref name="value"/> unless it is null, empty or white space,
    /// else the package's localized text <paramref name="key"/>.
    /// </summary>
    /// <param name="value">The host's text, usually an optional parameter.</param>
    /// <param name="key">The resource key of the default text.</param>
    protected string LocalizeOr(string? value, string key) => string.IsNullOrWhiteSpace(value) ? Localize(key) : value;
}
