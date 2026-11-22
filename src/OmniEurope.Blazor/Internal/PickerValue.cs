namespace OmniEurope.Blazor.Internal;

/// <summary>Reads the text of a picker field into a value.</summary>
/// <typeparam name="T">The value type of the picker.</typeparam>
/// <param name="text">The non-blank text of the field.</param>
/// <param name="value">The value read.</param>
/// <returns>True when the text is a value.</returns>
internal delegate bool PickerParser<T>(string text, out T value);

/// <summary>
/// The checks and the parsing shared by the date, date and time, and time pickers: ordered bounds, and a
/// blank-or-bounded reading of the field.
/// </summary>
internal static class PickerValue
{
    /// <summary>Throws <see cref="InvalidOperationException"/> when both bounds are set and the minimum is later than the maximum.</summary>
    internal static void EnsureOrdered<T>(T? minimum, T? maximum)
        where T : struct, IComparable<T>
    {
        if (minimum is { } floor && maximum is { } ceiling && floor.CompareTo(ceiling) > 0)
        {
            throw new InvalidOperationException("Minimum cannot be greater than Maximum.");
        }
    }

    /// <summary>
    /// Blank text is no value; else the text read by <paramref name="parse"/> when it is not outside the bounds;
    /// else a failure with the message of <paramref name="invalidMessage"/>.
    /// </summary>
    /// <returns>True when the text is blank or a value within the bounds.</returns>
    internal static bool TryRead<T>(
        string? text,
        PickerParser<T> parse,
        Func<T, bool> isOutside,
        Func<string> invalidMessage,
        out T? result,
        out string validationErrorMessage)
        where T : struct
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            result = null;
            validationErrorMessage = null!;
            return true;
        }

        if (parse(text, out var value) && !isOutside(value))
        {
            result = value;
            validationErrorMessage = null!;
            return true;
        }

        result = null;
        validationErrorMessage = invalidMessage();
        return false;
    }
}
