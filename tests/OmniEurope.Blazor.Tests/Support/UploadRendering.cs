using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Renders a multiple upload over a list of files it binds, for the upload test classes.</summary>
internal static class UploadRendering
{
    /// <summary>Renders a multiple upload holding <paramref name="files"/>, bound as <c>@bind-Files</c> would.</summary>
    public static IRenderedComponent<OmniUpload> RenderMultipleUpload(this BunitContext context, IReadOnlyList<OmniUploadFile> files) =>
        context.Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next));
}
