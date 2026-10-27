using System.Security.Cryptography;
using System.Text;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Preferences;

/// <summary>
/// The anonymous identity of this browser: a 256-bit random id (<see cref="VisitorPreferenceApi.CreateVisitorId"/>)
/// made on the first visit and kept in <c>localStorage</c>. It is the only key to the visitor's
/// preferences on the API, so it travels in a header and is never shown, logged or put in an address.
/// </summary>
public sealed class ShowcaseVisitor(IJSRuntime javaScript)
{
    /// <summary>The <c>localStorage</c> entry that holds the id.</summary>
    public const string StorageKey = "omni-showcase-visitor";

    private string? _id;

    /// <summary>
    /// False once the browser refused to read or keep the id (storage disabled): the id then lasts for
    /// this page only, and the next page load starts as a new visitor.
    /// </summary>
    public bool Persisted { get; private set; } = true;

    /// <summary>The visitor id: the stored one when it is valid, a new one otherwise.</summary>
    /// <param name="cancellationToken">Cancels the storage calls.</param>
    public async ValueTask<string> GetIdAsync(CancellationToken cancellationToken = default)
    {
        if (_id is not null)
        {
            return _id;
        }

        var stored = await ReadAsync(cancellationToken);
        if (VisitorPreferenceApi.IsValidVisitorId(stored))
        {
            return _id = stored!;
        }

        var created = VisitorPreferenceApi.CreateVisitorId();
        await WriteAsync(created, cancellationToken);
        return _id = created;
    }

    /// <summary>
    /// The partition of the <c>OmniPreferences</c> browser cache: <c>v</c> and the first 16 hexadecimal
    /// digits of the id's SHA-256. The cache keys are readable in the browser storage, so they carry a hash,
    /// never the id itself, and never a colon.
    /// </summary>
    /// <param name="cancellationToken">Cancels the storage calls.</param>
    public async ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        var id = await GetIdAsync(cancellationToken);
        return "v" + Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(id)))[..16];
    }

    private async ValueTask<string?> ReadAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await javaScript.InvokeAsync<string?>("localStorage.getItem", cancellationToken, StorageKey);
        }
        catch (JSException)
        {
            Persisted = false;
            return null;
        }
    }

    private async ValueTask WriteAsync(string id, CancellationToken cancellationToken)
    {
        if (!Persisted)
        {
            return;
        }

        try
        {
            await javaScript.InvokeVoidAsync("localStorage.setItem", cancellationToken, StorageKey, id);
        }
        catch (JSException)
        {
            Persisted = false;
        }
    }
}
