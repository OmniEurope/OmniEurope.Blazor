using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Showcase.Components.Layout;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders the showcase layout around a one-paragraph page, for the showcase shell test classes.</summary>
internal static class ShowcaseLayoutRendering
{
    public static IRenderedComponent<MainLayout> RenderShowcaseLayout(this BunitContext context) =>
        context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p id=\"page\">page</p>"))));
}
