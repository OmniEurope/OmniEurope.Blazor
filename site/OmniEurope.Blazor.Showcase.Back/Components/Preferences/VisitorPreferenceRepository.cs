using Microsoft.EntityFrameworkCore;
using OmniEurope.Blazor.Showcase.Back.Data;
using OmniEurope.Blazor.Showcase.Back.Models;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

public sealed class VisitorPreferenceRepository(ShowcaseDbContext db) : IVisitorPreferenceRepository
{
    /// <summary>The PostgreSQL advisory lock key of visitor admissions (an arbitrary constant, "OEVISADM" in ASCII).</summary>
    internal const long AdmissionLockKey = 0x4F45_5649_5341_444D;

    public Task LockVisitorAdmissionAsync(CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AdmissionLockKey})", cancellationToken);

    /// <summary>
    /// The first key of the two-key PostgreSQL advisory locks of visitor writes ("OEVW" in ASCII); the second is a hash of
    /// the visitor. Two-key locks never collide with the single-key <see cref="AdmissionLockKey"/>; two visitors whose
    /// hashes collide only wait for each other.
    /// </summary>
    internal const int VisitorWriteLockSpace = 0x4F45_5657;

    public Task LockVisitorWritesAsync(string visitorKey, CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({VisitorWriteLockSpace}, hashtext({visitorKey}))", cancellationToken);

    public Task<DateTimeOffset?> GetLastReceivedAtAsync(string visitorKey, CancellationToken cancellationToken) =>
        db.VisitorPreferences.AsNoTracking()
            .Where(preference => preference.VisitorId == visitorKey)
            .MaxAsync(preference => (DateTimeOffset?)preference.ReceivedAt, cancellationToken);

    public Task<Visitor?> FindVisitorAsync(string visitorKey, CancellationToken cancellationToken) =>
        db.Visitors.FirstOrDefaultAsync(visitor => visitor.Id == visitorKey, cancellationToken);

    public Task<int> CountVisitorsAsync(CancellationToken cancellationToken) =>
        db.Visitors.CountAsync(cancellationToken);

    public void AddVisitor(Visitor visitor) => db.Visitors.Add(visitor);

    public Task<VisitorPreference?> FindPreferenceAsync(string visitorKey, string key, CancellationToken cancellationToken) =>
        db.VisitorPreferences.FirstOrDefaultAsync(preference => preference.VisitorId == visitorKey && preference.Key == key, cancellationToken);

    public void AddPreference(VisitorPreference preference) => db.VisitorPreferences.Add(preference);

    public async Task<VisitorUsage> GetUsageAsync(string visitorKey, string exceptKey, CancellationToken cancellationToken)
    {
        var others = db.VisitorPreferences.AsNoTracking()
            .Where(preference => preference.VisitorId == visitorKey && preference.Key != exceptKey);
        var keys = await others.CountAsync(cancellationToken).ConfigureAwait(false);
        var length = await others.SumAsync(preference => (long)(preference.Value == null ? 0 : preference.Value.Length), cancellationToken)
            .ConfigureAwait(false);
        return new VisitorUsage(keys, length);
    }

    public Task<VisitorPreference?> GetPreferenceAsync(string visitorKey, string key, CancellationToken cancellationToken) =>
        db.VisitorPreferences.AsNoTracking()
            .FirstOrDefaultAsync(preference => preference.VisitorId == visitorKey && preference.Key == key, cancellationToken);

    public async Task<IReadOnlyList<VisitorPreference>> GetChangedSinceAsync(string visitorKey, DateTimeOffset? since, int limit, CancellationToken cancellationToken)
    {
        var query = db.VisitorPreferences.AsNoTracking().Where(preference => preference.VisitorId == visitorKey);
        if (since is { } from)
        {
            query = query.Where(preference => preference.ReceivedAt >= from);
        }

        return await query.OrderBy(preference => preference.Key).Take(limit).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task TouchAsync(string visitorKey, DateTimeOffset seenAt, DateTimeOffset staleBefore, CancellationToken cancellationToken) =>
        db.Visitors.Where(visitor => visitor.Id == visitorKey && visitor.LastSeenAt < staleBefore)
            .ExecuteUpdateAsync(setters => setters.SetProperty(visitor => visitor.LastSeenAt, seenAt), cancellationToken);

    public Task<int> DeleteIdleVisitorsAsync(DateTimeOffset idleBefore, int batchSize, CancellationToken cancellationToken) =>
        db.Visitors.Where(visitor => visitor.LastSeenAt < idleBefore)
            .OrderBy(visitor => visitor.LastSeenAt)
            .Take(batchSize)
            .ExecuteDeleteAsync(cancellationToken);
}
