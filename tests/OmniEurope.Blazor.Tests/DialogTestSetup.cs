using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>The dialogs the dialog suites open: the components host over an overlay service, and an open dialog of their own.</summary>
internal static class DialogTestSetup
{
    /// <summary>The components host that shows what <paramref name="service"/> opens.</summary>
    internal static IRenderedComponent<OmniComponentsHost> RenderOverlayHost(this BunitContext context, OmniOverlayService service) =>
        context.Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));

    /// <summary>An open "Dossier" dialog, given <paramref name="width"/> and <paramref name="size"/> when there are.</summary>
    internal static IRenderedComponent<OmniDialog> RenderFolderDialog(this BunitContext context, string? width, OmniDialogSize? size) =>
        context.Render<OmniDialog>(parameters =>
        {
            parameters
                .Add(component => component.Open, true)
                .Add(component => component.Title, "Dossier")
                .AddChildContent("Contenu");
            if (width is not null)
            {
                parameters.Add(component => component.Width, width);
            }

            if (size is { } value)
            {
                parameters.Add(component => component.Size, value);
            }
        });

    /// <summary>A content made of <paramref name="value"/>.</summary>
    internal static RenderFragment Content(string value) => builder => builder.AddContent(0, value);
}
