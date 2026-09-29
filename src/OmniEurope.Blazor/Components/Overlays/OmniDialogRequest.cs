using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// A dialog opened by <see cref="OmniOverlayService.OpenDialog(OmniDialogRequest)"/>: its title, its
/// content and the options of <see cref="OmniDialog"/> it is drawn with.
/// </summary>
/// <param name="Title">The dialog title.</param>
/// <param name="Content">The body of the dialog.</param>
/// <param name="CloseLabel">
/// Accessible name and tooltip of the close button. Null, the default, is the localized "Close"
/// (<see cref="OmniComponentsHost"/> also reads the legacy French value of already compiled alpha
/// clients as null).
/// </param>
public sealed record OmniDialogRequest(string Title, RenderFragment Content, string? CloseLabel = null)
{
    /// <summary>The footer, in general the action buttons, passed to <see cref="OmniDialog.Footer"/>.</summary>
    public RenderFragment? Footer { get; init; }

    /// <summary>What the title heading shows in place of <c>Title</c>, passed to <see cref="OmniDialog.TitleContent"/>. Null shows <c>Title</c>.</summary>
    public RenderFragment? TitleContent { get; init; }

    /// <summary>
    /// Whether a click on the backdrop closes the dialog. True by default: false keeps the dialog
    /// open when the reader clicks beside it, while the close button and Escape still close it.
    /// </summary>
    public bool CloseOnBackdrop { get; init; } = true;

    /// <summary>
    /// Whether the reader can dismiss the dialog. True by default: the close button, Escape and the
    /// backdrop close it. False removes the close button, ignores Escape and the backdrop, and
    /// announces the panel as an <c>alertdialog</c>: it closes only when its content calls
    /// <see cref="OmniOverlayService.CloseDialog()"/> or <see cref="OmniOverlayService.CloseDialog(object?)"/>.
    /// Focus stays trapped inside it.
    /// </summary>
    public bool Dismissible { get; init; } = true;

    /// <summary>Draws the close button of a dismissible dialog, passed to <see cref="OmniDialog.ShowClose"/>. True by default.</summary>
    public bool ShowClose { get; init; } = true;

    /// <summary>Lets the reader move the dialog by its header, passed to <see cref="OmniDialog.Draggable"/>.</summary>
    public bool Draggable { get; init; }

    /// <summary>Lets the reader resize the dialog, passed to <see cref="OmniDialog.Resizable"/>.</summary>
    public bool Resizable { get; init; }

    /// <summary>
    /// How wide the dialog may grow, passed to <see cref="OmniDialog.Size"/>.
    /// <see cref="OmniDialogSize.Medium"/> by default.
    /// </summary>
    public OmniDialogSize Size { get; init; }

    /// <summary>
    /// What the dialog is for, passed to <see cref="OmniDialog.Intent"/>: its header and footer take the
    /// tint and the title the mark. <see cref="OmniTone.Neutral"/> by default: no tint, no mark.
    /// </summary>
    public OmniTone Intent { get; init; } = OmniTone.Neutral;
}
