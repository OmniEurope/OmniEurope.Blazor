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

    /// <summary>
    /// Where the focus goes when the dialog opens, passed to <see cref="OmniDialog.InitialFocus"/>.
    /// <see cref="OmniDialogInitialFocus.CloseButton"/> by default.
    /// </summary>
    public OmniDialogInitialFocus InitialFocus { get; init; }

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
    /// A free width in place of <see cref="Size"/>, passed to <see cref="OmniDialog.Width"/>: a positive
    /// number followed by px, rem, em, ch, vw or % (<c>30rem</c>). Checked when the dialog is opened:
    /// <see cref="OmniOverlayService.OpenDialog(OmniDialogRequest)"/> throws
    /// <see cref="ArgumentException"/> for anything else. Null by default.
    /// </summary>
    public string? Width { get; init; }

    /// <summary>
    /// A request whose content is <typeparamref name="TComponent"/>, each entry of
    /// <paramref name="parameters"/> passed to it as the parameter of that name, as
    /// <see cref="OmniOverlayService.OpenDialogAsync{TComponent}(string, IReadOnlyDictionary{string, object?}?, string)"/>
    /// builds it; set the other options with <c>with</c> (<c>Width</c>, <c>CloseOnBackdrop</c>...)
    /// before passing it to <see cref="OmniOverlayService.OpenDialogAsync(OmniDialogRequest)"/>.
    /// </summary>
    /// <typeparam name="TComponent">The component drawn in the dialog body.</typeparam>
    /// <param name="title">The dialog title.</param>
    /// <param name="parameters">The parameters of the component, by name; null passes none.</param>
    /// <param name="closeLabel">Accessible name of the close button; null is the localized "Close".</param>
    /// <returns>The request, every other option at its default.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="title"/> is null.</exception>
    public static OmniDialogRequest ForComponent<TComponent>(
        string title,
        IReadOnlyDictionary<string, object?>? parameters = null,
        string? closeLabel = null)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(title);
        return new OmniDialogRequest(title, builder =>
        {
            builder.OpenComponent<TComponent>(0);
            if (parameters is not null)
            {
                foreach (var (name, value) in parameters)
                {
                    builder.AddComponentParameter(1, name, value);
                }
            }

            builder.CloseComponent();
        }, closeLabel);
    }

    /// <summary>
    /// What the dialog is for, passed to <see cref="OmniDialog.Intent"/>: its header and footer take the
    /// tint and the title the mark. <see cref="OmniTone.Neutral"/> by default: no tint, no mark.
    /// </summary>
    public OmniTone Intent { get; init; } = OmniTone.Neutral;
}
