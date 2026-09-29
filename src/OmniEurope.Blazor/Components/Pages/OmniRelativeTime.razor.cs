namespace OmniEurope.Blazor.Components;

/// <summary>
/// A moment told relative to now, "2 min ago", in a <c>time</c> element carrying the exact instant,
/// with the absolute date in a tooltip shown on hover and on keyboard focus. The component keeps no
/// clock of its own: now is the host's registered <see cref="TimeProvider"/>, the system clock
/// when it registers none.
/// </summary>
/// <remarks>
/// Left alone the label is computed at each render. <see cref="RefreshInterval"/> redraws it on a
/// timer the component owns, created from that clock and disposed with it.
/// </remarks>
public partial class OmniRelativeTime : IDisposable
{
    private readonly string _tooltipId = $"omni-relative-time-{Guid.NewGuid():N}";
    private ITimer? _timer;
    private TimeSpan _timerInterval;
    private TimeProvider? _timerProvider;

    /// <summary>The moment to tell.</summary>
    [Parameter, EditorRequired]
    public DateTimeOffset Value { get; set; }

    /// <summary>Time zone of the absolute date in the tooltip; the local zone by default.</summary>
    [Parameter]
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>Format of the absolute date in the tooltip; the culture's full date and time when empty.</summary>
    [Parameter]
    public string? Format { get; set; }

    /// <summary>
    /// How often the label is redrawn; never when null or not positive. A minute suits most lists.
    /// </summary>
    [Parameter]
    public TimeSpan? RefreshInterval { get; set; }

    private string TooltipId => Id is null ? _tooltipId : $"{Id}-tooltip";

    private DateTimeOffset Reference => Clock.GetUtcNow();

    private string MachineText => Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private string AbsoluteText => TimeZoneInfo.ConvertTime(Value, TimeZone)
        .ToString(string.IsNullOrWhiteSpace(Format) ? "F" : Format, CultureInfo.CurrentCulture);

    private string RelativeText
    {
        get
        {
            var span = Reference - Value;
            var distance = span.Duration();
            if (distance < TimeSpan.FromSeconds(5))
            {
                return Localize("RelativeTimeNow");
            }

            // A moment to come reads its unit from keys of its own: languages such as Finnish or Estonian
            // inflect the unit differently after "in" than after "ago".
            var future = span < TimeSpan.Zero;
            var amount = Measure(distance, out var unitKeys);
            return Localize(future ? "RelativeTimeFuture" : "RelativeTimePast", Localize(future ? unitKeys.Future : unitKeys.Past, amount));
        }
    }

    /// <summary>
    /// The largest unit the distance holds at least once, rounded down, and the resource keys of that
    /// unit for a past and for a future moment.
    /// </summary>
    private static int Measure(TimeSpan distance, out (string Past, string Future) unitKeys)
    {
        int amount;
        if (distance < TimeSpan.FromMinutes(1))
        {
            unitKeys = ("RelativeTimeSeconds", "RelativeTimeFutureSeconds");
            return (int)distance.TotalSeconds;
        }

        if (distance < TimeSpan.FromHours(1))
        {
            unitKeys = ("RelativeTimeMinutes", "RelativeTimeFutureMinutes");
            return (int)distance.TotalMinutes;
        }

        if (distance < TimeSpan.FromDays(1))
        {
            unitKeys = ("RelativeTimeHours", "RelativeTimeFutureHours");
            return (int)distance.TotalHours;
        }

        if (distance < TimeSpan.FromDays(30))
        {
            amount = (int)distance.TotalDays;
            unitKeys = amount == 1 ? ("RelativeTimeDay", "RelativeTimeFutureDay") : ("RelativeTimeDays", "RelativeTimeFutureDays");
            return amount;
        }

        if (distance < TimeSpan.FromDays(365))
        {
            amount = (int)(distance.TotalDays / 30);
            unitKeys = amount == 1 ? ("RelativeTimeMonth", "RelativeTimeFutureMonth") : ("RelativeTimeMonths", "RelativeTimeFutureMonths");
            return amount;
        }

        amount = (int)(distance.TotalDays / 365);
        unitKeys = amount == 1 ? ("RelativeTimeYear", "RelativeTimeFutureYear") : ("RelativeTimeYears", "RelativeTimeFutureYears");
        return amount;
    }

    /// <summary>Checks the parameters.</summary>
    /// <exception cref="ArgumentNullException"><see cref="TimeZone"/> is null.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(TimeZone);
    }

    /// <summary>
    /// The timer starts after a render, never during a prerender that has no interactivity to redraw
    /// into, and follows the parameters: a new interval or clock replaces it.
    /// </summary>
    protected override void OnAfterRender(bool firstRender)
    {
        var interval = RefreshInterval is { } requested && requested > TimeSpan.Zero
            ? requested
            : TimeSpan.Zero;

        if (interval == TimeSpan.Zero)
        {
            StopTimer();
            return;
        }

        if (_timer is not null && interval == _timerInterval && ReferenceEquals(_timerProvider, Clock))
        {
            return;
        }

        StopTimer();
        _timerInterval = interval;
        var clock = Clock;
        _timerProvider = clock;
        _timer = clock.CreateTimer(_ => _ = InvokeAsync(StateHasChanged), null, interval, interval);
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
        _timerInterval = TimeSpan.Zero;
        _timerProvider = null;
    }

    /// <summary>Stops the refresh timer.</summary>
    public void Dispose()
    {
        StopTimer();
        GC.SuppressFinalize(this);
    }
}
