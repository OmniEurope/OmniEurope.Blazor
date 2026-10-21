namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// One row of the grid whose columns size themselves to their content.
/// </summary>
/// <param name="Name">The package name, a long value without a break that the column must show whole.</param>
/// <param name="Description">A long sentence, localized, that its column cuts with an ellipsis (Truncate).</param>
public sealed record PackageRow(string Name, string Description);
