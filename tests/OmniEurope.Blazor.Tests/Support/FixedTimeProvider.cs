namespace OmniEurope.Blazor.Tests;

/// <summary>A component clock that always reads <paramref name="value"/>.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
