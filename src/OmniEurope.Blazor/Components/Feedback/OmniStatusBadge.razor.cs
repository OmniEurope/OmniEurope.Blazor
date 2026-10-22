namespace OmniEurope.Blazor.Components;

/// <summary>
/// The badge of a status value: its colour, label, icon and explanation come from an
/// <see cref="OmniStatusMap{TValue}"/> the host builds once per status type. Given a
/// <see cref="Timestamp"/> and a <see cref="StaleAfter"/> threshold, it also says when the value is
/// stale, and turns stale on its own when the threshold passes while it is on screen.
/// </summary>
/// <typeparam name="TValue">The status type; a nullable type lets a missing value be drawn.</typeparam>
/// <remarks>
/// The component keeps no clock of its own: now is the host's registered <see cref="TimeProvider"/>
/// (the system clock when it registers none), whose timer also redraws the badge at the moment it
/// turns stale.
/// </remarks>
public partial class OmniStatusBadge<TValue> : IDisposable
{
    // A system timer cannot wait longer than about 49.7 days: a longer wait is cut and re-armed when the cut elapses.
    private static readonly TimeSpan MaxTimerDelay = TimeSpan.FromDays(40);

    private ITimer? _timer;
    private DateTimeOffset _timerDue;
    private TimeProvider? _timerProvider;

    /// <summary>The value to draw.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>The table giving each value its badge.</summary>
    [Parameter, EditorRequired]
    public OmniStatusMap<TValue> Map { get; set; } = default!;

    /// <summary>Whether the status icon is drawn before the label.</summary>
    [Parameter]
    public bool ShowIcon { get; set; } = true;

    /// <summary>
    /// A common minimum width for the badge, as <see cref="OmniBadge.Width"/>: the same step on every
    /// status badge of a column makes "Failed", "Cancelled" and "Success" the same width. <see cref="OmniBadgeWidth.Auto"/>
    /// by default.
    /// </summary>
    [Parameter]
    public OmniBadgeWidth Width { get; set; } = OmniBadgeWidth.Auto;

    /// <summary>When the value was last known to be true. Without it, the badge is never stale.</summary>
    [Parameter]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>How old <see cref="Timestamp"/> may grow before the value reads as stale. Without it, never.</summary>
    [Parameter]
    public TimeSpan? StaleAfter { get; set; }

    /// <summary>
    /// The time zone <see cref="Timestamp"/> is shown in, in the "last updated" explanation. The local zone
    /// of the host's <see cref="TimeProvider"/> when null.
    /// </summary>
    [Parameter]
    public TimeZoneInfo? TimeZone { get; set; }

    /// <summary>The word shown beside a stale badge; the localized "stale" when empty.</summary>
    [Parameter]
    public string? StaleText { get; set; }

    private OmniStatus Status => Map.Resolve(Value);

    private string LabelText => Map.Localize(Status.Text);

    private string? DescriptionText => string.IsNullOrWhiteSpace(Status.Description) ? null : Map.Localize(Status.Description);

    private string EffectiveStaleText => LocalizeOr(StaleText, "StatusBadgeStale");

    private DateTimeOffset Reference => Clock.GetUtcNow();

    private string? TimestampText => Timestamp is { } timestamp
        ? Localize("StatusBadgeStaleSince", TimeZoneInfo.ConvertTime(timestamp, TimeZone ?? Clock.LocalTimeZone).ToString("g", CultureInfo.CurrentCulture))
        : null;

    /// <summary>Whether the value is older than <see cref="StaleAfter"/>.</summary>
    public bool IsStale => StaleDue is { } due && Reference >= due;

    // A threshold too long for the date to be represented (TimeSpan.MaxValue) is a value that never grows stale.
    private DateTimeOffset? StaleDue => Timestamp is { } timestamp && StaleAfter is { } threshold
        && threshold >= TimeSpan.Zero && threshold <= DateTimeOffset.MaxValue - timestamp
        ? timestamp + threshold
        : null;

    /// <summary>Refuses a missing <see cref="Map"/>.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Map);
    }

    /// <summary>
    /// A fresh value read against the live clock gets a one-shot timer set at the moment it turns
    /// stale, so a list left open does not keep showing yesterday's state as current.
    /// </summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (StaleDue is not { } due || IsStale)
        {
            StopTimer();
            return;
        }

        if (_timer is not null && due == _timerDue && ReferenceEquals(_timerProvider, Clock))
        {
            return;
        }

        StopTimer();
        _timerDue = due;
        var clock = Clock;
        _timerProvider = clock;
        var delay = due - clock.GetUtcNow();
        var wait = delay < TimeSpan.Zero ? TimeSpan.Zero : delay > MaxTimerDelay ? MaxTimerDelay : delay;
        _timer = clock.CreateTimer(_ => _ = InvokeAsync(Elapsed), null, wait, Timeout.InfiniteTimeSpan);
    }

    // The timer is one-shot: it is dropped, so that the render it causes arms the next one when the
    // badge is still fresh (a wait that was cut short) and none when it just turned stale.
    private void Elapsed()
    {
        StopTimer();
        StateHasChanged();
    }

    private void StopTimer()
    {
        _timer?.Dispose();
        _timer = null;
        _timerProvider = null;
    }

    /// <summary>Stops the stale timer.</summary>
    public void Dispose()
    {
        StopTimer();
        GC.SuppressFinalize(this);
    }
}
