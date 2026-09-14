using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Renders one portal entry and follows its owner. The host only re-renders when an entry comes or
/// goes, so without this slot a menu whose items changed while it was open kept showing the old
/// ones; the slot re-renders alone, never the owner, so it cannot loop back into a registration.
/// </summary>
internal sealed class OmniPortalSlot : ComponentBase, IDisposable
{
    private OmniOverlayCoordinator? _subscribed;

    [Parameter, EditorRequired]
    public OmniOverlayCoordinator Coordinator { get; set; } = default!;

    [Parameter, EditorRequired]
    public object Owner { get; set; } = default!;

    protected override void OnParametersSet()
    {
        if (ReferenceEquals(_subscribed, Coordinator))
        {
            return;
        }

        if (_subscribed is not null)
        {
            _subscribed.EntryUpdated -= HandleEntryUpdated;
        }

        _subscribed = Coordinator;
        _subscribed.EntryUpdated += HandleEntryUpdated;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Coordinator.Find(Owner) is { } entry)
        {
            builder.AddContent(0, entry.Content);
        }
    }

    private void HandleEntryUpdated(object owner)
    {
        if (ReferenceEquals(owner, Owner))
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        if (_subscribed is not null)
        {
            _subscribed.EntryUpdated -= HandleEntryUpdated;
        }
    }
}
