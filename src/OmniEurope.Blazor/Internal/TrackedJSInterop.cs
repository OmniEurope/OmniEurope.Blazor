using System.Diagnostics;
using Microsoft.JSInterop;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// JavaScript interop calls that report a lost circuit where it happens (BL0016: an interop call sits in a
/// try/catch). A <see cref="JSDisconnectedException"/> is recorded on the current <see cref="Activity"/>, so a
/// trace shows which call met the lost circuit, then rethrown untouched: whether a lost circuit is ignored, and
/// what is skipped after it, stays the decision of the component's entry point (the lifecycle method or event
/// handler that catches it), exactly as before. Every other failure, a <see cref="JSException"/> from the script
/// and a cancellation included, propagates without being touched.
/// </summary>
internal static class TrackedJSInterop
{
    /// <summary>Calls a module function that returns nothing.</summary>
    internal static async ValueTask InvokeVoidTrackedAsync(this IJSObjectReference reference, string identifier, params object?[]? args)
    {
        try
        {
            await reference.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Calls a module function that returns nothing, cancellable.</summary>
    internal static async ValueTask InvokeVoidTrackedAsync(this IJSObjectReference reference, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        try
        {
            await reference.InvokeVoidAsync(identifier, cancellationToken, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Calls a module function and reads its result.</summary>
    internal static async ValueTask<TValue> InvokeTrackedAsync<TValue>(this IJSObjectReference reference, string identifier, params object?[]? args)
    {
        try
        {
            return await reference.InvokeAsync<TValue>(identifier, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Calls a module function and reads its result, cancellable.</summary>
    internal static async ValueTask<TValue> InvokeTrackedAsync<TValue>(this IJSObjectReference reference, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        try
        {
            return await reference.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Calls a global function that returns nothing.</summary>
    internal static async ValueTask InvokeVoidTrackedAsync(this IJSRuntime runtime, string identifier, params object?[]? args)
    {
        try
        {
            await runtime.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Calls a global function and reads its result.</summary>
    internal static async ValueTask<TValue> InvokeTrackedAsync<TValue>(this IJSRuntime runtime, string identifier, params object?[]? args)
    {
        try
        {
            return await runtime.InvokeAsync<TValue>(identifier, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    /// <summary>Calls a global function and reads its result, cancellable.</summary>
    internal static async ValueTask<TValue> InvokeTrackedAsync<TValue>(this IJSRuntime runtime, string identifier, CancellationToken cancellationToken, params object?[]? args)
    {
        try
        {
            return await runtime.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }
        catch (JSDisconnectedException exception)
        {
            Record(exception);
            throw;
        }
    }

    private static void Record(JSDisconnectedException exception) => Activity.Current?.AddException(exception);
}
