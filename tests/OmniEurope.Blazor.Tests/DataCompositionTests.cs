using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Lot 8 data compositions: dynamic form and status badge.</summary>
public sealed class DataCompositionTests : OmniBunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);

    // ---- OmniDynamicForm -------------------------------------------------------------------------

    [Fact]
    public void DynamicForm_DrawsEachKindWithItsLabelMarkerHelpAndDefault()
    {
        var host = Render<DynamicFormTestHost>();

        Assert.Equal("text", host.Find("#form-0-input").GetAttribute("type"));
        Assert.Equal("TEXTAREA", host.Find("#form-1-input").TagName);
        Assert.Equal("3", host.Find("#form-1-input").GetAttribute("rows"));
        var number = host.Find("#form-2-input");
        Assert.Equal("number", number.GetAttribute("type"));
        Assert.Equal("1", number.GetAttribute("min"));
        Assert.Equal("5", number.GetAttribute("max"));
        Assert.Equal("switch", host.Find("#form-3-input").GetAttribute("role"));
        Assert.Equal(["Europe ouest", "Europe centre"], host.FindAll("#form-4-input option").Skip(1).Select(option => option.TextContent));

        // Required marker, help text wired to the control.
        Assert.Single(host.FindAll("label[for='form-0-input'] .omni-label__required"));
        Assert.Equal("true", host.Find("#form-0-input").GetAttribute("aria-required"));
        Assert.Equal("form-0-description", host.Find("#form-0-input").GetAttribute("aria-describedby"));
        Assert.Equal("Le nom affiché.", host.Find("#form-0-description").TextContent);

        // The default is shown and reported to the host once.
        Assert.Equal("2", number.GetAttribute("value"));
        Assert.Equal("2", host.Instance.Values!["replicas"]);
        Assert.False(host.Instance.Values!.ContainsKey("confirm"));
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DynamicForm_Changes_FlowIntoTheBoundValuesAsStrings()
    {
        var host = Render<DynamicFormTestHost>();

        host.Find("#form-0-input").Input("srv-01");
        host.Find("#form-2-input").Change("3.5");
        host.Find("#form-3-input").Click();
        host.Find("#form-4-input").Change("1");

        var values = host.Instance.Values!;
        Assert.Equal("srv-01", values["name"]);
        Assert.Equal("3.5", values["replicas"]);
        Assert.Equal("true", values["confirm"]);
        Assert.Equal("eu-central", values["region"]);
        Assert.False(values.ContainsKey("notes"));
    }

    [Fact]
    public async Task DynamicForm_Validate_ShowsEveryMessageAndMarksTheControlsInvalid()
    {
        var host = Render<DynamicFormTestHost>();

        var valid = true;
        await host.InvokeAsync(() => valid = host.Instance.Form!.Validate());

        Assert.False(valid);
        Assert.Equal("Nom est obligatoire.", host.Find("#form-0-error").TextContent);
        Assert.Equal("true", host.Find("#form-0-input").GetAttribute("aria-invalid"));
        Assert.Equal("form-0-description form-0-error", host.Find("#form-0-input").GetAttribute("aria-describedby"));
        // An unanswered required switch is not answered "no" by default.
        Assert.Equal("Confirmer est obligatoire.", host.Find("#form-3-error").TextContent);
        Assert.Equal("Région est obligatoire.", host.Find("#form-4-error").TextContent);
        Assert.Empty(host.FindAll("#form-2-error"));

        host.Find("#form-0-input").Input("srv");
        host.Find("#form-3-input").Click();
        host.Find("#form-3-input").Click();
        host.Find("#form-4-input").Change("0");
        Assert.Empty(host.FindAll(".omni-form-field__error"));
        Assert.Equal("false", host.Instance.Values!["confirm"]);
        Assert.True(host.Instance.Form!.IsValid);
    }

    [Fact]
    public void DynamicForm_NumberOutsideItsBounds_SaysWhichBound()
    {
        var host = Render<DynamicFormTestHost>();

        host.Find("#form-2-input").Change("9");
        Assert.Equal("Répliques doit valoir au plus 5.", host.Find("#form-2-error").TextContent);

        host.Find("#form-2-input").Change("0");
        Assert.Equal("Répliques doit valoir au moins 1.", host.Find("#form-2-error").TextContent);
    }

    [Fact]
    public void DynamicForm_InsideAnEditForm_BlocksTheSubmitWhileAFieldIsInvalid()
    {
        var host = Render<DynamicFormTestHost>(parameters => parameters.Add(component => component.InEditForm, true));

        host.Find("form").Submit();
        Assert.Equal(0, host.Instance.ValidSubmits);
        Assert.Equal(1, host.Instance.InvalidSubmits);
        Assert.Equal("Nom est obligatoire.", host.Find("#form-0-error").TextContent);

        host.Find("#form-0-input").Input("srv");
        host.Find("#form-3-input").Click();
        host.Find("#form-4-input").Change("0");
        host.Find("form").Submit();
        Assert.Equal(1, host.Instance.ValidSubmits);
    }

    [Fact]
    public async Task DynamicForm_HostErrors_ShowUntilTheFieldChanges()
    {
        var host = Render<DynamicFormTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Errors = new Dictionary<string, string> { ["name"] = "Nom déjà pris." }));
        Assert.Equal("Nom déjà pris.", host.Find("#form-0-error").TextContent);

        host.Find("#form-0-input").Input("autre");
        Assert.Empty(host.FindAll("#form-0-error"));
    }

    [Fact]
    public async Task DynamicForm_ValuesFromTheHost_FillTheControls()
    {
        var host = Render<DynamicFormTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Values = new Dictionary<string, string>
        {
            ["name"] = "db-02", ["replicas"] = "4", ["confirm"] = "true", ["region"] = "eu-west"
        }));

        Assert.Equal("db-02", host.Find("#form-0-input").GetAttribute("value"));
        Assert.Equal("4", host.Find("#form-2-input").GetAttribute("value"));
        Assert.Equal("true", host.Find("#form-3-input").GetAttribute("aria-checked"));
        Assert.True(host.Instance.Form!.IsValid);
    }

    // ---- OmniStatusBadge -------------------------------------------------------------------------

    private enum RunState
    {
        Succeeded,
        Failed,
        Queued,
        Unknown
    }

    private static readonly OmniStatusMap<RunState?> RunStates = new()
    {
        { RunState.Succeeded, OmniTone.Success, "Réussi", OmniIconName.CheckCircle },
        { RunState.Failed, new OmniStatus(OmniTone.Danger, "Échoué") { Icon = OmniIconName.Error, Description = "Une étape a échoué." } },
        { RunState.Queued, new OmniStatus(OmniTone.Neutral, "En file") { Fill = OmniFill.Outline } }
    };

    [Fact]
    public void StatusBadge_DrawsTheMappedVariantLabelIconAndExplanation()
    {
        var badge = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Failed)
            .Add(component => component.Map, RunStates));

        var inner = badge.Find(".omni-badge");
        Assert.Contains("omni-badge--danger", inner.ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-badge--tonal", inner.ClassName, StringComparison.Ordinal);
        Assert.Equal("Échoué", badge.Find(".omni-status-badge__text").TextContent);
        Assert.Single(badge.FindAll(".omni-status-badge__icon"));
        Assert.Equal("Une étape a échoué.", badge.Find(".omni-status-badge").GetAttribute("title"));
        Assert.Empty(badge.FindAll(".omni-status-badge__stale"));
    }

    [Fact]
    public void StatusBadge_FallsBackForAnUnmappedValueAndDrawsADashForNull()
    {
        var unmapped = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Unknown)
            .Add(component => component.Map, RunStates));
        Assert.Equal("Unknown", unmapped.Find(".omni-status-badge__text").TextContent);
        Assert.Contains("omni-badge--neutral", unmapped.Find(".omni-badge").ClassName, StringComparison.Ordinal);

        var missing = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.Map, RunStates));
        Assert.Equal("-", missing.Find(".omni-status-badge__text").TextContent);

        var custom = new OmniStatusMap<string> { Fallback = new OmniStatus(OmniTone.Warning, "Autre") };
        Assert.Equal("Autre", custom.Resolve("x").Text);
        Assert.Contains("omni-badge--outline", Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Queued)
            .Add(component => component.Map, RunStates)).Find(".omni-badge").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void StatusMap_WithALocalizer_ReadsTheTextsAsResourceKeys()
    {
        var map = new OmniStatusMap<int> { Localizer = new KeyLocalizer() };
        map.Add(1, OmniTone.Accent, "State_Running");

        var badge = Render<OmniStatusBadge<int>>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.Map, map));

        Assert.Equal("[State_Running]", badge.Find(".omni-status-badge__text").TextContent);
    }

    [Fact]
    public void StatusBadge_OlderThanItsThreshold_ReadsStale()
    {
        Services.AddSingleton<TimeProvider>(new ManualClock(Now));
        var badge = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Succeeded)
            .Add(component => component.Map, RunStates)
            .Add(component => component.Timestamp, Now.AddMinutes(-20))
            .Add(component => component.StaleAfter, TimeSpan.FromMinutes(15)));

        Assert.True(badge.Instance.IsStale);
        Assert.Contains("omni-status-badge--stale", badge.Find(".omni-status-badge").ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-badge--outline", badge.Find(".omni-badge").ClassName, StringComparison.Ordinal);
        Assert.Equal("périmé", badge.Find(".omni-status-badge__stale").TextContent.Trim());
        Assert.StartsWith("Dernière mise à jour : ", badge.Find(".omni-status-badge__stale").GetAttribute("title"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusBadge_TurnsStaleOnItsOwnWhenTheThresholdPasses()
    {
        var clock = new ManualClock(Now);
        Services.AddSingleton<TimeProvider>(clock);
        var badge = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Succeeded)
            .Add(component => component.Map, RunStates)
            .Add(component => component.Timestamp, Now.AddMinutes(-10))
            .Add(component => component.StaleAfter, TimeSpan.FromMinutes(15))
            .Add(component => component.StaleText, "ancien"));

        Assert.Empty(badge.FindAll(".omni-status-badge__stale"));
        Assert.Single(clock.Pending);

        await badge.InvokeAsync(() => clock.Advance(TimeSpan.FromMinutes(6)));

        badge.WaitForAssertion(() => Assert.Equal("ancien", badge.Find(".omni-status-badge__stale").TextContent.Trim()));
        Assert.Empty(clock.Pending);

        badge.Instance.Dispose();
    }

    private sealed class KeyLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, $"[{name}]");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    /// <summary>A clock moved by hand that fires the timers falling due.</summary>
    internal sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private readonly List<Timer> _timers = [];
        private DateTimeOffset _now = now;

        public IReadOnlyList<Timer> Pending => [.. _timers.Where(timer => !timer.Disposed)];

        public override DateTimeOffset GetUtcNow() => _now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new Timer(this, callback, state, _now + dueTime);
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

        internal sealed class Timer(ManualClock owner, TimerCallback callback, object? state, DateTimeOffset due) : ITimer
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
                Disposed = dueTime == Timeout.InfiniteTimeSpan;
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
