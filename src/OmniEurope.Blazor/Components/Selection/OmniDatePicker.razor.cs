namespace OmniEurope.Blazor.Components;

/// <summary>
/// A date field with the house calendar: the date is typed in the culture's short date or chosen in a
/// month grid that opens on the floating-surface layer, below the field.
/// </summary>
/// <remarks>
/// The grid starts the week on the culture's first day and names the months in its language; today
/// is circled, the chosen day filled with the accent. Choosing a day, Today or Clear sets the value and
/// closes the panel; a press outside it or Escape closes it too, Escape giving the focus back to the
/// toggle. Days out of <see cref="Minimum"/> and <see cref="Maximum"/> cannot be chosen, and a typed date
/// out of them is refused and marks the field invalid rather than being clamped. The field reads the
/// culture's short date, the ISO shape (<c>yyyy-MM-dd</c>) and what the culture's parser accepts.
/// </remarks>
public partial class OmniDatePicker
{
    private const string FocusedDaySelector = ".omni-calendar__day[tabindex='0']";

    private readonly string _generatedId = $"omni-date-{Guid.NewGuid():N}";
    private ElementReference _root;
    private ElementReference _panel;
    private ElementReference _toggle;
    private PickerPopup? _popup;
    private DateOnly _visibleMonth;
    private DateOnly _focusedDate;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    /// <summary>The earliest date that can be chosen or typed, included; none when null (the default).</summary>
    [Parameter]
    public DateOnly? Minimum { get; set; }

    /// <summary>The latest date that can be chosen or typed, included; none when null (the default). Not before <see cref="Minimum"/>.</summary>
    [Parameter]
    public DateOnly? Maximum { get; set; }

    /// <summary>Whether the field and its calendar toggle are disabled. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the date can be read and selected but not changed: the field is <c>readonly</c> and the calendar toggle disabled. Off by default.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Identifiers of the elements that describe the field, written as <c>aria-describedby</c> on the input; none when null.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>Hint shown in the empty field; the culture's date pattern (<c>jj/mm/aaaa</c>) when null.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    // Class goes on the outermost element; the validation classes of the form stay on the input they describe.
    private string RootClass => OmniEurope.Blazor.Internal.CssClassBuilder.Combine(["omni-date", "omni-date--date", Class]);

    private string InputClass => OmniEurope.Blazor.Internal.CssClassBuilder.Combine(["omni-input", "omni-date-input", "omni-date-picker", SizeClass, CssClass]);

    private PickerPopup Popup => _popup ??= new PickerPopup(JavaScript, DismissAsync);

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private string PanelId => $"{Id ?? _generatedId}-calendar";

    private string ToggleLabel => Localize("DatePickerToggle");

    private DateOnly Today => DateOnly.FromDateTime(Clock.GetLocalNow().DateTime);

    private bool TodayIsOutside => PickerCalendar.IsOutside(Today, Minimum, Maximum);

    private string EffectivePlaceholder => Placeholder
        ?? PickerFormat.Placeholder(PickerFormat.DatePattern(Culture), PickerLetters.From(key => Localize(key)));

    // Built here rather than as a tag: the grid is internal, and Razor only finds public components.
    private RenderFragment CalendarContent => builder =>
    {
        builder.OpenComponent<PickerCalendar>(0);
        builder.AddComponentParameter(1, nameof(PickerCalendar.IdPrefix), PanelId);
        builder.AddComponentParameter(2, nameof(PickerCalendar.Culture), Culture);
        builder.AddComponentParameter(3, nameof(PickerCalendar.VisibleMonth), _visibleMonth);
        builder.AddComponentParameter(4, nameof(PickerCalendar.FocusedDate), _focusedDate);
        builder.AddComponentParameter(5, nameof(PickerCalendar.SelectedDate), CurrentValue);
        builder.AddComponentParameter(6, nameof(PickerCalendar.Today), Today);
        builder.AddComponentParameter(7, nameof(PickerCalendar.Minimum), Minimum);
        builder.AddComponentParameter(8, nameof(PickerCalendar.Maximum), Maximum);
        builder.AddComponentParameter(9, nameof(PickerCalendar.OnSelect), EventCallback.Factory.Create<DateOnly>(this, Select));
        builder.AddComponentParameter(10, nameof(PickerCalendar.OnFocusChange), EventCallback.Factory.Create<DateOnly>(this, MoveFocus));
        builder.AddComponentParameter(11, nameof(PickerCalendar.OnMonthStep), EventCallback.Factory.Create<int>(this, StepMonth));
        builder.CloseComponent();
    };

    /// <summary>Checks the bounds.</summary>
    /// <exception cref="InvalidOperationException"><see cref="Minimum"/> is later than <see cref="Maximum"/>.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Minimum is not null && Maximum is not null && Minimum > Maximum)
        {
            throw new InvalidOperationException("Minimum cannot be greater than Maximum.");
        }
    }

    /// <summary>Attaches the panel script when the calendar opened, detaches it when it closed, and moves the focus into an open grid.</summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the panel is wired.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_popup is not null)
        {
            await _popup.AfterRenderAsync(_root, _panel, _toggle);
        }
    }

    /// <summary>Writes the date in the current culture's short date pattern, with a two-digit day and month and a four-digit year.</summary>
    /// <param name="value">The date, or null.</param>
    /// <returns>The text of the date, or null for no date.</returns>
    protected override string? FormatValueAsString(DateOnly? value) =>
        value?.ToString(PickerFormat.DatePattern(Culture), Culture);

    /// <summary>
    /// Reads the culture's short date, the ISO shape (<c>yyyy-MM-dd</c>) or what the culture's parser
    /// accepts; blank text means no date. A date out of the bounds is refused.
    /// </summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The date read, or null.</param>
    /// <param name="validationErrorMessage">Null on success; on failure, the localized "invalid date" message.</param>
    /// <returns>True when the text is blank or a date within the bounds.</returns>
    protected override bool TryParseValueFromString(string? value, out DateOnly? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        if (PickerFormat.TryParseDate(value, Culture, out var date) && !PickerCalendar.IsOutside(date, Minimum, Maximum))
        {
            result = date;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = Localize("DatePickerInvalid");
        return false;
    }

    /// <summary>Releases the listeners of a calendar still open, then the form subscription.</summary>
    /// <param name="disposing">True when called from <see cref="IDisposable.Dispose"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _popup?.Release();
        }

        base.Dispose(disposing);
    }

    private void HandleChange(ChangeEventArgs args)
    {
        CurrentValueAsString = args.Value?.ToString();
        if (Popup.IsOpen && CurrentValue is { } date)
        {
            _visibleMonth = date;
            _focusedDate = date;
        }
    }

    private void Toggle()
    {
        if (Popup.IsOpen)
        {
            Popup.Close(restoreFocus: false);
            return;
        }

        _focusedDate = PickerCalendar.Clamp(CurrentValue ?? Today, Minimum, Maximum);
        _visibleMonth = _focusedDate;
        Popup.Open(FocusedDaySelector);
    }

    private void Select(DateOnly day)
    {
        CurrentValueAsString = FormatValueAsString(day);
        Popup.Close(restoreFocus: true);
    }

    private void SelectToday() => Select(Today);

    private void Clear()
    {
        CurrentValueAsString = null;
        Popup.Close(restoreFocus: true);
    }

    private void MoveFocus(DateOnly day)
    {
        _focusedDate = day;
        _visibleMonth = day;
        Popup.FocusAfterRender(FocusedDaySelector);
    }

    private void StepMonth(int step)
    {
        _visibleMonth = new DateOnly(_visibleMonth.Year, _visibleMonth.Month, 1).AddMonths(step);
        _focusedDate = PickerCalendar.Clamp(_focusedDate.AddMonths(step), Minimum, Maximum);
    }

    private Task DismissAsync(bool fromKeyboard) => InvokeAsync(() =>
    {
        Popup.Close(fromKeyboard);
        StateHasChanged();
    });
}
