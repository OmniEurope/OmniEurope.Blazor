using System.Globalization;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A date and a time in one native control, bound to <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// Separate from <see cref="OmniDatePicker"/> rather than a flag on it: the two bind different
/// types (<see cref="DateOnly"/> against <see cref="DateTime"/>), and a picker that changed the
/// type of its own value depending on a boolean could not be bound at compile time.
/// The value is local time, exactly what the browser control shows and returns; converting to UTC
/// is the caller's decision, because the component cannot know whether the instant or the wall
/// clock is what the domain means.
/// </remarks>
public partial class OmniDateTimePicker
{
    private const string Format = "yyyy-MM-ddTHH:mm";

    [Parameter]
    public DateTime? Minimum { get; set; }

    [Parameter]
    public DateTime? Maximum { get; set; }

    /// <summary>Whether the control offers seconds. Off by default, as most schedules are to the minute.</summary>
    [Parameter]
    public bool ShowSeconds { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string? StepAttribute => ShowSeconds ? "1" : null;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Minimum is not null && Maximum is not null && Minimum > Maximum)
        {
            throw new InvalidOperationException("Minimum cannot be greater than Maximum.");
        }
    }

    protected override string? FormatValueAsString(DateTime? value) => FormatDateTime(value);

    private string? FormatDateTime(DateTime? value) =>
        value?.ToString(ShowSeconds ? "yyyy-MM-ddTHH:mm:ss" : Format, CultureInfo.InvariantCulture);

    private void HandleChange(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out DateTime? result, out string validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        // Both shapes are accepted whatever ShowSeconds says: a browser sends seconds when the value
        // it already holds has them, and rejecting that would silently empty the field.
        string[] formats = ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"];
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment)
            && (Minimum is null || moment >= Minimum)
            && (Maximum is null || moment <= Maximum))
        {
            result = moment;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = Localize("DateTimePickerInvalid");
        return false;
    }
}
