namespace OmniEurope.Blazor.Components;

/// <summary>
/// A date and a time in one field, bound to <see cref="DateTime"/>: typed, or chosen in a panel on
/// the floating-surface layer that holds the month grid and the time columns side by side.
/// </summary>
/// <remarks>
/// Separate from <see cref="OmniDatePicker"/> rather than a flag on it: the two bind different
/// types (<see cref="DateOnly"/> against <see cref="DateTime"/>), and a picker that changed the
/// type of its own value depending on a boolean could not be bound at compile time.
/// The value is local wall-clock time, exactly what the field shows; converting to UTC is the
/// caller's decision, because the component cannot know whether the instant or the wall clock is
/// what the domain means.
/// A choice in the grid or the columns applies at once, the other half kept (midnight when there is
/// no time yet, today when there is no date); Now takes the current time rounded down to
/// <see cref="Step"/>, and Confirm closes the panel and gives the focus back to the toggle, as Escape
/// does. A choice that would fall out of <see cref="Minimum"/> and <see cref="Maximum"/> is brought
/// back to the nearest bound; a typed moment out of them is refused and marks the field invalid.
/// </remarks>
public partial class OmniDateTimePicker
{
    private const string FocusedDaySelector = ".omni-calendar__day[tabindex='0']";
    private const string SelectedItemSelector = ".omni-time__list[data-omni-part='{0}'] [tabindex='0']";

    private readonly string _generatedId = $"omni-date-time-{Guid.NewGuid():N}";
    private ElementReference _root;
    private ElementReference _panel;
    private ElementReference _toggle;
    private PickerPopup? _popup;
    private DateOnly _visibleMonth;
    private DateOnly _focusedDate;

    [Inject]
    private IJSRuntime JavaScript { get; set; } = default!;

    [Parameter]
    public DateTime? Minimum { get; set; }

    [Parameter]
    public DateTime? Maximum { get; set; }

    /// <summary>Whether the field and the panel offer seconds. Off by default, as most schedules are to the minute.</summary>
    [Parameter]
    public bool ShowSeconds { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>Minutes between two items of the minute column, from 1 to 30; 5 by default.</summary>
    [Parameter]
    public int Step { get; set; } = 5;

    /// <summary>Hint shown in the empty field; the culture's pattern (<c>jj/mm/aaaa hh:mm</c>) when null.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The clock that says which day is today and what Now is.</summary>
    [Parameter]
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private PickerPopup Popup => _popup ??= new PickerPopup(JavaScript, DismissAsync);

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private string PanelId => $"{Id ?? _generatedId}-panel";

    private string ToggleLabel => Localize("DateTimePickerToggle");

    private string EffectivePlaceholder => Placeholder
        ?? PickerFormat.Placeholder(PickerFormat.DateTimePattern(Culture, ShowSeconds), PickerLetters.From(key => Localize(key)));

    private DateTime LocalNow => TimeProvider.GetLocalNow().DateTime;

    private DateOnly Today => DateOnly.FromDateTime(LocalNow);

    private DateTime Now
    {
        get
        {
            var now = LocalNow;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute - (now.Minute % Step), 0, DateTimeKind.Unspecified);
        }
    }

    private bool NowIsOutside => IsOutside(Now);

    private DateOnly? MinimumDate => Minimum is { } floor ? DateOnly.FromDateTime(floor) : null;

    private DateOnly? MaximumDate => Maximum is { } ceiling ? DateOnly.FromDateTime(ceiling) : null;

    private DateOnly? SelectedDate => CurrentValue is { } value ? DateOnly.FromDateTime(value) : null;

    private TimeOnly? SelectedTime => CurrentValue is { } value ? TimeOnly.FromDateTime(value) : null;

    // Built here rather than as tags: the grid and the columns are internal, and Razor only finds
    // public components.
    private RenderFragment CalendarContent => builder =>
    {
        builder.OpenComponent<PickerCalendar>(0);
        builder.AddComponentParameter(1, nameof(PickerCalendar.IdPrefix), PanelId);
        builder.AddComponentParameter(2, nameof(PickerCalendar.Culture), Culture);
        builder.AddComponentParameter(3, nameof(PickerCalendar.VisibleMonth), _visibleMonth);
        builder.AddComponentParameter(4, nameof(PickerCalendar.FocusedDate), _focusedDate);
        builder.AddComponentParameter(5, nameof(PickerCalendar.SelectedDate), SelectedDate);
        builder.AddComponentParameter(6, nameof(PickerCalendar.Today), Today);
        builder.AddComponentParameter(7, nameof(PickerCalendar.Minimum), MinimumDate);
        builder.AddComponentParameter(8, nameof(PickerCalendar.Maximum), MaximumDate);
        builder.AddComponentParameter(9, nameof(PickerCalendar.OnSelect), EventCallback.Factory.Create<DateOnly>(this, SelectDay));
        builder.AddComponentParameter(10, nameof(PickerCalendar.OnFocusChange), EventCallback.Factory.Create<DateOnly>(this, MoveFocus));
        builder.AddComponentParameter(11, nameof(PickerCalendar.OnMonthStep), EventCallback.Factory.Create<int>(this, StepMonth));
        builder.CloseComponent();
    };

    private RenderFragment TimeContent => builder =>
    {
        builder.OpenComponent<PickerTimeColumns>(0);
        builder.AddComponentParameter(1, nameof(PickerTimeColumns.IdPrefix), PanelId);
        builder.AddComponentParameter(2, nameof(PickerTimeColumns.Value), SelectedTime);
        builder.AddComponentParameter(3, nameof(PickerTimeColumns.Step), Step);
        builder.AddComponentParameter(4, nameof(PickerTimeColumns.ShowSeconds), ShowSeconds);
        builder.AddComponentParameter(5, nameof(PickerTimeColumns.IsRangeAllowed), (Func<TimeOnly, TimeOnly, bool>)IsRangeAllowed);
        builder.AddComponentParameter(6, nameof(PickerTimeColumns.OnChange), EventCallback.Factory.Create<PickerTimeChange>(this, ChangeTime));
        builder.CloseComponent();
    };

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

    protected override string? FormatValueAsString(DateTime? value) =>
        value?.ToString(PickerFormat.DateTimePattern(Culture, ShowSeconds), Culture);

    protected override bool TryParseValueFromString(string? value, out DateTime? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        // Seconds are accepted whatever ShowSeconds says: a pasted or older value may have them, and
        // rejecting it would silently empty the field.
        if (PickerFormat.TryParseDateTime(value, Culture, out var moment) && !IsOutside(moment))
        {
            result = moment;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = Localize("DateTimePickerInvalid");
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

    private bool IsOutside(DateTime moment) => (Minimum is { } floor && moment < floor) || (Maximum is { } ceiling && moment > ceiling);

    private DateTime Clamp(DateTime moment) => Minimum is { } floor && moment < floor
        ? floor
        : Maximum is { } ceiling && moment > ceiling ? ceiling : moment;

    /// <summary>Whether any moment between the two times, on the chosen day, is within the bounds.</summary>
    private bool IsRangeAllowed(TimeOnly first, TimeOnly last)
    {
        var day = SelectedDate ?? Today;
        return (Minimum is not { } floor || day.ToDateTime(last) >= floor)
            && (Maximum is not { } ceiling || day.ToDateTime(first) <= ceiling);
    }

    private void SetMoment(DateTime moment) => CurrentValueAsString = FormatValueAsString(Clamp(moment));

    private void HandleChange(ChangeEventArgs args)
    {
        CurrentValueAsString = args.Value?.ToString();
        if (Popup.IsOpen && SelectedDate is { } date)
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

        _focusedDate = PickerCalendar.Clamp(SelectedDate ?? Today, MinimumDate, MaximumDate);
        _visibleMonth = _focusedDate;
        Popup.Open(FocusedDaySelector);
    }

    private void SelectDay(DateOnly day)
    {
        SetMoment(day.ToDateTime(SelectedTime ?? TimeOnly.MinValue));
        _focusedDate = day;
        _visibleMonth = day;
    }

    private void ChangeTime(PickerTimeChange change)
    {
        SetMoment((SelectedDate ?? Today).ToDateTime(change.Time));
        if (change.FromKeyboard)
        {
            Popup.FocusAfterRender(string.Format(CultureInfo.InvariantCulture, SelectedItemSelector, change.Part.ToString().ToLowerInvariant()));
        }
    }

    private void SelectNow()
    {
        SetMoment(Now);
        _focusedDate = Today;
        _visibleMonth = Today;
    }

    private void Confirm() => Popup.Close(restoreFocus: true);

    private void MoveFocus(DateOnly day)
    {
        _focusedDate = day;
        _visibleMonth = day;
        Popup.FocusAfterRender(FocusedDaySelector);
    }

    private void StepMonth(int step)
    {
        _visibleMonth = new DateOnly(_visibleMonth.Year, _visibleMonth.Month, 1).AddMonths(step);
        _focusedDate = PickerCalendar.Clamp(_focusedDate.AddMonths(step), MinimumDate, MaximumDate);
    }

    private Task DismissAsync(bool fromKeyboard) => InvokeAsync(() =>
    {
        Popup.Close(fromKeyboard);
        StateHasChanged();
    });
}
