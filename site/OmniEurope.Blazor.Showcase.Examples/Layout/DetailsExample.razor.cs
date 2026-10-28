using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A detail page whose state, back button and header frame are parameters.</summary>
public partial class DetailsExample
{
    /// <summary>Whether the record is loading, found or missing.</summary>
    [Parameter]
    public OmniDetailState State { get; set; } = OmniDetailState.Found;

    /// <summary>The look of the back arrow: in the accent colour or discreet.</summary>
    [Parameter]
    public OmniButtonVariant BackVariant { get; set; } = OmniButtonVariant.Primary;

    /// <summary>Whether the header is drawn as a bordered block instead of sitting on the page.</summary>
    [Parameter]
    public bool Framed { get; set; }

    private string? LastAction { get; set; }

    private bool TokenShown { get; set; }

    private DateTimeOffset UpdatedAt { get; } = TimeProvider.System.GetUtcNow().AddMinutes(-4);

    private DateTimeOffset Heartbeat { get; } = TimeProvider.System.GetUtcNow().AddMinutes(-2);

    private DateTimeOffset CertificateExpiry { get; } = TimeProvider.System.GetUtcNow().AddDays(47);
}
