using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Showcase.Preferences;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The showcase's API preference store and its anonymous visitor. The API is a test double here
/// (<see cref="ApiHandler"/>, an <see cref="HttpMessageHandler"/> that answers in memory), and so is the
/// browser (<see cref="PreferenceBrowser"/>, <see cref="VisitorStorage"/>): these tests prove the HTTP
/// contract the store speaks and that OmniPreferences keeps working when the API cannot be reached. The
/// real API over PostgreSQL is proved by tests/OmniEurope.Blazor.Showcase.Back.Tests.
/// </summary>
public sealed class ShowcaseApiPreferenceStoreTests : OmniBunitContext
{
    private static readonly DateTimeOffset Start = new(2026, 10, 27, 8, 0, 0, TimeSpan.Zero);

    private readonly ApiHandler _api = new();
    private readonly VisitorStorage _storage = new();
    private readonly PreferenceBrowser _browser = new();
    private readonly RecordingLogger _logger = new();

    private static CancellationToken Token => Xunit.TestContext.Current.CancellationToken;

    private ShowcaseApiPreferenceStore CreateStore(ShowcaseVisitor? visitor = null) =>
        new(new ShowcaseApiHttpClient(new HttpClient(_api) { BaseAddress = new Uri("https://api.showcase.example/") }), visitor ?? new ShowcaseVisitor(_storage));

    private OmniPreferences CreatePreferences(ShowcaseApiPreferenceStore store)
    {
        Services.AddSingleton<IJSRuntime>(_browser);
        Services.AddSingleton<TimeProvider>(new PreferenceClock(Start));
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<OmniPreferences>>(_logger);
        Services.AddSingleton<IOmniPreferenceStore>(store);
        return Services.GetRequiredService<OmniPreferences>();
    }

    [Fact]
    public async Task Save_PutsTheValueWithTheVisitorHeader_ThenGetReadsItBack()
    {
        var store = CreateStore();
        _api.Respond = request => request.Method == HttpMethod.Put
            ? new HttpResponseMessage(HttpStatusCode.NoContent)
            : Json(new VisitorPreferenceDto("omni.appearance", "{\"theme\":\"givre\"}", Start));

        await store.SaveAsync(new OmniPreference("omni.appearance", "{\"theme\":\"givre\"}", Start), Token);
        var read = await store.GetAsync("omni.appearance", Token);

        var put = _api.Requests[0];
        Assert.Equal(HttpMethod.Put, put.Method);
        Assert.Equal("https://api.showcase.example/api/preferences/omni.appearance", put.Uri);
        Assert.Equal(_storage.Values[ShowcaseVisitor.StorageKey], put.Visitor);
        Assert.True(VisitorPreferenceApi.IsValidVisitorId(put.Visitor));
        var body = JsonSerializer.Deserialize<SaveVisitorPreferenceRequest>(put.Body!, JsonSerializerOptions.Web);
        Assert.Equal(new SaveVisitorPreferenceRequest("{\"theme\":\"givre\"}", Start), body);
        Assert.Equal(new OmniPreference("omni.appearance", "{\"theme\":\"givre\"}", Start), read);
        // The visitor id never travels in the address.
        Assert.All(_api.Requests, request => Assert.DoesNotContain(put.Visitor!, request.Uri, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_OfAKeyNeverStored_IsNull_ButAnErrorStatusIsAFailure()
    {
        var store = CreateStore();
        _api.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);
        Assert.Null(await store.GetAsync("omni.grid.orders", Token));

        _api.Respond = _ => new HttpResponseMessage(HttpStatusCode.NotFound);
        await Assert.ThrowsAsync<HttpRequestException>(() => store.GetAsync("omni.grid.orders", Token));
    }

    [Fact]
    public async Task GetChangedSince_SendsTheDateInUtc_AndReadsTheList()
    {
        var store = CreateStore();
        _api.Respond = _ => Json(new[] { new VisitorPreferenceDto("omni.appearance", null, Start) });

        var changed = await store.GetChangedSinceAsync(new DateTimeOffset(2026, 10, 27, 10, 0, 0, TimeSpan.FromHours(2)), Token);

        Assert.Equal([new OmniPreference("omni.appearance", null, Start)], changed);
        Assert.Equal("https://api.showcase.example/api/preferences?since=2026-10-27T08%3A00%3A00.0000000%2B00%3A00", _api.Requests[0].Uri);
    }

    [Fact]
    public async Task WhenTheApiIsUnreachable_PreferencesStillWorkFromTheBrowser()
    {
        _api.Respond = _ => throw new HttpRequestException("No connection could be made.");
        var store = CreateStore();
        var preferences = CreatePreferences(store);

        await preferences.SetAsync("omni.appearance", "givre", Token);
        var read = await preferences.GetAsync<string>("omni.appearance", Token);
        // An explicit synchronization reports the failure to its caller; the periodic one only logs it.
        await Assert.ThrowsAsync<HttpRequestException>(() => preferences.SyncAsync(Token));
        await preferences.WhenIdleAsync();

        Assert.Equal("givre", read);
        Assert.Equal("givre", await preferences.GetAsync<string>("omni.appearance", Token));
        // The value is in the browser cache under the visitor's scope, marked as not yet in the store.
        var scope = await new ShowcaseVisitor(_storage).GetScopeAsync(Token);
        var cached = JsonSerializer.Deserialize<PreferenceCacheEntry>(_browser.Storage[$"omni:pref:{scope}:omni.appearance"]);
        Assert.Equal(new PreferenceCacheEntry("\"givre\"", Start, Dirty: true), cached);
        Assert.NotEmpty(_api.Requests);
        Assert.Contains(_logger.Entries, entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
    }

    [Fact]
    public async Task Visitor_IsCreatedOnce_KeptInTheBrowser_AndReused()
    {
        var first = new ShowcaseVisitor(_storage);
        var id = await first.GetIdAsync(Token);

        Assert.True(VisitorPreferenceApi.IsValidVisitorId(id));
        Assert.Equal(id, _storage.Values[ShowcaseVisitor.StorageKey]);
        Assert.Equal(id, await new ShowcaseVisitor(_storage).GetIdAsync(Token));
        Assert.Equal(1, _storage.Writes);
    }

    [Fact]
    public async Task Visitor_ReplacesAStoredValueThatIsNotAnId()
    {
        _storage.Values[ShowcaseVisitor.StorageKey] = "forged";

        var id = await new ShowcaseVisitor(_storage).GetIdAsync(Token);

        Assert.True(VisitorPreferenceApi.IsValidVisitorId(id));
        Assert.Equal(id, _storage.Values[ShowcaseVisitor.StorageKey]);
    }

    [Fact]
    public async Task Visitor_WithoutStorage_KeepsAnIdForThePageOnly()
    {
        _storage.Failure = new JSException("SecurityError: The operation is insecure.");
        var visitor = new ShowcaseVisitor(_storage);

        var id = await visitor.GetIdAsync(Token);

        Assert.True(VisitorPreferenceApi.IsValidVisitorId(id));
        Assert.Equal(id, await visitor.GetIdAsync(Token));
        Assert.False(visitor.Persisted);
    }

    [Fact]
    public async Task Visitor_Scope_IsAStableHash_NeverTheIdNorAColon()
    {
        var visitor = new ShowcaseVisitor(_storage);
        var id = await visitor.GetIdAsync(Token);

        var scope = await visitor.GetScopeAsync(Token);

        Assert.Matches("^v[0-9a-f]{16}$", scope);
        Assert.DoesNotContain(scope[1..], id, StringComparison.Ordinal);
        Assert.Equal(scope, await new ShowcaseVisitor(_storage).GetScopeAsync(Token));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("https://api.showcase.example", "https://api.showcase.example/")]
    [InlineData("http://localhost:5285/", "http://localhost:5285/")]
    [InlineData("api.showcase.example", null)]
    [InlineData("ftp://api.showcase.example/", null)]
    public void BaseAddress_IsAnAbsoluteHttpAddress_OrNone(string? configured, string? expected)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ShowcaseApiConfiguration.ApiBaseUrlKey] = configured })
            .Build();

        Assert.Equal(expected, ShowcaseApiConfiguration.BaseAddress(configuration)?.ToString());
    }

    private static HttpResponseMessage Json<T>(T value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, JsonSerializerOptions.Web), Encoding.UTF8, "application/json") };

    /// <summary>The API, answered in memory by <see cref="Respond"/>; it records every request.</summary>
    private sealed class ApiHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

        public List<(HttpMethod Method, string Uri, string? Visitor, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var visitor = request.Headers.TryGetValues(VisitorPreferenceApi.VisitorHeader, out var values) ? values.Single() : null;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method, request.RequestUri!.AbsoluteUri, visitor, body));
            return Respond(request);
        }
    }

    /// <summary>The browser's <c>localStorage</c> for <see cref="ShowcaseVisitor"/>, which can refuse every call.</summary>
    private sealed class VisitorStorage : IJSRuntime
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

        public JSException? Failure { get; set; }

        public int Writes { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (Failure is not null)
            {
                return ValueTask.FromException<TValue>(Failure);
            }

            args ??= [];
            switch (identifier)
            {
                case "localStorage.getItem":
                    return ValueTask.FromResult((TValue)(object?)(Values.TryGetValue((string)args[0]!, out var value) ? value : null)!);
                case "localStorage.setItem":
                    Writes++;
                    Values[(string)args[0]!] = (string)args[1]!;
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected call {identifier}.");
            }
        }
    }
}
