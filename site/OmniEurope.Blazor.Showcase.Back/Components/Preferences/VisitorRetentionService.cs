using Microsoft.Extensions.Options;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>
/// Deletes the visitors idle for longer than <see cref="VisitorRetentionOptions.IdleDays"/>, with their
/// preferences: at start-up, then every <see cref="VisitorRetentionOptions.SweepInterval"/>. The showcase
/// keeps no data about a visitor who stopped coming, and an abandoned id cannot hold rows forever.
/// </summary>
public sealed class VisitorRetentionService(
    IServiceScopeFactory scopes,
    IOptions<VisitorRetentionOptions> options,
    TimeProvider time,
    ILogger<VisitorRetentionService> logger) : BackgroundService
{
    private readonly VisitorRetentionOptions _options = options.Value;

    /// <summary>Runs one sweep: deletes batch after batch until no idle visitor is left.</summary>
    /// <returns>How many visitors were deleted.</returns>
    public async Task<int> SweepAsync(CancellationToken cancellationToken)
    {
        var idleBefore = time.GetUtcNow() - TimeSpan.FromDays(_options.IdleDays);
        var total = 0;
        await using var scope = scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IVisitorPreferenceRepository>();
        int deleted;
        do
        {
            deleted = await repository.DeleteIdleVisitorsAsync(idleBefore, _options.BatchSize, cancellationToken).ConfigureAwait(false);
            total += deleted;
        }
        while (deleted == _options.BatchSize);

        if (total > 0)
        {
            logger.LogInformation("Retention: {Count} visitors idle since before {IdleBefore:u} were deleted.", total, idleBefore);
        }

        return total;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.SweepInterval, time);
        do
        {
            try
            {
                await SweepAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // A failed sweep (the database restarting) is retried at the next tick; the API keeps serving.
                logger.LogError(exception, "The retention sweep failed; the next one runs in {Interval}.", _options.SweepInterval);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }
}
