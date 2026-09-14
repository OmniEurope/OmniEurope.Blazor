using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

// An empty optional value is resolved by OmniDialog from the current UI culture.
// OmniOverlayHosts also recognizes the legacy French value passed by already compiled alpha clients.
public sealed record OmniDialogRequest(string Title, RenderFragment Content, string CloseLabel = "")
{
    public RenderFragment? Footer { get; init; }

    /// <summary>
    /// Whether a click on the backdrop closes the dialog. True by default, as before: false keeps
    /// the dialog open when the reader clicks beside it, while the close button and Escape still
    /// close it.
    /// </summary>
    public bool CloseOnBackdropClick { get; init; } = true;

    /// <summary>
    /// Whether the reader can dismiss the dialog. True by default, as before. False removes the
    /// close button, ignores Escape and the backdrop, and announces the panel as an
    /// <c>alertdialog</c>: it closes only when its content calls
    /// <see cref="OmniOverlayService.CloseDialog()"/> or <see cref="OmniOverlayService.CloseDialog(object?)"/>.
    /// Focus stays trapped inside it.
    /// </summary>
    public bool Dismissible { get; init; } = true;
}
