namespace OmniEurope.Blazor.Components;

/// <summary>
/// A check box bound to a <see cref="bool"/> or a <see cref="Nullable{T}">bool?</see>. Bound to a
/// <c>bool</c>, it is a native check box input; bound to a <c>bool?</c>, it is a three-state button
/// (<c>role="checkbox"</c>, <c>aria-checked="mixed"</c> while the value is null) that cycles null, true,
/// false. Any other value type throws <see cref="InvalidOperationException"/>.
/// </summary>
/// <typeparam name="TValue"><see cref="bool"/> or <see cref="Nullable{T}">bool?</see>, inferred from <c>@bind-Value</c>.</typeparam>
public partial class OmniCheckBox<TValue>
{
    private static readonly bool IsNullable = OmniBooleanValue<TValue>.IsNullable;

    private readonly string _generatedId = $"omni-checkbox-{Guid.NewGuid():N}";

    /// <summary>The settings tile around the control, whose name labels it and whose whole surface toggles it.</summary>
    [CascadingParameter]
    private OmniSettingsTileContext? SettingsTile { get; set; }

    // Only a control inside a settings tile needs an id of its own: the label of the tile points at it.
    private string? EffectiveId => Id ?? (SettingsTile is null ? null : _generatedId);

    /// <summary>Disables the control: it keeps its value and ignores clicks.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether a click on an unchecked box bound to a <c>bool?</c> brings it back to null, so the cycle
    /// is null, true, false, null. On by default; off, the box never returns to null once set (true,
    /// false, true). Ignored for a <c>bool</c>, which has no third state.
    /// </summary>
    [Parameter]
    public bool AllowIndeterminate { get; set; } = true;

    /// <summary>The id of the element that describes the control.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// The text beside the box, which names it and toggles it when clicked. Bound to a <c>bool</c>, the
    /// box and the text are wrapped in a <c>label.omni-checkbox-label</c>, which then carries
    /// <see cref="OmniInputBase{TValue}.Class"/>, and the middle of the box meets the middle of the letters;
    /// bound to a <c>bool?</c>, the text is inside the button. Without it, a <c>bool</c> box renders the
    /// input alone.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Puts the text before the box (a "label ....... box" line) instead of after it. Only for a box with
    /// text bound to a <c>bool</c>; the order of focus and of reading does not change, the box stays the
    /// control and the text its name.
    /// </summary>
    [Parameter]
    public bool TextFirst { get; set; }

    private string LabelCss => CssClassBuilder.Combine(["omni-checkbox-label", TextFirst ? "omni-checkbox-label--text-first" : null, Class]);

    // Beside a text, the box keeps its own class and the validation state; Class is the label's.
    private string BoxCss => CssClassBuilder.Combine(["omni-checkbox", CssClass]);

    private bool? State => OmniBooleanValue<TValue>.Read(CurrentValue);

    private string AriaChecked => State is null ? "mixed" : State == true ? "true" : "false";

    private void HandleChange(ChangeEventArgs args)
    {
        if (args.Value is bool value)
        {
            CurrentValue = OmniBooleanValue<TValue>.Write(value);
        }
    }

    private void Cycle()
    {
        if (!Disabled)
        {
            CurrentValue = OmniBooleanValue<TValue>.Write(OmniBooleanValue<TValue>.Next(State, AllowIndeterminate));
        }
    }

    /// <summary>
    /// Parses <c>true</c> or <c>false</c> (any case), and an empty text as null for a <c>bool?</c>. Anything
    /// else fails with the localized "The checkbox value is invalid."
    /// </summary>
    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (OmniBooleanValue<TValue>.TryParse(value, out result))
        {
            validationErrorMessage = null!;
            return true;
        }

        validationErrorMessage = Localize("CheckBoxInvalid");
        return false;
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> when <typeparamref name="TValue"/> is neither
    /// <c>bool</c> nor <c>bool?</c>, then joins the enclosing <see cref="OmniSettingsTile"/>, if any.
    /// </summary>
    protected override void OnParametersSet()
    {
        OmniBooleanValue<TValue>.EnsureSupported("OmniCheckBox");
        base.OnParametersSet();
        SettingsTile?.Join(this, EffectiveId!);
    }

    /// <summary>Leaves the enclosing <see cref="OmniSettingsTile"/>, if any, then disposes the input.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SettingsTile?.Leave(this);
        }

        base.Dispose(disposing);
    }
}
