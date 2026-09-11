using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

public sealed class NotificationTests : OmniBunitContext
{
    [Theory]
    [InlineData(300, 5, 5)]
    [InlineData(301, 5, 8)]
    [InlineData(350, 5, 8)]
    [InlineData(1000, 5, 14)]
    [InlineData(1000, 20, 20)]
    [InlineData(4000, 5, 30)]
    public void Store_LongMessage_LivesLongEnoughToBeRead(int length, double requestedSeconds, double expectedSeconds)
    {
        var life = OmniNotificationStore.LifeFor(new string('a', length), TimeSpan.FromSeconds(requestedSeconds));

        Assert.Equal(expectedSeconds, life.TotalSeconds, 3);
    }

    [Fact]
    public void Store_MessageThatStaysUntilDismissed_Stays()
    {
        Assert.Equal(TimeSpan.Zero, OmniNotificationStore.LifeFor(new string('a', 4000), TimeSpan.Zero));
    }

    [Fact]
    public void Store_HeldNotification_OutlivesItsDurationAndClosesOnceReleased()
    {
        var clock = new ManualTimeProvider();
        using var store = new OmniNotificationStore(clock, () => { }, 5, TimeSpan.FromSeconds(5));
        var id = store.Add("Short", OmniNotificationSeverity.Information, null, null);

        clock.Advance(TimeSpan.FromSeconds(2));
        store.Pause(id);
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Single(store.Messages);

        store.Resume(id);
        clock.Advance(TimeSpan.FromSeconds(2.5));
        Assert.Single(store.Messages);

        clock.Advance(TimeSpan.FromSeconds(1));
        // The expiry resumes on the thread pool once its delay completes.
        Assert.True(SpinWait.SpinUntil(() => store.Messages.Count == 0, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Service_DetailsLink_IsCarriedAndHeldToTheUriPolicy()
    {
        using var service = new OmniOverlayService();

        service.Notify("Rapport", OmniNotificationSeverity.Information, null, null, "/journal");

        Assert.Equal("/journal", Assert.Single(service.Notifications).DetailsHref);
        Assert.Throws<InvalidOperationException>(() => service.Notify("Rapport", OmniNotificationSeverity.Information, null, null, "javascript:alert(1)"));
    }

    [Fact]
    public void Notification_Tint_SpellsItsLifeInTensAndUnits()
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Saved")
            .Add(component => component.ShowCountdown, true)
            .Add(component => component.Duration, TimeSpan.FromSeconds(27)));

        var classes = notification.Find("article").ClassList;
        Assert.Contains("omni-notification--tinted", classes);
        Assert.Contains("omni-notification--t2", classes);
        Assert.Contains("omni-notification--u7", classes);
    }

    [Fact]
    public void Notification_WithoutTint_KeepsTheSurface()
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Saved")
            .Add(component => component.ShowCountdown, false)
            .Add(component => component.Duration, TimeSpan.FromSeconds(7)));

        Assert.DoesNotContain("omni-notification--tinted", notification.Find("article").ClassList);
    }

    [Theory]
    [InlineData(OmniNotificationSeverity.Success, "omni-notification--success")]
    [InlineData(OmniNotificationSeverity.Warning, "omni-notification--warning")]
    [InlineData(OmniNotificationSeverity.Error, "omni-notification--error")]
    [InlineData(OmniNotificationSeverity.Information, "omni-notification--information")]
    public void Notification_CarriesItsRoleIconAndClass(OmniNotificationSeverity severity, string expectedClass)
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Saved")
            .Add(component => component.Severity, severity));

        Assert.Contains(expectedClass, notification.Find("article").ClassList);
        Assert.NotNull(notification.Find("article > svg.omni-notification__icon"));
    }

    [Fact]
    public void Notification_LongMessage_FoldsAndUnfolds()
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, new string('a', 301)));

        Assert.Contains("omni-notification--long", notification.Find("article").ClassList);
        Assert.Equal("true", notification.Find(".omni-notification__message").GetAttribute("data-collapsed"));
        var toggle = notification.Find(".omni-notification__more");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));

        toggle.Click();

        Assert.Null(notification.Find(".omni-notification__message").GetAttribute("data-collapsed"));
        Assert.Equal("true", notification.Find(".omni-notification__more").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("style=", notification.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Notification_ShortMessage_HasNothingToFold()
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, new string('a', 300)));

        Assert.DoesNotContain("omni-notification--long", notification.Find("article").ClassList);
        Assert.Empty(notification.FindAll(".omni-notification__more"));
    }

    [Theory]
    [InlineData(2000, false)]
    [InlineData(2001, true)]
    public void Notification_DetailsLink_OnlyForAReport(int length, bool offered)
    {
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, new string('a', length))
            .Add(component => component.DetailsHref, "/journal"));

        Assert.Equal(offered, notification.FindAll("a.omni-notification__details").Count == 1);
    }

    [Fact]
    public void Notification_ReportsBeingHeldOnceWhilePointerAndFocusOverlap()
    {
        var changes = new List<bool>();
        var notification = Render<OmniNotification>(parameters => parameters
            .Add(component => component.Message, "Saved")
            .Add(component => component.OnHeldChanged, (bool held) => changes.Add(held)));
        var article = notification.Find("article");

        article.MouseEnter();
        article.FocusIn();
        article.MouseLeave();
        Assert.Equal([true], changes);

        article.FocusOut();
        Assert.Equal([true, false], changes);
    }

    /// <summary>A clock the test moves by hand, firing the timers it owes on each step.</summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state, _now + dueTime);
            _timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
            foreach (var timer in _timers.Where(timer => !timer.Disposed && timer.Due <= _now).ToList())
            {
                timer.Fire();
            }
        }

        private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state, DateTimeOffset due) : ITimer
        {
            public DateTimeOffset Due { get; private set; } = due;

            public bool Disposed { get; private set; }

            public void Fire()
            {
                Disposed = true;
                callback(state);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                Due = owner._now + dueTime;
                return true;
            }

            public void Dispose() => Disposed = true;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
