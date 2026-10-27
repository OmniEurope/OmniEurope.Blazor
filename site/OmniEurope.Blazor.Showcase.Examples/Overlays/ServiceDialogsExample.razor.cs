using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>Dialogs opened from code: two confirmations awaited as booleans, a sticky dialog and a blocking one.</summary>
public partial class ServiceDialogsExample : IDisposable
{
    private OmniOverlayService Overlays { get; } = new();

    private string? LastOutcome { get; set; }

    /// <summary>A destructive confirmation: the action in the danger variant, Cancel after it.</summary>
    private async Task ConfirmDeletionAsync()
    {
        var confirmed = await Overlays.ConfirmAsync(new OmniConfirmRequest(
            "Delete the file",
            "The file will be removed from the list. This is only a demonstration: nothing is deleted.")
        {
            ConfirmText = "Delete",
            ConfirmVariant = OmniButtonVariant.Danger,
            ConfirmIcon = OmniIconName.Delete
        });

        LastOutcome = confirmed ? "Deletion confirmed" : "Deletion refused";
    }

    /// <summary>A plain confirmation: the package draws the footer by its own button convention.</summary>
    private async Task ConfirmArchiveAsync()
    {
        var confirmed = await Overlays.ConfirmAsync(new OmniConfirmRequest(
            "Archive the file",
            "The file leaves the list and stays readable in the archive.")
        {
            ConfirmText = "Archive",
            ConfirmIcon = OmniIconName.Archive
        });

        LastOutcome = confirmed ? "Archiving confirmed" : "Archiving refused";
    }

    private void OpenStickyDialog() => Overlays.OpenDialog(
        new OmniDialogRequest("Filter in progress", StickyContent())
        {
            CloseOnBackdrop = false
        });

    // The text and a button that opens the blocking dialog over this one: a dialog opened from a dialog
    // gives the focus back to the button that opened it, inside the lower dialog, when it closes.
    private RenderFragment StickyContent() => builder =>
    {
        builder.AddContent(0, Paragraph("A click outside does not close this dialog: the cross and Escape do."));
        builder.OpenComponent<OmniButton>(1);
        builder.AddComponentParameter(2, nameof(OmniButton.Id), "dialog-nested");
        builder.AddComponentParameter(3, nameof(OmniButton.Variant), OmniButtonVariant.Secondary);
        builder.AddComponentParameter(4, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, OpenBlockingDialog));
        builder.AddComponentParameter(5, nameof(OmniButton.ChildContent), (RenderFragment)(content => content.AddContent(0, "Dialog with an enforced close")));
        builder.CloseComponent();
    };

    private void OpenBlockingDialog() => Overlays.OpenDialog(
        new OmniDialogRequest("Migration in progress", Paragraph("Neither the cross, nor Escape, nor the backdrop: only the dialog's own button closes it."))
        {
            Dismissible = false,
            FooterContent = builder =>
            {
                builder.OpenComponent<OmniButton>(0);
                builder.AddComponentParameter(1, nameof(OmniButton.Id), "dialog-blocking-done");
                builder.AddComponentParameter(2, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, CloseBlockingDialog));
                builder.AddComponentParameter(3, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<OmniIcon>(0);
                    content.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Check);
                    content.CloseComponent();
                    content.OpenElement(2, "span");
                    content.AddContent(3, "Finish");
                    content.CloseElement();
                }));
                builder.CloseComponent();
            }
        });

    private void CloseBlockingDialog()
    {
        Overlays.CloseDialog();
        LastOutcome = "Migration finished";
    }

    private static RenderFragment Paragraph(string text) => builder =>
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, text);
        builder.CloseElement();
    };

    /// <inheritdoc />
    public void Dispose()
    {
        Overlays.Dispose();
        GC.SuppressFinalize(this);
    }
}
