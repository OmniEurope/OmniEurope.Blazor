using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What the items of a tree need from the tree itself, through their shared context: its drag-and-drop
/// settings, the report of a drop, and a redraw. The context holds the tree through this contract, so it never
/// names the tree component, which creates the context.
/// </summary>
/// <typeparam name="TValue">The type of the values the items stand for.</typeparam>
internal interface ITreeDragHost<TValue>
{
    /// <summary>Whether items can be dragged onto one another.</summary>
    bool AllowDragDrop { get; }

    /// <summary>Whether an item can be dragged; null lets every enabled item be.</summary>
    Func<TValue, bool>? CanDrag { get; }

    /// <summary>Whether the dragged value can be dropped onto the target value; null accepts every valid target.</summary>
    Func<TValue, TValue, bool>? CanDrop { get; }

    /// <summary>Reports an accepted drop to the host of the tree.</summary>
    Task ReportDropAsync(OmniTreeDropEventArgs<TValue> drop);

    /// <summary>Redraws the tree, and with it every item, which reads the context.</summary>
    void Redraw();
}
