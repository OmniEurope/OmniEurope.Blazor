using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>The HTML editor with a toolbar the page picks, reorders, trims and extends, and the HTML it binds.</summary>
public partial class HtmlEditorExample
{
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> EssentialCommands =
    [
        OmniHtmlEditorCommands.BlockFormat,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Bold,
        OmniHtmlEditorCommands.Italic,
        OmniHtmlEditorCommands.Link,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.BulletList,
        OmniHtmlEditorCommands.NumberedList,
        OmniHtmlEditorCommands.Separator,
        OmniHtmlEditorCommands.Undo,
        OmniHtmlEditorCommands.Redo,
        OmniHtmlEditorCommands.ToggleSource
    ];

    private static readonly IReadOnlyList<OmniHtmlEditorCommand> ReorderedCommands =
    [
        OmniHtmlEditorCommands.Undo,
        OmniHtmlEditorCommands.Redo,
        OmniHtmlEditorCommands.ToggleSource,
        OmniHtmlEditorCommands.Separator,
        .. OmniHtmlEditorCommands.Default
            .Where(command => command.Action is not (OmniHtmlEditorAction.Undo or OmniHtmlEditorAction.Redo or OmniHtmlEditorAction.ToggleSource))
            .Reverse()
    ];

    // A command without an icon shows its label as text; each one appends its paragraph at the end.
    private static readonly IReadOnlyList<OmniHtmlEditorCommand> CustomTools =
    [
        OmniHtmlEditorCommand.Create("append-signature", "Add the signature", context => context.SetHtmlAsync(context.Html + "<p>The files department</p>")),
        OmniHtmlEditorCommand.Create("append-reference", "Insert the reference", context => context.SetHtmlAsync(context.Html + "<p>Reference: D-2401</p>"))
    ];

    // A command of the page: it inserts at the caret, in the face on display, and goes through the sanitising
    // and the history of the editor like a built-in command.
    private static readonly OmniHtmlEditorCommand SignatureCommand = OmniHtmlEditorCommand.Create(
        "signature",
        "Insert the signature at the caret",
        context => context.InsertHtmlAsync("<p><em>The files department</em></p>"),
        OmniIconName.Edit);

    private IReadOnlyList<OmniHtmlEditorCommand> _commands = [];

    private (HtmlEditorToolbarChoice Toolbar, bool Signature, bool Tools, OmniHtmlEditorAction[] Hidden)? _key;

    /// <summary>The HTML the editor binds.</summary>
    [Parameter]
    public string Body { get; set; } = HtmlEditorSamples.Letter;

    /// <summary>Raised with the HTML after each change, already sanitised.</summary>
    [Parameter]
    public EventCallback<string> BodyChanged { get; set; }

    /// <summary>The face on display: the formatted document or its HTML source.</summary>
    [Parameter]
    public OmniHtmlEditorMode Mode { get; set; } = OmniHtmlEditorMode.Visual;

    /// <summary>Raised when the face changes, by the setting or by the Source button of the toolbar.</summary>
    [Parameter]
    public EventCallback<OmniHtmlEditorMode> ModeChanged { get; set; }

    /// <summary>The toolbar the editor starts from.</summary>
    [Parameter]
    public HtmlEditorToolbarChoice Toolbar { get; set; }

    /// <summary>Adds the command of the page, which inserts a signature at the caret, at the end of the toolbar.</summary>
    [Parameter]
    public bool ShowSignatureCommand { get; set; } = true;

    /// <summary>Adds two commands labelled in text after the toolbar.</summary>
    [Parameter]
    public bool ShowCustomTools { get; set; }

    /// <summary>The commands left out of the toolbar, whatever the toolbar chosen.</summary>
    [Parameter]
    public IReadOnlyCollection<OmniHtmlEditorAction> HiddenActions { get; set; } = [];

    /// <summary>Folds the toolbar on two rows, the rest going to its overflow menu.</summary>
    [Parameter]
    public bool LimitToolbarRows { get; set; } = true;

    /// <summary>Writes the name of each command next to its icon.</summary>
    [Parameter]
    public bool ToolbarLabels { get; set; } = true;

    /// <summary>In the source face, renders the sanitised markup under the text area.</summary>
    [Parameter]
    public bool ShowPreview { get; set; } = true;

    /// <summary>Locks the document, the source and every command.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The lines of the source and the minimum height of the document.</summary>
    [Parameter]
    public int Rows { get; set; } = 10;

    private IReadOnlyList<OmniHtmlEditorCommand> ActiveCommands => _commands;

    /// <summary>Builds the toolbar again only when a setting changes, so it is not redrawn on every render.</summary>
    protected override void OnParametersSet()
    {
        var hidden = HiddenActions.Order().ToArray();
        if (_key is { } known && known.Toolbar == Toolbar && known.Signature == ShowSignatureCommand
            && known.Tools == ShowCustomTools && known.Hidden.SequenceEqual(hidden))
        {
            return;
        }

        _key = (Toolbar, ShowSignatureCommand, ShowCustomTools, hidden);
        IReadOnlyList<OmniHtmlEditorCommand> baseCommands = Toolbar switch
        {
            HtmlEditorToolbarChoice.Essential => EssentialCommands,
            HtmlEditorToolbarChoice.Reordered => ReorderedCommands,
            _ => OmniHtmlEditorCommands.Default
        };
        List<OmniHtmlEditorCommand> commands = [.. baseCommands.Where(command => !hidden.Contains(command.Action))];
        if (ShowSignatureCommand)
        {
            commands.AddRange([OmniHtmlEditorCommands.Separator, SignatureCommand]);
        }

        if (ShowCustomTools)
        {
            commands.AddRange([OmniHtmlEditorCommands.Separator, .. CustomTools]);
        }

        _commands = commands;
    }

    private Task NotifyBodyAsync() => BodyChanged.InvokeAsync(Body);

    private Task NotifyModeAsync() => ModeChanged.InvokeAsync(Mode);

    private async Task ResetBodyAsync()
    {
        Body = HtmlEditorSamples.Letter;
        await BodyChanged.InvokeAsync(Body);
    }

    private async Task ClearBodyAsync()
    {
        Body = string.Empty;
        await BodyChanged.InvokeAsync(Body);
    }
}
