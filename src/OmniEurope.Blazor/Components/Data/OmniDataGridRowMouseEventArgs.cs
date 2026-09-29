namespace OmniEurope.Blazor.Components;

/// <summary>
/// A pointer event on a data row, passed to <c>OnRowClick</c>, <c>OnRowDoubleClick</c> and <c>OnRowContextMenu</c>: the row's
/// item and position, and the modifier keys held, so a host can tell a Ctrl or Shift click from a
/// plain one. Activated from the keyboard, a row reports no modifier and no pointer position.
/// </summary>
public sealed class OmniDataGridRowMouseEventArgs<TItem>
{
    internal OmniDataGridRowMouseEventArgs(TItem item, int index, Microsoft.AspNetCore.Components.Web.MouseEventArgs? mouse)
    {
        Item = item;
        Index = index;
        CtrlKey = mouse?.CtrlKey ?? false;
        ShiftKey = mouse?.ShiftKey ?? false;
        AltKey = mouse?.AltKey ?? false;
        MetaKey = mouse?.MetaKey ?? false;
        ClientX = mouse?.ClientX ?? 0;
        ClientY = mouse?.ClientY ?? 0;
    }

    /// <summary>The row's item.</summary>
    public TItem Item { get; }

    /// <summary>The row's position among the rows of the current view, from 0.</summary>
    public int Index { get; }

    /// <summary>Whether Ctrl was held.</summary>
    public bool CtrlKey { get; }

    /// <summary>Whether Shift was held.</summary>
    public bool ShiftKey { get; }

    /// <summary>Whether Alt was held.</summary>
    public bool AltKey { get; }

    /// <summary>Whether the Meta (Command, Windows) key was held.</summary>
    public bool MetaKey { get; }

    /// <summary>Horizontal pointer position in the viewport, 0 from the keyboard.</summary>
    public double ClientX { get; }

    /// <summary>Vertical pointer position in the viewport, 0 from the keyboard.</summary>
    public double ClientY { get; }
}
