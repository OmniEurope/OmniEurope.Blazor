using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders the components host on an overlay service, for the dialog and overlay test classes.</summary>
internal static class OverlayHostRendering
{
    /// <summary>Renders an empty components host that draws the dialogs of <paramref name="service"/>.</summary>
    public static IRenderedComponent<OmniComponentsHost> RenderOverlayHost(this BunitContext context, OmniOverlayService service) =>
        context.Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));
}
