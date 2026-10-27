using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>A form dialog and an information dialog whose behaviour, size and look are parameters.</summary>
public partial class DialogsExample
{
    /// <summary>Whether the cross, Escape and the backdrop close the dialog; off, only its own actions do.</summary>
    [Parameter]
    public bool Dismissible { get; set; } = true;

    /// <summary>Whether a click outside the panel closes the dialog.</summary>
    [Parameter]
    public bool CloseOnBackdrop { get; set; } = true;

    /// <summary>Whether the form shows its Save and Cancel buttons.</summary>
    [Parameter]
    public bool ShowFooter { get; set; } = true;

    /// <summary>The widest the dialog grows, always bounded by the screen.</summary>
    [Parameter]
    public OmniDialogSize Size { get; set; } = OmniDialogSize.Medium;

    /// <summary>The intention tinting the header and the footer; neutral draws none.</summary>
    [Parameter]
    public OmniTone Intent { get; set; } = OmniTone.Neutral;

    /// <summary>Whether the dialog is moved by its header.</summary>
    [Parameter]
    public bool Draggable { get; set; }

    /// <summary>What holds the focus when the dialog opens.</summary>
    [Parameter]
    public OmniDialogInitialFocus InitialFocus { get; set; } = OmniDialogInitialFocus.CloseButton;

    private bool FormOpen { get; set; }

    private bool InfoOpen { get; set; }

    private string DraftName { get; set; } = string.Empty;

    private string DraftComment { get; set; } = string.Empty;

    private string SavedName { get; set; } = string.Empty;

    private string SavedComment { get; set; } = string.Empty;

    private string? LastOutcome { get; set; }

    /// <summary>
    /// A dialog that cannot be dismissed keeps its footer even when the footer is switched off:
    /// otherwise nothing could close it.
    /// </summary>
    private bool HasFooter => ShowFooter || !Dismissible;

    /// <summary>The draft starts from what was saved: cancelling must leave nothing behind.</summary>
    private void OpenForm()
    {
        DraftName = SavedName;
        DraftComment = SavedComment;
        FormOpen = true;
    }

    // A close by the cross, Escape or the backdrop abandons the draft the way Cancel does.
    private void OnFormOpenChanged(bool open)
    {
        if (!open && FormOpen)
        {
            LastOutcome = "Closed without saving";
        }

        FormOpen = open;
    }

    private void CancelForm()
    {
        LastOutcome = "Cancelled";
        FormOpen = false;
    }

    private void SaveForm()
    {
        SavedName = DraftName.Trim();
        SavedComment = DraftComment.Trim();
        LastOutcome = "Saved";
        FormOpen = false;
    }
}
