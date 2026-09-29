namespace OmniEurope.Blazor.Components;

/// <summary>
/// A time field with two scrolling columns, hours from 00 to 23 and minutes by <see cref="Step"/>,
/// in a panel on the floating-surface layer, bound to <see cref="TimeOnly"/>.
/// </summary>
/// <remarks>
/// The field is written and read on 24 hours (<c>HH:mm</c>, <c>H:mm</c> and seconds accepted), like
/// the hour column. A choice in a column applies at once; Now takes the current time, rounded down to
/// the step, and Confirm closes the panel and gives the focus back to the toggle, as Escape does. A
/// press outside the panel closes it too. Times out of <see cref="Minimum"/> and <see cref="Maximum"/>
/// cannot be chosen, and a typed time out of them is refused and marks the field invalid.
/// </remarks>
public partial class OmniTimePicker
{
    private const string SelectedItemSelector = ".omni-time__list[data-omni-part='{0}'] [tabindex='0']";

    private readonly string _generatedId = $"omni-time-{Guid.NewGuid():N}";
    private ElementReference _root;
    private ElementReference _panel;
    private ElementReference _toggle;
    private PickerPopup? _popup;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The earliest time that can be chosen or typed, included; none when null (the default).</summary>
    [Parameter]
    public TimeOnly? Minimum { get; set; }

    /// <summary>The latest time that can be chosen or typed, included; none when null (the default). Not before <see cref="Minimum"/>.</summary>
    [Parameter]
    public TimeOnly? Maximum { get; set; }

    /// <summary>
    /// The time between two items of the minute column, a whole number of minutes from 1 to 30; five
    /// minutes by default. Any other value throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Parameter]
    public TimeSpan Step { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Whether the field and its panel toggle are disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the time can be read and selected but not changed: the field is <c>readonly</c> and the panel toggle disabled. Off by default.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Identifiers of the elements that describe the field, written as <c>aria-describedby</c> on the input; none when null.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>Hint shown in the empty field; <c>hh:mm</c> in the interface language when null.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    // Class goes on the outermost element; the validation classes of the form stay on the input they describe.
    private string RootClass => OmniEurope.Blazor.Internal.CssClassBuilder.Combine(["omni-date", "omni-date--time", Class]);

    private string InputClass => OmniEurope.Blazor.Internal.CssClassBuilder.Combine(["omni-input", "omni-time-picker", CssClass]);

    private PickerPopup Popup => _popup ??= new PickerPopup(JavaScript, DismissAsync);

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private string PanelId => $"{Id ?? _generatedId}-time";

    private string ToggleLabel => Localize("TimePickerToggle");

    private string EffectivePlaceholder => Placeholder
        ?? PickerFormat.Placeholder(PickerFormat.TimePattern(seconds: false), PickerLetters.From(key => Localize(key)));

    private TimeOnly Now => RoundDown(TimeOnly.FromDateTime(Clock.GetLocalNow().DateTime), StepMinutes(Step));

    private bool NowIsOutside => IsOutside(Now);

    // Built here rather than as a tag: the columns are internal, and Razor only finds public components.
    private RenderFragment TimeContent => builder =>
    {
        builder.OpenComponent<PickerTimeColumns>(0);
        builder.AddComponentParameter(1, nameof(PickerTimeColumns.IdPrefix), PanelId);
        builder.AddComponentParameter(2, nameof(PickerTimeColumns.Value), CurrentValue);
        builder.AddComponentParameter(3, nameof(PickerTimeColumns.Step), StepMinutes(Step));
        builder.AddComponentParameter(4, nameof(PickerTimeColumns.IsRangeAllowed), (Func<TimeOnly, TimeOnly, bool>)IsRangeAllowed);
        builder.AddComponentParameter(5, nameof(PickerTimeColumns.OnChange), EventCallback.Factory.Create<PickerTimeChange>(this, Change));
        builder.CloseComponent();
    };

    internal static TimeOnly RoundDown(TimeOnly time, int step) => new(time.Hour, time.Minute - (time.Minute % step));

    /// <summary>
    /// The step of a time picker in whole minutes, from 1 to 30, the only steps the minute column can
    /// draw; anything else, a fraction of a minute included, throws.
    /// </summary>
    internal static int StepMinutes(TimeSpan step)
    {
        if (step < TimeSpan.FromMinutes(1) || step > TimeSpan.FromMinutes(30) || step.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Step), step, "Step must be a whole number of minutes from 1 to 30.");
        }

        return (int)step.TotalMinutes;
    }

    /// <summary>Checks <see cref="Step"/> and the bounds.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Step"/> is not a whole number of minutes from 1 to 30.</exception>
    /// <exception cref="InvalidOperationException"><see cref="Minimum"/> is later than <see cref="Maximum"/>.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        StepMinutes(Step);
        if (Minimum is not null && Maximum is not null && Minimum > Maximum)
        {
            throw new InvalidOperationException("Minimum cannot be greater than Maximum.");
        }
    }

    /// <summary>Attaches the panel script when the panel opened, detaches it when it closed, and moves the focus into an open panel.</summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the panel is wired.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_popup is not null)
        {
            await _popup.AfterRenderAsync(_root, _panel, _toggle);
        }
    }

    /// <summary>Writes the time on 24 hours, <c>HH:mm</c>, or <c>HH:mm:ss</c> when it has seconds.</summary>
    /// <param name="value">The time, or null.</param>
    /// <returns>The text of the time, or null for none.</returns>
    protected override string? FormatValueAsString(TimeOnly? value) =>
        value?.ToString(PickerFormat.TimePattern(value.Value.Second != 0), CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a 24-hour time (<c>HH:mm</c> or <c>H:mm</c>, seconds accepted) or what the culture's parser
    /// accepts; blank text means no time. A time out of the bounds is refused.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The time read, or null.</param>
    /// <param name="validationErrorMessage">Null on success; on failure, the localized "invalid time" message.</param>
    /// <returns>True when the text is blank or a time within the bounds.</returns>
    protected override bool TryParseValueFromString(string? value, out TimeOnly? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        if (PickerFormat.TryParseTime(value, Culture, out var time) && !IsOutside(time))
        {
            result = time;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = Localize("TimePickerInvalid");
        return false;
    }

    /// <summary>Releases the listeners of a panel still open, then the form subscription.</summary>
    /// <param name="disposing">True when called from <see cref="IDisposable.Dispose"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _popup?.Release();
        }

        base.Dispose(disposing);
    }

    private bool IsOutside(TimeOnly time) => (Minimum is { } floor && time < floor) || (Maximum is { } ceiling && time > ceiling);

    private bool IsRangeAllowed(TimeOnly first, TimeOnly last) =>
        (Minimum is not { } floor || last >= floor) && (Maximum is not { } ceiling || first <= ceiling);

    private void HandleChange(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    private void Toggle()
    {
        if (Popup.IsOpen)
        {
            Popup.Close(restoreFocus: false);
            return;
        }

        Popup.Open(string.Format(CultureInfo.InvariantCulture, SelectedItemSelector, "hour"));
    }

    private void Change(PickerTimeChange change)
    {
        var time = change.Time;
        if (Minimum is { } floor && time < floor)
        {
            time = floor;
        }
        else if (Maximum is { } ceiling && time > ceiling)
        {
            time = ceiling;
        }

        CurrentValueAsString = FormatValueAsString(time);
        if (change.FromKeyboard)
        {
            Popup.FocusAfterRender(string.Format(CultureInfo.InvariantCulture, SelectedItemSelector, change.Part.ToString().ToLowerInvariant()));
        }
    }

    private void SelectNow() => CurrentValueAsString = FormatValueAsString(Now);

    private void Confirm() => Popup.Close(restoreFocus: true);

    private Task DismissAsync(bool fromKeyboard) => InvokeAsync(() =>
    {
        Popup.Close(fromKeyboard);
        StateHasChanged();
    });
}
