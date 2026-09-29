namespace OmniEurope.Blazor.Internal;

/// <summary>What a portal entry holds; it names the entry's modifier class (<c>omni-overlay-portal__entry--{kind}</c>).</summary>
internal enum OmniPortalKind
{
    /// <summary>The menu of an <see cref="Components.OmniContextMenu"/>.</summary>
    ContextMenu,

    /// <summary>The menu of an <see cref="Components.OmniOverflowMenu"/>.</summary>
    OverflowMenu,

    /// <summary>The menu of an <see cref="Components.OmniSplitButton"/>.</summary>
    SplitButtonMenu,

    /// <summary>The menu of an <see cref="Components.OmniProfileMenu"/>.</summary>
    ProfileMenu
}
