namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The preferences of a circuit with no signed-in user yet (the store answered
/// <see cref="Components.OmniPreferences.NoUserScope"/>): kept in memory only, never in the browser cache,
/// which belongs to a user, nor in the store. Reads give what was written in this circuit, else nothing,
/// so every component keeps its defaults; the values go when a user signs in
/// (<see cref="Components.OmniPreferences.ResetUserAsync"/>).
/// </summary>
internal sealed class SignedOutPreferences
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);

    /// <summary>Whether the last scope the store gave was <see cref="Components.OmniPreferences.NoUserScope"/>.</summary>
    internal bool Active { get; set; }

    internal string? Read(string key)
    {
        lock (_gate)
        {
            return _values.GetValueOrDefault(key);
        }
    }

    /// <summary>Keeps a value; false when it is the one already kept, so nothing is raised.</summary>
    internal bool Write(string key, string? value)
    {
        lock (_gate)
        {
            if (_values.TryGetValue(key, out var current) ? current == value : value is null)
            {
                return false;
            }

            _values[key] = value;
            return true;
        }
    }

    internal void Clear()
    {
        lock (_gate)
        {
            _values.Clear();
            Active = false;
        }
    }
}
