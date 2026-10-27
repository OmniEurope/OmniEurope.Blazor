namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The selection, date, choice and file fields of the vertical form that stay in this demo.
/// </summary>
public sealed class VerticalFormDemoModel
{
    /// <summary>The target environment.</summary>
    public string Environment { get; set; } = "production";

    /// <summary>The deployment date.</summary>
    public DateOnly? Date { get; set; } = new(2026, 9, 18);

    /// <summary>The deployment time.</summary>
    public TimeOnly? Time { get; set; } = new(14, 30);

    /// <summary>The maintenance window.</summary>
    public DateTime? Moment { get; set; } = new(2026, 9, 18, 22, 0, 0, DateTimeKind.Unspecified);

    /// <summary>The deployment strategy, required; none is chosen at first so the error shows.</summary>
    public string? Strategy { get; set; }

    /// <summary>The build agent, disabled.</summary>
    public string Agent { get; set; } = "shared";
}
