using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Tests;

/// <summary>A JavaScript runtime double whose call without a token is the call with an empty one.</summary>
internal abstract class ForwardingJSRuntime : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public abstract ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args);
}

/// <summary>A script module double whose call without a token is the call with an empty one.</summary>
internal abstract class ForwardingJSObjectReference : ForwardingJSRuntime, IJSObjectReference
{
    public abstract ValueTask DisposeAsync();
}
