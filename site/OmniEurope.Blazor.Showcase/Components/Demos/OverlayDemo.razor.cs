using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class OverlayDemo : IDisposable
{
    private bool DialogOpen { get; set; }

    private bool MenuOpen { get; set; }

    private string? Last { get; set; }

    private OmniOverlayService Overlays { get; } = new();

    private bool SizedOpen { get; set; }

    private OmniDialogSize SizedDialog { get; set; }

    private void OpenSized(OmniDialogSize size)
    {
        SizedDialog = size;
        SizedOpen = true;
    }

    private void Confirm()
    {
        Last = "Suppression confirmée";
        DialogOpen = false;
    }

    private void OpenStickyDialog() => Overlays.OpenDialog(
        new OmniDialogRequest("Filtre en cours", Paragraph("Un clic à côté ne ferme pas ce dialogue : la croix et Échap le ferment."))
        {
            CloseOnBackdropClick = false
        });

    private void OpenBlockingDialog() => Overlays.OpenDialog(
        new OmniDialogRequest("Migration en cours", Paragraph("Ni la croix, ni Échap, ni le voile : seul le bouton du dialogue le ferme."))
        {
            Dismissible = false,
            Footer = builder =>
            {
                builder.OpenComponent<OmniButton>(0);
                builder.AddComponentParameter(1, nameof(OmniButton.Id), "demo-dialog-blocking-done");
                builder.AddComponentParameter(2, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, CloseBlockingDialog));
                builder.AddComponentParameter(3, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
                {
                    content.OpenComponent<OmniIcon>(0);
                    content.AddComponentParameter(1, nameof(OmniIcon.Name), OmniIconName.Check);
                    content.CloseComponent();
                    content.OpenElement(2, "span");
                    content.AddContent(3, "Terminer");
                    content.CloseElement();
                }));
                builder.CloseComponent();
            }
        });

    private void CloseBlockingDialog()
    {
        Overlays.CloseDialog();
        Last = "Migration terminée";
    }

    private static RenderFragment Paragraph(string text) => builder =>
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, text);
        builder.CloseElement();
    };

    public void Dispose()
    {
        Overlays.Dispose();
        GC.SuppressFinalize(this);
    }
}
