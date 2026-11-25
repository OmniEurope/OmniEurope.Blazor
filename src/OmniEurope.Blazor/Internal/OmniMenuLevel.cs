using System.Runtime.CompilerServices;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// One level of a menu: the entries that sit side by side, the top of the menu or the content of an
/// <see cref="Components.OmniSubMenu"/>. It knows which submenu of the level is open, so that opening one closes
/// the one that was.
/// </summary>
internal sealed class OmniMenuLevel
{
    private static readonly ConditionalWeakTable<IOmniMenu, OmniMenuLevel> TopLevels = [];
    private object? _owner;
    private Func<Task>? _close;

    /// <summary>The top level of a menu: the entries that sit directly in its list.</summary>
    internal static OmniMenuLevel TopOf(IOmniMenu menu) => TopLevels.GetValue(menu, static _ => new OmniMenuLevel());

    /// <summary>Records <paramref name="owner"/> as the entry whose list is open, closing the one that was with the callback it gave.</summary>
    internal Task OpenAsync(object owner, Func<Task> closeAsync)
    {
        var previous = _close;
        var same = ReferenceEquals(_owner, owner);
        _owner = owner;
        _close = closeAsync;
        return previous is null || same ? Task.CompletedTask : previous();
    }

    /// <summary>
    /// Whether <paramref name="owner"/> is still the entry recorded as open: false once a sibling opened
    /// while it waited for the previous list to close.
    /// </summary>
    internal bool Holds(object owner) => ReferenceEquals(_owner, owner);

    /// <summary>Forgets <paramref name="owner"/> once its list is closed or gone.</summary>
    internal void Closed(object owner)
    {
        if (ReferenceEquals(_owner, owner))
        {
            _owner = null;
            _close = null;
        }
    }
}
