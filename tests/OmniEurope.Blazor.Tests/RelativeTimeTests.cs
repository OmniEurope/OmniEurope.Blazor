using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Tests;

public sealed class RelativeTimeTests : OmniBunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 10, 30, 0, TimeSpan.Zero);

    private readonly ManualTimeProvider _clock = new(Now);

    public RelativeTimeTests() => Services.AddSingleton<TimeProvider>(_clock);

    [Theory]
    [InlineData(-3, "à l'instant")]
    [InlineData(-45, "il y a 45 s")]
    [InlineData(-150, "il y a 2 min")]
    [InlineData(-3 * 3600, "il y a 3 h")]
    [InlineData(-26 * 3600, "il y a 1 jour")]
    [InlineData(-3 * 86400, "il y a 3 jours")]
    [InlineData(-40 * 86400, "il y a 1 mois")]
    [InlineData(-100 * 86400, "il y a 3 mois")]
    [InlineData(-400 * 86400, "il y a 1 an")]
    [InlineData(-800 * 86400, "il y a 2 ans")]
    [InlineData(300, "dans 5 min")]
    [InlineData(3 * 86400, "dans 3 jours")]
    public void Label_TellsTheLargestWholeUnitInFrench(int offsetSeconds, string expected)
    {
        var time = RenderAt(Now.AddSeconds(offsetSeconds));

        Assert.Equal(expected, time.Find("time").TextContent);
    }

    [Theory]
    [InlineData(-3, "just now")]
    [InlineData(-150, "2 min ago")]
    [InlineData(-26 * 3600, "1 day ago")]
    [InlineData(-100 * 86400, "3 months ago")]
    [InlineData(-800 * 86400, "2 years ago")]
    [InlineData(3 * 86400, "in 3 days")]
    public void Label_FollowsTheUiCulture(int offsetSeconds, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var time = RenderAt(Now.AddSeconds(offsetSeconds));

            Assert.Equal(expected, time.Find("time").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Element_CarriesTheExactInstantAndAFocusableTooltipWithTheAbsoluteDate()
    {
        var value = new DateTimeOffset(2026, 9, 14, 10, 28, 0, TimeSpan.FromHours(2));
        var time = Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.Format, "yyyy-MM-dd HH:mm"));

        Assert.Equal("2026-09-14T08:28:00Z", time.Find("time").GetAttribute("datetime"));
        var trigger = time.Find(".omni-tooltip__trigger");
        Assert.Equal("0", trigger.GetAttribute("tabindex"));
        var tooltip = time.Find("[role=tooltip]");
        Assert.Equal(tooltip.Id, trigger.GetAttribute("aria-describedby"));
        Assert.Equal("2026-09-14 08:28", tooltip.TextContent);
        Assert.Equal("il y a 2 h", time.Find("time").TextContent);
        Assert.DoesNotContain("style=", time.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(45, "Seconds")]
    [InlineData(150, "Minutes")]
    [InlineData(3 * 3600, "Hours")]
    [InlineData(26 * 3600, "Day")]
    [InlineData(3 * 86400, "Days")]
    [InlineData(40 * 86400, "Month")]
    [InlineData(100 * 86400, "Months")]
    [InlineData(400 * 86400, "Year")]
    [InlineData(800 * 86400, "Years")]
    public void Future_ReadsItsUnitFromTheFutureKeys_AndThePastFromThePastKeys(int offsetSeconds, string unit)
    {
        // Finnish or Estonian inflect the unit after "in" differently than after "ago": each direction
        // must read a key of its own, which this localizer tells apart.
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new UnitMarkingLocalizer());

        Assert.Equal($"in [future {unit}]", RenderAt(Now.AddSeconds(offsetSeconds)).Find("time").TextContent);
        Assert.Equal($"[past {unit}] ago", RenderAt(Now.AddSeconds(-offsetSeconds)).Find("time").TextContent);
    }

    [Fact]
    public void TwoInstances_NeverShareATooltipId()
    {
        var first = RenderAt(Now);
        var second = RenderAt(Now);

        Assert.NotEqual(first.Find("[role=tooltip]").Id, second.Find("[role=tooltip]").Id);
    }

    [Fact]
    public async Task ReadsTheRegisteredTimeProvider_AndRefreshesOnItsOwnTimerUntilDisposed()
    {
        var clock = _clock;
        var time = Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, Now.AddSeconds(-90))
            .Add(component => component.RefreshInterval, TimeSpan.FromSeconds(30)));

        Assert.Equal("il y a 1 min", time.Find("time").TextContent);
        var timer = Assert.Single(clock.Timers);
        Assert.Equal(TimeSpan.FromSeconds(30), timer.Period);

        clock.Now = Now.AddSeconds(60);
        await time.InvokeAsync(timer.Fire);
        time.WaitForAssertion(() => Assert.Equal("il y a 2 min", time.Find("time").TextContent));

        await DisposeComponentsAsync();
        Assert.True(timer.Disposed);
    }

    [Fact]
    public void NoInterval_RunsNoTimer()
    {
        Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, Now));
        Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, Now)
            .Add(component => component.RefreshInterval, TimeSpan.Zero));

        Assert.Empty(_clock.Timers);
    }

    private IRenderedComponent<OmniRelativeTime> RenderAt(DateTimeOffset value) =>
        Render<OmniRelativeTime>(parameters => parameters.Add(component => component.Value, value));

    /// <summary>
    /// Writes "in {0}" and "{0} ago" for the two directions, and each unit key as "[future Unit]" or
    /// "[past Unit]", so a test sees which key a label read.
    /// </summary>
    private sealed class UnitMarkingLocalizer : IStringLocalizer<AppStrings>
    {
        // Like a real localizer: the text as stored without arguments, formatted with them.
        public LocalizedString this[string name] => new(name, Stored(name));

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.InvariantCulture, Stored(name), arguments));

        private static string Stored(string name) => name switch
        {
            "RelativeTimeFuture" => "in {0}",
            "RelativeTimePast" => "{0} ago",
            _ when name.StartsWith("RelativeTimeFuture", StringComparison.Ordinal) => $"[future {name["RelativeTimeFuture".Length..]}]",
            _ when name.StartsWith("RelativeTime", StringComparison.Ordinal) => $"[past {name["RelativeTime".Length..]}]",
            _ => name
        };

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public List<ManualTimer> Timers { get; } = [];

        public override DateTimeOffset GetUtcNow() => Now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, period);
            Timers.Add(timer);
            return timer;
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan period) : ITimer
    {
        public TimeSpan Period { get; } = period;

        public bool Disposed { get; private set; }

        public void Fire() => callback(state);

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
