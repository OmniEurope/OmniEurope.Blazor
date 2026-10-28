namespace OmniEurope.Blazor.Showcase.Examples.Scheduling;

/// <summary>How the date of a timeline entry is written.</summary>
public enum TimelineDateMode
{
    /// <summary>The date is formatted by the component.</summary>
    Automatic,

    /// <summary>The date is written as a relative text; the machine date stays in the datetime attribute.</summary>
    Relative,

    /// <summary>The entry has no date.</summary>
    None
}
