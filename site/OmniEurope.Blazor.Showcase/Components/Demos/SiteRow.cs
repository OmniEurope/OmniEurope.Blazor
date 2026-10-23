namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// One row of the grid whose columns are sized to their content.
/// </summary>
/// <param name="Name">The site name.</param>
/// <param name="Online">Whether the site answers.</param>
/// <param name="Visitors">The visitors on the site now.</param>
/// <param name="Project">The project the site belongs to.</param>
/// <param name="Since">When the site went live.</param>
public sealed record SiteRow(string Name, bool Online, int Visitors, string Project, DateOnly Since);
