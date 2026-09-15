namespace OmniEurope.Blazor.Components;

/// <summary>
/// The badge of a status value: its colour, label, icon and explanation come from an
/// <see cref="OmniStatusMap{TValue}"/> the host builds once per status type. Given a
/// <see cref="Timestamp"/> and a <see cref="StaleAfter"/> threshold, it also says when the value is
/// stale, and turns stale on its own when the threshold passes while it is on screen.
/// </summary>
/// <typeparam name="TValue">The status type; a nullable type lets a missing value be drawn.</typeparam>
/// <remarks>
/// The component keeps no clock of its own: now is <see cref="Now"/> when given, else
/// <see cref="TimeProvider"/>, whose timer also redraws the badge at the moment it turns stale.
/// </remarks>
public partial class OmniStatusBadge<TValue> : IDisposable
{
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

    /// <summary>When the value was last known to be true. Without it, the badge is never stale.</summary>
    [Parameter]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>How old <see cref="Timestamp"/> may grow before the value reads as stale. Without it, never.</summary>
    [Parameter]
    public TimeSpan? StaleAfter { get; set; }

    /// <summary>The reference instant; <see cref="TimeProvider"/>'s current time when null.</summary>
    [Parameter]
    public DateTimeOffset? Now { get; set; }

    /// <summary>The clock read when <see cref="Now"/> is null, and the one the stale timer runs on.</summary>
    [Parameter]
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>The word shown beside a stale badge; the localized "stale" when empty.</summary>
    [Parameter]
    public string? StaleText { get; set; }

    private OmniStatus Status => Map.Resolve(Value);

    private string LabelText => Map.Localize(Status.Text);

    private string? DescriptionText => string.IsNullOrWhiteSpace(Status.Description) ? null : Map.Localize(Status.Description);

    private string EffectiveStaleText => string.IsNullOrWhiteSpace(StaleText) ? Localize("StatusBadgeStale") : StaleText;

    private DateTimeOffset Reference => Now ?? TimeProvider.GetUtcNow();

    private string? TimestampText => Timestamp is { } timestamp
        ? Localize("StatusBadgeStaleSince", TimeZoneInfo.ConvertTime(timestamp, TimeZoneInfo.Local).ToString("g", CultureInfo.CurrentCulture))
        : null;

    /// <summary>Whether the value is older than <see cref="StaleAfter"/>.</summary>
    public bool IsStale => StaleDue is { } due && Reference >= due;

    private DateTimeOffset? StaleDue => Timestamp is { } timestamp && StaleAfter is { } threshold && threshold >= TimeSpan.Zero
        ? timestamp + threshold
        : null;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(Map);
        ArgumentNullException.ThrowIfNull(TimeProvider);
    }

    /// <summary>
    /// A fresh value read against the live clock gets a one-shot timer set at the moment it turns
    /// stale, so a list left open does not keep showing yesterday's state as current. A pinned
    /// <see cref="Now"/> never moves and runs no timer.
    /// </summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (Now is not null || StaleDue is not { } due || IsStale)
        {
            StopTimer();
            return;
        }

        if (_timer is not null && due == _timerDue && ReferenceEquals(_timerProvider, TimeProvider))
        {
            return;
        }

        StopTimer();
        _timerDue = due;
        _timerProvider = TimeProvider;
        var delay = due - TimeProvider.GetUtcNow();
        _timer = TimeProvider.CreateTimer(_ => _ = InvokeAsync(StateHasChanged), null, delay < TimeSpan.Zero ? TimeSpan.Zero : delay, Timeout.InfiniteTimeSpan);
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
