namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// One run of a pipeline, as the two grids of the mockup list them.
/// </summary>
/// <param name="Run">The run number.</param>
/// <param name="Pipeline">The pipeline it ran.</param>
/// <param name="Trigger">What started it, localized.</param>
/// <param name="Seconds">How long it took, a number the grid aligns at the end.</param>
/// <param name="Status">How it ended, localized.</param>
/// <param name="Tone">The intention of that status: success, info, warning or danger.</param>
public sealed record PipelineRun(string Run, string Pipeline, string Trigger, int Seconds, string Status, string Tone);
