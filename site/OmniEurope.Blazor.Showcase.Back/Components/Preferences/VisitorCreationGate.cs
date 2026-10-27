using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>Whether a client address may create one more visitor now.</summary>
public interface IVisitorCreationGate
{
    /// <summary>Takes one creation from the quota of <paramref name="clientPartition"/>.</summary>
    /// <returns>False when the address already created its share of visitors in the current hour.</returns>
    bool TryEnter(string clientPartition);
}

/// <summary>
/// The per-address limit on new visitors. The request rate limits bound how often an address writes;
/// this bounds how many visitor ids it can fill, since each fresh id brings a fresh per-visitor quota.
/// The count is kept in memory: a restart forgets it, which only ever lets an address start again.
/// </summary>
public sealed class VisitorCreationGate : IVisitorCreationGate, IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    public VisitorCreationGate(IOptions<PreferenceLimitsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var permits = options.Value.NewVisitorsPerAddressPerHour;
        _limiter = PartitionedRateLimiter.Create<string, string>(partition => RateLimitPartition.GetFixedWindowLimiter(partition, _ =>
            new FixedWindowRateLimiterOptions { PermitLimit = permits, Window = TimeSpan.FromHours(1), QueueLimit = 0, AutoReplenishment = true }));
    }

    public bool TryEnter(string clientPartition)
    {
        using var lease = _limiter.AttemptAcquire(clientPartition);
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
