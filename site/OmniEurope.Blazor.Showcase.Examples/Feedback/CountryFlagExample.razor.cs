using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>A few countries, with or without a picture template and with or without their code; an invalid code last.</summary>
public partial class CountryFlagExample
{
    private const string Template = "https://flagcdn.com/{code}.svg";

    private static readonly string[] Codes = ["FR", "DE", "BE", "ES", "IT", "JP", "ZZ"];

    /// <summary>Whether the code is written beside the picture.</summary>
    [Parameter]
    public bool ShowCode { get; set; } = true;

    /// <summary>Whether the page gives a picture template; without it the flags are codes in badges.</summary>
    [Parameter]
    public bool UseTemplate { get; set; } = true;
}
