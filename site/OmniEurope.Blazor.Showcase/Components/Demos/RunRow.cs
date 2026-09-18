namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// One run of the pipeline grid, the one the mockup draws.
/// </summary>
/// <param name="Run">The run number.</param>
/// <param name="Pipeline">The pipeline it ran.</param>
/// <param name="Seconds">How long it took, a number the grid aligns at the end.</param>
/// <param name="Status">How it ended.</param>
public sealed record RunRow(string Run, string Pipeline, int Seconds, string Status);
