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

    [Parameter]
    public DateOnly? Minimum { get; set; }

    [Parameter]
    public DateOnly? Maximum { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>Hint shown in the empty field; the culture's date pattern (<c>jj/mm/aaaa</c>) when null.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The clock that says which day is today.</summary>
    [Parameter]
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private PickerPopup Popup => _popup ??= new PickerPopup(JavaScript, DismissAsync);

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private string PanelId => $"{Id ?? _generatedId}-calendar";

    private string ToggleLabel => Localize("DatePickerToggle");

    private DateOnly Today => DateOnly.FromDateTime(TimeProvider.GetLocalNow().DateTime);

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

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(TimeProvider);
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

    protected override string? FormatValueAsString(DateOnly? value) =>
        value?.ToString(PickerFormat.DatePattern(Culture), Culture);

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
