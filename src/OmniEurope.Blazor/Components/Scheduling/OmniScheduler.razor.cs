namespace OmniEurope.Blazor.Components;

public partial class OmniScheduler
{
    private CancellationTokenSource? _loadCancellation;
    private int _loadGeneration;
    private IReadOnlyList<OmniSchedulerAppointment> _loadedItems = Array.Empty<OmniSchedulerAppointment>();
    private SchedulerLoadKey? _loadedKey;
    private bool _loading;
    private Exception? _error;

    [Parameter] public IReadOnlyList<OmniSchedulerAppointment> Items { get; set; } = Array.Empty<OmniSchedulerAppointment>();
    [Parameter] public Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>>? Load { get; set; }
    [Parameter] public DateTimeOffset Date { get; set; }
    [Parameter] public EventCallback<DateTimeOffset> DateChanged { get; set; }
    [Parameter] public OmniSchedulerView View { get; set; } = OmniSchedulerView.Month;
    [Parameter] public EventCallback<OmniSchedulerView> ViewChanged { get; set; }
    [Parameter] public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    [Parameter] public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.CurrentCulture;
    [Parameter] public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    private DateTimeOffset LocalDate => TimeZoneInfo.ConvertTime(Date, TimeZone);
    private IReadOnlyList<OmniSchedulerAppointment> SourceItems => Load is null ? Items : _loadedItems;
    private IReadOnlyList<OmniSchedulerAppointment> LocalAppointments => SourceItems.Select(item => item with
    {
        Start = TimeZoneInfo.ConvertTime(item.Start, TimeZone),
        End = TimeZoneInfo.ConvertTime(item.End, TimeZone)
    }).ToArray();

    protected override void OnInitialized()
    {
        if (Date == default)
        {
            Date = TimeProvider.GetUtcNow();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        base.OnParametersSet();
        if (Load is not null && !_loading && _error is null && _loadedKey != CreateLoadKey())
        {
            await ReloadAsync();
        }
    }

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
        _loading = true;
        _error = null;
        try
        {
            var items = await Load(range.Start, range.End, token);
            if (generation == _loadGeneration)
            {
                _loadedItems = items;
                _loadedKey = key;
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
    private async Task TodayAsync() { Date = TimeZoneInfo.ConvertTime(TimeProvider.GetUtcNow(), TimeZone); await DateChanged.InvokeAsync(Date); if (Load is not null) await ReloadAsync(); }
    private async Task ChangeViewAsync(OmniSchedulerView view) { View = view; await ViewChanged.InvokeAsync(view); if (Load is not null) await ReloadAsync(); }
    private string ViewClass(OmniSchedulerView view) => View == view ? "omni-select-bar__item omni-select-bar__item--selected" : "omni-select-bar__item";
    private string Text(string key, params object[] arguments) => Localize(Culture, key, arguments);
    private string ViewLabel(OmniSchedulerView view) => view switch
    {
        OmniSchedulerView.Day => Text("SchedulerDay"),
        OmniSchedulerView.Week => Text("SchedulerWeek"),
        _ => Text("SchedulerMonth")
    };

    public ValueTask DisposeAsync()
    {
        _loadGeneration++;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed record SchedulerLoadKey(
        DateTimeOffset Start,
        DateTimeOffset End,
        Func<DateTimeOffset, DateTimeOffset, CancellationToken, Task<IReadOnlyList<OmniSchedulerAppointment>>>? Loader);
}
