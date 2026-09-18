using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The month grid shared by <c>OmniDatePicker</c> and <c>OmniDateTimePicker</c>: a heading with the
/// month and the previous and next buttons, the week days of the culture, starting on its first day,
/// then six weeks of days, so that the panel keeps its height from one month to the next.
/// </summary>
/// <remarks>
/// Controlled: the picker holds the shown month, the day that has the focus and the chosen day, and
/// this grid only reports what the user asks for. The days are a roving <c>tabindex</c>: only the
/// focused day is in the tab order, the arrows move it by a day or a week, the page keys by a month
/// (a year with Shift), Home and End to the start and the end of the week, never past the bounds.
/// Enter and Space are the native activation of the day button. Written as a render tree rather than
/// in Razor so that it stays internal: a Razor component is always public.
/// </remarks>
internal sealed class PickerCalendar : ComponentBase
{
    private const string PreviousGlyph = "m15 18-6-6 6-6";
    private const string NextGlyph = "m9 18 6-6-6-6";

    [Inject]
    private IStringLocalizer<AppStrings> Strings { get; set; } = default!;

    [Parameter, EditorRequired]
    public string IdPrefix { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>Any day of the month on show.</summary>
    [Parameter]
    public DateOnly VisibleMonth { get; set; }

    [Parameter]
    public DateOnly FocusedDate { get; set; }

    [Parameter]
    public DateOnly? SelectedDate { get; set; }

    [Parameter]
    public DateOnly Today { get; set; }

    [Parameter]
    public DateOnly? Minimum { get; set; }

    [Parameter]
    public DateOnly? Maximum { get; set; }

    [Parameter]
    public EventCallback<DateOnly> OnSelect { get; set; }

    /// <summary>The focused day moved from the keyboard; the picker shows its month and focuses it.</summary>
    [Parameter]
    public EventCallback<DateOnly> OnFocusChange { get; set; }

    /// <summary>The previous (-1) or next (+1) month button.</summary>
    [Parameter]
    public EventCallback<int> OnMonthStep { get; set; }

    private DateOnly FirstOfMonth => new(VisibleMonth.Year, VisibleMonth.Month, 1);

    private DayOfWeek FirstDayOfWeek => Culture.DateTimeFormat.FirstDayOfWeek;

    internal static DateOnly Clamp(DateOnly date, DateOnly? minimum, DateOnly? maximum)
    {
        if (minimum is { } floor && date < floor)
        {
            return floor;
        }

        return maximum is { } ceiling && date > ceiling ? ceiling : date;
    }

    internal static bool IsOutside(DateOnly date, DateOnly? minimum, DateOnly? maximum) =>
        (minimum is { } floor && date < floor) || (maximum is { } ceiling && date > ceiling);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var titleId = $"{IdPrefix}-title";
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-picker__cal");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "omni-calendar__head");
        builder.OpenElement(4, "span");
        builder.AddAttribute(5, "class", "omni-calendar__title");
        builder.AddAttribute(6, "id", titleId);
        builder.AddAttribute(7, "aria-live", "polite");
        builder.AddContent(8, FirstOfMonth.ToString(Culture.DateTimeFormat.YearMonthPattern, Culture));
        builder.CloseElement();
        builder.OpenElement(9, "span");
        builder.AddAttribute(10, "class", "omni-calendar__nav");
        var previousMonthEnd = FirstOfMonth.AddDays(-1);
        var nextMonthStart = FirstOfMonth.AddMonths(1);
        AddStepButton(builder, 11, -1, Strings["CalendarPreviousMonth"].Value, PreviousGlyph, Minimum is { } floor && previousMonthEnd < floor);
        AddStepButton(builder, 12, 1, Strings["CalendarNextMonth"].Value, NextGlyph, Maximum is { } ceiling && nextMonthStart > ceiling);
        builder.CloseElement();
        builder.CloseElement();

        builder.OpenElement(20, "div");
        builder.AddAttribute(21, "class", "omni-calendar__grid");
        builder.AddAttribute(22, "role", "grid");
        builder.AddAttribute(23, "aria-labelledby", titleId);
        builder.AddAttribute(24, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));

        builder.OpenElement(25, "div");
        builder.AddAttribute(26, "class", "omni-calendar__row");
        builder.AddAttribute(27, "role", "row");
        for (var index = 0; index < 7; index++)
        {
            var day = (int)(FirstDayOfWeek + index) % 7;
            builder.OpenElement(28, "span");
            builder.SetKey(day);
            builder.AddAttribute(29, "class", "omni-calendar__weekday");
            builder.AddAttribute(30, "role", "columnheader");
            builder.AddAttribute(31, "aria-label", Culture.DateTimeFormat.DayNames[day]);
            builder.AddContent(32, Culture.DateTimeFormat.AbbreviatedDayNames[day].TrimEnd('.'));
            builder.CloseElement();
        }

        builder.CloseElement();

        var leading = (7 + (int)FirstOfMonth.DayOfWeek - (int)FirstDayOfWeek) % 7;
        var start = FirstOfMonth.AddDays(-leading);
        for (var week = 0; week < 6; week++)
        {
            builder.OpenElement(40, "div");
            builder.SetKey(week);
            builder.AddAttribute(41, "class", "omni-calendar__row");
            builder.AddAttribute(42, "role", "row");
            for (var column = 0; column < 7; column++)
            {
                AddDay(builder, start.AddDays((week * 7) + column));
            }

            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();
    }

    private void AddStepButton(RenderTreeBuilder builder, int sequence, int step, string label, string glyph, bool disabled)
    {
        builder.OpenRegion(sequence);
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "type", "button");
        builder.AddAttribute(2, "class", "omni-button omni-button--ghost omni-button--medium omni-calendar__step");
        builder.AddAttribute(3, "aria-label", label);
        builder.AddAttribute(4, "disabled", disabled);
        builder.AddAttribute(5, "data-omni-step", step < 0 ? "previous" : "next");
        builder.AddAttribute(6, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnMonthStep.InvokeAsync(step)));
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "class", "omni-button__content");
        AddGlyph(builder, 9, glyph);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseRegion();
    }

    internal static void AddGlyph(RenderTreeBuilder builder, int sequence, string path)
    {
        builder.OpenRegion(sequence);
        builder.OpenElement(0, "svg");
        builder.AddAttribute(1, "class", "omni-icon omni-icon--small");
        builder.AddAttribute(2, "viewBox", "0 0 24 24");
        builder.AddAttribute(3, "fill", "none");
        builder.AddAttribute(4, "stroke", "currentColor");
        builder.AddAttribute(5, "stroke-linecap", "round");
        builder.AddAttribute(6, "stroke-linejoin", "round");
        builder.AddAttribute(7, "stroke-width", "1.9");
        builder.AddAttribute(8, "focusable", "false");
        builder.AddAttribute(9, "aria-hidden", "true");
        builder.OpenElement(10, "path");
        builder.AddAttribute(11, "d", path);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseRegion();
    }

    private void AddDay(RenderTreeBuilder builder, DateOnly day)
    {
        var outsideMonth = day.Month != FirstOfMonth.Month;
        var isToday = day == Today;
        var disabled = IsOutside(day, Minimum, Maximum);
        var css = CssClassBuilder.Combine(
        [
            "omni-calendar__day",
            outsideMonth ? "omni-calendar__day--outside" : null,
            isToday ? "omni-calendar__day--today" : null
        ]);

        builder.OpenElement(50, "button");
        builder.SetKey(day);
        builder.AddAttribute(51, "type", "button");
        builder.AddAttribute(52, "class", css);
        builder.AddAttribute(53, "role", "gridcell");
        builder.AddAttribute(54, "tabindex", day == FocusedDate ? "0" : "-1");
        builder.AddAttribute(55, "aria-selected", day == SelectedDate ? "true" : "false");
        builder.AddAttribute(56, "aria-current", isToday ? "date" : null);
        builder.AddAttribute(57, "aria-label", day.ToString("D", Culture));
        builder.AddAttribute(58, "disabled", disabled);
        builder.AddAttribute(59, "data-date", day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        builder.AddAttribute(60, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnSelect.InvokeAsync(day)));
        builder.AddContent(61, day.Day.ToString(Culture));
        builder.CloseElement();
    }

    private Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        var from = FocusedDate;
        DateOnly? target = args.Key switch
        {
            "ArrowLeft" => from.AddDays(-1),
            "ArrowRight" => from.AddDays(1),
            "ArrowUp" => from.AddDays(-7),
            "ArrowDown" => from.AddDays(7),
            "PageUp" => from.AddMonths(args.ShiftKey ? -12 : -1),
            "PageDown" => from.AddMonths(args.ShiftKey ? 12 : 1),
            "Home" => from.AddDays(-((7 + (int)from.DayOfWeek - (int)FirstDayOfWeek) % 7)),
            "End" => from.AddDays(6 - ((7 + (int)from.DayOfWeek - (int)FirstDayOfWeek) % 7)),
            _ => null
        };

        return target is { } next
            ? OnFocusChange.InvokeAsync(Clamp(next, Minimum, Maximum))
            : Task.CompletedTask;
    }
}
