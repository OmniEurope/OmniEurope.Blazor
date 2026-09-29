namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="Components.OmniMenuItem"/> finds above it: the menu it belongs to, whichever
/// component draws it (overflow, context, split button or profile menu).
/// </summary>
internal interface IOmniMenu
{
    /// <summary>Closes the menu once an item is chosen; <paramref name="restoreFocus"/> puts the focus back on its trigger at once.</summary>
    Task CloseAsync(bool restoreFocus);
}
