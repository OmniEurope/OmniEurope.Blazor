using System.Text.Json;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>
/// A preference saved, deleted and synchronized through <see cref="OmniPreferences"/>, read back from the browser
/// cache and, when the host registers an <see cref="IOmniPreferenceStore"/>, from that store.
/// </summary>
/// <remarks>
/// The store is read directly here only to prove what it holds; an application reads through
/// <see cref="OmniPreferences"/>, which also serializes its own calls to the store.
/// </remarks>
public partial class PreferencesExample : IDisposable
{
    /// <summary>The key of the preference: ASCII letters, digits, <c>.</c>, <c>-</c> and <c>_</c>.</summary>
    [Parameter]
    public string Key { get; set; } = "example.preference";

    [Inject]
    private OmniPreferences Preferences { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private IOmniPreferenceStore? Store { get; set; }

    private bool HasStore => Store is not null;

    private string Value { get; set; } = string.Empty;

    /// <summary>What <see cref="OmniPreferences"/> reads for the key: the browser cache, brought up to date by the store.</summary>
    private string Stored { get; set; } = string.Empty;

    /// <summary>What the store itself holds for the key, empty when it holds none or a removal.</summary>
    private string DatabaseValue { get; set; } = string.Empty;

    /// <summary>Why the last read of the store failed; <see langword="null"/> when it answered.</summary>
    private string? StoreFailure { get; set; }

    private bool SyncFailed { get; set; }

    private bool Working { get; set; }

    private bool InSync => string.Equals(Stored, DatabaseValue, StringComparison.Ordinal);

    private string StoreTerm => Store is null
        ? "Value read from the store"
        : $"Value read from the store ({Store.GetType().Name})";

    protected override async Task OnInitializedAsync()
    {
        Store = Services.GetService(typeof(IOmniPreferenceStore)) as IOmniPreferenceStore;
        Preferences.Changed += OnChanged;
        await ReadAsync();
        Value = Stored;
        await ReadStoreAsync();
    }

    private static string Display(string value) => value.Length == 0 ? "(none)" : value;

    private async Task ReadAsync() => Stored = await Preferences.GetAsync<string>(Key) ?? string.Empty;

    /// <summary>Reads the key from the store itself: a refused or unreachable store is a state to show, not a failure to hide.</summary>
    private async Task ReadStoreAsync()
    {
        if (Store is null)
        {
            return;
        }

        try
        {
            var preference = await Store.GetAsync(Key);
            DatabaseValue = preference?.Value is { } json ? JsonSerializer.Deserialize<string>(json) ?? string.Empty : string.Empty;
            StoreFailure = null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException or InvalidDataException or JsonException or NotSupportedException)
        {
            DatabaseValue = string.Empty;
            StoreFailure = exception.Message;
        }
    }

    private async Task SaveAsync() => await RunAsync(async () =>
    {
        await Preferences.SetAsync(Key, Value);
        await ReadAsync();
        await ReadStoreAsync();
    });

    private async Task RemoveAsync() => await RunAsync(async () =>
    {
        await Preferences.RemoveAsync(Key);
        await ReadAsync();
        Value = string.Empty;
        await ReadStoreAsync();
    });

    private async Task SyncAsync() => await RunAsync(async () =>
    {
        try
        {
            await Preferences.SyncAsync();
            SyncFailed = false;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or TimeoutException or InvalidDataException or JsonException or NotSupportedException)
        {
            SyncFailed = true;
        }

        await ReadAsync();
        await ReadStoreAsync();
    });

    private async Task RunAsync(Func<Task> action)
    {
        Working = true;
        try
        {
            await action();
        }
        finally
        {
            Working = false;
        }
    }

    /// <summary>A change of the key, from this page, another tab or the store: the cache is read again.</summary>
    private Task OnChanged(OmniPreferenceChangedEventArgs args) => args.Key == Key
        ? InvokeAsync(async () =>
        {
            await ReadAsync();
            StateHasChanged();
        })
        : Task.CompletedTask;

    /// <inheritdoc />
    public void Dispose() => Preferences.Changed -= OnChanged;
}
