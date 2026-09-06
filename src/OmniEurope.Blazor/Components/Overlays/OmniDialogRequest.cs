using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

// An empty optional value is resolved by OmniDialog from the current UI culture.
// OmniOverlayHosts also recognizes the legacy French value passed by already compiled alpha clients.
public sealed record OmniDialogRequest(string Title, RenderFragment Content, string CloseLabel = "")
{
    public RenderFragment? Footer { get; init; }
}
