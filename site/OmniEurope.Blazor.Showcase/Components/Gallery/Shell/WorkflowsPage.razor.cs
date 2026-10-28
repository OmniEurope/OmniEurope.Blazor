using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Shell;

/// <summary>The Workflows page: a wizard, a wizard in a dialog, a sign-in page and the lost-connection overlay.</summary>
public partial class WorkflowsPage
{
    [Inject]
    private IStringLocalizer<ShellStrings> Text { get; set; } = default!;

    private OmniConnectionState Connection { get; set; } = OmniConnectionState.Connected;

    private bool ShowSignUp { get; set; } = true;

    private IReadOnlyList<OmniOption<OmniConnectionState>> ConnectionOptions =>
    [
        new(OmniConnectionState.Connected, Text["WorkflowsConnected"]),
        new(OmniConnectionState.Reconnecting, Text["WorkflowsReconnecting"]),
        new(OmniConnectionState.Failed, Text["WorkflowsFailed"]),
        new(OmniConnectionState.Rejected, Text["WorkflowsRejected"])
    ];
}
