using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What the <see cref="MindMapInteropBridge"/> of an <see cref="OmniMindMap"/> needs from the
/// component: the gestures to forward and the renderer to run them on.
/// </summary>
internal interface IMindMapBridgeHost
{
    /// <summary>What the map does with the gestures <c>omni-mindmap.js</c> reports.</summary>
    MindMapGestures Gestures { get; }

    /// <summary>Runs a notification from the script on the renderer, then redraws.</summary>
    Task DispatchAsync(Func<Task> work);
}
