using System.Globalization;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class OverlayDemo : IDisposable
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private bool DialogOpen { get; set; }

    private bool MenuOpen { get; set; }

    private string? Last { get; set; }

    private OmniOverlayService Overlays { get; } = new();

    private bool SizedOpen { get; set; }

    private OmniDialogSize SizedDialog { get; set; }

    private string? SizedWidth { get; set; }

    private bool IntentOpen { get; set; }

    private OmniTone IntentDialog { get; set; }

    /// <summary>The start of the night job, written the way the reader's culture writes a time.</summary>
    private static string NightJobStart => new TimeOnly(23, 0).ToString("t", CultureInfo.CurrentCulture);

    private void OpenIntent(OmniTone intent)
    {
        IntentDialog = intent;
        IntentOpen = true;
    }

    private void OpenSized(OmniDialogSize size)
    {
        SizedDialog = size;
        SizedWidth = null;
        SizedOpen = true;
    }

    private void OpenFreeWidth()
    {
        SizedWidth = "30rem";
        SizedOpen = true;
    }

    private void Confirm()
    {
        Last = Text["DemoOverlayDeletionConfirmed"];
        DialogOpen = false;
    }

    private void OpenStickyDialog() => Overlays.OpenDialog(
        new OmniDialogRequest(Text["DemoOverlayStickyTitle"], Paragraph(Text["DemoOverlayStickyBody"]))
        {
            CloseOnBackdrop = false
        });

    private void OpenBlockingDialog()
    {
        string finish = Text["DemoOverlayFinish"];
        Overlays.OpenDialog(
            new OmniDialogRequest(Text["DemoOverlayBlockingTitle"], Paragraph(Text["DemoOverlayBlockingBody"]))
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
                        content.AddContent(3, finish);
                        content.CloseElement();
                    }));
                    builder.CloseComponent();
                }
            });
    }

    private void CloseBlockingDialog()
    {
        Overlays.CloseDialog();
        Last = Text["DemoOverlayMigrationDone"];
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
