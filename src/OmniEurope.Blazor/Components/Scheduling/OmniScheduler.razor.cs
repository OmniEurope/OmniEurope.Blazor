using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A calendar with a day, a week and a month view, over appointments given as a list or loaded for the
/// visible range.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="DayStart"/> and <see cref="DayEnd"/>, the day and week views become a time grid of
/// <see cref="SlotDuration"/> slots; an appointment sits in the slot where it starts (one starting before
/// the first slot or after the last sits in that slot), its times written on it.
/// </para>
/// <para>
/// The scheduler never changes <see cref="Items"/>: a click is reported through
/// <see cref="OnAppointmentClick"/> and a move through <see cref="OnAppointmentMove"/>, which the host
/// applies (and saves). An appointment is moved by dragging it onto a slot or a day, or without a drag:
/// its move button picks it up, each slot or day then shows a button that puts it there, and Escape or
/// the cancel button puts it back; each step is announced. A slot gives the new start; a day keeps the
/// time of day. The duration never changes.
/// </para>
/// <para>
/// <see cref="Culture"/>, when set, drives the whole calendar: dates, times, the first day of the week
/// and every text the scheduler writes (buttons, view names, announcements) are in that culture.
/// </para>
/// </remarks>
public partial class OmniScheduler
{
    private const string ModulePath = OmniModules.Scheduler;

    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private IReadOnlyList<OmniSchedulerAppointment> _loadedItems = Array.Empty<OmniSchedulerAppointment>();
    private SchedulerLoadKey? _requestedKey;
    private bool _loading;
    private Exception? _error;
    private ElementReference _root;
    private IJSObjectReference? _module;
    private bool _importing;
    private bool _disposed;
    private Carried? _moving;
    private Carried? _dragged;
    private MoveTarget? _dropTarget;
    private string _announcement = string.Empty;

    // The date and view drawn: the parameters, until the reader navigates. The component never writes
    // its own parameters; a new value from the host replaces what the reader picked.
    private DateTimeOffset _date;
    private DateTimeOffset? _lastDateParameter;
    private OmniCalendarView _view = OmniCalendarView.Month;
    private OmniCalendarView? _lastViewParameter;

    /// <summary>The appointments, when the host gives them all at once; ignored while <see cref="Load"/> is set.</summary>
    [Parameter] public IReadOnlyList<OmniSchedulerAppointment> Items { get; set; } = Array.Empty<OmniSchedulerAppointment>();

    /// <summary>
    /// Loads the appointments of the visible range (start included, end excluded) instead of taking
    /// <see cref="Items"/>. Called again when the range changes, the previous call being cancelled.
    /// </summary>
    [Parameter] public Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>>? Load { get; set; }

    /// <summary>What is shown while <see cref="Load"/> runs; the localized "Loading" by default.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// What is shown, before the Retry button, when <see cref="Load"/> fails; it is given the exception.
    /// A localized sentence by default.
    /// </summary>
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

    /// <summary>Raised with the exception when <see cref="Load"/> fails; a cancelled load is not a failure.</summary>
    [Parameter] public EventCallback<Exception> OnLoadError { get; set; }

    /// <summary>A day of the period shown; the current date of the component clock when left unset.</summary>
    [Parameter] public DateTimeOffset Date { get; set; }

    /// <summary>Raised with the new date when the reader moves to another period.</summary>
    [Parameter] public EventCallback<DateTimeOffset> DateChanged { get; set; }

    /// <summary>The span shown: a day, a week or a month (the default).</summary>
    [Parameter] public OmniCalendarView View { get; set; } = OmniCalendarView.Month;

    /// <summary>Raised with the new view when the reader picks another one.</summary>
    [Parameter] public EventCallback<OmniCalendarView> ViewChanged { get; set; }

    /// <summary>The time zone appointments are drawn in and the visible range is computed in.</summary>
    [Parameter] public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>
    /// The culture of the whole calendar: dates and times, the first day of the week, and every text
    /// the scheduler writes (buttons, view names, announcements). Null (the default) follows the page:
    /// dates in the current culture, texts in the current UI culture.
    /// </summary>
    [Parameter] public CultureInfo? Culture { get; set; }

    /// <summary>The first hour of the time grid; with <see cref="DayEnd"/>, the day and week views show one.</summary>
    [Parameter] public TimeOnly? DayStart { get; set; }

    /// <summary>The end of the time grid, after <see cref="DayStart"/>.</summary>
    [Parameter] public TimeOnly? DayEnd { get; set; }

    /// <summary>The length of a slot of the time grid; one hour by default. Zero or less draws no grid.</summary>
    [Parameter] public TimeSpan SlotDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Raised with the appointment the user activates; each appointment becomes a button when it is set.</summary>
    [Parameter] public EventCallback<OmniSchedulerAppointment> OnAppointmentClick { get; set; }

    /// <summary>Raised when an appointment is dropped at another start; appointments can be moved when it is set.</summary>
    [Parameter] public EventCallback<OmniSchedulerAppointmentMove> OnAppointmentMove { get; set; }

    /// <summary>
    /// Raised with the slot the user activates in its free area: a slot of the time grid, or a whole day
    /// of the week list, of the month or of the day list. When it is set, that free area becomes a
    /// button under the appointments (reached by Tab, activated by Enter or Space, named by its date and
    /// time), so the host can open its own creation dialog; it is withdrawn while an appointment is
    /// being moved. Unset by default: no slot is a button.
    /// </summary>
    [Parameter] public EventCallback<OmniSchedulerSlot> OnSlotClick { get; set; }

    internal string Announcement => _announcement;

    /// <summary>The date drawn: <see cref="Date"/>, or the period the reader navigated to since.</summary>
    internal DateTimeOffset CurrentDate => _date;

    /// <summary>The view drawn: <see cref="View"/>, or the one the reader picked since.</summary>
    internal OmniCalendarView CurrentView => _view;

    private bool Clickable => OnAppointmentClick.HasDelegate;
    private bool Movable => OnAppointmentMove.HasDelegate;
    private bool SlotClickable => OnSlotClick.HasDelegate;
    private bool HasTimeGrid => DayStart is { } start && DayEnd is { } end && end > start && SlotDuration > TimeSpan.Zero;
    private int SlotCount => (int)Math.Ceiling((DayEnd!.Value - DayStart!.Value).TotalMinutes / SlotDuration.TotalMinutes);
    private DateTimeOffset LocalDate => TimeZoneInfo.ConvertTime(_date, TimeZone);
    private DateOnly LocalToday => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone).Date);
    private IReadOnlyList<OmniSchedulerAppointment> SourceItems => Load is null ? Items : _loadedItems;
    private IReadOnlyList<OmniSchedulerAppointment> LocalAppointments => SourceItems.Select(ToLocal).ToArray();

    /// <summary>
    /// Adopts a new <see cref="Date"/> (the default value means today, from the component clock) or
    /// <see cref="View"/> from the host, a navigation of the user staying while the host passes the same
    /// values. Drops a drag in progress when moving is no longer handled or its appointment left <see cref="Items"/>.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_lastDateParameter != Date)
        {
            _lastDateParameter = Date;
            _date = Date == default ? Clock.GetUtcNow() : Date;
        }

        if (_lastViewParameter != View)
        {
            _lastViewParameter = View;
            _view = View;
        }

        if (!Movable || (Load is null && _moving is not null && Items.All(item => item.Id != _moving.Local.Id)))
        {
            // The host took moving away, or removed the appointment being carried.
            _moving = null;
            _dragged = null;
            _dropTarget = null;
        }
    }

    /// <summary>
    /// With <see cref="Load"/> set, loads the appointments again when the visible range (moved by the date
    /// or the view) or the loader changed since the last load started, cancelling a load still running.
    /// </summary>
    /// <returns>A task that completes once the load is done.</returns>
    protected override async Task OnParametersSetAsync()
    {
        // Keyed on the last load started, not the last one that succeeded: a range, view or loader
        // changed while a load runs, or after it failed, loads again at once (the previous load is
        // cancelled), while the same key waits for its load or for Retry.
        if (Load is not null && _requestedKey != CreateLoadKey())
        {
            await ReloadAsync();
        }
    }

    /// <summary>
    /// Once appointments can move (<see cref="OnAppointmentMove"/> handled), loads and attaches the drag
    /// script; a lost circuit is ignored.
    /// </summary>
    /// <param name="firstRender">True on the first render of the component.</param>
    /// <returns>A task that completes once the script is attached.</returns>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The script only gives each drag the data some browsers require before they start one; a
        // scheduler whose appointments cannot move never loads it.
        if (!Movable || _module is not null || _importing || _disposed)
        {
            return;
        }

        _importing = true;
        try
        {
            var module = await JavaScript.InvokeAsync<IJSObjectReference>("import", ModulePath);
            if (_disposed)
            {
                await module.DisposeAsync();
                return;
            }

            _module = module;
            await _module.InvokeVoidAsync("attach", _root);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone, and the scheduler with it.
        }
        finally
        {
            _importing = false;
        }
    }

    private OmniSchedulerAppointment ToLocal(OmniSchedulerAppointment item) => item with
    {
        Start = TimeZoneInfo.ConvertTime(item.Start, TimeZone),
        End = TimeZoneInfo.ConvertTime(item.End, TimeZone)
    };

    private (DateTimeOffset Start, DateTimeOffset End) Range()
    {
        var local = LocalDate;
        var (start, end) = _view switch
        {
            OmniCalendarView.Day => (local.Date, local.Date.AddDays(1)),
            OmniCalendarView.Week => WeekRange(local),
            _ => (new DateTime(local.Year, local.Month, 1), new DateTime(local.Year, local.Month, 1).AddMonths(1))
        };
        return (CreateBoundary(start), CreateBoundary(end));
    }

    private (DateTime Start, DateTime End) WeekRange(DateTimeOffset local)
    {
        var firstDay = Formats.DateTimeFormat.FirstDayOfWeek;
        var daysSinceStart = (7 + (int)local.DayOfWeek - (int)firstDay) % 7;
        var start = local.AddDays(-daysSinceStart).Date;
        return (start, start.AddDays(7));
    }

    private DateTimeOffset CreateBoundary(DateTime localDate)
    {
        var local = DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        while (TimeZone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        var offset = TimeZone.IsAmbiguousTime(local)
            ? TimeZone.GetAmbiguousTimeOffsets(local).Max()
            : TimeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }

    private SchedulerLoadKey CreateLoadKey()
    {
        var range = Range();
        return new SchedulerLoadKey(range.Start, range.End, Load);
    }

    /// <summary>Loads the visible range again through <see cref="Load"/>, cancelling a load still running; does nothing without a loader.</summary>
    public async Task ReloadAsync()
    {
        if (Load is null) return;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var token = _loadCancellation.Token;
        var generation = ++_loadGeneration;
        var range = Range();
        var key = new SchedulerLoadKey(range.Start, range.End, Load);
        _requestedKey = key;
        _loading = true;
        _error = null;
        try
        {
            var items = await Load(range.Start, range.End, token);
            if (generation == _loadGeneration)
            {
                _loadedItems = items;
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) when (generation == _loadGeneration)
        {
            // Shown with a Retry button, and reported: the host logs it or tells the user its own way.
            _error = exception;
            _loading = false;
            await OnLoadError.InvokeAsync(exception);
        }
        catch (Exception)
        {
            // A load a newer one replaced failed: its error belongs to a range no longer shown.
        }
        finally
        {
            if (generation == _loadGeneration) _loading = false;
        }
    }

    private async Task NavigateAsync(int direction)
    {
        var next = _view switch
        {
            OmniCalendarView.Day => _date.AddDays(direction),
            OmniCalendarView.Week => _date.AddDays(direction * 7),
            _ => _date.AddMonths(direction)
        };
        _date = next;
        await DateChanged.InvokeAsync(next);
        if (Load is not null) await ReloadAsync();
    }

    private Task PreviousAsync() => NavigateAsync(-1);
    private Task NextAsync() => NavigateAsync(1);

    private async Task TodayAsync()
    {
        _date = TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone);
        await DateChanged.InvokeAsync(_date);
        if (Load is not null) await ReloadAsync();
    }

    private async Task ChangeViewAsync(OmniCalendarView view)
    {
        _view = view;
        await ViewChanged.InvokeAsync(view);
        if (Load is not null) await ReloadAsync();
    }

    private string ViewClass(OmniCalendarView view) => _view == view ? "omni-select-bar__item omni-select-bar__item--selected" : "omni-select-bar__item";

    /// <summary>The culture dates are written in: <see cref="Culture"/>, else the current culture.</summary>
    private CultureInfo Formats => Culture ?? CultureInfo.CurrentCulture;

    /// <summary>A library text in <see cref="Culture"/> when it is set, else in the current UI culture.</summary>
    private string Text(string key, params object[] arguments) => CultureText.In(Culture, () => Localize(key, arguments));

    // ----- Click and move ------------------------------------------------------------------

    private Task ClickAsync(OmniSchedulerAppointment local) =>
        Original(local) is { } original ? OnAppointmentClick.InvokeAsync(original) : Task.CompletedTask;

    private Task SlotClickAsync(OmniSchedulerSlot slot) => OnSlotClick.InvokeAsync(slot);

    /// <summary>
    /// A slot of the time grid (its start, and its end capped by <see cref="DayEnd"/>), or a whole day
    /// when <paramref name="slot"/> is null, with the offsets of the scheduler's time zone.
    /// </summary>
    private OmniSchedulerSlot CreateSlot(DateOnly day, int? slot)
    {
        var midnight = day.ToDateTime(TimeOnly.MinValue);
        if (slot is not { } index)
        {
            return new OmniSchedulerSlot(CreateBoundary(midnight), CreateBoundary(midnight.AddDays(1)));
        }

        var start = DayStart!.Value.ToTimeSpan() + SlotDuration * index;
        var end = DayStart.Value.ToTimeSpan() + SlotDuration * (index + 1);
        var limit = DayEnd!.Value.ToTimeSpan();
        return new OmniSchedulerSlot(CreateBoundary(midnight + start), CreateBoundary(midnight + (end < limit ? end : limit)));
    }

    private string SlotLabel(OmniSchedulerSlot slot, bool timed) =>
        Text("SchedulerNewAppointment", slot.Start.ToString(timed ? "f" : "D", Formats));

    /// <summary>The move button: picks the appointment up, or puts it back when it is the one carried.</summary>
    private void PickUp(OmniSchedulerAppointment local)
    {
        if (_moving?.Local.Id == local.Id)
        {
            CancelMove();
            return;
        }

        if (Original(local) is not { } original)
        {
            return;
        }

        _moving = new Carried(local, original);
        _announcement = Text("SchedulerMoving", local.Title);
    }

    private void CancelMove()
    {
        if (_moving is not { } moving)
        {
            return;
        }

        _moving = null;
        _announcement = Text("SchedulerMoveCancelled", moving.Local.Title);
    }

    private void OnKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            CancelMove();
        }
    }

    private async Task PlaceAsync(MoveTarget target)
    {
        if (_moving is not { } moving)
        {
            return;
        }

        _moving = null;
        await MoveAsync(moving, target);
    }

    private void StartDrag(OmniSchedulerAppointment local)
    {
        if (Movable && Original(local) is { } original)
        {
            _moving = null;
            _dragged = new Carried(local, original);
        }
    }

    private void DragEnter(MoveTarget target)
    {
        if (_dragged is not null)
        {
            _dropTarget = target;
        }
    }

    private void EndDrag()
    {
        _dragged = null;
        _dropTarget = null;
    }

    private async Task DropAsync(MoveTarget target)
    {
        var dragged = _dragged;
        EndDrag();
        if (dragged is not null)
        {
            await MoveAsync(dragged, target);
        }
    }

    private async Task MoveAsync(Carried carried, MoveTarget target)
    {
        var start = NewStart(carried.Local, target);
        if (start == carried.Local.Start)
        {
            _announcement = Text("SchedulerMoveCancelled", carried.Local.Title);
            return;
        }

        var end = start + (carried.Original.End - carried.Original.Start);
        _announcement = Text("SchedulerMoved", carried.Local.Title, start.ToString("f", Formats));
        await OnAppointmentMove.InvokeAsync(new OmniSchedulerAppointmentMove(carried.Original, start, end));
    }

    /// <summary>Where an appointment would start in a target: the slot's start, or the same time on another day.</summary>
    private DateTimeOffset NewStart(OmniSchedulerAppointment local, MoveTarget target) =>
        CreateBoundary(target.Slot is { } slot
            ? target.Day.ToDateTime(SlotStart(slot))
            : target.Day.ToDateTime(TimeOnly.FromTimeSpan(local.Start.TimeOfDay)));

    private bool CanPlace(MoveTarget target) => _moving is { } moving && NewStart(moving.Local, target) != moving.Local.Start;

    private string TargetLabel(Carried moving, MoveTarget target) => NewStart(moving.Local, target).ToString("f", Formats);

    private OmniSchedulerAppointment? Original(OmniSchedulerAppointment local) =>
        SourceItems.FirstOrDefault(item => item.Id == local.Id);

    // ----- Layout --------------------------------------------------------------------------

    /// <summary>The appointments starting on a local day, in the order they start.</summary>
    private static IEnumerable<OmniSchedulerAppointment> AppointmentsOn(IReadOnlyList<OmniSchedulerAppointment> appointments, DateTime day) =>
        appointments.Where(item => item.Start.Date == day).OrderBy(item => item.Start);

    /// <summary>The appointments of a local day whose start falls in a slot, the first and last slots taking those outside the grid.</summary>
    private IEnumerable<OmniSchedulerAppointment> AppointmentsInSlot(IReadOnlyList<OmniSchedulerAppointment> appointments, DateOnly day, int slot) =>
        AppointmentsOn(appointments, day.ToDateTime(TimeOnly.MinValue)).Where(item => SlotOf(item) == slot);

    private int SlotOf(OmniSchedulerAppointment appointment)
    {
        var minutes = (appointment.Start.TimeOfDay - DayStart!.Value.ToTimeSpan()).TotalMinutes;
        return Math.Clamp((int)Math.Floor(minutes / SlotDuration.TotalMinutes), 0, SlotCount - 1);
    }

    private TimeOnly SlotStart(int slot) => DayStart!.Value.Add(SlotDuration * slot);

    private static string DayCss(string baseClass, DateOnly day, DateOnly today) =>
        day == today ? $"{baseClass} {baseClass}--today" : baseClass;

    private string TargetCss(string css, MoveTarget target) =>
        _dragged is not null && _dropTarget == target ? $"{css} omni-scheduler__drop" : css;

    private string AppointmentCss(OmniSchedulerAppointment appointment)
    {
        var carried = _moving?.Local.Id == appointment.Id || _dragged?.Local.Id == appointment.Id;
        return string.Join(' ', new[]
        {
            "omni-scheduler__appointment",
            carried ? "omni-scheduler__appointment--moving" : null,
            appointment.Class
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string DurationMinutes(OmniSchedulerAppointment appointment) =>
        (appointment.End - appointment.Start).TotalMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The seven days of the week holding a date, from the first day of the week of the culture.</summary>
    private IEnumerable<DateTimeOffset> WeekDays(DateTimeOffset date)
    {
        var start = date.AddDays(-((7 + (int)date.DayOfWeek - (int)Formats.DateTimeFormat.FirstDayOfWeek) % 7));
        return Enumerable.Range(0, 7).Select(offset => start.AddDays(offset));
    }

    private IReadOnlyList<string> MonthDayNames
    {
        get
        {
            var names = Formats.DateTimeFormat.AbbreviatedDayNames;
            var first = (int)Formats.DateTimeFormat.FirstDayOfWeek;
            return Enumerable.Range(0, 7).Select(index => names[(first + index) % 7]).ToArray();
        }
    }

    /// <summary>The cells of a month grid: whole weeks, null outside the month.</summary>
    private IReadOnlyList<DateOnly?> MonthCells(DateTimeOffset date)
    {
        var first = new DateOnly(date.Year, date.Month, 1);
        var leading = (7 + (int)first.DayOfWeek - (int)Formats.DateTimeFormat.FirstDayOfWeek) % 7;
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        var cellCount = (int)Math.Ceiling((leading + days) / 7d) * 7;
        return Enumerable.Range(0, cellCount)
            .Select(index => index < leading || index >= leading + days
                ? (DateOnly?)null
                : first.AddDays(index - leading))
            .ToArray();
    }

    private string ViewLabel(OmniCalendarView view) => view switch
    {
        OmniCalendarView.Day => Text("SchedulerDay"),
        OmniCalendarView.Week => Text("SchedulerWeek"),
        _ => Text("SchedulerMonth")
    };

    /// <summary>Cancels a load still running and detaches the drag script.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _loadGeneration++;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("detach", _root);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }

    private sealed record SchedulerLoadKey(
        DateTimeOffset Start,
        DateTimeOffset End,
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>>? Loader);

    /// <summary>An appointment being moved: as drawn (in the scheduler's time zone) and as the host gave it.</summary>
    private sealed record Carried(OmniSchedulerAppointment Local, OmniSchedulerAppointment Original);

    /// <summary>Where an appointment can be dropped: a slot of a day in the time grid, or a whole day.</summary>
    private readonly record struct MoveTarget(DateOnly Day, int? Slot);
}
