namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>How the slider example turns the number it holds into the text it displays.</summary>
public enum SliderFormat
{
    /// <summary>The number as it is.</summary>
    Raw,

    /// <summary>The share of the travel covered, whatever the bounds.</summary>
    Percent,

    /// <summary>An amount in euros.</summary>
    Euro,

    /// <summary>A distance in kilometres.</summary>
    Kilometres
}
