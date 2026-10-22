using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Defects of the feedback, forms and selection families found by the 2026-10-22 audit: a stale threshold
/// the clock cannot wait for, pickers at the first and last day, a form paying for a guard it does not use,
/// shared identifiers, the file filter, accent-blind filtering and the accessible name of a multiple choice.
/// </summary>
public sealed class FormsSelectionFeedbackDefectTests : OmniBunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 14, 33, 20, TimeSpan.Zero);

    public FormsSelectionFeedbackDefectTests() => JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;

    // ── OmniStatusBadge ──

    private static OmniStatusMap<int> Map()
    {
        var map = new OmniStatusMap<int>();
        map.Add(1, OmniTone.Accent, "Actif");
        return map;
    }

    /// <summary>A clock that refuses a wait the system timer cannot do, as the real one does.</summary>
    private sealed class StrictClock(DateTimeOffset now, TimeZoneInfo? zone = null) : TimeProvider
    {
        private const double MaximumMilliseconds = 4294967294;
        private readonly List<StrictTimer> _timers = [];
        private DateTimeOffset _now = now;

        public List<TimeSpan> Waits { get; } = [];

        public int Pending => _timers.Count(timer => !timer.Disposed);

        public override TimeZoneInfo LocalTimeZone => zone ?? TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => _now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dueTime.TotalMilliseconds, MaximumMilliseconds);
            Waits.Add(dueTime);
            var timer = new StrictTimer(callback, state, _now + dueTime);
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

        private sealed class StrictTimer(TimerCallback callback, object? state, DateTimeOffset due) : ITimer
        {
            public DateTimeOffset Due { get; } = due;

            public bool Disposed { get; private set; }

            public void Fire()
            {
                Disposed = true;
                callback(state);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period) => true;

            public void Dispose() => Disposed = true;

            public ValueTask DisposeAsync()
            {
                Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }

    [Fact]
    public void StatusBadge_WithAnEndlessThreshold_NeverGrowsStale_AndArmsNoTimer()
    {
        var clock = new StrictClock(Now);
        Services.AddSingleton<TimeProvider>(clock);

        var badge = Render<OmniStatusBadge<int>>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.Map, Map())
            .Add(component => component.Timestamp, Now)
            .Add(component => component.StaleAfter, TimeSpan.MaxValue));

        Assert.False(badge.Instance.IsStale);
        Assert.Equal(0, clock.Pending);
    }

    [Fact]
    public void StatusBadge_WithAThresholdBeyondTheTimerLimit_WaitsInStepsAndTurnsStaleOnTime()
    {
        var clock = new StrictClock(Now);
        Services.AddSingleton<TimeProvider>(clock);
        var threshold = TimeSpan.FromDays(100);

        var badge = Render<OmniStatusBadge<int>>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.Map, Map())
            .Add(component => component.Timestamp, Now)
            .Add(component => component.StaleAfter, threshold));

        // A first wait below the limit of a system timer (about 49.7 days), not the 100 days asked for.
        var first = Assert.Single(clock.Waits);
        Assert.True(first < TimeSpan.FromDays(49));
        Assert.Equal(1, clock.Pending);

        clock.Advance(first);
        badge.WaitForAssertion(() => Assert.Equal(2, clock.Waits.Count));
        Assert.False(badge.Instance.IsStale);
        Assert.Empty(badge.FindAll(".omni-status-badge__stale"));

        clock.Advance(threshold - first);
        badge.WaitForAssertion(() => Assert.NotEmpty(badge.FindAll(".omni-status-badge__stale")));
        Assert.True(badge.Instance.IsStale);
        Assert.Equal(0, clock.Pending);
    }

    [Fact]
    public void StatusBadge_ShowsTheLastUpdateInTheTimeZoneGiven_ElseInThatOfTheClock()
    {
        var clock = new StrictClock(Now.AddDays(1));
        Services.AddSingleton<TimeProvider>(clock);
        var plusFive = TimeZoneInfo.CreateCustomTimeZone("test+5", TimeSpan.FromHours(5), "test+5", "test+5");
        var timestamp = new DateTimeOffset(2026, 9, 18, 10, 0, 0, TimeSpan.Zero);

        IRenderedComponent<OmniStatusBadge<int>> RenderWith(TimeZoneInfo? zone) => Render<OmniStatusBadge<int>>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.Map, Map())
            .Add(component => component.Timestamp, timestamp)
            .Add(component => component.StaleAfter, TimeSpan.FromHours(1))
            .Add(component => component.TimeZone, zone));

        Assert.Contains("10:00", RenderWith(null).Find(".omni-status-badge__stale").GetAttribute("title"), StringComparison.Ordinal);
        Assert.Contains("15:00", RenderWith(plusFive).Find(".omni-status-badge__stale").GetAttribute("title"), StringComparison.Ordinal);
    }

    // ── Pickers at the first and the last day ──

    private IRenderedComponent<PickerTestHost> OpenDate(DateOnly value)
    {
        Services.AddSingleton<TimeProvider>(new StrictClock(Now));
        var model = new PickerTestHost.PickerTestModel { Date = value };
        var host = Render<PickerTestHost>(builder => builder.Add(component => component.Model, model));
        host.Find("#date ~ .omni-date__toggle").Click();
        return host;
    }

    [Theory]
    [InlineData("ArrowLeft")]
    [InlineData("ArrowUp")]
    [InlineData("PageUp")]
    [InlineData("Home")]
    public void DatePicker_OnTheFirstDay_ShowsItsMonth_AndKeysNeverGoBeforeIt(string key)
    {
        var host = OpenDate(DateOnly.MinValue);

        Assert.True(host.Find("[data-omni-step='previous']").HasAttribute("disabled"));
        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = key });
        host.Find("[data-omni-step='previous']").Click();

        Assert.NotNull(host.Find(".omni-calendar__day[data-date='0001-01-01']"));
        Assert.Equal(DateOnly.MinValue, host.Instance.Model.Date);
    }

    [Theory]
    [InlineData("ArrowRight")]
    [InlineData("ArrowDown")]
    [InlineData("PageDown")]
    [InlineData("End")]
    public void DatePicker_OnTheLastDay_ShowsItsMonth_AndKeysNeverGoAfterIt(string key)
    {
        var host = OpenDate(DateOnly.MaxValue);

        Assert.True(host.Find("[data-omni-step='next']").HasAttribute("disabled"));
        host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = key });
        host.Find("[data-omni-step='next']").Click();

        Assert.NotNull(host.Find(".omni-calendar__day[data-date='9999-12-31']"));
        // The days that do not exist keep the week whole but are not days.
        Assert.NotEmpty(host.FindAll(".omni-calendar__day[aria-hidden='true']"));
        Assert.Empty(host.FindAll(".omni-calendar__day[aria-hidden='true'][data-date]"));
        Assert.Equal(DateOnly.MaxValue, host.Instance.Model.Date);
    }

    [Fact]
    public void DateTimePicker_OnTheFirstAndLastDay_Opens()
    {
        Services.AddSingleton<TimeProvider>(new StrictClock(Now));
        foreach (var value in new[] { DateTime.MinValue, DateTime.MaxValue })
        {
            var model = new PickerTestHost.PickerTestModel { Moment = value };
            var host = Render<PickerTestHost>(builder => builder.Add(component => component.Model, model));

            host.Find("#moment ~ .omni-date__toggle").Click();
            host.Find("[role='grid']").KeyDown(new KeyboardEventArgs { Key = value == DateTime.MinValue ? "PageUp" : "PageDown" });

            Assert.NotEmpty(host.FindAll(".omni-calendar__day"));
        }
    }

    [Fact]
    public void PickerCalendar_Steps_StopAtTheFirstAndTheLastDay()
    {
        Assert.Equal(DateOnly.MinValue, PickerCalendar.AddDays(DateOnly.MinValue, -1));
        Assert.Equal(DateOnly.MaxValue, PickerCalendar.AddDays(DateOnly.MaxValue, 7));
        Assert.Equal(DateOnly.MinValue, PickerCalendar.AddMonths(DateOnly.MinValue, -12));
        Assert.Equal(DateOnly.MaxValue, PickerCalendar.AddMonths(DateOnly.MaxValue, 1));
        Assert.Equal(new DateOnly(2026, 2, 28), PickerCalendar.AddMonths(new DateOnly(2026, 1, 31), 1));
        Assert.Equal(new DateOnly(2025, 12, 15), PickerCalendar.AddMonths(new DateOnly(2026, 12, 15), -12));
        Assert.Equal(new DateOnly(2026, 3, 1), PickerCalendar.AddDays(new DateOnly(2026, 2, 28), 1));
    }

    // ── The time zone of the pickers ──

    [Fact]
    public void Pickers_ReadToday_InTheTimeZoneGiven_ElseInThatOfTheClock()
    {
        Services.AddSingleton<TimeProvider>(new StrictClock(Now));
        var plusFourteen = TimeZoneInfo.CreateCustomTimeZone("test+14", TimeSpan.FromHours(14), "test+14", "test+14");

        // 14:33 UTC on the 18th is 04:33 on the 19th at +14.
        string Today<TPicker>(Action<ComponentParameterCollectionBuilder<TPicker>> arrange) where TPicker : IComponent
        {
            var picker = Render(arrange);
            picker.Find(".omni-date__toggle").Click();
            return picker.Find(".omni-calendar__day--today").GetAttribute("data-date")!;
        }

        DateOnly? date = null;
        DateTime? moment = null;
        Assert.Equal("2026-09-18", Today<OmniDatePicker>(p => p.Add(c => c.Value, date).Add(c => c.ValueExpression, () => date)));
        Assert.Equal("2026-09-19", Today<OmniDatePicker>(p => p.Add(c => c.Value, date).Add(c => c.ValueExpression, () => date).Add(c => c.TimeZone, plusFourteen)));
        Assert.Equal("2026-09-18", Today<OmniDateTimePicker>(p => p.Add(c => c.Value, moment).Add(c => c.ValueExpression, () => moment)));
        Assert.Equal("2026-09-19", Today<OmniDateTimePicker>(p => p.Add(c => c.Value, moment).Add(c => c.ValueExpression, () => moment).Add(c => c.TimeZone, plusFourteen)));
    }

    [Fact]
    public void TimePicker_Now_ReadsTheTimeZoneGiven()
    {
        Services.AddSingleton<TimeProvider>(new StrictClock(Now));
        var plusTwo = TimeZoneInfo.CreateCustomTimeZone("test+2", TimeSpan.FromHours(2), "test+2", "test+2");
        TimeOnly? value = null;
        var picker = Render<OmniTimePicker>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.TimeZone, plusTwo)
            .Add(component => component.Step, TimeSpan.FromMinutes(5)));

        picker.Find(".omni-date__toggle").Click();
        picker.FindAll(".omni-calendar__foot button")[0].Click();

        Assert.Equal("16:30", picker.Find("input").GetAttribute("value"));
    }

    // ── OmniTemplateForm ──

    private sealed class CountingModel
    {
        private string _name = string.Empty;

        public int Reads { get; private set; }

        public string Name
        {
            get
            {
                Reads++;
                return _name;
            }

            set => _name = value;
        }
    }

    private IRenderedComponent<OmniTemplateForm<CountingModel>> RenderForm(CountingModel model, bool guard) =>
        Render<OmniTemplateForm<CountingModel>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.GuardUnsavedChanges, guard)
            .Add(component => component.OnValidSubmit, _ => { })
            .Add(component => component.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "submit");
                builder.CloseElement();
            })));

    private static void Change(IRenderedComponent<OmniTemplateForm<CountingModel>> form, CountingModel model)
    {
        var context = form.FindComponent<EditForm>().Instance.EditContext!;
        form.InvokeAsync(() => context.NotifyFieldChanged(context.Field(nameof(CountingModel.Name))));
    }

    [Fact]
    public async Task TemplateForm_WithoutTheGuard_NeverReadsTheModelByReflection()
    {
        var model = new CountingModel();
        var form = RenderForm(model, guard: false);

        model.Name = "Ada";
        var before = model.Reads;
        Change(form, model);
        form.Find("form").Submit();
        await form.InvokeAsync(() => Task.Delay(50, Xunit.TestContext.Current.CancellationToken));

        Assert.Equal(before, model.Reads);
        Assert.Empty(form.FindComponents<OmniUnsavedChangesGuard>());
    }

    [Fact]
    public void TemplateForm_WithTheGuard_StillRecordsTheModel()
    {
        var model = new CountingModel();
        var form = RenderForm(model, guard: true);

        Assert.True(model.Reads > 0);
        model.Name = "Ada";
        Change(form, model);
        Assert.True(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
    }

    [Fact]
    public void TemplateForm_GuardTurnedOnLater_StartsCleanFromTheValuesOfThatMoment()
    {
        var model = new CountingModel();
        var form = RenderForm(model, guard: false);
        model.Name = "Ada";
        Change(form, model);

        form.Render(parameters => parameters.Add(component => component.GuardUnsavedChanges, true));

        Assert.False(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
        model.Name = "Grace";
        Change(form, model);
        Assert.True(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
        model.Name = "Ada";
        Change(form, model);
        Assert.False(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
    }

    [Fact]
    public void TemplateForm_GuardTurnedOffAgain_StopsRecording_AndAnotherChangeOfTheSameFieldKeepsItModified()
    {
        var model = new CountingModel();
        var form = RenderForm(model, guard: true);
        model.Name = "Ada";
        Change(form, model);
        Change(form, model);
        form.Render();
        Assert.True(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);

        form.Render(parameters => parameters.Add(component => component.GuardUnsavedChanges, false));
        var reads = model.Reads;
        Change(form, model);

        Assert.Equal(reads, model.Reads);
        Assert.Empty(form.FindComponents<OmniUnsavedChangesGuard>());

        form.Render(parameters => parameters.Add(component => component.GuardUnsavedChanges, true));
        Assert.False(form.FindComponent<OmniUnsavedChangesGuard>().Instance.HasChanges);
    }

    // ── Identifiers, empty results and the choice of an empty option ──

    private static readonly IReadOnlyList<OmniOption<int?>> Countries =
    [
        new(1, "Belgique"),
        new(2, "France"),
        new(3, "Liège")
    ];

    [Fact]
    public void Autocomplete_WithoutAnId_GivesEachInstanceItsOwnListAndErrorIds()
    {
        string ResultsId()
        {
            var value = string.Empty;
            var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)
                .Add(component => component.Search, (text, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>([new(text, text)]))
                .Add(component => component.Debounce, TimeSpan.Zero));
            return autocomplete.Find("input").GetAttribute("aria-controls")!;
        }

        var first = ResultsId();
        var second = ResultsId();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("value-results", first, StringComparison.Ordinal);
    }

    [Fact]
    public void FilterableDropDown_TwoOnTheSameField_DoNotShareTheIdOfTheirList()
    {
        string ResultsId()
        {
            int? value = null;
            var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
                .Add(component => component.Filterable, true)
                .Add(component => component.Options, Countries)
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value));
            dropDown.Find("[data-omni-autocomplete-toggle]").Click();
            return dropDown.WaitForElement("ul[role='listbox']").Id!;
        }

        Assert.NotEqual(ResultsId(), ResultsId());
    }

    [Fact]
    public void Autocomplete_WhoseSearchAnswersNull_ListsNothing_WithoutAnError()
    {
        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>(null!))
            .Add(component => component.Debounce, TimeSpan.Zero));

        autocomplete.Find("input").Input("a");
        autocomplete.Find("input").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        autocomplete.WaitForAssertion(() => Assert.Empty(autocomplete.FindAll(".omni-autocomplete__error")));
        Assert.Empty(autocomplete.FindAll("[role='option']"));
    }

    [Fact]
    public void FilterableDropDown_IgnoresAccentsAndCaseWhenNarrowing()
    {
        int? value = null;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        dropDown.Find("input").Input("LIEGE");

        dropDown.WaitForAssertion(() => Assert.Equal(["Liège"], dropDown.FindAll("[role='option']").Select(option => option.TextContent.Trim())));
    }

    [Fact]
    public void FilterableDropDown_KeepsTheSizeOfTheControl()
    {
        int? value = null;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Size, OmniControlSize.Small)
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Contains("omni-input--small", dropDown.Find("input").ClassList);
    }

    [Fact]
    public void FilterableDropDown_WithAllowEmpty_OffersTheEmptyChoice_AndClearsTheValue()
    {
        int? value = 2;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.AllowEmpty, true)
            .Add(component => component.Placeholder, "Aucun")
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.ValueChanged, EventCallback.Factory.Create<int?>(this, changed => value = changed)));
        Assert.Equal("France", dropDown.Find("input").GetAttribute("value"));

        dropDown.Find("[data-omni-autocomplete-toggle]").Click();
        var options = dropDown.WaitForElements("[role='option']");
        Assert.Equal("Aucun", options[0].TextContent.Trim());
        options[0].Click();

        Assert.Null(value);
        // The field reads as the empty choice reads in the list: with the text of the placeholder.
        dropDown.WaitForAssertion(() => Assert.Equal("Aucun", dropDown.Find("input").GetAttribute("value")));
    }

    [Fact]
    public void FilterableDropDown_WithAllowEmpty_OffersNoEmptyChoiceOnceTextIsTyped()
    {
        int? value = null;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.AllowEmpty, true)
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        dropDown.Find("input").Input("fra");

        dropDown.WaitForAssertion(() => Assert.Equal(["France"], dropDown.FindAll("[role='option']").Select(option => option.TextContent.Trim())));
    }

    [Fact]
    public void FilterableDropDown_OfANonNullableValue_ShowsNoTextForTheEmptyChoice_AndTheValueOtherwise()
    {
        var value = 0;
        IReadOnlyList<OmniOption<int>> numbers = [new(1, "Un"), new(2, "Deux")];
        var empty = Render<OmniDropDown<int>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.AllowEmpty, true)
            .Add(component => component.Options, numbers)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));
        var strict = Render<OmniDropDown<int>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, numbers)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.True(string.IsNullOrEmpty(empty.Find("input").GetAttribute("value")));
        Assert.Equal("0", strict.Find("input").GetAttribute("value"));

        var other = 7;
        var unknown = Render<OmniDropDown<int>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.AllowEmpty, true)
            .Add(component => component.Options, numbers)
            .Add(component => component.Value, other)
            .Add(component => component.ValueExpression, () => other));
        Assert.Equal("7", unknown.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void FilterableDropDown_WithoutAllowEmpty_OffersNoEmptyChoice()
    {
        int? value = null;
        var dropDown = Render<OmniDropDown<int?>>(parameters => parameters
            .Add(component => component.Filterable, true)
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        dropDown.Find("[data-omni-autocomplete-toggle]").Click();

        Assert.Equal(3, dropDown.WaitForElements("[role='option']").Count);
    }

    // ── OmniMultiSelect ──

    [Fact]
    public void MultiSelect_IgnoresAccentsAndCaseWhenFiltering()
    {
        IReadOnlyList<int?> value = [];
        var select = Render<OmniMultiSelect<int?>>(parameters => parameters
            .Add(component => component.Options, Countries)
            .Add(component => component.Filterable, true)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        select.Find(".omni-multi-select-compact__filter").Input("LIEGE");

        Assert.Equal(["Liège"], select.FindAll(".omni-multi-select-compact__option").Select(option => option.TextContent.Trim()));
    }

    [Fact]
    public void MultiSelect_WithALabel_NamesTheControlAndItsPanel()
    {
        IReadOnlyList<int?> value = [2];
        var select = Render<OmniMultiSelect<int?>>(parameters => parameters
            .Add(component => component.Options, Countries)
            .Add(component => component.Label, "Pays")
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Equal("Pays: France", select.Find("summary").GetAttribute("aria-label"));
        Assert.Equal("Pays", select.Find("[role='group']").GetAttribute("aria-label"));
    }

    [Fact]
    public void MultiSelect_InAFormField_IsNamedByItsLabel()
    {
        IReadOnlyList<int?> value = [2];
        var field = Render<OmniFormField>(parameters => parameters
            .Add(component => component.For, "pays")
            .Add(component => component.Label, "Pays")
            .AddChildContent<OmniMultiSelect<int?>>(select => select
                .Add(component => component.Id, "pays")
                .Add(component => component.Options, Countries)
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value)));

        var labelId = field.Find("label").Id;
        var textId = field.Find(".omni-multi-select-compact__text").Id;
        Assert.False(string.IsNullOrEmpty(labelId));
        Assert.Equal($"{labelId} {textId}", field.Find("summary").GetAttribute("aria-labelledby"));
        Assert.Equal(labelId, field.Find("[role='group']").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void MultiSelect_WithoutALabel_KeepsNamingItsPanelWithTheSummary()
    {
        IReadOnlyList<int?> value = [];
        var select = Render<OmniMultiSelect<int?>>(parameters => parameters
            .Add(component => component.Options, Countries)
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value));

        Assert.Null(select.Find("summary").GetAttribute("aria-label"));
        Assert.Equal(select.Find(".omni-multi-select-compact__text").TextContent.Trim(), select.Find("[role='group']").GetAttribute("aria-label"));
    }

    // ── OmniUpload ──

    private IRenderedComponent<OmniUpload> RenderUpload(IReadOnlyList<string> allowed, List<int> selections)
    {
        return Render<OmniUpload>(parameters => parameters
            .Add(component => component.AllowedContentTypes, allowed)
            .Add(component => component.OnFilesSelect, EventCallback.Factory.Create<IReadOnlyList<IBrowserFile>>(this, files => selections.Add(files.Count))));
    }

    [Theory]
    [InlineData("image/*", "image/png", true)]
    [InlineData("image/*", "IMAGE/JPEG", true)]
    [InlineData("image/*", "text/plain", false)]
    [InlineData("image/*", "imagery/png", false)]
    [InlineData("image/png", "image/jpeg", false)]
    [InlineData("image/png", "image/PNG", true)]
    public void Upload_AcceptsAFamilyWrittenWithAWildcard(string allowed, string contentType, bool accepted)
    {
        var selections = new List<int>();
        var upload = RenderUpload([allowed], selections);

        upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "f.bin", contentType: contentType));

        Assert.Equal(accepted, selections.Count == 1);
        Assert.Equal(!accepted, upload.Markup.Contains("omni-upload__message--error", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Upload_ASelectionWithNoFile_IsIgnored()
    {
        var selections = new List<int>();
        var upload = RenderUpload([], selections);

        await upload.InvokeAsync(() => upload.FindComponent<InputFile>().Instance.OnChange.InvokeAsync(new InputFileChangeEventArgs([])));

        Assert.Empty(selections);
        Assert.DoesNotContain("omni-upload__message--error", upload.Markup, StringComparison.Ordinal);
    }

    // ── The index of an option ──

    [Theory]
    [InlineData("0", true, "A")]
    [InlineData("2", true, "C")]
    [InlineData("1", false, null)]
    [InlineData("3", false, null)]
    [InlineData("-1", false, null)]
    [InlineData("+0", false, null)]
    [InlineData(" 0", false, null)]
    [InlineData("1.0", false, null)]
    [InlineData("", false, null)]
    [InlineData(null, false, null)]
    [InlineData("x", false, null)]
    public void OptionIndex_ReadsOnlyTheDigitsOfAnEnabledOption(string? text, bool found, string? expected)
    {
        IReadOnlyList<OmniOption<string>> options = [new("A", "A"), new("B", "B", Disabled: true), new("C", "C")];

        Assert.Equal(found, OmniOptionIndex.TryGet(options, text, out var option));
        Assert.Equal(expected, found ? option.Value : null);
    }
}
