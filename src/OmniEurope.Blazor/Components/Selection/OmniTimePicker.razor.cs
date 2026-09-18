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

    [Parameter]
    public TimeOnly? Minimum { get; set; }

    [Parameter]
    public TimeOnly? Maximum { get; set; }

    /// <summary>Minutes between two items of the minute column, from 1 to 30; 5 by default.</summary>
    [Parameter]
    public int Step { get; set; } = 5;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>Hint shown in the empty field; <c>hh:mm</c> in the interface language when null.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The clock Now reads.</summary>
    [Parameter]
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private PickerPopup Popup => _popup ??= new PickerPopup(JavaScript, DismissAsync);

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private string PanelId => $"{Id ?? _generatedId}-time";

    private string ToggleLabel => Localize("TimePickerToggle");

    private string EffectivePlaceholder => Placeholder
        ?? PickerFormat.Placeholder(PickerFormat.TimePattern(seconds: false), PickerLetters.From(key => Localize(key)));

    private TimeOnly Now => RoundDown(TimeOnly.FromDateTime(TimeProvider.GetLocalNow().DateTime), Step);

    private bool NowIsOutside => IsOutside(Now);

    // Built here rather than as a tag: the columns are internal, and Razor only finds public components.
    private RenderFragment TimeContent => builder =>
    {
        builder.OpenComponent<PickerTimeColumns>(0);
        builder.AddComponentParameter(1, nameof(PickerTimeColumns.IdPrefix), PanelId);
        builder.AddComponentParameter(2, nameof(PickerTimeColumns.Value), CurrentValue);
        builder.AddComponentParameter(3, nameof(PickerTimeColumns.Step), Step);
        builder.AddComponentParameter(4, nameof(PickerTimeColumns.IsRangeAllowed), (Func<TimeOnly, TimeOnly, bool>)IsRangeAllowed);
        builder.AddComponentParameter(5, nameof(PickerTimeColumns.OnChange), EventCallback.Factory.Create<PickerTimeChange>(this, Change));
        builder.CloseComponent();
    };

    internal static TimeOnly RoundDown(TimeOnly time, int step) => new(time.Hour, time.Minute - (time.Minute % step));

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(TimeProvider);
        ArgumentOutOfRangeException.ThrowIfLessThan(Step, 1, nameof(Step));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Step, 30, nameof(Step));
        if (Minimum is not null && Maximum is not null && Minimum > Maximum)
        {
            throw new InvalidOperationException("Minimum cannot be greater than Maximum.");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_popup is not null)
        {
            await _popup.AfterRenderAsync(_root, _panel, _toggle);
        }
    }

    protected override string? FormatValueAsString(TimeOnly? value) =>
        value?.ToString(PickerFormat.TimePattern(value.Value.Second != 0), CultureInfo.InvariantCulture);

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
