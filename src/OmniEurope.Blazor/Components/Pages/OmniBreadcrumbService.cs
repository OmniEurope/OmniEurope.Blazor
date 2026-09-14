namespace OmniEurope.Blazor.Components;

/// <summary>
/// The breadcrumb trail of the page being shown, read by <see cref="OmniPageHeader"/>. On every change
/// of path it installs the trail the host's <see cref="IOmniBreadcrumbResolver"/> gives for the new
/// route, so the trail is never empty while a page loads; the page may then replace it, or one crumb of
/// it, once it knows better (an entity name loaded late).
/// </summary>
/// <remarks>
/// A navigation that only changes the query or the fragment (a tab, a filter, a page of results) keeps
/// the same page, so it keeps the trail the page set: resetting it there would strand the trail on the
/// route fallback, the page having no reason to set it again. A trailing slash and the letter case do
/// not make another page either. Registered as scoped by <c>AddOmniEuropeBlazor</c>; it must be
/// resolved once the navigation manager is initialized, which is the case in any component.
/// </remarks>
public sealed class OmniBreadcrumbService : IDisposable
{
    private readonly NavigationManager _navigation;
    private readonly IOmniBreadcrumbResolver? _resolver;
    private readonly List<OmniBreadcrumbEntry> _items = [];
    private string _path;
    private bool _disposed;

    /// <summary>Creates the service for the current location.</summary>
    /// <param name="navigation">The navigation manager of the application.</param>
    /// <param name="resolver">The host's route resolver; without one the fallback trail is empty.</param>
    public OmniBreadcrumbService(NavigationManager navigation, IOmniBreadcrumbResolver? resolver = null)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        _navigation = navigation;
        _resolver = resolver;
        _path = PathOf(navigation.ToBaseRelativePath(navigation.Uri));
        _items.AddRange(Fallback(_path));
        _navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>Raised whenever the trail changes, by navigation or by a page.</summary>
    public event Action? Changed;

    /// <summary>The whole trail, the current page last.</summary>
    public IReadOnlyList<OmniBreadcrumbEntry> Items => _items;

    /// <summary>The current page, the last entry of the trail; null when the trail is empty.</summary>
    public OmniBreadcrumbEntry? Current => _items.Count == 0 ? null : _items[^1];

    /// <summary>Every entry but the last: the ancestors of the current page.</summary>
    public IReadOnlyList<OmniBreadcrumbEntry> Ancestors => _items.Count <= 1 ? [] : _items.GetRange(0, _items.Count - 1);

    /// <summary>
    /// The nearest ancestor that is a link, where a back action leads. The last entry never counts,
    /// even when given a link by mistake: it is the page the user is on.
    /// </summary>
    public string? ParentHref
    {
        get
        {
            for (var index = _items.Count - 2; index >= 0; index--)
            {
                if (!string.IsNullOrWhiteSpace(_items[index].Href))
                {
                    return _items[index].Href;
                }
            }

            return null;
        }
    }

    /// <summary>Replaces the whole trail.</summary>
    public void Set(params IReadOnlyList<OmniBreadcrumbEntry> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items.Clear();
        _items.AddRange(items);
        Changed?.Invoke();
    }

    /// <summary>Appends one crumb at the end of the trail, which makes it the current page.</summary>
    public void Push(OmniBreadcrumbEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _items.Add(entry);
        Changed?.Invoke();
    }

    /// <summary>
    /// Replaces the crumb at <paramref name="index"/>, the usual way to name an entity once it is
    /// loaded: <c>Replace(1, Items[1] with { Text = name, Loading = false })</c>.
    /// </summary>
    public void Replace(int index, OmniBreadcrumbEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _items.Count);
        _items[index] = entry;
        Changed?.Invoke();
    }

    /// <summary>Puts back the trail the resolver gives for the current route.</summary>
    public void Reset()
    {
        _items.Clear();
        _items.AddRange(Fallback(_path));
        Changed?.Invoke();
    }

    /// <summary>Stops following the navigation.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _navigation.LocationChanged -= OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var path = PathOf(_navigation.ToBaseRelativePath(args.Location));
        if (string.Equals(path, _path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _path = path;
        Reset();
    }

    private IReadOnlyList<OmniBreadcrumbEntry> Fallback(string path) => _resolver?.Resolve(path) ?? [];

    private static string PathOf(string relative)
    {
        var end = relative.IndexOfAny(['?', '#']);
        return (end < 0 ? relative : relative[..end]).Trim('/');
    }
}
