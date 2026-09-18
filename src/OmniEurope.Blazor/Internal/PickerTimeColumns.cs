using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Internal;

/// <summary>A change made in the time columns: the new time and the column it was made in.</summary>
internal readonly record struct PickerTimeChange(TimeOnly Time, PickerTimePart Part, bool FromKeyboard);

internal enum PickerTimePart
{
    Hour,
    Minute,
    Second
}

/// <summary>
/// The scrolling columns shared by <c>OmniTimePicker</c> and <c>OmniDateTimePicker</c>: hours from
/// 00 to 23, minutes by the picker's step and, when asked, seconds. Each column is a
/// <c>listbox</c> whose options are buttons; a choice applies at once, like a native select.
/// </summary>
/// <remarks>
/// Only one item per column is in the tab order: the chosen one, or the first that can be chosen.
/// The arrows move the choice up and down the column, Home and End to its ends, skipping what the
/// bounds exclude; Enter and Space are the native activation of the item button. Written as a
/// render tree so that it stays internal.
/// </remarks>
internal sealed class PickerTimeColumns : ComponentBase
{
    [Inject]
    private IStringLocalizer<AppStrings> Strings { get; set; } = default!;

    [Parameter, EditorRequired]
    public string IdPrefix { get; set; } = string.Empty;

    /// <summary>The chosen time, or null when there is none yet.</summary>
    [Parameter]
    public TimeOnly? Value { get; set; }

    /// <summary>Minutes between two items of the minute column.</summary>
    [Parameter]
    public int Step { get; set; } = 5;

    [Parameter]
    public bool ShowSeconds { get; set; }

    /// <summary>Whether a time can be chosen; the columns disable an item no time of which can.</summary>
    [Parameter]
    public Func<TimeOnly, TimeOnly, bool> IsRangeAllowed { get; set; } = static (_, _) => true;

    [Parameter]
    public EventCallback<PickerTimeChange> OnChange { get; set; }

    private IEnumerable<int> Values(PickerTimePart part) => part switch
    {
        PickerTimePart.Hour => Enumerable.Range(0, 24),
        PickerTimePart.Minute => Enumerable.Range(0, (59 / Step) + 1).Select(index => index * Step),
        _ => Enumerable.Range(0, 60)
    };

    private int? Selected(PickerTimePart part) => Value is not { } time
        ? null
        : part switch
        {
            PickerTimePart.Hour => time.Hour,
            PickerTimePart.Minute => time.Minute,
            _ => time.Second
        };

    private TimeOnly Compose(PickerTimePart part, int value)
    {
        var current = Value ?? TimeOnly.MinValue;
        var second = ShowSeconds ? current.Second : 0;
        return part switch
        {
            PickerTimePart.Hour => new TimeOnly(value, current.Minute, second),
            PickerTimePart.Minute => new TimeOnly(current.Hour, value, second),
            _ => new TimeOnly(current.Hour, current.Minute, value)
        };
    }

    /// <summary>Whether any time the item stands for can be chosen, given the other columns.</summary>
    private bool IsEnabled(PickerTimePart part, int value)
    {
        var current = Value ?? TimeOnly.MinValue;
        return part switch
        {
            PickerTimePart.Hour => IsRangeAllowed(new TimeOnly(value, 0), new TimeOnly(value, 59, 59)),
            PickerTimePart.Minute => IsRangeAllowed(new TimeOnly(current.Hour, value), new TimeOnly(current.Hour, value, 59)),
            _ => IsRangeAllowed(new TimeOnly(current.Hour, current.Minute, value), new TimeOnly(current.Hour, current.Minute, value))
        };
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-time");
        AddColumn(builder, 2, PickerTimePart.Hour, "TimePickerHourCaption", "TimePickerHours");
        AddColumn(builder, 3, PickerTimePart.Minute, "TimePickerMinuteCaption", "TimePickerMinutes");
        if (ShowSeconds)
        {
            AddColumn(builder, 4, PickerTimePart.Second, "TimePickerSecondCaption", "TimePickerSeconds");
        }

        builder.CloseElement();
    }

    private void AddColumn(RenderTreeBuilder builder, int sequence, PickerTimePart part, string captionKey, string labelKey)
    {
        var selected = Selected(part);
        var enabled = Values(part).Where(value => IsEnabled(part, value)).ToArray();
        int? tabStop = selected is { } chosen && enabled.Contains(chosen) ? chosen : enabled.Length > 0 ? enabled[0] : null;
        var listId = $"{IdPrefix}-{part.ToString().ToLowerInvariant()}";

        builder.OpenRegion(sequence);
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-time__col");
        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "class", "omni-time__label");
        builder.AddAttribute(4, "aria-hidden", "true");
        builder.AddContent(5, Strings[captionKey].Value);
        builder.CloseElement();
        builder.OpenElement(6, "div");
        builder.AddAttribute(7, "class", "omni-time__list");
        builder.AddAttribute(8, "id", listId);
        builder.AddAttribute(9, "role", "listbox");
        builder.AddAttribute(10, "aria-label", Strings[labelKey].Value);
        builder.AddAttribute(11, "data-omni-part", part.ToString().ToLowerInvariant());
        builder.AddAttribute(12, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, args => HandleKeyDownAsync(part, enabled, args)));
        foreach (var value in Values(part))
        {
            builder.OpenElement(13, "button");
            builder.SetKey(value);
            builder.AddAttribute(14, "type", "button");
            builder.AddAttribute(15, "class", "omni-time__item");
            builder.AddAttribute(16, "role", "option");
            builder.AddAttribute(17, "tabindex", value == tabStop ? "0" : "-1");
            builder.AddAttribute(18, "aria-selected", value == selected ? "true" : "false");
            builder.AddAttribute(19, "disabled", !enabled.Contains(value));
            builder.AddAttribute(20, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnChange.InvokeAsync(new PickerTimeChange(Compose(part, value), part, FromKeyboard: false))));
            builder.AddContent(21, value.ToString("00", CultureInfo.InvariantCulture));
            builder.CloseElement();
        }

        builder.CloseElement();
        builder.CloseElement();
        builder.CloseRegion();
    }

    private Task HandleKeyDownAsync(PickerTimePart part, int[] enabled, KeyboardEventArgs args)
    {
        if (enabled.Length == 0)
        {
            return Task.CompletedTask;
        }

        var index = Selected(part) is { } chosen ? Array.IndexOf(enabled, chosen) : -1;
        int? next = args.Key switch
        {
            "ArrowDown" => enabled[Math.Min(enabled.Length - 1, index + 1)],
            "ArrowUp" => enabled[Math.Max(0, index < 0 ? 0 : index - 1)],
            "Home" => enabled[0],
            "End" => enabled[^1],
            _ => null
        };

        return next is { } value
            ? OnChange.InvokeAsync(new PickerTimeChange(Compose(part, value), part, FromKeyboard: true))
            : Task.CompletedTask;
    }
}
