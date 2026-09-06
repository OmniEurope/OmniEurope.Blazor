using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Pages;

public partial class Documentation
{
    private const string InstallSnippet = "dotnet add package OmniEurope.Blazor";

    private const string RegisterSnippet = """
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddOmniEuropeBlazor();
        """;

    private const string StylesheetSnippet =
        """<link rel="stylesheet" href="_content/OmniEurope.Blazor/omnieurope.blazor.css" />""";

    private const string CspSnippet =
        "default-src 'self'; style-src 'self'; script-src 'self' 'wasm-unsafe-eval'";

    private const string ThemeSnippet = """
        :root {
            --omni-color-accent: #6d28d9;
            --omni-radius: 0.5rem;
        }
        """;

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;
}
