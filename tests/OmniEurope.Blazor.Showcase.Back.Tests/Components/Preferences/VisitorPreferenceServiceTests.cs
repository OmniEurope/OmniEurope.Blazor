using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Back.Exceptions;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Components.Preferences;

/// <summary>The rules of the service over the in-memory doubles of <c>PreferenceDoubles.cs</c>: quotas, dates, retries.</summary>
public sealed class VisitorPreferenceServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly MemoryVisitorRepository _repository = new();
    private readonly ManualClock _clock = new(Now);
    private readonly PreferenceLimitsOptions _limits = new() { MaxKeysPerVisitor = 3, MaxStoredLengthPerVisitor = 300, MaxValueLength = 256, MaxVisitors = 2 };
    private readonly MemoryUnitOfWork _unitOfWork;
    private CountingGate _gate = new(permits: 10);

    public VisitorPreferenceServiceTests() => _unitOfWork = new MemoryUnitOfWork(_repository);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private VisitorPreferenceService Create() =>
        new(_repository, _unitOfWork, _gate, Options.Create(_limits), _clock, NullLogger<VisitorPreferenceService>.Instance);

    private static SaveVisitorPreferenceRequest Request(string? value, DateTimeOffset updatedAt) => new(value, updatedAt);

    [Fact]
    public async Task Save_ThenGet_ReturnsTheValueWithTheBrowserDate()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        var outcome = await service.SaveAsync(visitor, "omni.appearance", Request("{\"theme\":\"aplat\"}", Now.AddMinutes(-5)), "203.0.113.7", Token);

        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "{\"theme\":\"aplat\"}", Now.AddMinutes(-5), Now), outcome);
        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "{\"theme\":\"aplat\"}", Now.AddMinutes(-5), Now),
            await service.GetAsync(visitor, "omni.appearance", Token));
        var stored = Assert.Single(_repository.Visitors);
        // The database holds the hash of the id, never the id.
        Assert.NotEqual(visitor, stored.Id);
        Assert.Equal(VisitorPreferenceValidator.VisitorKey(visitor), stored.Id);
        Assert.Equal(["203.0.113.7"], _gate.Partitions);
    }

    [Fact]
    public async Task Save_AnOlderValue_KeepsTheStoredOne()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        await service.SaveAsync(visitor, "omni.appearance", Request("1", Now.AddMinutes(-1)), "a", Token);

        var outcome = await service.SaveAsync(visitor, "omni.appearance", Request("2", Now.AddMinutes(-2)), "a", Token);

        // The answer is the value the server kept, so the browser learns it lost.
        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "1", Now.AddMinutes(-1), Now), outcome);
        Assert.Equal("1", (await service.GetAsync(visitor, "omni.appearance", Token))!.Value);
    }

    [Fact]
    public async Task Save_ADateInTheFuture_IsStoredAtTheServerTime()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        var acknowledged = await service.SaveAsync(visitor, "omni.appearance", Request("1", Now.AddYears(5)), "a", Token);

        Assert.Equal(new VisitorPreferenceDto("omni.appearance", "1", Now, Now), acknowledged);
        Assert.Equal(Now, (await service.GetAsync(visitor, "omni.appearance", Token))!.UpdatedAt);
    }

    [Fact]
    public async Task Save_ATombstone_IsKeptAndListed()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        await service.SaveAsync(visitor, "omni.grid.orders", Request("{\"columns\":[]}", Now.AddMinutes(-3)), "a", Token);

        await service.SaveAsync(visitor, "omni.grid.orders", Request(null, Now.AddMinutes(-1)), "a", Token);

        // The clock did not move: the second write is received one microsecond after the first, and listed from it.
        var tombstone = new VisitorPreferenceDto("omni.grid.orders", null, Now.AddMinutes(-1), Now.AddTicks(TimeSpan.TicksPerMicrosecond));
        Assert.Equal([tombstone], await service.GetChangedSinceAsync(visitor, Now.AddTicks(TimeSpan.TicksPerMicrosecond), Token));
        Assert.Empty(await service.GetChangedSinceAsync(visitor, Now.AddTicks(2 * TimeSpan.TicksPerMicrosecond), Token));
    }

    [Fact]
    public async Task Save_PastTheKeyQuota_IsRefused_ButAnExistingKeyCanStillChange()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        foreach (var key in new[] { "k1", "k2", "k3" })
        {
            await service.SaveAsync(visitor, key, Request("1", Now.AddMinutes(-10)), "a", Token);
        }

        await Assert.ThrowsAsync<QuotaExceededException>(() => service.SaveAsync(visitor, "k4", Request("1", Now), "a", Token));
        Assert.Equal(new VisitorPreferenceDto("k2", "2", Now, Now.AddTicks(3 * TimeSpan.TicksPerMicrosecond)), await service.SaveAsync(visitor, "k2", Request("2", Now), "a", Token));
    }

    [Fact]
    public async Task Save_PastTheStoredLength_IsRefused()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        var twoHundred = "\"" + new string('a', 198) + "\"";
        await service.SaveAsync(visitor, "k1", Request(twoHundred, Now.AddMinutes(-10)), "a", Token);

        var exception = await Assert.ThrowsAsync<QuotaExceededException>(() => service.SaveAsync(visitor, "k2", Request(twoHundred, Now), "a", Token));

        Assert.Contains("300", exception.Message, StringComparison.Ordinal);
        Assert.Single(_repository.Preferences);
    }

    [Fact]
    public async Task Save_ANewVisitorPastTheCapacity_IsRefused()
    {
        var service = Create();
        await service.SaveAsync(VisitorPreferenceApi.CreateVisitorId(), "k", Request("1", Now), "a", Token);
        await service.SaveAsync(VisitorPreferenceApi.CreateVisitorId(), "k", Request("1", Now), "a", Token);

        await Assert.ThrowsAsync<CapacityReachedException>(() =>
            service.SaveAsync(VisitorPreferenceApi.CreateVisitorId(), "k", Request("1", Now), "a", Token));
        Assert.Equal(2, _repository.Visitors.Count);
        // Every admission counted under the admission lock; only the two admitted ones committed.
        Assert.Equal(3, _repository.AdmissionLocks);
        Assert.Equal(2, _unitOfWork.Commits);
    }

    [Fact]
    public async Task Save_OfAnExistingVisitor_TakesNoAdmissionLock()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        await service.SaveAsync(visitor, "k1", Request("1", Now), "a", Token);

        await service.SaveAsync(visitor, "k2", Request("1", Now), "a", Token);

        Assert.Equal(1, _repository.AdmissionLocks);
    }

    [Fact]
    public async Task EveryWrite_TakesTheWriteLockOfItsVisitor_AndCommits_EvenARefusedOne()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        await service.SaveAsync(visitor, "k", Request("2", Now), "a", Token);

        var kept = await service.SaveAsync(visitor, "k", Request("1", Now.AddMinutes(-1)), "a", Token);

        var key = VisitorPreferenceValidator.VisitorKey(visitor);
        Assert.Equal([key, key], _repository.WriteLocks);
        Assert.Equal(2, _unitOfWork.Commits);
        // The refused write was not received: the kept row answers with its own receipt date.
        Assert.Equal(new VisitorPreferenceDto("k", "2", Now, Now), kept);
    }

    [Fact]
    public async Task Save_AfterTheClockWentBack_IsReceivedJustAfterTheVisitorsLatestReceipt()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();
        await service.SaveAsync(visitor, "k1", Request("1", Now.AddMinutes(-5)), "a", Token);
        _clock.Now = Now.AddMinutes(-1);

        var later = await service.SaveAsync(visitor, "k2", Request("1", Now.AddMinutes(-5)), "a", Token);

        Assert.Equal(Now.AddTicks(TimeSpan.TicksPerMicrosecond), later.ReceivedAt);
    }

    [Theory]
    [InlineData(0, null, 0)]
    [InlineData(7, null, 0)]
    [InlineData(25, 10, 20)]
    [InlineData(25, 20, 30)]
    [InlineData(25, 30, 40)]
    public void ReceivedAt_IsTheClockAtTheMicrosecond_OrJustAfterTheLatestReceipt(int nowTicks, int? lastTicks, int expectedTicks)
    {
        var received = VisitorPreferenceService.ReceivedAt(Now.AddTicks(nowTicks), lastTicks is { } last ? Now.AddTicks(last) : null);

        Assert.Equal(Now.AddTicks(expectedTicks), received);
        Assert.Equal(TimeSpan.Zero, received.Offset);
    }

    [Fact]
    public async Task Save_ANewVisitorPastTheAddressQuota_IsRefused_AnExistingVisitorIsNot()
    {
        _gate = new CountingGate(permits: 1);
        var service = Create();
        var first = VisitorPreferenceApi.CreateVisitorId();
        await service.SaveAsync(first, "k", Request("1", Now.AddMinutes(-1)), "198.51.100.1", Token);

        await Assert.ThrowsAsync<TooManyRequestsException>(() =>
            service.SaveAsync(VisitorPreferenceApi.CreateVisitorId(), "k", Request("1", Now), "198.51.100.1", Token));
        Assert.Equal(new VisitorPreferenceDto("k", "2", Now, Now.AddTicks(TimeSpan.TicksPerMicrosecond)), await service.SaveAsync(first, "k", Request("2", Now), "198.51.100.1", Token));
        // The existing visitor never asked the gate again.
        Assert.Equal(2, _gate.Partitions.Count);
    }

    [Fact]
    public async Task Save_AfterAConcurrentWrite_ReplaysAndStores()
    {
        var service = Create();
        _unitOfWork.ConflictsLeft = VisitorPreferenceService.MaxAttempts - 1;

        var outcome = await service.SaveAsync(VisitorPreferenceApi.CreateVisitorId(), "k", Request("1", Now), "a", Token);

        Assert.Equal(new VisitorPreferenceDto("k", "1", Now, Now), outcome);
        Assert.Equal(VisitorPreferenceService.MaxAttempts - 1, _unitOfWork.Discards);
        Assert.Single(_repository.Preferences);
    }

    [Fact]
    public async Task Save_WhenEveryAttemptConflicts_ReportsAConflictAndStoresNothing()
    {
        var service = Create();
        _unitOfWork.ConflictsLeft = VisitorPreferenceService.MaxAttempts;

        await Assert.ThrowsAsync<ConflictException>(() => service.SaveAsync(VisitorPreferenceApi.CreateVisitorId(), "k", Request("1", Now), "a", Token));

        Assert.Empty(_repository.Preferences);
        Assert.Empty(_repository.Visitors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("0123456789012345678901234567890123456789+/=")]
    public async Task EveryCall_WithoutAValidVisitorId_IsABadRequest(string? visitor)
    {
        var service = Create();

        await Assert.ThrowsAsync<BadRequestException>(() => service.GetAsync(visitor, "k", Token));
        await Assert.ThrowsAsync<BadRequestException>(() => service.GetChangedSinceAsync(visitor, null, Token));
        await Assert.ThrowsAsync<BadRequestException>(() => service.SaveAsync(visitor, "k", Request("1", Now), "a", Token));
        Assert.Empty(_repository.Touches);
    }

    [Fact]
    public async Task Reads_OfAnUnknownVisitor_HoldNothingAndCreateNothing()
    {
        var service = Create();
        var visitor = VisitorPreferenceApi.CreateVisitorId();

        Assert.Null(await service.GetAsync(visitor, "omni.appearance", Token));
        Assert.Empty(await service.GetChangedSinceAsync(visitor, null, Token));

        Assert.Empty(_repository.Visitors);
        Assert.Equal(0, _unitOfWork.Saves);
        // A read asks to move the last-seen date, at most once an hour.
        Assert.All(_repository.Touches, touch => Assert.Equal(Now - VisitorPreferenceService.TouchGranularity, touch.StaleBefore));
    }
}
