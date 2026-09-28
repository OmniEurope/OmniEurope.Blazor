namespace OmniEurope.Blazor.Components;

/// <summary>
/// The panel rendered outside the steps that shows every step, as the body of <see cref="OmniWizard"/>:
/// cascaded to the items, it makes each button control that panel instead of rendering one of its own.
/// </summary>
internal sealed record OmniStepsSharedPanel(string Id);
