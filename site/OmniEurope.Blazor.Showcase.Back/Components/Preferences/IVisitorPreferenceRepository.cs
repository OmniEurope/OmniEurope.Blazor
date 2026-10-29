using OmniEurope.Blazor.Showcase.Back.Models;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>What the other preferences of a visitor already take, besides the key being written.</summary>
/// <param name="Keys">How many keys, tombstones included.</param>
/// <param name="StoredLength">How many characters of JSON.</param>
public sealed record VisitorUsage(int Keys, long StoredLength);

/// <summary>
/// The visitor rows. The tracked reads and the <c>Add</c> methods stage changes that
/// <see cref="Shared.IUnitOfWork.SaveChangesAsync"/> saves; the other methods run at once.
/// </summary>
public interface IVisitorPreferenceRepository
{
    /// <summary>The visitor, tracked for a write, or null.</summary>
    Task<Visitor?> FindVisitorAsync(string visitorKey, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for, then holds until the end of the open transaction, the one lock every visitor admission takes,
    /// across every API instance on the database: the count and the insert of an admission run alone.
    /// </summary>
    /// <remarks>Only meaningful inside <see cref="Shared.IUnitOfWork.BeginTransactionAsync"/>: outside, the lock ends with the statement.</remarks>
    Task LockVisitorAdmissionAsync(CancellationToken cancellationToken);

    Task<int> CountVisitorsAsync(CancellationToken cancellationToken);

    void AddVisitor(Visitor visitor);

    /// <summary>The preference, tracked for a write, or null.</summary>
    Task<VisitorPreference?> FindPreferenceAsync(string visitorKey, string key, CancellationToken cancellationToken);

    void AddPreference(VisitorPreference preference);

    Task<VisitorUsage> GetUsageAsync(string visitorKey, string exceptKey, CancellationToken cancellationToken);

    /// <summary>The preference, untracked, or null.</summary>
    Task<VisitorPreference?> GetPreferenceAsync(string visitorKey, string key, CancellationToken cancellationToken);

    /// <summary>The preferences changed at or after <paramref name="since"/> (all when null), untracked, at most <paramref name="limit"/>.</summary>
    Task<IReadOnlyList<VisitorPreference>> GetChangedSinceAsync(string visitorKey, DateTimeOffset? since, int limit, CancellationToken cancellationToken);

    /// <summary>Moves the last-seen date to <paramref name="seenAt"/> when it is older than <paramref name="staleBefore"/>.</summary>
    Task TouchAsync(string visitorKey, DateTimeOffset seenAt, DateTimeOffset staleBefore, CancellationToken cancellationToken);

    /// <summary>Deletes at most <paramref name="batchSize"/> visitors last seen before <paramref name="idleBefore"/>, with their preferences.</summary>
    /// <returns>How many visitors were deleted.</returns>
    Task<int> DeleteIdleVisitorsAsync(DateTimeOffset idleBefore, int batchSize, CancellationToken cancellationToken);
}
