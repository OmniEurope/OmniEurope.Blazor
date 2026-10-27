using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Gallery;

/// <summary>
/// The sources of a gallery page, one tab per file, and the setup a fresh project needs to run them.
/// </summary>
public partial class GalleryCodeView
{
    // The setup of README.md, "Getting started": the package, its namespace, its services and its
    // stylesheet. The examples use nothing else; Microsoft.AspNetCore.Components.Web comes with every
    // Blazor project template. The version is the one of the library this showcase runs.
    private static readonly string PackageReference =
        $"<PackageReference Include=\"OmniEurope.Blazor\" Version=\"{typeof(OmniButton).Assembly.GetName().Version?.ToString(3)}\" />";

    private const string Imports = "@using Microsoft.AspNetCore.Components.Web\n@using OmniEurope.Blazor.Components";

    private const string Registration = "builder.Services.AddOmniEuropeBlazor();";

    private const string Stylesheet = "<link rel=\"stylesheet\" href=\"_content/OmniEurope.Blazor/omnieurope.blazor.css\" />";

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    /// <summary>The paths of the sources, relative to the examples project.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<string> SourceFiles { get; set; } = [];

    private string? Selected { get; set; }

    protected override void OnParametersSet()
    {
        if (Selected is null || !SourceFiles.Contains(Selected))
        {
            Selected = SourceFiles.Count > 0 ? SourceFiles[0] : null;
        }
    }

    private void Select(string? path) => Selected = path;
}
