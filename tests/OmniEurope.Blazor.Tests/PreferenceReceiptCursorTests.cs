using Microsoft.Extensions.Logging.Abstractions;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The synchronization cursor of <see cref="OmniPreferences"/> follows <see cref="OmniPreference.ReceivedAt"/>
/// (the store's receipt date) when the store sets it, and <see cref="OmniPreference.UpdatedAt"/> otherwise: a
/// value written later but dated before the latest date already read (another device) is still read. The store
/// is an in-memory fake that stamps its own receipt clock; the real HTTP and PostgreSQL path is covered by the
/// showcase back tests.
/// </summary>
public sealed class PreferenceReceiptCursorTests
{
    private static readonly DateTimeOffset Device = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    private readonly PreferenceBrowser _browser = new();
    private readonly PreferenceClock _time = new(Device);

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private OmniPreferences Open(IOmniPreferenceStore store) =>
        new(_browser, store, new OmniPreferencesOptions(), NullLogger<OmniPreferences>.Instance, _time);

    [Fact]
    public async Task ALateWriteDatedBeforeTheLatestValueRead_IsStillRead_WhenTheStoreSetsReceivedAt()
    {
        // The store's clock is an hour behind the devices: the receipt dates are all before the arbitration dates.
        var store = new ReceiptPreferenceStore(Device.AddHours(-1));
        store.Receive("b", "\"old\"", Device.AddMinutes(-3));
        store.Receive("a", "\"a\"", Device.AddMinutes(-1));
        await using var preferences = Open(store);
        await preferences.SyncAsync(Token);
        Assert.Equal("old", await preferences.GetAsync<string>("b", Token));

        // Another device writes b, later than its stored value but dated before a.
        var late = store.Receive("b", "\"new\"", Device.AddMinutes(-2));
        await preferences.SyncAsync(Token);

        Assert.Equal("new", await preferences.GetAsync<string>("b", Token));
        Assert.Equal([null, store.ReceivedOf("a")], store.ChangedSinceCalls);
        Assert.True(late.ReceivedAt > store.ReceivedOf("a") && late.UpdatedAt < Device.AddMinutes(-1));
    }

    [Fact]
    public async Task AStoreThatLeavesReceivedAtNull_KeepsTheCursorOnUpdatedAt()
    {
        var store = new ReceiptPreferenceStore(Device.AddHours(-1)) { StampReceipts = false };
        store.Receive("b", "\"old\"", Device.AddMinutes(-3));
        store.Receive("a", "\"a\"", Device.AddMinutes(-1));
        await using var preferences = Open(store);

        await preferences.SyncAsync(Token);
        await preferences.SyncAsync(Token);

        Assert.Equal([null, Device.AddMinutes(-1)], store.ChangedSinceCalls);
    }

    /// <summary>
    /// A store that stamps each write with its own receipt clock (one second apart) and lists what it received at
    /// or after the cursor; with <see cref="StampReceipts"/> false, it leaves <see cref="OmniPreference.ReceivedAt"/>
    /// null and lists by <see cref="OmniPreference.UpdatedAt"/>, as a store written before the receipt date.
    /// </summary>
    private sealed class ReceiptPreferenceStore(DateTimeOffset clock) : IOmniPreferenceStore
    {
        private readonly Dictionary<string, OmniPreference> _values = new(StringComparer.Ordinal);
        private DateTimeOffset _clock = clock;

        public bool StampReceipts { get; init; } = true;

        public List<DateTimeOffset?> ChangedSinceCalls { get; } = [];

        public DateTimeOffset? ReceivedOf(string key) => _values[key].ReceivedAt;

        public OmniPreference Receive(string key, string? value, DateTimeOffset updatedAt)
        {
            _clock = _clock.AddSeconds(1);
            return _values[key] = new(key, value, updatedAt, StampReceipts ? _clock : null);
        }

        public Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.GetValueOrDefault(key));

        public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
        {
            ChangedSinceCalls.Add(since);
            IReadOnlyList<OmniPreference> changed = [.. _values.Values.Where(value => since is null || (value.ReceivedAt ?? value.UpdatedAt) >= since)];
            return Task.FromResult(changed);
        }

        public Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) =>
            Task.FromResult(Receive(preference.Key, preference.Value, preference.UpdatedAt));

        public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult("u-7f3a");
    }
}
