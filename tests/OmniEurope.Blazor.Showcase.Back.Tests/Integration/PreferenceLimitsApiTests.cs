using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Back.Data;
using OmniEurope.Blazor.Showcase.Back.Models;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>The quotas, the rate limits and the retention, each on an API configured with small limits, over a real PostgreSQL.</summary>
public sealed class PreferenceLimitsApiTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Written = new(2026, 10, 27, 9, 30, 0, TimeSpan.Zero);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static Task<HttpResponseMessage> PutAsync(HttpClient client, string visitor, string key, string? value = "1")
    {
        var message = new HttpRequestMessage(HttpMethod.Put, $"{VisitorPreferenceApi.Route}/{key}")
        {
            Content = JsonContent.Create(new SaveVisitorPreferenceRequest(value, Written)),
        };
        message.Headers.Add(VisitorPreferenceApi.VisitorHeader, visitor);
        return client.SendAsync(message, Token);
    }

    [Fact]
    public async Task TheKeyQuota_RefusesTheNextKeyWith422()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string> { ["Preferences:Limits:MaxKeysPerVisitor"] = "2" });
        using var client = factory.CreateClient();
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, visitor, "k1")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, visitor, "k2")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PutAsync(client, visitor, "k3")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentWrites_OfOneVisitor_NeverGoPastTheKeyQuota()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string> { ["Preferences:Limits:MaxKeysPerVisitor"] = "5" });
        using var client = factory.CreateClient();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        // The first write creates the visitor, so the concurrent ones all meet the revision token.
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, visitor, "k0")).StatusCode);

        var responses = await Task.WhenAll(Enumerable.Range(1, 12).Select(index => PutAsync(client, visitor, $"k{index}")));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShowcaseDbContext>();
        var stored = db.VisitorPreferences.Count(preference => preference.VisitorId == VisitorPreferenceValidator.VisitorKey(visitor));
        // Every accepted write is stored and none past the quota; a write that lost every replay is a 409.
        Assert.InRange(stored, 2, 5);
        Assert.Equal(stored - 1, responses.Count(response => response.StatusCode == HttpStatusCode.NoContent));
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NoContent, HttpStatusCode.UnprocessableEntity, HttpStatusCode.Conflict }));
    }

    [Fact]
    public async Task TheWriteRate_RefusesPastTheLimitWith429()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string> { ["RateLimiting:WritesPerMinute"] = "2" });
        using var client = factory.CreateClient();
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, visitor, "k1")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, visitor, "k2")).StatusCode);
        using var refused = await PutAsync(client, visitor, "k3");

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal("application/problem+json", refused.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task NewVisitors_PastTheAddressQuota_AreRefusedWith429()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string> { ["Preferences:Limits:NewVisitorsPerAddressPerHour"] = "2" });
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, VisitorPreferenceApi.CreateVisitorId(), "k")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, VisitorPreferenceApi.CreateVisitorId(), "k")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await PutAsync(client, VisitorPreferenceApi.CreateVisitorId(), "k")).StatusCode);
    }

    [Fact]
    public async Task TheVisitorCapacity_RefusesANewVisitorWith503()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string> { ["Preferences:Limits:MaxVisitors"] = "1" });
        using var client = factory.CreateClient();
        var first = VisitorPreferenceApi.CreateVisitorId();

        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, first, "k")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await PutAsync(client, VisitorPreferenceApi.CreateVisitorId(), "k")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, first, "k2")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentNewVisitors_NeverGoPastTheVisitorCapacity()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string>
        {
            ["Preferences:Limits:MaxVisitors"] = "1",
            ["Preferences:Limits:NewVisitorsPerAddressPerHour"] = "100",
        });
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => PutAsync(client, VisitorPreferenceApi.CreateVisitorId(), "k")));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShowcaseDbContext>();
        Assert.Equal(1, db.Visitors.Count());
        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(11, responses.Count(response => response.StatusCode == HttpStatusCode.ServiceUnavailable));
    }

    [Fact]
    public async Task ConcurrentFirstWrites_OfOneNewVisitor_AreAllAdmitted()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string> { ["Preferences:Limits:MaxVisitors"] = "1" });
        using var client = factory.CreateClient();
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(index => PutAsync(client, visitor, $"k{index}")));

        // The writes that wait for the admission lock find the visitor the first one created: none is refused as over capacity.
        Assert.DoesNotContain(responses, response => response.StatusCode == HttpStatusCode.ServiceUnavailable);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(1, scope.ServiceProvider.GetRequiredService<ShowcaseDbContext>().Visitors.Count());
    }

    [Fact]
    public async Task TheRetention_DeletesIdleVisitorsWithTheirPreferences_AndKeepsTheOthers()
    {
        await using var factory = new ShowcaseApiFactory(postgres, new Dictionary<string, string>
        {
            ["Preferences:Retention:IdleDays"] = "30",
            ["Preferences:Retention:BatchSize"] = "2",
        });
        using var client = factory.CreateClient();
        var active = VisitorPreferenceApi.CreateVisitorId();
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, active, "k")).StatusCode);
        var now = TimeProvider.System.GetUtcNow();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShowcaseDbContext>();
            for (var index = 0; index < 5; index++)
            {
                var key = VisitorPreferenceValidator.VisitorKey(VisitorPreferenceApi.CreateVisitorId());
                db.Visitors.Add(new Visitor { Id = key, CreatedAt = now.AddDays(-90), LastSeenAt = now.AddDays(-31) });
                db.VisitorPreferences.Add(new VisitorPreference { VisitorId = key, Key = "k", Value = "1", UpdatedAt = now.AddDays(-31) });
            }

            await db.SaveChangesAsync(Token);
        }

        var retention = factory.Services.GetServices<IHostedService>().OfType<VisitorRetentionService>().Single();
        var deleted = await retention.SweepAsync(Token);

        await using var check = factory.Services.CreateAsyncScope();
        var after = check.ServiceProvider.GetRequiredService<ShowcaseDbContext>();
        Assert.Equal(5, deleted);
        Assert.Equal([VisitorPreferenceValidator.VisitorKey(active)], after.Visitors.Select(visitor => visitor.Id).ToArray());
        Assert.Equal(1, after.VisitorPreferences.Count());
    }

    [Fact]
    public async Task AReadOfAStaleVisitor_MovesItsLastSeenDate()
    {
        await using var factory = new ShowcaseApiFactory(postgres);
        using var client = factory.CreateClient();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        Assert.Equal(HttpStatusCode.NoContent, (await PutAsync(client, visitor, "k")).StatusCode);
        var key = VisitorPreferenceValidator.VisitorKey(visitor);
        var longAgo = TimeProvider.System.GetUtcNow().AddDays(-10);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShowcaseDbContext>();
            var row = db.Visitors.Single(row => row.Id == key);
            row.LastSeenAt = longAgo;
            await db.SaveChangesAsync(Token);
        }

        using var get = new HttpRequestMessage(HttpMethod.Get, VisitorPreferenceApi.Route);
        get.Headers.Add(VisitorPreferenceApi.VisitorHeader, visitor);
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(get, Token)).StatusCode);

        await using var check = factory.Services.CreateAsyncScope();
        var seen = check.ServiceProvider.GetRequiredService<ShowcaseDbContext>().Visitors.Single(row => row.Id == key).LastSeenAt;
        Assert.True(seen > longAgo.AddDays(9), $"Last seen {seen:O} was not moved from {longAgo:O}.");
    }
}
