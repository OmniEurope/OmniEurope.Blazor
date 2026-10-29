namespace OmniEurope.Blazor.Components;

/// <summary>Where a form shows its <see cref="OmniValidationSummary"/>.</summary>
public enum OmniValidationSummaryPosition
{
    /// <summary>Above the fields.</summary>
    Top,

    /// <summary>Just above the form's actions, after the fields: the default, nearest to the button that sends the form.</summary>
    Bottom,

    /// <summary>Nowhere: the errors show only under their fields.</summary>
    Hidden
}
