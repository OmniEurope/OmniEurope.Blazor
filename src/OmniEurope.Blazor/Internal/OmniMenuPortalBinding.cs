using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="OmniMenuController"/> needs from the menu component that owns it to keep that
/// menu in the overlay portal: the owner and the kind of its portal entry, and how to read its current
/// coordinator, popup fragment and <c>Open</c> state, raise <c>OpenChanged</c>, re-render and run work
/// on its renderer. Read through delegates, because the owner's parameters change between renders.
/// </summary>
internal sealed class OmniMenuPortalBinding(
    object owner,
    OmniPortalKind kind,
    Func<OmniOverlayCoordinator?> coordinator,
    Func<RenderFragment> popup,
    Func<bool?> open,
    Func<EventCallback<bool>> openChanged,
    Action render,
    Func<Func<Task>, Task> dispatch)
{
    /// <summary>The component the portal entry belongs to.</summary>
    internal object Owner => owner;

    /// <summary>What the portal entry holds.</summary>
    internal OmniPortalKind Kind => kind;

    /// <summary>The coordinator of the overlay portal, null when the component sits outside one.</summary>
    internal OmniOverlayCoordinator? Coordinator => coordinator();

    /// <summary>The fragment the portal renders for the open menu.</summary>
    internal RenderFragment Popup => popup();

    /// <summary>The <c>Open</c> parameter: null leaves the menu its own state.</summary>
    internal bool? Open => open();

    /// <summary>The <c>OpenChanged</c> callback of the owner.</summary>
    internal EventCallback<bool> OpenChanged => openChanged();

    /// <summary>Re-renders the owner.</summary>
    internal void Render() => render();

    /// <summary>Runs <paramref name="work"/> on the renderer of the owner.</summary>
    internal Task Dispatch(Func<Task> work) => dispatch(work);
}
