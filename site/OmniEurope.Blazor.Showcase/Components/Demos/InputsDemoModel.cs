namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The state the extended-input demonstration edits.
/// </summary>
public sealed class InputsDemoModel
{
    /// <summary>The numeric field.</summary>
    public decimal Amount { get; set; } = 1250;

    /// <summary>The password field.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>The date field.</summary>
    public DateOnly? Date { get; set; } = new(2026, 3, 2);

    /// <summary>The date-and-time field.</summary>
    public DateTime? Appointment { get; set; } = new(2026, 3, 2, 14, 30, 0, DateTimeKind.Unspecified);

    /// <summary>The colour field.</summary>
    public string Color { get; set; } = "#165dff";

    /// <summary>The autocompleted city.</summary>
    public string City { get; set; } = string.Empty;
}
