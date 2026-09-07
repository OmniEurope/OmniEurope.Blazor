namespace OmniEurope.Blazor.Components;

/// <summary>
/// How a menu entry decides it is the current page.
/// </summary>
public enum OmniNavMatch
{
    /// <summary>
    /// The entry is current on its own route and on every route below it. The default, because an
    /// entry that names a section is expected to stay lit while the reader is inside that section.
    /// </summary>
    Prefix,

    /// <summary>
    /// The entry is current on its own route only. What the landing page of an area needs: its
    /// address is the prefix of every other page of the area, so under <see cref="Prefix"/> it
    /// would never go out.
    /// </summary>
    Exact,

    /// <summary>
    /// The entry is never current, whatever the route. What a group whose address duplicates one of
    /// its own entries needs: it stays clickable, and the entry it points at is the only one lit,
    /// instead of the two of them lighting together.
    /// </summary>
    None
}
