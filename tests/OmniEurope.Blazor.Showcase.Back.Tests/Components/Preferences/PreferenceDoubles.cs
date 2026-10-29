using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Back.Components.Shared;
using OmniEurope.Blazor.Showcase.Back.Exceptions;
using OmniEurope.Blazor.Showcase.Back.Models;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Components.Preferences;

// Test doubles of the service's collaborators, for the unit tests of its rules only. They are not
// PostgreSQL: the queries, the concurrency token and the cascade are proved against a real database by
// the tests of the Integration folder.

/// <summary>An in-memory repository: staged additions become visible once <see cref="MemoryUnitOfWork"/> saves.</summary>
internal sealed class MemoryVisitorRepository : IVisitorPreferenceRepository
{
    public List<Visitor> Visitors { get; } = [];

    public List<VisitorPreference> Preferences { get; } = [];

    public List<Visitor> PendingVisitors { get; } = [];

    public List<VisitorPreference> PendingPreferences { get; } = [];

    public List<(string VisitorKey, DateTimeOffset SeenAt, DateTimeOffset StaleBefore)> Touches { get; } = [];

    public Task<Visitor?> FindVisitorAsync(string visitorKey, CancellationToken cancellationToken) =>
        Task.FromResult(Visitors.FirstOrDefault(visitor => visitor.Id == visitorKey));

    public int AdmissionLocks { get; private set; }

    public Task LockVisitorAdmissionAsync(CancellationToken cancellationToken)
    {
        AdmissionLocks++;
        return Task.CompletedTask;
    }

    public Task<int> CountVisitorsAsync(CancellationToken cancellationToken) => Task.FromResult(Visitors.Count);

    public void AddVisitor(Visitor visitor) => PendingVisitors.Add(visitor);

    public Task<VisitorPreference?> FindPreferenceAsync(string visitorKey, string key, CancellationToken cancellationToken) =>
        Task.FromResult(Preferences.FirstOrDefault(preference => preference.VisitorId == visitorKey && preference.Key == key));

    public void AddPreference(VisitorPreference preference) => PendingPreferences.Add(preference);

    public Task<VisitorUsage> GetUsageAsync(string visitorKey, string exceptKey, CancellationToken cancellationToken)
    {
        var others = Preferences.Where(preference => preference.VisitorId == visitorKey && preference.Key != exceptKey).ToList();
        return Task.FromResult(new VisitorUsage(others.Count, others.Sum(preference => (long)(preference.Value?.Length ?? 0))));
    }

    public Task<VisitorPreference?> GetPreferenceAsync(string visitorKey, string key, CancellationToken cancellationToken) =>
        FindPreferenceAsync(visitorKey, key, cancellationToken);

    public Task<IReadOnlyList<VisitorPreference>> GetChangedSinceAsync(string visitorKey, DateTimeOffset? since, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VisitorPreference>>([.. Preferences
            .Where(preference => preference.VisitorId == visitorKey && (since is null || preference.UpdatedAt >= since))
            .OrderBy(preference => preference.Key, StringComparer.Ordinal)
            .Take(limit)]);

    public Task TouchAsync(string visitorKey, DateTimeOffset seenAt, DateTimeOffset staleBefore, CancellationToken cancellationToken)
    {
        Touches.Add((visitorKey, seenAt, staleBefore));
        return Task.CompletedTask;
    }

    public Task<int> DeleteIdleVisitorsAsync(DateTimeOffset idleBefore, int batchSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException("The retention is proved against PostgreSQL.");

    public void Commit()
    {
        Visitors.AddRange(PendingVisitors);
        Preferences.AddRange(PendingPreferences);
        PendingVisitors.Clear();
        PendingPreferences.Clear();
    }

    public void Drop()
    {
        PendingVisitors.Clear();
        PendingPreferences.Clear();
    }
}

/// <summary>Saves the repository's staged rows, or fails as a concurrent write would, <see cref="ConflictsLeft"/> times.</summary>
internal sealed class MemoryUnitOfWork(MemoryVisitorRepository repository) : IUnitOfWork
{
    public int ConflictsLeft { get; set; }

    public int Saves { get; private set; }

    public int Discards { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ConflictsLeft > 0)
        {
            ConflictsLeft--;
            return Task.FromException(new ConcurrentWriteException(new InvalidOperationException("simulated concurrent write")));
        }

        Saves++;
        repository.Commit();
        return Task.CompletedTask;
    }

    public int Commits { get; private set; }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IUnitOfWorkTransaction>(new MemoryTransaction(this));

    public void CountCommit() => Commits++;

    public void Discard()
    {
        Discards++;
        repository.Drop();
    }
}

/// <summary>A transaction of <see cref="MemoryUnitOfWork"/>: it only counts the commits, the saves already kept the rows.</summary>
internal sealed class MemoryTransaction(MemoryUnitOfWork unitOfWork) : IUnitOfWorkTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken)
    {
        unitOfWork.CountCommit();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>A visitor creation gate that lets through a fixed number of creations.</summary>
internal sealed class CountingGate(int permits) : IVisitorCreationGate
{
    public List<string> Partitions { get; } = [];

    public bool TryEnter(string clientPartition)
    {
        Partitions.Add(clientPartition);
        return Partitions.Count <= permits;
    }
}

/// <summary>A clock the test sets by hand.</summary>
internal sealed class ManualClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
