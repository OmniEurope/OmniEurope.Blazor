using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A calendar with a day, a week and a month view, over appointments given as a list or loaded for the
/// visible range.
/// </summary>
/// <remarks>
/// <para>
/// With <see cref="DayStart"/> and <see cref="DayEnd"/>, the day and week views become a time grid of
/// <see cref="SlotMinutes"/> slots; an appointment sits in the slot where it starts (one starting before
/// the first slot or after the last sits in that slot), its times written on it.
/// </para>
/// <para>
/// The scheduler never changes <see cref="Items"/>: a click is reported through
/// <see cref="AppointmentClicked"/> and a move through <see cref="AppointmentMoved"/>, which the host
/// applies (and saves). An appointment is moved by dragging it onto a slot or a day, or without a drag:
/// its move button picks it up, each slot or day then shows a button that puts it there, and Escape or
/// the cancel button puts it back; each step is announced. A slot gives the new start; a day keeps the
/// time of day. The duration never changes.
/// </para>
/// </remarks>
public partial class OmniScheduler
{
    private const string ModulePath = "./_content/OmniEurope.Blazor/omni-scheduler.js";

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

    [Parameter] public IReadOnlyList<OmniSchedulerAppointment> Items { get; set; } = Array.Empty<OmniSchedulerAppointment>();
    [Parameter] public Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>>? Load { get; set; }
    [Parameter] public DateTimeOffset Date { get; set; }
    [Parameter] public EventCallback<DateTimeOffset> DateChanged { get; set; }
    [Parameter] public OmniSchedulerView View { get; set; } = OmniSchedulerView.Month;
    [Parameter] public EventCallback<OmniSchedulerView> ViewChanged { get; set; }
    [Parameter] public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    [Parameter] public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CurrentCulture;

    /// <summary>The first hour of the time grid; with <see cref="DayEnd"/>, the day and week views show one.</summary>
    [Parameter] public TimeOnly? DayStart { get; set; }

    /// <summary>The end of the time grid, after <see cref="DayStart"/>.</summary>
    [Parameter] public TimeOnly? DayEnd { get; set; }

    /// <summary>The length of a slot of the time grid, in minutes.</summary>
    [Parameter] public int SlotMinutes { get; set; } = 60;

    /// <summary>Raised with the appointment the user activates; each appointment becomes a button when it is set.</summary>
    [Parameter] public EventCallback<OmniSchedulerAppointment> AppointmentClicked { get; set; }

    /// <summary>Raised when an appointment is dropped at another start; appointments can be moved when it is set.</summary>
    [Parameter] public EventCallback<OmniSchedulerAppointmentMove> AppointmentMoved { get; set; }

    internal string Announcement => _announcement;

    private bool Clickable => AppointmentClicked.HasDelegate;
    private bool Movable => AppointmentMoved.HasDelegate;
    private bool HasTimeGrid => DayStart is { } start && DayEnd is { } end && end > start && SlotMinutes > 0;
    private int SlotCount => (int)Math.Ceiling((DayEnd!.Value - DayStart!.Value).TotalMinutes / SlotMinutes);
    private DateTimeOffset LocalDate => TimeZoneInfo.ConvertTime(Date, TimeZone);
    private DateOnly LocalToday => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone).Date);
    private IReadOnlyList<OmniSchedulerAppointment> SourceItems => Load is null ? Items : _loadedItems;
    private IReadOnlyList<OmniSchedulerAppointment> LocalAppointments => SourceItems.Select(ToLocal).ToArray();

    protected override void OnInitialized()
    {
        if (Date == default)
        {
            Date = Clock.GetUtcNow();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        if (!Movable || (Load is null && _moving is not null && Items.All(item => item.Id != _moving.Local.Id)))
        {
            // The host took moving away, or removed the appointment being carried.
            _moving = null;
            _dragged = null;
            _dropTarget = null;
        }

        // Keyed on the last load started, not the last one that succeeded: a range, view or loader
        // changed while a load runs, or after it failed, loads again at once (the previous load is
        // cancelled), while the same key waits for its load or for Retry.
        if (Load is not null && _requestedKey != CreateLoadKey())
        {
            await ReloadAsync();
        }
    }

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
        var (start, end) = View switch
        {
            OmniSchedulerView.Day => (local.Date, local.Date.AddDays(1)),
            OmniSchedulerView.Week => WeekRange(local),
            _ => (new DateTime(local.Year, local.Month, 1), new DateTime(local.Year, local.Month, 1).AddMonths(1))
        };
        return (CreateBoundary(start), CreateBoundary(end));
    }

    private (DateTime Start, DateTime End) WeekRange(DateTimeOffset local)
    {
        var firstDay = Culture.DateTimeFormat.FirstDayOfWeek;
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
        catch (Exception exception)
        {
            if (generation == _loadGeneration) _error = exception;
        }
        finally
        {
            if (generation == _loadGeneration) _loading = false;
        }
    }

    private async Task NavigateAsync(int direction)
    {
        var next = View switch
        {
            OmniSchedulerView.Day => Date.AddDays(direction),
            OmniSchedulerView.Week => Date.AddDays(direction * 7),
            _ => Date.AddMonths(direction)
        };
        Date = next;
        await DateChanged.InvokeAsync(next);
        if (Load is not null) await ReloadAsync();
    }

    private Task PreviousAsync() => NavigateAsync(-1);
    private Task NextAsync() => NavigateAsync(1);
    private async Task TodayAsync() { Date = TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone); await DateChanged.InvokeAsync(Date); if (Load is not null) await ReloadAsync(); }
    private async Task ChangeViewAsync(OmniSchedulerView view) { View = view; await ViewChanged.InvokeAsync(view); if (Load is not null) await ReloadAsync(); }
    private string ViewClass(OmniSchedulerView view) => View == view ? "omni-select-bar__item omni-select-bar__item--selected" : "omni-select-bar__item";
    private string Text(string key, params object[] arguments)
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = Culture;
        try
        {
            return Localize(key, arguments);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    // ----- Click and move ------------------------------------------------------------------

    private Task ClickAsync(OmniSchedulerAppointment local) =>
        Original(local) is { } original ? AppointmentClicked.InvokeAsync(original) : Task.CompletedTask;

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
        _announcement = Text("SchedulerMoved", carried.Local.Title, start.ToString("f", Culture));
        await AppointmentMoved.InvokeAsync(new OmniSchedulerAppointmentMove(carried.Original, start, end));
    }

    /// <summary>Where an appointment would start in a target: the slot's start, or the same time on another day.</summary>
    private DateTimeOffset NewStart(OmniSchedulerAppointment local, MoveTarget target) =>
        CreateBoundary(target.Slot is { } slot
            ? target.Day.ToDateTime(SlotStart(slot))
            : target.Day.ToDateTime(TimeOnly.FromTimeSpan(local.Start.TimeOfDay)));

    private bool CanPlace(MoveTarget target) => _moving is { } moving && NewStart(moving.Local, target) != moving.Local.Start;

    private string TargetLabel(Carried moving, MoveTarget target) => NewStart(moving.Local, target).ToString("f", Culture);

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
        return Math.Clamp((int)Math.Floor(minutes / SlotMinutes), 0, SlotCount - 1);
    }

    private TimeOnly SlotStart(int slot) => DayStart!.Value.AddMinutes(slot * SlotMinutes);

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
            appointment.CssClass
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string DurationMinutes(OmniSchedulerAppointment appointment) =>
        (appointment.End - appointment.Start).TotalMinutes.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The seven days of the week holding a date, from the first day of the week of the culture.</summary>
    private IEnumerable<DateTimeOffset> WeekDays(DateTimeOffset date)
    {
        var start = date.AddDays(-((7 + (int)date.DayOfWeek - (int)Culture.DateTimeFormat.FirstDayOfWeek) % 7));
        return Enumerable.Range(0, 7).Select(offset => start.AddDays(offset));
    }

    private IReadOnlyList<string> MonthDayNames
    {
        get
        {
            var names = Culture.DateTimeFormat.AbbreviatedDayNames;
            var first = (int)Culture.DateTimeFormat.FirstDayOfWeek;
            return Enumerable.Range(0, 7).Select(index => names[(first + index) % 7]).ToArray();
        }
    }

    /// <summary>The cells of a month grid: whole weeks, null outside the month.</summary>
    private IReadOnlyList<DateOnly?> MonthCells(DateTimeOffset date)
    {
        var first = new DateOnly(date.Year, date.Month, 1);
        var leading = (7 + (int)first.DayOfWeek - (int)Culture.DateTimeFormat.FirstDayOfWeek) % 7;
        var days = DateTime.DaysInMonth(date.Year, date.Month);
        var cellCount = (int)Math.Ceiling((leading + days) / 7d) * 7;
        return Enumerable.Range(0, cellCount)
            .Select(index => index < leading || index >= leading + days
                ? (DateOnly?)null
                : first.AddDays(index - leading))
            .ToArray();
    }

    private string ViewLabel(OmniSchedulerView view) => view switch
    {
        OmniSchedulerView.Day => Text("SchedulerDay"),
        OmniSchedulerView.Week => Text("SchedulerWeek"),
        _ => Text("SchedulerMonth")
    };

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
