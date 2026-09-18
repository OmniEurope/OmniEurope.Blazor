using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The date, time and date and time pickers of PLAN-008 (T18 a, T19): the field, the house panel on
/// the floating layer, the grid and column keyboard, the bounds, the binding and both languages. What
/// only a browser can prove (the single open panel, the page not scrolling under the arrows, the real
/// focus) is left to the showcase probe; here the script calls are checked as calls.
/// </summary>
public sealed class PickerTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    /// <summary>Friday 18 September 2026, 14:33:20, the day of the mockup.</summary>
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 9, 18, 14, 33, 20, TimeSpan.Zero));

    private readonly BunitJSModuleInterop _module;

    public PickerTests()
    {
        _module = JSInterop.SetupModule(FocusModule);
        _module.Mode = JSRuntimeMode.Loose;
    }

    // ---- OmniDatePicker ----

    [Fact]
    public void DatePicker_Closed_IsATextFieldWithItsToggle()
    {
        var host = RenderHost(model => model.Date = new DateOnly(2026, 9, 21));

        var input = host.Find("#date");
        Assert.Equal("text", input.GetAttribute("type"));
        Assert.Equal("21/09/2026", input.GetAttribute("value"));
        Assert.Equal("jj/mm/aaaa", input.GetAttribute("placeholder"));
        var toggle = Toggle(host, "date");
        Assert.Equal("Choisir une date", toggle.GetAttribute("aria-label"));
        Assert.Equal("dialog", toggle.GetAttribute("aria-haspopup"));
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Null(toggle.GetAttribute("aria-controls"));
        Assert.Empty(host.FindAll("[role='dialog']"));
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DatePicker_TypedInput_IsReadInTheCultureAndInIso_AndOutOfBoundsIsRefused()
    {
        var host = RenderHost(parameters: parameters => parameters
            .Add(component => component.DateMinimum, new DateOnly(2026, 1, 1))
            .Add(component => component.DateMaximum, new DateOnly(2026, 12, 31)));

        host.Find("#date").Change("21/09/2026");
        Assert.Equal(new DateOnly(2026, 9, 21), host.Instance.Model.Date);

        host.Find("#date").Change("2026-10-02");
        Assert.Equal(new DateOnly(2026, 10, 2), host.Instance.Model.Date);
        Assert.Equal("02/10/2026", host.Find("#date").GetAttribute("value"));

        host.Find("#date").Change("15/01/2027");
        Assert.Equal(new DateOnly(2026, 10, 2), host.Instance.Model.Date);
        Assert.Equal("true", host.Find("#date").GetAttribute("aria-invalid"));
        Assert.Contains("La date saisie n'est pas valide.", host.Instance.EditContext.GetValidationMessages());

        host.Find("#date").Change(string.Empty);
        Assert.Null(host.Instance.Model.Date);
    }

    [Fact]
    public void DatePicker_Opens_ANamedGridOfSixWeeksFromMonday_WithTodayCircledAndTheValueSelected()
    {
        var host = RenderHost(model => model.Date = new DateOnly(2026, 9, 21));

        Toggle(host, "date").Click();

        var toggle = Toggle(host, "date");
        var panel = host.Find("[role='dialog']");
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        Assert.Equal(panel.Id, toggle.GetAttribute("aria-controls"));
        Assert.Equal("Choisir une date", panel.GetAttribute("aria-label"));
        Assert.Contains("omni-calendar", panel.ClassList);

        var grid = panel.QuerySelector("[role='grid']")!;
        var title = panel.QuerySelector(".omni-calendar__title")!;
        Assert.Equal(title.Id, grid.GetAttribute("aria-labelledby"));
        Assert.Equal("septembre 2026", title.TextContent);
        Assert.Equal("polite", title.GetAttribute("aria-live"));

        var headers = grid.QuerySelectorAll("[role='columnheader']");
        Assert.Equal(["lun", "mar", "mer", "jeu", "ven", "sam", "dim"], headers.Select(header => header.TextContent));
        Assert.Equal("lundi", headers[0].GetAttribute("aria-label"));
        Assert.Equal(7, grid.QuerySelectorAll("[role='row']").Length);

        var days = grid.QuerySelectorAll("[role='gridcell']");
        Assert.Equal(42, days.Length);
        // September 2026 starts on a Tuesday: the grid opens on Monday 31 August.
        Assert.Equal("2026-08-31", days[0].GetAttribute("data-date"));
        Assert.Contains("omni-calendar__day--outside", days[0].ClassList);

        var today = Day(host, "2026-09-18");
        Assert.Equal("date", today.GetAttribute("aria-current"));
        Assert.Contains("omni-calendar__day--today", today.ClassList);
        Assert.Equal("vendredi 18 septembre 2026", today.GetAttribute("aria-label"));

        var selected = Assert.Single(grid.QuerySelectorAll("[aria-selected='true']"));
        Assert.Equal("2026-09-21", selected.GetAttribute("data-date"));
        var focused = Assert.Single(grid.QuerySelectorAll("[tabindex='0']"));
        Assert.Equal("2026-09-21", focused.GetAttribute("data-date"));

        Assert.Equal(["Aujourd'hui", "Effacer"], panel.QuerySelectorAll(".omni-calendar__foot button").Select(button => button.TextContent.Trim()));
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["attachPicker"]));
        Assert.Equal(".omni-calendar__day[tabindex='0']", _module.Invocations["focusPickerItem"][^1].Arguments[1]);
    }

    [Fact]
    public void DatePicker_WithoutValue_OpensOnToday()
    {
        var host = RenderHost();

        Toggle(host, "date").Click();

        Assert.Equal("2026-09-18", host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));
        Assert.Empty(host.FindAll(".omni-calendar__day[aria-selected='true']"));
    }

    [Theory]
    [InlineData("ArrowRight", false, "2026-09-22")]
    [InlineData("ArrowLeft", false, "2026-09-20")]
    [InlineData("ArrowDown", false, "2026-09-28")]
    [InlineData("ArrowUp", false, "2026-09-14")]
    [InlineData("PageDown", false, "2026-10-21")]
    [InlineData("PageUp", false, "2026-08-21")]
    [InlineData("PageDown", true, "2027-09-21")]
    [InlineData("Home", false, "2026-09-21")]
    [InlineData("End", false, "2026-09-27")]
    public void DatePicker_GridKeys_MoveTheFocusedDay(string key, bool shift, string expected)
    {
        var host = RenderHost(model => model.Date = new DateOnly(2026, 9, 21));
        Toggle(host, "date").Click();

        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = key, ShiftKey = shift });

        Assert.Equal(expected, host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));
        var month = DateOnly.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        Assert.Equal(month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR")), host.Find(".omni-calendar__title").TextContent);
        // The value does not move with the focus: only a choice sets it.
        Assert.Equal(new DateOnly(2026, 9, 21), host.Instance.Model.Date);
        host.WaitForAssertion(() => Assert.Equal(2, _module.Invocations["focusPickerItem"].Count));
    }

    [Fact]
    public void DatePicker_HomeFromSunday_GoesBackToMonday()
    {
        var host = RenderHost(model => model.Date = new DateOnly(2026, 9, 27));
        Toggle(host, "date").Click();

        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = "Home" });

        Assert.Equal("2026-09-21", host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));
    }

    [Fact]
    public void DatePicker_ChoosingADay_SetsTheValue_ClosesAndGivesTheFocusBack()
    {
        var host = RenderHost();
        Toggle(host, "date").Click();
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["attachPicker"]));

        Day(host, "2026-09-24").Click();

        Assert.Equal(new DateOnly(2026, 9, 24), host.Instance.Model.Date);
        Assert.Equal("24/09/2026", host.Find("#date").GetAttribute("value"));
        Assert.Empty(host.FindAll("[role='dialog']"));
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["detachPicker"]));
        Assert.Equal(true, _module.Invocations["detachPicker"][0].Arguments[1]);
    }

    [Fact]
    public void DatePicker_TodayAndClear_SetAndEmptyTheValue()
    {
        var host = RenderHost(model => model.Date = new DateOnly(2026, 1, 5));

        Toggle(host, "date").Click();
        host.FindAll(".omni-calendar__foot button")[0].Click();
        Assert.Equal(new DateOnly(2026, 9, 18), host.Instance.Model.Date);
        Assert.Empty(host.FindAll("[role='dialog']"));

        Toggle(host, "date").Click();
        host.FindAll(".omni-calendar__foot button")[1].Click();
        Assert.Null(host.Instance.Model.Date);
        Assert.Equal(string.Empty, host.Find("#date").GetAttribute("value") ?? string.Empty);
    }

    [Fact]
    public void DatePicker_Bounds_DisableTheDaysAndTheMonthButtons_AndStopTheKeyboard()
    {
        var host = RenderHost(
            model => model.Date = new DateOnly(2026, 9, 10),
            parameters => parameters
                .Add(component => component.DateMinimum, new DateOnly(2026, 9, 8))
                .Add(component => component.DateMaximum, new DateOnly(2026, 9, 25)));
        Toggle(host, "date").Click();

        Assert.True(Day(host, "2026-09-07").HasAttribute("disabled"));
        Assert.False(Day(host, "2026-09-08").HasAttribute("disabled"));
        Assert.True(Day(host, "2026-09-26").HasAttribute("disabled"));
        Assert.True(host.Find("[data-omni-step='previous']").HasAttribute("disabled"));
        Assert.True(host.Find("[data-omni-step='next']").HasAttribute("disabled"));

        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = "PageDown" });
        Assert.Equal("2026-09-25", host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));
        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = "PageUp" });
        Assert.Equal("2026-09-08", host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));

        // Today (18 September) is inside; a clock outside the bounds disables the Today button.
        Assert.False(host.FindAll(".omni-calendar__foot button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void DatePicker_MonthButtons_ChangeTheMonthOnShow()
    {
        var host = RenderHost(model => model.Date = new DateOnly(2026, 9, 21));
        Toggle(host, "date").Click();

        host.Find("[data-omni-step='next']").Click();
        Assert.Equal("octobre 2026", host.Find(".omni-calendar__title").TextContent);
        Assert.Equal("2026-10-21", host.Find(".omni-calendar__day[tabindex='0']").GetAttribute("data-date"));

        host.Find("[data-omni-step='previous']").Click();
        host.Find("[data-omni-step='previous']").Click();
        Assert.Equal("août 2026", host.Find(".omni-calendar__title").TextContent);
        Assert.Equal("Mois précédent", host.Find("[data-omni-step='previous']").GetAttribute("aria-label"));
        Assert.Equal("Mois suivant", host.Find("[data-omni-step='next']").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DatePicker_EscapeOrAPressOutside_ClosesThePanel_OnlyEscapeGivesTheFocusBack(bool fromKeyboard)
    {
        var host = RenderHost();
        Toggle(host, "date").Click();
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["attachPicker"]));
        var bridge = (DotNetObjectReference<PickerPopupBridge>)_module.Invocations["attachPicker"][0].Arguments[3]!;

        await host.InvokeAsync(() => bridge.Value.OnDismissRequestedAsync(fromKeyboard));

        Assert.Empty(host.FindAll("[role='dialog']"));
        Assert.Equal("false", Toggle(host, "date").GetAttribute("aria-expanded"));
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["detachPicker"]));
        var detach = _module.Invocations["detachPicker"][0].Arguments;
        Assert.Equal(_module.Invocations["attachPicker"][0].Arguments[4], detach[0]);
        Assert.Equal(fromKeyboard, detach[1]);
    }

    [Fact]
    public void DatePicker_TheToggleClosesItsOwnPanel_AndIsDisabledWhenTheFieldIsReadOnly()
    {
        var host = RenderHost();
        Toggle(host, "date").Click();
        Toggle(host, "date").Click();
        Assert.Empty(host.FindAll("[role='dialog']"));

        DateOnly? value = null;
        var readOnly = Render<OmniDatePicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ReadOnly, true));
        Assert.True(readOnly.Find(".omni-date__toggle").HasAttribute("disabled"));
        Assert.True(readOnly.Find("input").HasAttribute("readonly"));
    }

    // ---- OmniTimePicker ----

    [Fact]
    public void TimePicker_Closed_IsA24HourTextField()
    {
        var host = RenderHost(model => model.Time = new TimeOnly(9, 5));

        var input = host.Find("#time");
        Assert.Equal("09:05", input.GetAttribute("value"));
        Assert.Equal("hh:mm", input.GetAttribute("placeholder"));
        Assert.Equal("Choisir une heure", Toggle(host, "time").GetAttribute("aria-label"));

        host.Find("#time").Change("7:45");
        Assert.Equal(new TimeOnly(7, 45), host.Instance.Model.Time);
        host.Find("#time").Change("25:00");
        Assert.Equal(new TimeOnly(7, 45), host.Instance.Model.Time);
        Assert.Equal("true", host.Find("#time").GetAttribute("aria-invalid"));
        Assert.Contains("L'heure saisie n'est pas valide.", host.Instance.EditContext.GetValidationMessages());
    }

    [Fact]
    public void TimePicker_Opens_TwoNamedColumns_HoursAndMinutesByStep()
    {
        var host = RenderHost(model => model.Time = new TimeOnly(14, 30));

        Toggle(host, "time").Click();

        var panel = host.Find("[role='dialog']");
        Assert.Contains("omni-calendar--time", panel.ClassList);
        var lists = panel.QuerySelectorAll("[role='listbox']");
        Assert.Equal(["Heures", "Minutes"], lists.Select(list => list.GetAttribute("aria-label")));
        Assert.Equal(24, lists[0].QuerySelectorAll("[role='option']").Length);
        Assert.Equal(12, lists[1].QuerySelectorAll("[role='option']").Length);
        Assert.Equal("00", lists[1].QuerySelectorAll("[role='option']")[0].TextContent);
        Assert.Equal("55", lists[1].QuerySelectorAll("[role='option']")[^1].TextContent);
        Assert.Equal("14", Assert.Single(lists[0].QuerySelectorAll("[aria-selected='true']")).TextContent);
        Assert.Equal("30", Assert.Single(lists[1].QuerySelectorAll("[aria-selected='true']")).TextContent);
        Assert.Equal("14", Assert.Single(lists[0].QuerySelectorAll("[tabindex='0']")).TextContent);
        Assert.Equal(["h", "min"], panel.QuerySelectorAll(".omni-time__label").Select(label => label.TextContent));
        Assert.Equal(["Maintenant", "Valider"], panel.QuerySelectorAll(".omni-calendar__foot button").Select(button => button.TextContent.Trim()));
        host.WaitForAssertion(() => Assert.Equal(".omni-time__list[data-omni-part='hour'] [tabindex='0']", _module.Invocations["focusPickerItem"][^1].Arguments[1]));
    }

    [Fact]
    public void TimePicker_Step_SetsTheMinuteColumn_AndIsBounded()
    {
        var host = RenderHost(parameters: parameters => parameters.Add(component => component.Step, 15));
        Toggle(host, "time").Click();
        Assert.Equal(["00", "15", "30", "45"], host.FindAll("[data-omni-part='minute'] [role='option']").Select(item => item.TextContent));

        TimeOnly? value = null;
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Render<OmniTimePicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Step, 0)));
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => Render<OmniTimePicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Step, 31)));
    }

    [Fact]
    public void TimePicker_AClick_AppliesAtOnce_AndConfirmClosesAndGivesTheFocusBack()
    {
        var host = RenderHost(model => model.Time = new TimeOnly(14, 30));
        Toggle(host, "time").Click();

        host.FindAll("[data-omni-part='hour'] [role='option']")[9].Click();
        Assert.Equal(new TimeOnly(9, 30), host.Instance.Model.Time);
        host.FindAll("[data-omni-part='minute'] [role='option']")[3].Click();
        Assert.Equal(new TimeOnly(9, 15), host.Instance.Model.Time);
        Assert.Equal("09:15", host.Find("#time").GetAttribute("value"));
        Assert.NotEmpty(host.FindAll("[role='dialog']"));

        host.FindAll(".omni-calendar__foot button")[1].Click();
        Assert.Empty(host.FindAll("[role='dialog']"));
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["detachPicker"]));
        Assert.Equal(true, _module.Invocations["detachPicker"][0].Arguments[1]);
    }

    [Theory]
    [InlineData("hour", "ArrowDown", 15, 30)]
    [InlineData("hour", "ArrowUp", 13, 30)]
    [InlineData("hour", "Home", 0, 30)]
    [InlineData("hour", "End", 23, 30)]
    [InlineData("minute", "ArrowDown", 14, 35)]
    [InlineData("minute", "ArrowUp", 14, 25)]
    [InlineData("minute", "End", 14, 55)]
    public void TimePicker_ColumnKeys_MoveTheChoice(string part, string key, int hour, int minute)
    {
        var host = RenderHost(model => model.Time = new TimeOnly(14, 30));
        Toggle(host, "time").Click();

        host.Find($"[data-omni-part='{part}']").KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal(new TimeOnly(hour, minute), host.Instance.Model.Time);
        host.WaitForAssertion(() => Assert.Equal($".omni-time__list[data-omni-part='{part}'] [tabindex='0']", _module.Invocations["focusPickerItem"][^1].Arguments[1]));
    }

    [Fact]
    public void TimePicker_Now_TakesTheClockRoundedDownToTheStep()
    {
        var host = RenderHost();
        Toggle(host, "time").Click();

        host.FindAll(".omni-calendar__foot button")[0].Click();

        Assert.Equal(new TimeOnly(14, 30), host.Instance.Model.Time);
    }

    [Fact]
    public void TimePicker_Bounds_DisableTheHoursOutside_AndRefuseATypedTimeOutside()
    {
        var host = RenderHost(
            model => model.Time = new TimeOnly(8, 30),
            parameters => parameters
                .Add(component => component.TimeMinimum, new TimeOnly(8, 15))
                .Add(component => component.TimeMaximum, new TimeOnly(18, 0)));
        Toggle(host, "time").Click();

        var hours = host.FindAll("[data-omni-part='hour'] [role='option']");
        Assert.True(hours[7].HasAttribute("disabled"));
        Assert.False(hours[8].HasAttribute("disabled"));
        Assert.False(hours[18].HasAttribute("disabled"));
        Assert.True(hours[19].HasAttribute("disabled"));
        var minutes = host.FindAll("[data-omni-part='minute'] [role='option']");
        Assert.True(minutes[2].HasAttribute("disabled"));
        Assert.False(minutes[3].HasAttribute("disabled"));

        host.Find("[data-omni-part='hour']").KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal(new TimeOnly(8, 30), host.Instance.Model.Time);

        host.Find("#time").Change("19:00");
        Assert.Equal(new TimeOnly(8, 30), host.Instance.Model.Time);
        Assert.Equal("true", host.Find("#time").GetAttribute("aria-invalid"));
    }

    // ---- OmniDateTimePicker ----

    [Fact]
    public void DateTimePicker_Opens_TheGridAndTheColumnsSideBySide()
    {
        var host = RenderHost(model => model.Moment = new DateTime(2026, 9, 21, 14, 30, 0, DateTimeKind.Unspecified));

        Assert.Equal("21/09/2026 14:30", host.Find("#moment").GetAttribute("value"));
        Assert.Equal("jj/mm/aaaa hh:mm", host.Find("#moment").GetAttribute("placeholder"));
        Toggle(host, "moment").Click();

        var panel = host.Find("[role='dialog']");
        Assert.Equal("Choisir une date et une heure", panel.GetAttribute("aria-label"));
        var body = panel.QuerySelector(".omni-picker__body")!;
        Assert.NotNull(body.QuerySelector(":scope > .omni-picker__cal [role='grid']"));
        Assert.Equal(2, body.QuerySelectorAll(":scope > .omni-time [role='listbox']").Length);
        Assert.Equal("2026-09-21", host.Find(".omni-calendar__day[aria-selected='true']").GetAttribute("data-date"));
        Assert.Equal("14", host.Find("[data-omni-part='hour'] [aria-selected='true']").TextContent);
    }

    [Fact]
    public void DateTimePicker_ADay_KeepsTheTime_AnHour_KeepsTheDay_AndThePanelStaysOpen()
    {
        var host = RenderHost(model => model.Moment = new DateTime(2026, 9, 21, 14, 30, 0, DateTimeKind.Unspecified));
        Toggle(host, "moment").Click();

        Day(host, "2026-09-24").Click();
        Assert.Equal(new DateTime(2026, 9, 24, 14, 30, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);
        host.FindAll("[data-omni-part='hour'] [role='option']")[9].Click();
        Assert.Equal(new DateTime(2026, 9, 24, 9, 30, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);
        Assert.NotEmpty(host.FindAll("[role='dialog']"));

        host.FindAll(".omni-calendar__foot button")[0].Click();
        Assert.Equal(new DateTime(2026, 9, 18, 14, 30, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);

        host.FindAll(".omni-calendar__foot button")[1].Click();
        Assert.Empty(host.FindAll("[role='dialog']"));
    }

    [Fact]
    public void DateTimePicker_WithoutValue_AnHourTakesToday_ADayTakesMidnight()
    {
        var host = RenderHost();
        Toggle(host, "moment").Click();

        host.FindAll("[data-omni-part='hour'] [role='option']")[8].Click();
        Assert.Equal(new DateTime(2026, 9, 18, 8, 0, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);

        var other = RenderHost();
        other.Find("#moment ~ .omni-date__toggle").Click();
        Day(other, "2026-09-22").Click();
        Assert.Equal(new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Unspecified), other.Instance.Model.Moment);
    }

    [Fact]
    public void DateTimePicker_ShowSeconds_AddsAThirdColumnAndWritesTheSeconds()
    {
        var host = RenderHost(
            model => model.Moment = new DateTime(2026, 9, 21, 14, 30, 12, DateTimeKind.Unspecified),
            parameters => parameters.Add(component => component.ShowSeconds, true));

        Assert.Equal("21/09/2026 14:30:12", host.Find("#moment").GetAttribute("value"));
        Toggle(host, "moment").Click();
        var lists = host.FindAll("[role='listbox']");
        Assert.Equal(["Heures", "Minutes", "Secondes"], lists.Select(list => list.GetAttribute("aria-label")));
        Assert.Equal(60, lists[2].QuerySelectorAll("[role='option']").Length);

        host.Find("[data-omni-part='second']").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(new DateTime(2026, 9, 21, 14, 30, 13, DateTimeKind.Unspecified), host.Instance.Model.Moment);
    }

    [Fact]
    public void DateTimePicker_AChoiceOutOfTheBounds_IsBroughtBackToTheBound()
    {
        var host = RenderHost(
            model => model.Moment = new DateTime(2026, 9, 21, 14, 30, 0, DateTimeKind.Unspecified),
            parameters => parameters
                .Add(component => component.MomentMinimum, new DateTime(2026, 9, 10, 9, 0, 0, DateTimeKind.Unspecified))
                .Add(component => component.MomentMaximum, new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Unspecified)));
        Toggle(host, "moment").Click();

        Assert.True(Day(host, "2026-09-09").HasAttribute("disabled"));
        Day(host, "2026-09-25").Click();
        Assert.Equal(new DateTime(2026, 9, 25, 12, 0, 0, DateTimeKind.Unspecified), host.Instance.Model.Moment);
        var hours = host.FindAll("[data-omni-part='hour'] [role='option']");
        Assert.False(hours[12].HasAttribute("disabled"));
        Assert.True(hours[13].HasAttribute("disabled"));
    }

    // ---- English ----

    [Fact]
    public void Pickers_InEnglish_FollowTheCultureAndTheInterfaceLanguage()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var english = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = english;
            CultureInfo.CurrentUICulture = english;
            var host = RenderHost(model =>
            {
                model.Date = new DateOnly(2026, 9, 21);
                model.Moment = new DateTime(2026, 9, 21, 14, 30, 0, DateTimeKind.Unspecified);
            });

            Assert.Equal("09/21/2026", host.Find("#date").GetAttribute("value"));
            Assert.Equal("mm/dd/yyyy", host.Find("#date").GetAttribute("placeholder"));
            Assert.Equal("09/21/2026 14:30", host.Find("#moment").GetAttribute("value"));
            Assert.Equal("Choose a date", Toggle(host, "date").GetAttribute("aria-label"));
            Assert.Equal("Choose a time", Toggle(host, "time").GetAttribute("aria-label"));
            Assert.Equal("Choose a date and a time", Toggle(host, "moment").GetAttribute("aria-label"));

            host.Find("#date").Change("10/02/2026");
            Assert.Equal(new DateOnly(2026, 10, 2), host.Instance.Model.Date);

            Toggle(host, "date").Click();
            Assert.Equal("October 2026", host.Find(".omni-calendar__title").TextContent);
            // en-US starts the week on Sunday.
            Assert.Equal(["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"], host.FindAll("[role='columnheader']").Select(header => header.TextContent));
            Assert.Equal("Previous month", host.Find("[data-omni-step='previous']").GetAttribute("aria-label"));
            Assert.Equal(["Today", "Clear"], host.FindAll(".omni-calendar__foot button").Select(button => button.TextContent.Trim()));

            Toggle(host, "time").Click();
            var timePanel = host.Find(".omni-calendar--time");
            Assert.Equal(["Hours", "Minutes"], timePanel.QuerySelectorAll("[role='listbox']").Select(list => list.GetAttribute("aria-label")));
            Assert.Equal(["Now", "Confirm"], timePanel.QuerySelectorAll(".omni-calendar__foot button").Select(button => button.TextContent.Trim()));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    // ---- inside a dialog ----

    [Theory]
    [InlineData("date")]
    [InlineData("time")]
    [InlineData("moment")]
    public async Task Pickers_InADialog_EscapeInThePanel_ClosesThePanelAndNotTheDialog(string id)
    {
        var host = Render<PickerDialogTestHost>(parameters => parameters.Add(component => component.Clock, Clock));
        host.Find($"#{id} ~ .omni-date__toggle").Click();
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["attachPicker"]));
        var panel = host.Find(".omni-calendar");

        // The key as Blazor sees it: pressed on the focused item of the panel, it must not bubble up to
        // the dialog, which closes on Escape.
        panel.QuerySelector("[tabindex='0']")!.KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.True(host.Instance.Open);
        Assert.Single(host.FindAll(".omni-dialog"));

        // The key as omni-focus.js handles it on the picker: it closes the panel through the bridge and
        // gives the focus back to the toggle.
        var bridge = (DotNetObjectReference<PickerPopupBridge>)_module.Invocations["attachPicker"][0].Arguments[3]!;
        await host.InvokeAsync(() => bridge.Value.OnDismissRequestedAsync(true));

        Assert.Empty(host.FindAll(".omni-calendar"));
        Assert.Equal("false", host.Find($"#{id} ~ .omni-date__toggle").GetAttribute("aria-expanded"));
        host.WaitForAssertion(() => Assert.Single(_module.Invocations["detachPicker"]));
        Assert.Equal(true, _module.Invocations["detachPicker"][0].Arguments[1]);
        Assert.True(host.Instance.Open);
        Assert.Single(host.FindAll(".omni-dialog"));
    }

    // ---- helpers ----

    private IRenderedComponent<PickerTestHost> RenderHost(
        Action<PickerTestHost.PickerTestModel>? arrange = null,
        Action<ComponentParameterCollectionBuilder<PickerTestHost>>? parameters = null)
    {
        var model = new PickerTestHost.PickerTestModel();
        arrange?.Invoke(model);
        return Render<PickerTestHost>(builder =>
        {
            builder.Add(component => component.Clock, Clock).Add(component => component.Model, model);
            parameters?.Invoke(builder);
        });
    }

    private static IElement Toggle(IRenderedComponent<PickerTestHost> host, string id) =>
        host.Find($"#{id} ~ .omni-date__toggle");

    private static IElement Day(IRenderedComponent<PickerTestHost> host, string date) =>
        host.Find($".omni-calendar__day[data-date='{date}']");

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
