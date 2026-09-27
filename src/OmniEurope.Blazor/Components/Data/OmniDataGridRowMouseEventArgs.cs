namespace OmniEurope.Blazor.Components;

/// <summary>
/// A pointer event on a data row, passed to <c>RowMouseClick</c> and <c>RowContextMenu</c>: the row's
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

    public TItem Item { get; }

    public int Index { get; }

    public bool CtrlKey { get; }

    public bool ShiftKey { get; }

    public bool AltKey { get; }

    public bool MetaKey { get; }

    public double ClientX { get; }

    public double ClientY { get; }
}
