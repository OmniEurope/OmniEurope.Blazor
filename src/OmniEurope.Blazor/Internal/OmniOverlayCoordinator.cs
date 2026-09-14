using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class OmniOverlayCoordinator
{
    private readonly List<OmniPortalEntry> _entries = [];

    internal event Action? Changed;

    /// <summary>An entry already shown got new content: only its slot has to follow.</summary>
    internal event Action<object>? EntryUpdated;

    internal IReadOnlyList<OmniPortalEntry> Entries => _entries;

    internal OmniPortalEntry? Find(object owner) =>
        _entries.Find(entry => ReferenceEquals(entry.Owner, owner));

    internal void Register(object owner, OmniPortalKind kind, RenderFragment content, Func<Task> closeAsync)
    {
        var index = _entries.FindIndex(entry => ReferenceEquals(entry.Owner, owner));
        var entry = new OmniPortalEntry(owner, kind, content, closeAsync);
        if (index >= 0)
        {
            _entries[index] = entry;
            EntryUpdated?.Invoke(owner);
            return;
        }

        _entries.Add(entry);
        Changed?.Invoke();
    }

    internal void Unregister(object owner)
    {
        if (_entries.RemoveAll(entry => ReferenceEquals(entry.Owner, owner)) > 0)
        {
            Changed?.Invoke();
        }
    }

    internal async Task CloseTopAsync()
    {
        if (_entries.LastOrDefault() is not { } entry)
        {
            return;
        }

        await entry.CloseAsync();
        Unregister(entry.Owner);
    }
}
