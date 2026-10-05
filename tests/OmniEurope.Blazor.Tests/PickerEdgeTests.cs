using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The date, time and date and time pickers at their edges: the panel toggled shut, text typed while it is
/// open, blank text, one bound alone, crossed bounds, a time brought back within its bounds, the month
/// steps and grid keys of the date and time panel, a picker without id, and a release without disposing.
/// </summary>
public sealed class PickerEdgeTests : OmniBunitContext
{
    private readonly DateOnly? _date = null;

    private readonly TimeOnly? _time = null;

    private readonly DateTime? _moment = null;

    public PickerEdgeTests()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<TimeProvider>(new FixedClock(new DateTimeOffset(2026, 9, 18, 14, 33, 20, TimeSpan.Zero)));
    }

    private IRenderedComponent<PickerTestHost> RenderHost(
        Action<PickerTestHost.PickerTestModel>? arrange = null,
        Action<ComponentParameterCollectionBuilder<PickerTestHost>>? parameters = null)
    {
        var model = new PickerTestHost.PickerTestModel();
        arrange?.Invoke(model);
        return Render<PickerTestHost>(builder =>
        {
            builder.Add(component => component.Model, model);
            parameters?.Invoke(builder);
        });
    }

    private static IElement Toggle(IRenderedComponent<PickerTestHost> host, string id) => host.Find($"#{id} ~ .omni-date__toggle");

    private static string Title(IRenderedComponent<PickerTestHost> host) => host.Find(".omni-calendar__title").TextContent;

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("moment")]
    public void ToggleOfAnOpenPanel_ClosesIt(string id)
    {
        var host = RenderHost();

        Toggle(host, id).Click();
        Assert.NotEmpty(host.FindAll("[role='dialog']"));
        Toggle(host, id).Click();

        Assert.Empty(host.FindAll("[role='dialog']"));
    }

    [Fact]
    public void TextTypedWhileThePanelIsOpen_MovesTheCalendar_AndBlankOrNullTextEmptiesTheField()
    {
        var host = RenderHost(model =>
        {
            model.Date = new DateOnly(2026, 9, 21);
            model.Time = new TimeOnly(9, 0);
            model.Moment = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Unspecified);
        });

        Toggle(host, "date").Click();
        host.Find("#date").Change("15/03/2026");
        Assert.Equal("mars 2026", Title(host));
        Toggle(host, "date").Click();

        Toggle(host, "moment").Click();
        host.Find("#moment").Change("15/04/2026 10:00");
        Assert.Equal("avril 2026", Title(host));
        Toggle(host, "moment").Click();

        host.Find("#date").Change("   ");
        host.Find("#time").Change("   ");
        host.Find("#moment").Change((object?)null);
        host.Find("#time").Change((object?)null);
        host.Find("#date").Change((object?)null);
        Assert.Equal((null, null, null), (host.Instance.Model.Date, host.Instance.Model.Time, host.Instance.Model.Moment));
    }

    [Fact]
    public void DateTimePanel_StepsMonths_AndMovesItsFocusedDayWithTheKeys()
    {
        var host = RenderHost(model => model.Moment = new DateTime(2026, 9, 21, 14, 30, 0, DateTimeKind.Unspecified));
        Toggle(host, "moment").Click();

        host.FindAll(".omni-calendar__step")[1].Click();
        Assert.Equal("octobre 2026", Title(host));

        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("2026-10-22", host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));
    }

    [Fact]
    public void OneBoundAlone_LimitsOnlyItsSide()
    {
        var floorOnly = RenderHost(parameters: parameters => parameters
            .Add(component => component.DateMinimum, new DateOnly(2026, 9, 10))
            .Add(component => component.MomentMinimum, new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Unspecified))
            .Add(component => component.TimeMinimum, new TimeOnly(9, 0)));
        Toggle(floorOnly, "moment").Click();
        Assert.True(floorOnly.Find(".omni-calendar__day[data-date='2026-09-09']").HasAttribute("disabled"));
        Assert.False(floorOnly.Find(".omni-calendar__day[data-date='2026-09-30']").HasAttribute("disabled"));

        var ceilingOnly = RenderHost(parameters: parameters => parameters
            .Add(component => component.DateMaximum, new DateOnly(2026, 9, 25))
            .Add(component => component.MomentMaximum, new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Unspecified))
            .Add(component => component.TimeMaximum, new TimeOnly(17, 0)));
        Toggle(ceilingOnly, "moment").Click();
        Assert.False(ceilingOnly.Find(".omni-calendar__day[data-date='2026-09-01']").HasAttribute("disabled"));
        Assert.True(ceilingOnly.Find(".omni-calendar__day[data-date='2026-09-26']").HasAttribute("disabled"));
    }

    [Fact]
    public void BlankTextWhileThePanelIsOpen_LeavesTheCalendarWhereItIs()
    {
        var host = RenderHost(model =>
        {
            model.Date = new DateOnly(2026, 9, 21);
            model.Moment = new DateTime(2026, 9, 21, 9, 0, 0, DateTimeKind.Unspecified);
        });

        Toggle(host, "date").Click();
        host.Find("#date").Change("  ");
        Assert.Equal("septembre 2026", Title(host));
        Toggle(host, "date").Click();

        Toggle(host, "moment").Click();
        host.Find("#moment").Change("  ");
        Assert.Equal("septembre 2026", Title(host));
        Assert.Null(host.Instance.Model.Moment);
    }

    [Fact]
    public void TypedMomentsAndTimesOutsideTheBounds_AreRefused()
    {
        var host = RenderHost(
            model =>
            {
                model.Time = new TimeOnly(12, 0);
                model.Moment = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Unspecified);
            },
            parameters => parameters
                .Add(component => component.TimeMinimum, new TimeOnly(9, 0))
                .Add(component => component.MomentMinimum, new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Unspecified))
                .Add(component => component.MomentMaximum, new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Unspecified)));

        host.Find("#time").Change("08:00");
        host.Find("#moment").Change("10/09/2026 08:00");
        Assert.Equal("true", host.Find("#moment").GetAttribute("aria-invalid"));
        host.Find("#moment").Change("26/09/2026 08:00");

        Assert.Equal(new TimeOnly(12, 0), host.Instance.Model.Time);
        Assert.Equal(new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);
    }

    [Fact]
    public void DateTimeChoicesBelowTheFloor_AreBroughtUpToIt_AndHoursBeforeItAreLocked()
    {
        var floor = new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Unspecified);
        var host = RenderHost(
            model => model.Moment = new DateTime(2026, 9, 21, 8, 0, 0, DateTimeKind.Unspecified),
            parameters => parameters.Add(component => component.MomentMinimum, floor));
        Toggle(host, "moment").Click();

        host.Find(".omni-calendar__day[data-date='2026-09-10']").Click();

        Assert.Equal(floor, host.Instance.Model.Moment);
        var hours = host.FindAll("[data-omni-part='hour'] [role='option']");
        Assert.True(hours[8].HasAttribute("disabled"));
        Assert.False(hours[9].HasAttribute("disabled"));
    }

    [Fact]
    public void TodayAndNowOutsideTheBounds_CannotBeChosen()
    {
        var host = RenderHost(parameters: parameters => parameters
            .Add(component => component.DateMinimum, new DateOnly(2026, 10, 1))
            .Add(component => component.TimeMinimum, new TimeOnly(15, 0))
            .Add(component => component.MomentMinimum, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Unspecified)));

        foreach (var id in new[] { "date", "time", "moment" })
        {
            Toggle(host, id).Click();
            Assert.True(host.Find(".omni-calendar__foot button").HasAttribute("disabled"));
            Toggle(host, id).Click();
        }
    }

    [Fact]
    public void DateTimeChoiceWithinTheBounds_IsKeptAsChosen()
    {
        var host = RenderHost(
            model => model.Moment = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Unspecified),
            parameters => parameters
                .Add(component => component.MomentMinimum, new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Unspecified))
                .Add(component => component.MomentMaximum, new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Unspecified)));
        Toggle(host, "moment").Click();

        host.Find(".omni-calendar__day[data-date='2026-09-22']").Click();

        Assert.Equal(new DateTime(2026, 9, 22, 10, 0, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);
    }

    [Fact]
    public void CrossedTimeBounds_AreRefused() =>
        Assert.Throws<InvalidOperationException>(() => RenderHost(parameters: parameters => parameters
            .Add(component => component.TimeMinimum, new TimeOnly(18, 0))
            .Add(component => component.TimeMaximum, new TimeOnly(8, 0))));

    [Theory]
    [InlineData(8, "09:00")]
    [InlineData(18, "17:00")]
    public void TimeChosenOutsideTheBounds_IsBroughtBackToTheBound(int hour, string expected)
    {
        var host = RenderHost(
            model => model.Time = new TimeOnly(12, 0),
            parameters => parameters
                .Add(component => component.TimeMinimum, new TimeOnly(9, 0))
                .Add(component => component.TimeMaximum, new TimeOnly(17, 0)));
        Toggle(host, "time").Click();

        host.FindAll("[data-omni-part='hour'] [role='option']")[hour].Click();

        Assert.Equal(TimeOnly.ParseExact(expected, "HH:mm", CultureInfo.InvariantCulture), host.Instance.Model.Time);
    }

    [Fact]
    public void PickersWithoutId_NameTheirPanelThemselves_AndShowTheHostPlaceholder()
    {
        var date = Render<OmniDatePicker>(parameters => parameters.Add(component => component.ValueExpression, () => _date).Add(component => component.Placeholder, "quand ?"));
        var time = Render<OmniTimePicker>(parameters => parameters.Add(component => component.ValueExpression, () => _time).Add(component => component.Placeholder, "à quelle heure ?"));
        var moment = Render<OmniDateTimePicker>(parameters => parameters.Add(component => component.ValueExpression, () => _moment).Add(component => component.Placeholder, "quand précisément ?"));

        Assert.Equal("quand ?", date.Find("input").GetAttribute("placeholder"));
        Assert.Equal("à quelle heure ?", time.Find("input").GetAttribute("placeholder"));
        Assert.Equal("quand précisément ?", moment.Find("input").GetAttribute("placeholder"));
        date.Find(".omni-date__toggle").Click();
        time.Find(".omni-date__toggle").Click();
        moment.Find(".omni-date__toggle").Click();
        Assert.EndsWith("-calendar", date.Find(".omni-date__toggle").GetAttribute("aria-controls"), StringComparison.Ordinal);
        Assert.EndsWith("-time", time.Find(".omni-date__toggle").GetAttribute("aria-controls"), StringComparison.Ordinal);
        Assert.EndsWith("-panel", moment.Find(".omni-date__toggle").GetAttribute("aria-controls"), StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseWithoutDisposing_KeepsTheOpenPanel()
    {
        var date = Render<ReleasingDatePicker>(parameters => parameters.Add(component => component.ValueExpression, () => _date));
        var time = Render<ReleasingTimePicker>(parameters => parameters.Add(component => component.ValueExpression, () => _time));
        var moment = Render<ReleasingDateTimePicker>(parameters => parameters.Add(component => component.ValueExpression, () => _moment));

        date.Find(".omni-date__toggle").Click();
        time.Find(".omni-date__toggle").Click();
        moment.Find(".omni-date__toggle").Click();

        date.Instance.Release();
        time.Instance.Release();
        moment.Instance.Release();

        Assert.NotEmpty(date.FindAll("[role='dialog']"));
        Assert.NotEmpty(time.FindAll("[role='dialog']"));
        Assert.NotEmpty(moment.FindAll("[role='dialog']"));
    }

    /// <summary>Calls the release a finalizer would make: nothing to free beyond the form subscription.</summary>
    public sealed class ReleasingDatePicker : OmniDatePicker
    {
        public void Release() => Dispose(false);
    }

    /// <inheritdoc cref="ReleasingDatePicker"/>
    public sealed class ReleasingTimePicker : OmniTimePicker
    {
        public void Release() => Dispose(false);
    }

    /// <inheritdoc cref="ReleasingDatePicker"/>
    public sealed class ReleasingDateTimePicker : OmniDateTimePicker
    {
        public void Release() => Dispose(false);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
