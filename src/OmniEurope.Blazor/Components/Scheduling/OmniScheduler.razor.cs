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
/// <see cref="SlotDuration"/> slots. An appointment is drawn from its start to its end, to the minute,
/// and cut at the edges of the grid; one wholly before the first slot or after the last keeps a short
/// block in that slot, and one running into a later day shown goes on from the top of that column.
/// Appointments sharing their time stand side by side, at most eight abreast, and
/// each one names those it overlaps to assistive technology; <see cref="OmniSchedulerOverlap.Find"/>
/// gives the host the same pairs. A <see cref="DayEnd"/> earlier than <see cref="DayStart"/> draws a night
/// grid: the column of a day runs into the morning after, which is loaded with it.
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

    // The shortest block of the time grid, in slots: its one line stays readable. The stylesheet draws the same.
    private const double ShortestBlock = 0.5;

    // Under one slot a block has one line, its title; under two, its time and one line of title. These are
    // slots of the standard height: a compact slot is half as high, so the same heights span twice as many.
    private const double OneLine = 0.9;
    private const double TwoLinesOfTitle = 2;

    // The stylesheet draws this many lanes at most.
    private const int MostLanes = 8;

    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private IReadOnlyList<OmniSchedulerAppointment> _loadedItems = Array.Empty<OmniSchedulerAppointment>();
    private SchedulerLoadKey? _requestedKey;
    private bool _loading;
    private Exception? _error;
    private ElementReference _root;
    private IJSObjectReference? _module;
    private bool _importing;
    private bool _pageKeys;
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

    /// <summary>
    /// Shows the day, week and month picker of the header; a host with its own toolbar hides it and drives
    /// <see cref="View"/>. The period buttons stay.
    /// </summary>
    [Parameter] public bool ShowViewPicker { get; set; } = true;

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

    /// <summary>
    /// The end of the time grid. Earlier than <see cref="DayStart"/>, the grid crosses midnight: from 20:00
    /// to 08:00, the column of a day holds its evening and the morning after. Equal to it, no grid is drawn.
    /// </summary>
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

    /// <summary>
    /// Hatches what is already over, a pattern rather than a colour alone, and mutes its date: a slot of
    /// the time grid once its end has passed, a day of the week list, of the month or of the day list
    /// once it is before today. Now is read on the host's registered <see cref="TimeProvider"/> (the
    /// system clock otherwise), in <see cref="TimeZone"/>. False by default: nothing is marked.
    /// </summary>
    [Parameter] public bool MarkPastSlots { get; set; }

    /// <summary>
    /// The height of a slot of the time grid: <see cref="OmniSchedulerSlotHeight.Standard"/> (the default),
    /// or <see cref="OmniSchedulerSlotHeight.Compact"/>, half of it, to see a long day on a large screen.
    /// An appointment is drawn on its duration, so it follows the slot; in Compact, a block gets the looks it has
    /// at the same height: under two slots its title alone (its time stays in its tooltip and for assistive
    /// technology), under four its time and one line of title.
    /// </summary>
    [Parameter] public OmniSchedulerSlotHeight SlotHeight { get; set; } = OmniSchedulerSlotHeight.Standard;

    /// <summary>
    /// The direction of the time axis of the time grid of the day and week views:
    /// <see cref="OmniOrientation.Vertical"/> (the default) runs the time down the page, one column per day;
    /// <see cref="OmniOrientation.Horizontal"/> runs it across the page, the slot times as column headers and
    /// one row per day, each appointment drawn from its start to its end along the row. The month view and the
    /// lists drawn without a time grid are not affected. Moving works the same either way: a drop reads the day
    /// and the slot of the cell under the pointer, and the move button of an appointment reaches the same cells.
    /// In Horizontal, <see cref="SlotHeight"/> sets the width of a slot, and a row grows with the appointments
    /// that share their time. A narrow screen scrolls the grid, not the page.
    /// </summary>
    [Parameter] public OmniOrientation Orientation { get; set; } = OmniOrientation.Vertical;

    /// <summary>
    /// A .NET date format (such as <c>"dddd d MMM"</c>) applied with <see cref="Culture"/> (the current
    /// culture when it is null) to the header of each day of the time grid and over each day of the week list.
    /// Null (the default) writes the abbreviated day name and the day number (<c>"ddd d"</c>).
    /// </summary>
    [Parameter] public string? DayHeaderFormat { get; set; }

    /// <summary>
    /// Writes the title of the period shown in the header, in the patterns of <see cref="Culture"/>: the long date of
    /// a day ("mardi 3 novembre 2026"), the first and last days of a week ("2 au 8 novembre 2026"), the month
    /// ("novembre 2026"). False by default: the header holds the navigation and the view picker only.
    /// </summary>
    [Parameter] public bool ShowPeriodTitle { get; set; }

    /// <summary>
    /// Marks, in the time grid, an appointment that shares its time with another: the dashed outline
    /// <c>omni-scheduler__appointment--overlapping</c> and a hidden text naming the others. True by default.
    /// False for a host whose own rule of an overlap differs: the side-by-side lanes stay, and only an
    /// appointment with <see cref="OmniSchedulerAppointment.OverlapText"/> is marked.
    /// </summary>
    [Parameter] public bool ShowOverlaps { get; set; } = true;

    internal string Announcement => _announcement;

    /// <summary>The date drawn: <see cref="Date"/>, or the period the reader navigated to since.</summary>
    internal DateTimeOffset CurrentDate => _date;

    /// <summary>The view drawn: <see cref="View"/>, or the one the reader picked since.</summary>
    internal OmniCalendarView CurrentView => _view;

    private bool Clickable => OnAppointmentClick.HasDelegate;
    private bool Movable => OnAppointmentMove.HasDelegate;
    private bool SlotClickable => OnSlotClick.HasDelegate;
    private bool HasTimeGrid => DayStart is { } start && DayEnd is { } end && end != start && SlotDuration > TimeSpan.Zero;
    private bool Horizontal => Orientation == OmniOrientation.Horizontal;
    private bool CrossesMidnight => HasTimeGrid && DayEnd!.Value < DayStart!.Value;

    // TimeOnly subtraction wraps around midnight, so this holds for a night grid too.
    private TimeSpan GridLength => DayEnd!.Value - DayStart!.Value;
    private int SlotCount => (int)Math.Ceiling(GridLength / SlotDuration);
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
        else if (Load is null && _requestedKey is not null)
        {
            ForgetLoader();
        }
    }

    // The loader taken away, the scheduler shows Items: a load still running is cancelled and its
    // status, error and appointments go with it, so a late answer or a late failure shows nothing.
    private void ForgetLoader()
    {
        // Given together with the key of the load it serves (ReloadAsync).
        _loadGeneration++;
        _loadCancellation!.Cancel();
        _loadCancellation.Dispose();
        _loadCancellation = null;
        _requestedKey = null;
        _loading = false;
        _error = null;
        _loadedItems = [];
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
            var module = await OmniScriptModule.ImportAsync(JavaScript, ModulePath);
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

    // The view switcher is a radio group: its arrows are the page keys of the focus module, turned on
    // at its first focus, so a scheduler nobody reaches with the keyboard loads no script.
    private Task EnablePageKeysAsync()
    {
        if (_pageKeys)
        {
            return Task.CompletedTask;
        }

        _pageKeys = true;
        return OmniPageKeys.EnableAsync(JavaScript);
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

        // The last column of a night grid runs into the morning after.
        if (_view != OmniCalendarView.Month && CrossesMidnight)
        {
            end += DayEnd!.Value.ToTimeSpan();
        }

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
        // By calendar day, week or month of the time zone, on the wall clock: adding 24 hours to an instant
        // skips a day, or stays on the same one, around a clock change.
        var wall = LocalDate.DateTime;
        var next = CreateBoundary(_view switch
        {
            OmniCalendarView.Day => wall.AddDays(direction),
            OmniCalendarView.Week => wall.AddDays(direction * 7),
            _ => wall.AddMonths(direction)
        });
        _date = next;
        await DateChanged.InvokeAsync(next);
        await ReloadAsync();
    }

    private Task PreviousAsync() => NavigateAsync(-1);
    private Task NextAsync() => NavigateAsync(1);

    private async Task TodayAsync()
    {
        _date = TimeZoneInfo.ConvertTime(Clock.GetUtcNow(), TimeZone);
        await DateChanged.InvokeAsync(_date);
        await ReloadAsync();
    }

    private async Task ChangeViewAsync(OmniCalendarView view)
    {
        _view = view;
        await ViewChanged.InvokeAsync(view);
        await ReloadAsync();
    }

    // The header of a day of the time grid or of the week list, in the pattern of the host or "ddd d".
    // A DateTime, not a DateOnly: a pattern with a time part is then written as zeros rather than refused.
    private string DayHeader(DateOnly day) => day.ToDateTime(TimeOnly.MinValue).ToString(DayHeaderFormat ?? "ddd d", Formats);

    // The title of a period button: the day, week or month the button leads to.
    private string PeriodButtonTitle(bool next) => Text(_view switch
    {
        OmniCalendarView.Day => next ? "SchedulerNextDay" : "SchedulerPreviousDay",
        OmniCalendarView.Week => next ? "SchedulerNextWeek" : "SchedulerPreviousWeek",
        _ => next ? "SchedulerNextMonth" : "SchedulerPreviousMonth"
    });

    private string PeriodTitle()
    {
        var day = DateOnly.FromDateTime(_date.DateTime);
        var week = WeekDays(_date).Select(date => DateOnly.FromDateTime(date.DateTime)).ToList();
        return _view == OmniCalendarView.Week
            ? SchedulerPeriodTitle.Of(_view, week[0], week[^1], Formats, (start, end) => Text("SchedulerPeriodRange", start, end))
            : SchedulerPeriodTitle.Of(_view, day, day, Formats, (start, _) => start);
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
        if (slot is not { } index)
        {
            var midnight = day.ToDateTime(TimeOnly.MinValue);
            return new OmniSchedulerSlot(CreateBoundary(midnight), CreateBoundary(midnight.AddDays(1)));
        }

        var start = SlotStartOn(day, index);
        var end = start + SlotDuration;
        var limit = SlotStartOn(day, 0) + GridLength;
        return new OmniSchedulerSlot(CreateBoundary(start), CreateBoundary(end < limit ? end : limit));
    }

    /// <summary>Adds the past mark to a slot or day container when <see cref="MarkPastSlots"/> is on and it is over.</summary>
    private string PastCss(string css, DateOnly day, int? slot) =>
        MarkPastSlots && IsPast(day, slot) ? $"{css} omni-scheduler__past" : css;

    /// <summary>A slot is over once its end has passed; a whole day, once it is before today.</summary>
    private bool IsPast(DateOnly day, int? slot) => slot is null
        ? day < LocalToday
        : CreateSlot(day, slot).End <= Clock.GetUtcNow();

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
            ? SlotStartOn(target.Day, slot)
            : target.Day.ToDateTime(TimeOnly.FromTimeSpan(local.Start.TimeOfDay)));

    private bool CanPlace(Carried moving, MoveTarget target) => NewStart(moving.Local, target) != moving.Local.Start;

    private string TargetLabel(Carried moving, MoveTarget target) => NewStart(moving.Local, target).ToString("f", Formats);

    private OmniSchedulerAppointment? Original(OmniSchedulerAppointment local) =>
        SourceItems.FirstOrDefault(item => item.Id == local.Id);

    // ----- Layout --------------------------------------------------------------------------

    /// <summary>The appointments starting on a local day, in the order they start.</summary>
    private static IEnumerable<OmniSchedulerAppointment> AppointmentsOn(IReadOnlyList<OmniSchedulerAppointment> appointments, DateTime day) =>
        appointments.Where(item => item.Start.Date == day).OrderBy(item => item.Start);

    /// <summary>The blocks of the time grid, by the day and slot of the cell each one starts in.</summary>
    private ILookup<(DateOnly Day, int Slot), GridBlock> GridBlocks(IReadOnlyList<OmniSchedulerAppointment> appointments, IReadOnlyCollection<DateOnly> days)
    {
        var overlapping = OmniSchedulerOverlap.Find(appointments)
            .SelectMany(pair => new[] { (pair.First.Id, pair.Second.Title), (pair.Second.Id, pair.First.Title) })
            .ToLookup(pair => pair.Item1, pair => pair.Item2);
        return appointments
            .SelectMany(appointment => ColumnsOf(appointment, days).Select(day => (Day: day, Appointment: appointment)))
            .GroupBy(item => item.Day, item => item.Appointment)
            .SelectMany(group => LayOutDay(group.Key, group, overlapping))
            .ToLookup(block => (block.Day, block.Slot));
    }

    /// <summary>The columns shown that draw an appointment: the one it starts in, then each later one it reaches.</summary>
    private IEnumerable<DateOnly> ColumnsOf(OmniSchedulerAppointment appointment, IReadOnlyCollection<DateOnly> days)
    {
        var first = GridDay(appointment);
        return days.Where(day => day == first || (day > first && appointment.End.DateTime > SlotStartOn(day, 0)));
    }

    /// <summary>The column of an appointment: the day it starts on, or the day before in the morning of a night grid.</summary>
    private DateOnly GridDay(OmniSchedulerAppointment appointment)
    {
        var day = DateOnly.FromDateTime(appointment.Start.Date);
        return CrossesMidnight && TimeOnly.FromTimeSpan(appointment.Start.TimeOfDay) < DayEnd!.Value ? day.AddDays(-1) : day;
    }

    /// <summary>
    /// Places the appointments of a column, in slots from the top of the grid, on the wall clock so a block
    /// lines up with the times of the rows: each takes the first lane free when it starts, and appointments
    /// chained by shared time divide the width by the lanes they need.
    /// </summary>
    private List<GridBlock> LayOutDay(DateOnly day, IEnumerable<OmniSchedulerAppointment> appointments, ILookup<string, string> overlapping)
    {
        var origin = SlotStartOn(day, 0);
        var length = GridLength / SlotDuration;
        var placed = appointments
            .Select(appointment =>
            {
                var top = Math.Clamp((appointment.Start.DateTime - origin) / SlotDuration, 0, Math.Max(0, length - ShortestBlock));
                var bottom = Math.Min((appointment.End.DateTime - origin) / SlotDuration, length);
                return (Appointment: appointment, Top: top, Span: Math.Max(bottom - top, 0));
            })
            .OrderBy(item => item.Top)
            .ThenBy(item => item.Appointment.Start);

        var blocks = new List<GridBlock>();
        var chain = new List<GridBlock>();
        var laneEnds = new List<double>();
        foreach (var (appointment, top, span) in placed)
        {
            if (laneEnds.Count > 0 && laneEnds.Max() <= top)
            {
                blocks.AddRange(chain.Select(block => block with { Lanes = laneEnds.Count }));
                chain.Clear();
                laneEnds.Clear();
            }

            var lane = laneEnds.FindIndex(end => end <= top);
            if (lane < 0)
            {
                lane = laneEnds.Count;
                laneEnds.Add(0);
            }

            // Drawn at its shortest, a brief appointment still takes that height from its lane.
            laneEnds[lane] = top + Math.Max(span, ShortestBlock);
            var slot = Math.Min((int)top, SlotCount - 1);
            chain.Add(new GridBlock(appointment, day, slot, top - slot, span, lane, 1, [.. overlapping[appointment.Id]]));
        }

        blocks.AddRange(chain.Select(block => block with { Lanes = laneEnds.Count }));
        return blocks;
    }

    /// <summary>
    /// The overlap a block names to assistive technology, which also marks it: the host's
    /// <see cref="OmniSchedulerAppointment.OverlapText"/> first, else, with <see cref="ShowOverlaps"/>, the
    /// appointments it shares its time with; null when there is none.
    /// </summary>
    private string? OverlapText(GridBlock block) =>
        !string.IsNullOrWhiteSpace(block.Appointment.OverlapText)
            ? block.Appointment.OverlapText
            : ShowOverlaps && block.Overlaps.Count > 0 ? Text("SchedulerOverlaps", string.Join(", ", block.Overlaps)) : null;

    /// <summary>
    /// The classes of a block. An inline style being out of reach, its place and height are written digit
    /// by digit in hundredths of a slot, each digit a class setting a custom property the stylesheet adds
    /// up; the zero digits are left out. Past <see cref="MostLanes"/> lanes, the last one is shared.
    /// </summary>
    private string BlockCss(GridBlock block)
    {
        var offset = Math.Min((int)Math.Round(block.Offset * 100), 99);
        var span = Math.Min((int)Math.Round(block.Span * 100), 99_999);
        var lanes = Math.Min(block.Lanes, MostLanes);
        var lane = Math.Min(block.Lane, MostLanes - 1);
        var slotsPerStandardSlot = SlotHeight == OmniSchedulerSlotHeight.Compact ? 2 : 1;
        return string.Join(' ', new[]
        {
            AppointmentCss(block.Appointment),
            OverlapText(block) is null ? null : "omni-scheduler__appointment--overlapping",
            block.Span < OneLine * slotsPerStandardSlot ? "omni-scheduler__appointment--brief" : block.Span < TwoLinesOfTitle * slotsPerStandardSlot ? "omni-scheduler__appointment--short" : null,
            Digits("at", offset, 2),
            Digits("len", span, 5),
            lanes == 1 ? null : $"omni-scheduler__lanes-{lanes.ToString(CultureInfo.InvariantCulture)}",
            lane == 0 ? null : $"omni-scheduler__lane-{lane.ToString(CultureInfo.InvariantCulture)}"
        }.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string Digits(string name, int value, int count) => string.Join(' ', Enumerable.Range(0, count)
        .Select(position => (Position: position, Digit: value / (int)Math.Pow(10, position) % 10))
        .Where(item => item.Digit != 0)
        .Select(item => string.Create(CultureInfo.InvariantCulture, $"omni-scheduler__{name}-{item.Position}-{item.Digit}")));

    private TimeOnly SlotStart(int slot) => DayStart!.Value.Add(SlotDuration * slot);

    /// <summary>The wall-clock start of a slot in the column of a day, on the next date past midnight.</summary>
    private DateTime SlotStartOn(DateOnly day, int slot) =>
        day.ToDateTime(TimeOnly.MinValue) + DayStart!.Value.ToTimeSpan() + SlotDuration * slot;

    /// <summary>Whether a slot is the first of a night grid to fall on the next date.</summary>
    private bool StartsNextDay(int slot)
    {
        var start = DayStart!.Value.ToTimeSpan() + SlotDuration * slot;
        return CrossesMidnight && start >= TimeSpan.FromDays(1) && start - SlotDuration < TimeSpan.FromDays(1);
    }

    // A cell of the time grid. In Horizontal the first cell of a night grid on the next date also carries the marker
    // of its column, as the row does in Vertical.
    private string CellCss(DateOnly day, int slot, DateOnly today, MoveTarget target)
    {
        var css = PastCss(TargetCss(DayCss("omni-scheduler-grid__cell", day, today), target), day, slot);
        return Horizontal && StartsNextDay(slot) ? $"{css} omni-scheduler-grid__col--next-day" : css;
    }

    // The header of a slot of a horizontal time grid.
    private string SlotHeaderCss(int slot) =>
        StartsNextDay(slot) ? "omni-scheduler-grid__time omni-scheduler-grid__col--next-day" : "omni-scheduler-grid__time";

    // A horizontal row is as many lanes high as its busiest chain of appointments, so each lane keeps one line.
    private static string? RowLanesCss(DateOnly day, ILookup<(DateOnly Day, int Slot), GridBlock> blocks)
    {
        var lanes = Math.Min(blocks.Where(group => group.Key.Day == day).SelectMany(group => group).Select(block => block.Lanes).DefaultIfEmpty(1).Max(), MostLanes);
        return lanes == 1 ? null : $"omni-scheduler-grid__row--lanes-{lanes.ToString(CultureInfo.InvariantCulture)}";
    }

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

    /// <summary>
    /// An appointment placed in the time grid: the cell it starts in, its offset in that slot and its span,
    /// in slots, its lane among those of its chain, and the titles it overlaps.
    /// </summary>
    private sealed record GridBlock(OmniSchedulerAppointment Appointment, DateOnly Day, int Slot, double Offset, double Span, int Lane, int Lanes, IReadOnlyList<string> Overlaps);

    /// <summary>Where an appointment can be dropped: a slot of a day in the time grid, or a whole day.</summary>
    private readonly record struct MoveTarget(DateOnly Day, int? Slot);
}
