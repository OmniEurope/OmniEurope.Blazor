using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The state key a grid takes without a <c>StateKey</c>, when the host keeps every grid's state
/// (<c>OmniPreferencesOptions.PersistGridState</c>) or the grid asks for it (<c>PersistState</c> true): the
/// current route, then the grid's id, else a signature of its declared column keys.
/// </summary>
/// <remarks>
/// The route is the path relative to the base address, without query nor fragment, in lower case, without a
/// trailing slash; a segment that is an integer or a GUID becomes <c>*</c>, so <c>/projects/12</c> and
/// <c>/projects/34</c> share one key. Two grids of one route without an id and with the same columns share it
/// too: an id or a <c>StateKey</c> tells them apart. A key that would make the preference key longer than the
/// 200 characters of the store's advised column is replaced by a hash of itself.
/// </remarks>
internal static class GridStateKey
{
    /// <summary>The longest preference key the store's advised table holds (<c>Key</c>, text 200).</summary>
    internal const int MaxPreferenceKeyLength = 200;

    /// <summary>The prefix of the preference key of every grid state: <c>omni.grid.</c> then its key.</summary>
    internal const string PreferencePrefix = "omni.grid.";

    private const string Prefix = "route:";

    /// <summary>The derived key, or null when the route cannot be read (no navigation in this host).</summary>
    /// <param name="navigation">The host's navigation, null when it registers none.</param>
    /// <param name="id">The grid's id, or null.</param>
    /// <param name="columnKeys">The keys of the declared columns, in declaration order; read only without an id.</param>
    internal static string? Derive(NavigationManager? navigation, string? id, Func<IEnumerable<string>> columnKeys)
    {
        if (navigation is null)
        {
            return null;
        }

        string relative;
        try
        {
            relative = navigation.ToBaseRelativePath(navigation.Uri);
        }
        catch (InvalidOperationException)
        {
            // A navigation manager that was never initialized: there is no route to key the state by.
            return null;
        }

        return Compose(RoutePattern(relative), id, columnKeys);
    }

    /// <summary>The route part of the key: the base-relative path without query and fragment, integers and GUIDs as <c>*</c>.</summary>
    internal static string RoutePattern(string relativePath)
    {
        var end = relativePath.IndexOfAny(['?', '#']);
        var path = end < 0 ? relativePath : relativePath[..end];
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => IsIdentifier(segment) ? "*" : segment.ToLowerInvariant());
        return "/" + string.Join('/', segments);
    }

    /// <summary>The key from its route part: the id when there is one, else the signature of the column keys.</summary>
    internal static string Compose(string route, string? id, Func<IEnumerable<string>> columnKeys)
    {
        var owner = string.IsNullOrWhiteSpace(id) ? "cols:" + Signature(columnKeys()) : "id:" + id;
        var key = $"{Prefix}{route}#{owner}";
        return PreferencePrefix.Length + key.Length <= MaxPreferenceKeyLength
            ? key
            : Prefix + "~" + Hash(key);
    }

    /// <summary>A stable signature of the column keys: the same keys in the same order give the same text, in any process.</summary>
    internal static string Signature(IEnumerable<string> keys) => Hash(string.Join('\u001f', keys));

    // The segments come split without empty entries, so an all-digit segment has at least one digit.
    private static bool IsIdentifier(string segment) =>
        segment.All(char.IsAsciiDigit) || Guid.TryParse(segment, out _);

    // FNV-1a over the UTF-8 bytes, 64 bits: string.GetHashCode changes from one process to the next.
    private static string Hash(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var value in Encoding.UTF8.GetBytes(text))
        {
            hash ^= value;
            hash *= 1099511628211UL;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }
}
