using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Writes a bound date or moment as the date examples show it: ISO 8601, or <c>null</c> when empty.</summary>
public static class DateIsoFormat
{
    /// <summary>Writes a date as <c>yyyy-MM-dd</c>.</summary>
    /// <param name="value">The bound date, if any.</param>
    /// <returns>The ISO date, or <c>null</c>.</returns>
    public static string FormatIso(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "null";

    /// <summary>Writes a moment as <c>yyyy-MM-ddTHH:mm:ss</c>.</summary>
    /// <param name="value">The bound moment, if any.</param>
    /// <returns>The ISO moment, or <c>null</c>.</returns>
    public static string FormatIso(DateTime? value) =>
        value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) ?? "null";
}
