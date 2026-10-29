using Microsoft.Extensions.Options;
using OmniEurope.Blazor.Showcase.Back.Components.Shared;
using OmniEurope.Blazor.Showcase.Back.Exceptions;
using OmniEurope.Blazor.Showcase.Back.Models;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

public sealed class VisitorPreferenceService(
    IVisitorPreferenceRepository repository,
    IUnitOfWork unitOfWork,
    IVisitorCreationGate creationGate,
    IOptions<PreferenceLimitsOptions> limits,
    TimeProvider time,
    ILogger<VisitorPreferenceService> logger) : IVisitorPreferenceService
{
    /// <summary>How many times a write is replayed when another write of the same visitor got in first.</summary>
    internal const int MaxAttempts = 3;

    /// <summary>A read moves the last-seen date at most this often, so reading never writes on every call.</summary>
    internal static readonly TimeSpan TouchGranularity = TimeSpan.FromHours(1);

    private readonly PreferenceLimitsOptions _limits = limits.Value;

    public async Task<VisitorPreferenceDto?> GetAsync(string? visitorId, string? key, CancellationToken cancellationToken)
    {
        var visitorKey = VisitorPreferenceValidator.VisitorKey(visitorId);
        var validKey = VisitorPreferenceValidator.Key(key);
        await TouchAsync(visitorKey, cancellationToken).ConfigureAwait(false);
        var preference = await repository.GetPreferenceAsync(visitorKey, validKey, cancellationToken).ConfigureAwait(false);
        return preference is null ? null : ToDto(preference);
    }

    public async Task<IReadOnlyList<VisitorPreferenceDto>> GetChangedSinceAsync(string? visitorId, DateTimeOffset? since, CancellationToken cancellationToken)
    {
        var visitorKey = VisitorPreferenceValidator.VisitorKey(visitorId);
        await TouchAsync(visitorKey, cancellationToken).ConfigureAwait(false);
        // A visitor never holds more than its key quota, so the limit only bounds the query.
        var changed = await repository.GetChangedSinceAsync(visitorKey, since?.ToUniversalTime(), _limits.MaxKeysPerVisitor, cancellationToken)
            .ConfigureAwait(false);
        return [.. changed.Select(ToDto)];
    }

    public async Task<VisitorPreferenceSaveOutcome> SaveAsync(string? visitorId, string? key, SaveVisitorPreferenceRequest? request,
        string clientPartition, CancellationToken cancellationToken)
    {
        var visitorKey = VisitorPreferenceValidator.VisitorKey(visitorId);
        var validKey = VisitorPreferenceValidator.Key(key);
        var updatedAt = VisitorPreferenceValidator.Request(request, _limits, time.GetUtcNow());
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await TrySaveAsync(visitorKey, validKey, request!.Value, updatedAt, clientPartition, cancellationToken).ConfigureAwait(false);
            }
            catch (ConcurrentWriteException exception) when (attempt < MaxAttempts)
            {
                logger.LogDebug(exception, "A concurrent write of the same visitor got in first; attempt {Attempt} replays.", attempt + 1);
                unitOfWork.Discard();
            }
            catch (ConcurrentWriteException)
            {
                unitOfWork.Discard();
                throw new ConflictException("The preferences of this visitor kept changing during the write; send it again.");
            }
        }
    }

    private async Task<VisitorPreferenceSaveOutcome> TrySaveAsync(string visitorKey, string key, string? value, DateTimeOffset updatedAt,
        string clientPartition, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var visitor = await repository.FindVisitorAsync(visitorKey, cancellationToken).ConfigureAwait(false);
        if (visitor is not null)
        {
            return await WriteAsync(visitor, visitorKey, key, value, updatedAt, now, cancellationToken).ConfigureAwait(false);
        }

        // A new visitor: its admission (the capacity count, the insert and the save) holds the admission
        // lock until the commit, so concurrent admissions on every API instance see each other's rows.
        await using var admission = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var admitted = await AdmitVisitorAsync(visitorKey, clientPartition, now, cancellationToken).ConfigureAwait(false);
        var outcome = await WriteAsync(admitted, visitorKey, key, value, updatedAt, now, cancellationToken).ConfigureAwait(false);
        await admission.CommitAsync(cancellationToken).ConfigureAwait(false);
        return outcome;
    }

    private async Task<VisitorPreferenceSaveOutcome> WriteAsync(Visitor visitor, string visitorKey, string key, string? value,
        DateTimeOffset updatedAt, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Every write moves the revision, the concurrency token: of two writes of this visitor at once,
        // the second fails to save and replays with the quotas the first one left.
        visitor.LastSeenAt = now;
        visitor.Revision++;

        var stored = await repository.FindPreferenceAsync(visitorKey, key, cancellationToken).ConfigureAwait(false);
        if (stored is not null && stored.UpdatedAt >= updatedAt)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return VisitorPreferenceSaveOutcome.KeptNewer;
        }

        await EnsureQuotaAsync(visitorKey, key, value, cancellationToken).ConfigureAwait(false);
        if (stored is null)
        {
            repository.AddPreference(new VisitorPreference { VisitorId = visitorKey, Key = key, Value = value, UpdatedAt = updatedAt });
        }
        else
        {
            stored.Value = value;
            stored.UpdatedAt = updatedAt;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return VisitorPreferenceSaveOutcome.Stored;
    }

    private async Task<Visitor> AdmitVisitorAsync(string visitorKey, string clientPartition, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await repository.LockVisitorAdmissionAsync(cancellationToken).ConfigureAwait(false);
        // A concurrent first write of the same visitor may have been admitted while this one waited for the lock.
        if (await repository.FindVisitorAsync(visitorKey, cancellationToken).ConfigureAwait(false) is { } admitted)
        {
            return admitted;
        }

        if (await repository.CountVisitorsAsync(cancellationToken).ConfigureAwait(false) >= _limits.MaxVisitors)
        {
            logger.LogWarning("The visitor capacity ({MaxVisitors}) is reached: a new visitor was refused.", _limits.MaxVisitors);
            throw new CapacityReachedException("The showcase holds as many visitors as it can; the preferences stay in this browser.");
        }

        if (!creationGate.TryEnter(clientPartition))
        {
            throw new TooManyRequestsException("Too many new visitors from this address; try again later.");
        }

        var visitor = new Visitor { Id = visitorKey, CreatedAt = now, LastSeenAt = now };
        repository.AddVisitor(visitor);
        return visitor;
    }

    private async Task EnsureQuotaAsync(string visitorKey, string key, string? value, CancellationToken cancellationToken)
    {
        var usage = await repository.GetUsageAsync(visitorKey, key, cancellationToken).ConfigureAwait(false);
        if (usage.Keys + 1 > _limits.MaxKeysPerVisitor)
        {
            throw new QuotaExceededException($"A visitor holds at most {_limits.MaxKeysPerVisitor} preferences.");
        }

        if (usage.StoredLength + (value?.Length ?? 0) > _limits.MaxStoredLengthPerVisitor)
        {
            throw new QuotaExceededException($"The preferences of a visitor hold at most {_limits.MaxStoredLengthPerVisitor} characters.");
        }
    }

    private Task TouchAsync(string visitorKey, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        return repository.TouchAsync(visitorKey, now, now - TouchGranularity, cancellationToken);
    }

    private static VisitorPreferenceDto ToDto(VisitorPreference preference) => new(preference.Key, preference.Value, preference.UpdatedAt);
}
