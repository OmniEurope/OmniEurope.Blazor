using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A date picker and a date-time picker sharing bounds, whose seconds and state are parameters.</summary>
public partial class DatesExample
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>The earliest date accepted, fifteen days ago by default; null removes the bound.</summary>
    [Parameter]
    public DateOnly? Minimum { get; set; } = Today.AddDays(-15);

    /// <summary>The latest date accepted, in forty-five days by default; null removes the bound.</summary>
    [Parameter]
    public DateOnly? Maximum { get; set; } = Today.AddDays(45);

    /// <summary>Whether the date-time picker takes the time to the second.</summary>
    [Parameter]
    public bool ShowSeconds { get; set; }

    /// <summary>Whether the values are shown and can be copied but not changed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Whether both pickers are greyed out.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private DateOnly? Date { get; set; } = Today;

    private DateTime? Moment { get; set; } = DateTime.Today.AddHours(9).AddMinutes(30);

    /// <summary>The pickers throw when the minimum is after the maximum.</summary>
    private bool BoundsInverted => Minimum is not null && Maximum is not null && Minimum > Maximum;

    private DateOnly? EffectiveMinimum => BoundsInverted ? null : Minimum;

    private DateOnly? EffectiveMaximum => BoundsInverted ? null : Maximum;

    private DateTime? EffectiveMinimumMoment => EffectiveMinimum?.ToDateTime(TimeOnly.MinValue);

    private DateTime? EffectiveMaximumMoment => EffectiveMaximum?.ToDateTime(new TimeOnly(23, 59, 59));

    private string BoundsText => string.Format(
        CultureInfo.CurrentCulture,
        "Minimum: {0}. Maximum: {1}.",
        EffectiveMinimum?.ToString("d", CultureInfo.CurrentCulture) ?? "none",
        EffectiveMaximum?.ToString("d", CultureInfo.CurrentCulture) ?? "none");

    private static string FormatIso(DateOnly? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "null";

    private static string FormatIso(DateTime? value) =>
        value?.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) ?? "null";

    private static string FormatLong(DateOnly? value) =>
        value?.ToString("D", CultureInfo.CurrentCulture) ?? "(no date)";

    private static string FormatLong(DateTime? value) =>
        value?.ToString("F", CultureInfo.CurrentCulture) ?? "(no date)";
}
