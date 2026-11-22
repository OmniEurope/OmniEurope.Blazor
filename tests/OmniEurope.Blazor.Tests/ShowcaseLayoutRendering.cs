using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Showcase.Components.Layout;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders the showcase layout around a one-paragraph page, for the showcase shell test classes.</summary>
internal static class ShowcaseLayoutRendering
{
    public static IRenderedComponent<MainLayout> RenderShowcaseLayout(this BunitContext context) =>
        context.Render<MainLayout>(parameters => parameters.Add(layout => layout.Body, (RenderFragment)(builder => builder.AddMarkupContent(0, "<p id=\"page\">page</p>"))));

    /// <summary>The services the shell needs, the stylesheet it reads its tokens from answering <paramref name="css"/>.</summary>
    public static IServiceCollection AddShowcaseShell(this IServiceCollection services, string css)
    {
        services.AddScoped(_ => new HttpClient(new StylesheetResponder(css)) { BaseAddress = new Uri("https://localhost/") });
        services.AddScoped<ThemeTokenReader>();
        services.AddScoped<ThemeState>();
        services.AddScoped<ShowcaseLanguageSwitcher>();
        return services;
    }
}
