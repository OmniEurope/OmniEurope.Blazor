using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>The HTTP surface of the API, end to end over a real PostgreSQL.</summary>
public sealed class PreferencesApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Written = new(2026, 10, 27, 9, 30, 0, TimeSpan.Zero);

    private ShowcaseApiFactory _factory = null!;
    private HttpClient _client = null!;

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        _factory = new ShowcaseApiFactory(postgres);
        _client = _factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static HttpRequestMessage Message(HttpMethod method, string path, string? visitor, object? body = null)
    {
        var message = new HttpRequestMessage(method, path);
        if (visitor is not null)
        {
            message.Headers.Add(VisitorPreferenceApi.VisitorHeader, visitor);
        }

        if (body is not null)
        {
            message.Content = JsonContent.Create(body);
        }

        return message;
    }

    private async Task<HttpResponseMessage> PutAsync(string visitor, string key, string? value, DateTimeOffset updatedAt)
    {
        using var message = Message(HttpMethod.Put, $"{VisitorPreferenceApi.Route}/{key}", visitor, new SaveVisitorPreferenceRequest(value, updatedAt));
        return await _client.SendAsync(message, Token);
    }

    private static async Task<ProblemDetails> ProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync(Token);
        // No stack trace, exception type or SQL ever reaches a client.
        Assert.DoesNotContain("   at ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>(Token))!;
    }

    [Fact]
    public async Task Health_LiveAndReady_AreHealthyWithTheDatabase()
    {
        using var live = await _client.GetAsync("/health/live", Token);
        using var ready = await _client.GetAsync("/health/ready", Token);

        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal("Healthy", await ready.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task Put_ThenGet_RoundTripsThroughPostgreSql()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        using var put = await PutAsync(visitor, "omni.appearance", "{\"theme\":\"givre\",\"density\":2}", Written);
        using var get = Message(HttpMethod.Get, $"{VisitorPreferenceApi.Route}/omni.appearance", visitor);
        using var one = await _client.SendAsync(get, Token);
        using var list = Message(HttpMethod.Get, $"{VisitorPreferenceApi.Route}?since={Uri.EscapeDataString(Written.AddSeconds(-1).ToString("O"))}", visitor);
        using var changed = await _client.SendAsync(list, Token);

        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "{\"theme\":\"givre\",\"density\":2}", Written),
            await put.Content.ReadFromJsonAsync<VisitorPreferenceDto>(Token));
        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "{\"theme\":\"givre\",\"density\":2}", Written),
            await one.Content.ReadFromJsonAsync<VisitorPreferenceDto>(Token));
        Assert.Equal([new VisitorPreferenceDto("omni.appearance", "{\"theme\":\"givre\",\"density\":2}", Written)],
            (await changed.Content.ReadFromJsonAsync<VisitorPreferenceDto[]>(Token))!);
    }

    [Fact]
    public async Task Put_OfAFutureDate_AnswersTheServerDateItStored()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        var before = TimeProvider.System.GetUtcNow();

        using var put = await PutAsync(visitor, "omni.appearance", "1", before.AddDays(1));
        var acknowledged = await put.Content.ReadFromJsonAsync<VisitorPreferenceDto>(Token);
        using var get = Message(HttpMethod.Get, $"{VisitorPreferenceApi.Route}/omni.appearance", visitor);
        using var one = await _client.SendAsync(get, Token);

        // The server is the authority for dates: the browser learns the date the server kept, not its own clock's.
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.NotNull(acknowledged);
        Assert.Equal("1", acknowledged.Value);
        Assert.InRange(acknowledged.UpdatedAt, before, TimeProvider.System.GetUtcNow());
        Assert.Equal(acknowledged, await one.Content.ReadFromJsonAsync<VisitorPreferenceDto>(Token));
    }

    [Fact]
    public async Task Put_OlderThanTheStoredValue_AnswersTheValueTheServerKept()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        using var first = await PutAsync(visitor, "omni.appearance", "2", Written);

        using var older = await PutAsync(visitor, "omni.appearance", "1", Written.AddMinutes(-1));

        Assert.Equal(HttpStatusCode.OK, older.StatusCode);
        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "2", Written), await older.Content.ReadFromJsonAsync<VisitorPreferenceDto>(Token));
    }

    [Fact]
    public async Task Get_OfAKeyNeverStored_Is204_AndAnotherVisitorSeesNothing()
    {
        var owner = VisitorPreferenceApi.CreateVisitorId();
        using var put = await PutAsync(owner, "omni.appearance", "1", Written);

        using var missing = Message(HttpMethod.Get, $"{VisitorPreferenceApi.Route}/omni.grid.orders", owner);
        using var other = Message(HttpMethod.Get, $"{VisitorPreferenceApi.Route}/omni.appearance", VisitorPreferenceApi.CreateVisitorId());
        using var otherList = Message(HttpMethod.Get, VisitorPreferenceApi.Route, VisitorPreferenceApi.CreateVisitorId());

        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(missing, Token)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(other, Token)).StatusCode);
        Assert.Empty((await (await _client.SendAsync(otherList, Token)).Content.ReadFromJsonAsync<VisitorPreferenceDto[]>(Token))!);
    }

    [Fact]
    public async Task Requests_WithoutAValidVisitorHeader_AreRefusedAsProblems()
    {
        using var noHeader = Message(HttpMethod.Get, VisitorPreferenceApi.Route, visitor: null);
        using var forged = Message(HttpMethod.Get, VisitorPreferenceApi.Route, "' OR 1=1 --");

        var first = await ProblemAsync(await _client.SendAsync(noHeader, Token), HttpStatusCode.BadRequest);
        await ProblemAsync(await _client.SendAsync(forged, Token), HttpStatusCode.BadRequest);

        Assert.Contains(VisitorPreferenceApi.VisitorHeader, first.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Put_OfAValueThatIsNotJson_OrOfAMalformedBody_IsABadRequest()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        using var notJson = await PutAsync(visitor, "omni.appearance", "<script>alert(1)</script>", Written);
        using var malformed = Message(HttpMethod.Put, $"{VisitorPreferenceApi.Route}/omni.appearance", visitor);
        malformed.Content = new StringContent("{\"value\":", Encoding.UTF8, "application/json");

        await ProblemAsync(notJson, HttpStatusCode.BadRequest);
        await ProblemAsync(await _client.SendAsync(malformed, Token), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_OfAValueOverTheLimit_IsABadRequest_AndStoresNothing()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        var tooLong = "\"" + new string('a', 16_384) + "\"";

        await ProblemAsync(await PutAsync(visitor, "omni.appearance", tooLong, Written), HttpStatusCode.BadRequest);

        using var get = Message(HttpMethod.Get, $"{VisitorPreferenceApi.Route}/omni.appearance", visitor);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(get, Token)).StatusCode);
    }

    [Fact]
    public async Task Cors_AnswersThePreflightOfTheFrontOrigin_Only()
    {
        using var front = Preflight(ShowcaseApiFactory.FrontOrigin);
        using var other = Preflight("https://attacker.example");

        using var allowed = await _client.SendAsync(front, Token);
        using var refused = await _client.SendAsync(other, Token);

        Assert.Equal(ShowcaseApiFactory.FrontOrigin, Assert.Single(allowed.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains(VisitorPreferenceApi.VisitorHeader, allowed.Headers.GetValues("Access-Control-Allow-Headers").Single(), StringComparison.OrdinalIgnoreCase);
        Assert.False(refused.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(allowed.Headers.Contains("Access-Control-Allow-Credentials"));

        static HttpRequestMessage Preflight(string origin)
        {
            var message = new HttpRequestMessage(HttpMethod.Options, $"{VisitorPreferenceApi.Route}/omni.appearance");
            message.Headers.Add("Origin", origin);
            message.Headers.Add("Access-Control-Request-Method", "PUT");
            message.Headers.Add("Access-Control-Request-Headers", $"content-type,{VisitorPreferenceApi.VisitorHeader.ToLowerInvariant()}");
            return message;
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task Responses_CarryTheSecurityHeaders(HttpStatusCode status, bool withVisitor)
    {
        // A refusal is converted from an exception after the response was cleared: its headers too.
        using var get = Message(HttpMethod.Get, VisitorPreferenceApi.Route, withVisitor ? VisitorPreferenceApi.CreateVisitorId() : null);
        using var response = await _client.SendAsync(get, Token);

        Assert.Equal(status, response.StatusCode);
        SecurityHeaderAssert.Carried(response);
    }

    [Fact]
    public async Task AnUnexpectedFailure_Is500_WithTheSecurityHeaders()
    {
        await using var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddScoped<IVisitorPreferenceService, FailingPreferenceService>()));
        using var client = failing.CreateClient();
        using var get = Message(HttpMethod.Get, VisitorPreferenceApi.Route, VisitorPreferenceApi.CreateVisitorId());
        using var response = await client.SendAsync(get, Token);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        SecurityHeaderAssert.Carried(response);
    }

    [Fact]
    public async Task AnUnknownRoute_IsAProblemToo()
    {
        using var response = await _client.GetAsync("/api/unknown", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }
}
