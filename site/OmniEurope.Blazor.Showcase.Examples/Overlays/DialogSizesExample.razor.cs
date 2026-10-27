using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>One dialog opened at each size, then at a free width that can be dropped while it is open.</summary>
public partial class DialogSizesExample
{
    private bool Opened { get; set; }

    private OmniDialogSize DialogSize { get; set; }

    private string? FreeWidth { get; set; }

    private void OpenSized(OmniDialogSize size)
    {
        DialogSize = size;
        FreeWidth = null;
        Opened = true;
    }

    private void OpenFreeWidth()
    {
        FreeWidth = "30rem";
        Opened = true;
    }

    private void BackToMedium()
    {
        DialogSize = OmniDialogSize.Medium;
        FreeWidth = null;
    }
}
