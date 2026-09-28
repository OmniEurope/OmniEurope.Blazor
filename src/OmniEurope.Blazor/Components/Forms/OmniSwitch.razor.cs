namespace OmniEurope.Blazor.Components;

/// <summary>
/// An on/off switch (<c>role="switch"</c>) bound to a <see cref="bool"/> or a
/// <see cref="Nullable{T}">bool?</see>. Bound to a <c>bool?</c>, a null value draws the thumb in the
/// middle and is described as indeterminate, and a click cycles null, true, false. Any other value
/// type throws <see cref="InvalidOperationException"/>.
/// </summary>
/// <typeparam name="TValue"><see cref="bool"/> or <see cref="Nullable{T}">bool?</see>, inferred from <c>@bind-Value</c>.</typeparam>
public partial class OmniSwitch<TValue>
{
    private static readonly bool IsNullable = OmniBooleanValue<TValue>.IsNullable;

    private readonly string _generatedId = $"omni-switch-{Guid.NewGuid():N}";

    /// <summary>The settings tile around the control, whose name labels it and whose whole surface toggles it.</summary>
    [CascadingParameter]
    private OmniSettingsTileContext? SettingsTile { get; set; }

    // Only a control inside a settings tile needs an id of its own: the label of the tile points at it.
    private string? EffectiveId => Id ?? (SettingsTile is null ? null : _generatedId);

    /// <summary>Disables the switch: it keeps its value and ignores clicks.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether a click on an off switch bound to a <c>bool?</c> brings it back to null, so the cycle is
    /// null, true, false, null. On by default; off, the switch never returns to null once set. Ignored
    /// for a <c>bool</c>, which has no third state.
    /// </summary>
    [Parameter]
    public bool AllowIndeterminate { get; set; } = true;

    /// <summary>
    /// The description (<c>aria-description</c>) of a switch bound to a <c>bool?</c> while its value is
    /// null. Empty uses the localized "indeterminate state". Ignored for a <c>bool</c>.
    /// </summary>
    [Parameter]
    public string IndeterminateDescription { get; set; } = string.Empty;

    /// <summary>The id of the element that describes the switch.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>The text beside the track, inside the button, which names the switch.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private bool? State => OmniBooleanValue<TValue>.Read(CurrentValue);

    private string EffectiveIndeterminateDescription => string.IsNullOrWhiteSpace(IndeterminateDescription)
        ? Localize("NullableSwitchIndeterminate")
        : IndeterminateDescription;

    private void Toggle()
    {
        if (!Disabled)
        {
            CurrentValue = OmniBooleanValue<TValue>.Write(OmniBooleanValue<TValue>.Next(State, AllowIndeterminate));
        }
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (OmniBooleanValue<TValue>.TryParse(value, out result))
        {
            validationErrorMessage = null!;
            return true;
        }

        validationErrorMessage = Localize("SwitchInvalid");
        return false;
    }

    protected override void OnParametersSet()
    {
        OmniBooleanValue<TValue>.EnsureSupported("OmniSwitch");
        base.OnParametersSet();
        SettingsTile?.Join(this, EffectiveId!);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SettingsTile?.Leave(this);
        }

        base.Dispose(disposing);
    }
}
