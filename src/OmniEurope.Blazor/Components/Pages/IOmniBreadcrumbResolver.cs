namespace OmniEurope.Blazor.Components;

/// <summary>
/// Turns a route of the host application into its breadcrumb trail. The library knows no route: the
/// host registers one implementation in its container, and <see cref="OmniBreadcrumbService"/> asks it
/// for the trail of every page it navigates to, until the page replaces that trail with its own.
/// </summary>
public interface IOmniBreadcrumbResolver
{
    /// <summary>The trail of the page at <paramref name="relativePath"/>, its last entry being the page itself.</summary>
    /// <param name="relativePath">
    /// The path relative to the application base, without query, fragment, nor leading or trailing
    /// slash: <c>projects/12/overview</c>, or an empty string for the root.
    /// </param>
    IReadOnlyList<OmniBreadcrumbEntry> Resolve(string relativePath);
}
