namespace OmniEurope.Blazor.Components;

/// <summary>
/// Link between a panel-menu group and the items nested inside it. A child reports whether it is
/// the current page, which is the only way the group can know it should unfold: matching the route
/// against the group's own address is not enough, since a group commonly points at the same page as
/// its first child.
/// </summary>
internal sealed class OmniPanelMenuGroupContext(Action onChanged)
{
    private readonly HashSet<object> _activeChildren = [];

    public bool HasActiveChild => _activeChildren.Count > 0;

    public void Report(object child, bool active)
    {
        var changed = active ? _activeChildren.Add(child) : _activeChildren.Remove(child);
        if (changed)
        {
            onChanged();
        }
    }

    public void Remove(object child) => Report(child, false);
}
