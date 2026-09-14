namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a settings tile learns from the switch or check box beside its name: the id its label points
/// at, so a click anywhere on the tile reaches that one control. The first toggle to join keeps the
/// label; a second one in the same row is left alone rather than fought over.
/// </summary>
internal sealed class OmniSettingsTileContext(Action changed)
{
    private object? _owner;

    internal string? ControlId { get; private set; }

    internal void Join(object owner, string controlId)
    {
        if (_owner is not null && !ReferenceEquals(_owner, owner))
        {
            return;
        }

        if (ReferenceEquals(_owner, owner) && string.Equals(ControlId, controlId, StringComparison.Ordinal))
        {
            return;
        }

        _owner = owner;
        ControlId = controlId;
        changed();
    }

    internal void Leave(object owner)
    {
        if (!ReferenceEquals(_owner, owner))
        {
            return;
        }

        _owner = null;
        ControlId = null;
        changed();
    }
}
