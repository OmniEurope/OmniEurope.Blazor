using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>
/// The receipt date of the change feed, over HTTP and a real PostgreSQL: a browser that reads from the latest
/// <see cref="VisitorPreferenceDto.ReceivedAt"/> it saw still gets a write dated before the latest date it read.
/// </summary>
public sealed class PreferenceReceiptApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset Device = new(2026, 10, 27, 9, 30, 0, TimeSpan.Zero);

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

    private async Task<VisitorPreferenceDto> PutAsync(string visitor, string key, string? value, DateTimeOffset updatedAt)
    {
        using var message = new HttpRequestMessage(HttpMethod.Put, $"{VisitorPreferenceApi.Route}/{key}")
        {
            Content = JsonContent.Create(new SaveVisitorPreferenceRequest(value, updatedAt)),
        };
        message.Headers.Add(VisitorPreferenceApi.VisitorHeader, visitor);
        using var response = await _client.SendAsync(message, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<VisitorPreferenceDto>(Token))!;
    }

    private async Task<VisitorPreferenceDto[]> ChangedSinceAsync(string visitor, DateTimeOffset? since)
    {
        var path = since is { } from
            ? $"{VisitorPreferenceApi.Route}?since={Uri.EscapeDataString(from.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}"
            : VisitorPreferenceApi.Route;
        using var message = new HttpRequestMessage(HttpMethod.Get, path);
        message.Headers.Add(VisitorPreferenceApi.VisitorHeader, visitor);
        using var response = await _client.SendAsync(message, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<VisitorPreferenceDto[]>(Token))!;
    }

    [Fact]
    public async Task ALateWriteDatedBeforeTheLatestValueRead_IsInTheFeedFromTheLatestReceiptDate()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        await PutAsync(visitor, "b", "\"ancien\"", Device.AddMinutes(-3));
        await PutAsync(visitor, "a", "\"a\"", Device.AddMinutes(-1));
        var everything = await ChangedSinceAsync(visitor, null);
        var cursor = everything.Max(preference => preference.ReceivedAt);

        // Another device writes b: later than its stored value, but dated before a, the latest date read.
        await PutAsync(visitor, "b", "\"nouveau\"", Device.AddMinutes(-2));
        var changed = await ChangedSinceAsync(visitor, cursor);

        Assert.Equal(2, everything.Length);
        Assert.NotNull(cursor);
        var late = Assert.Single(changed, preference => preference.Key == "b");
        Assert.Equal("\"nouveau\"", late.Value);
        Assert.Equal(Device.AddMinutes(-2), late.UpdatedAt);
        Assert.True(late.ReceivedAt > cursor);
    }

    [Fact]
    public async Task Writes_OfOneVisitor_GetStrictlyIncreasingReceiptDates_EvenWhenConcurrent()
    {
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        var first = await PutAsync(visitor, "k0", "0", Device);

        // Concurrent writes of the same visitor are serialized: no two share a receipt date, none is before the first.
        var concurrent = await Task.WhenAll(Enumerable.Range(1, 8).Select(index => PutAsync(visitor, $"k{index}", "1", Device)));
        var sequential = new List<VisitorPreferenceDto>();
        for (var index = 0; index < 5; index++)
        {
            sequential.Add(await PutAsync(visitor, "k0", index.ToString(CultureInfo.InvariantCulture), Device.AddSeconds(index + 1)));
        }

        var receipts = concurrent.Select(preference => preference.ReceivedAt!.Value).ToList();
        Assert.Equal(receipts.Count, receipts.Distinct().Count());
        Assert.All(receipts, receipt => Assert.True(receipt > first.ReceivedAt));
        var ordered = sequential.Select(preference => preference.ReceivedAt!.Value).ToList();
        Assert.True(ordered[0] > receipts.Max());
        Assert.All(ordered.Zip(ordered.Skip(1)), pair => Assert.True(pair.Second > pair.First));
        // The feed reads back exactly the receipt dates the writes answered.
        var feed = await ChangedSinceAsync(visitor, null);
        Assert.Equal(ordered[^1], Assert.Single(feed, preference => preference.Key == "k0").ReceivedAt);
    }
}
