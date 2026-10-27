namespace OmniEurope.Blazor.Showcase.Gallery;

/// <summary>
/// One page of the gallery: its address, the keys of its title and description, its icon, the
/// components it exercises and the example sources its Code tab shows.
/// </summary>
/// <param name="Slug">The last segment of the page address, <c>components/{Slug}</c>.</param>
/// <param name="TitleKey">Resource key of the title, in the strings of the page's family.</param>
/// <param name="DescriptionKey">Resource key of the one-paragraph description, in the strings of the
/// page's family.</param>
/// <param name="Icon">The icon of the page in the menu and in its header.</param>
/// <param name="Components">The public components the page exercises, shown as badges.</param>
/// <param name="SourceFiles">The example files the Code tab shows, by their path in the examples
/// project (<c>Actions/ButtonsExample.razor</c>), in the order of the tabs.</param>
public sealed record GalleryPage(
    string Slug,
    string TitleKey,
    string DescriptionKey,
    OmniIconName Icon,
    IReadOnlyList<string> Components,
    IReadOnlyList<string> SourceFiles)
{
    /// <summary>The address prefix of every gallery page.</summary>
    public const string RoutePrefix = "components";

    /// <summary>The address of the page, relative to the base address of the site.</summary>
    public string Href => $"{RoutePrefix}/{Slug}";
}
