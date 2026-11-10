namespace OmniEurope.Blazor.Components;

/// <summary>
/// A duration entered as hours and minutes, bound to a nullable <see cref="TimeSpan"/> by
/// <c>Value</c>/<c>ValueChanged</c>/<c>ValueExpression</c>, so it works in an <c>EditForm</c> like the other
/// fields. Both fields empty is <see langword="null"/>; minutes of an hour or more carry into the hours.
/// </summary>
/// <remarks>
/// Hours are unbounded unless <see cref="Max"/> is set. The bounds are applied in code, on the committed
/// entry, never as native <c>min</c>/<c>max</c> on the field (a native maximum refuses an intermediate entry,
/// which would stop a reader from emptying a field to type another number). The group is named by
/// <see cref="Label"/>, the label of an enclosing form field or the localized "Duration"; each field is
/// named "Hours" or "Minutes" and shows its short unit after it.
/// </remarks>
public partial class OmniDurationPicker
{
    private int? _hours;
    private int? _minutes;
    private int _revision;
    private TimeSpan? _synced;
    private bool _first = true;

    /// <summary>
    /// Names the group. Null uses the label of an enclosing <see cref="OmniFormField"/> whose <c>For</c> is
    /// <see cref="OmniInputBase{TValue}.Id"/> (through <c>aria-labelledby</c>), else the localized "Duration".
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Disables both fields.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the duration without letting it change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The shortest duration; a shorter committed entry is brought back to it. Null for none.</summary>
    [Parameter]
    public TimeSpan? Min { get; set; }

    /// <summary>The longest duration; a longer committed entry is brought back to it. Null for none.</summary>
    [Parameter]
    public TimeSpan? Max { get; set; }

    /// <summary>The step of the minutes field's arrows (<c>step</c>); 1 by default, at least 1.</summary>
    [Parameter]
    public int MinuteStep { get; set; } = 1;

    private string? GroupLabelledBy => string.IsNullOrWhiteSpace(Label) ? FormFieldLabelId : null;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("DurationLabel") : Label;

    private double? MaximumHours => Max is { } max ? Math.Floor(max.TotalHours) : null;

    /// <summary>Checks the parameters and shows a value that changed from outside.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="MinuteStep"/> is less than 1.</exception>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentOutOfRangeException.ThrowIfLessThan(MinuteStep, 1);
        if (_first || CurrentValue != _synced)
        {
            _first = false;
            Show(CurrentValue);
            _synced = CurrentValue;
        }
    }

    /// <summary>Reads text as a <see cref="TimeSpan"/> in the invariant culture; empty text means no duration.</summary>
    /// <param name="value">The text to parse.</param>
    /// <param name="result">The duration read, or null.</param>
    /// <param name="validationErrorMessage">Empty on success; on failure, the localized "invalid number" message.</param>
    /// <returns>True when the text is empty or a duration.</returns>
    protected override bool TryParseValueFromString(string? value, out TimeSpan? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = string.Empty;
            return true;
        }

        var valid = TimeSpan.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var parsed);
        result = valid ? parsed : null;
        validationErrorMessage = valid ? string.Empty : Localize("NumericInvalid");
        return valid;
    }

    private void Show(TimeSpan? value)
    {
        if (value is { } span)
        {
            var total = (long)Math.Floor(span.TotalMinutes);
            _hours = (int)Math.Min(total / 60, int.MaxValue);
            _minutes = (int)(total % 60);
        }
        else
        {
            _hours = null;
            _minutes = null;
        }
    }

    private void SetHours(int? typed) => Commit(typed, _minutes);

    private void SetMinutes(int? typed) => Commit(_hours, typed);

    private void Commit(int? hours, int? minutes)
    {
        if (ReadOnly || Disabled)
        {
            return;
        }

        var result = Bounded(hours, minutes);
        Show(result);
        // A field left empty stays empty while its share is zero: only when both are empty is the group null.
        _hours = hours is null && _hours == 0 ? null : _hours;
        _minutes = minutes is null && _minutes == 0 ? null : _minutes;
        // The typed text on screen is not what the group now holds (minutes carried, a bound applied):
        // draw the fields again so the browser shows the held value.
        if (hours != _hours || minutes != _minutes)
        {
            _revision++;
        }

        _synced = result;
        CurrentValue = result;
    }

    /// <summary>The duration the two fields hold, held between <see cref="Min"/> and <see cref="Max"/>; null when both are empty.</summary>
    private TimeSpan? Bounded(int? hours, int? minutes)
    {
        if (hours is null && minutes is null)
        {
            return null;
        }

        var total = (long)Math.Max(hours ?? 0, 0) * 60 + Math.Max(minutes ?? 0, 0);
        var span = TimeSpan.FromMinutes(Math.Min(total, (long)TimeSpan.MaxValue.TotalMinutes));
        if (Min is { } min && span < min)
        {
            span = min;
        }

        if (Max is { } max && span > max)
        {
            span = max;
        }

        return span;
    }
}
