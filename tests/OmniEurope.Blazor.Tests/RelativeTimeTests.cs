using System.Globalization;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class RelativeTimeTests : OmniBunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 10, 30, 0, TimeSpan.Zero);

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
            .Add(component => component.Now, Now)
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

    [Fact]
    public void TwoInstances_NeverShareATooltipId()
    {
        var first = RenderAt(Now);
        var second = RenderAt(Now);

        Assert.NotEqual(first.Find("[role=tooltip]").Id, second.Find("[role=tooltip]").Id);
    }

    [Fact]
    public async Task WithoutNow_ReadsTheTimeProvider_AndRefreshesOnItsOwnTimerUntilDisposed()
    {
        var clock = new ManualTimeProvider(Now);
        var time = Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, Now.AddSeconds(-90))
            .Add(component => component.TimeProvider, clock)
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
    public void PinnedNowOrNoInterval_RunsNoTimer()
    {
        var clock = new ManualTimeProvider(Now);
        Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, Now)
            .Add(component => component.Now, Now)
            .Add(component => component.TimeProvider, clock)
            .Add(component => component.RefreshInterval, TimeSpan.FromSeconds(30)));
        Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, Now)
            .Add(component => component.TimeProvider, clock));

        Assert.Empty(clock.Timers);
    }

    private IRenderedComponent<OmniRelativeTime> RenderAt(DateTimeOffset value) =>
        Render<OmniRelativeTime>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.Now, Now));

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
