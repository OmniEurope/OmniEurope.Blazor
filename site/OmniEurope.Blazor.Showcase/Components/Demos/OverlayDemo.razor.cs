namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class OverlayDemo
{
    private bool DialogOpen { get; set; }

    private bool MenuOpen { get; set; }

    private string? Last { get; set; }

    private void Confirm()
    {
        Last = "Suppression confirmée";
        DialogOpen = false;
    }
}
