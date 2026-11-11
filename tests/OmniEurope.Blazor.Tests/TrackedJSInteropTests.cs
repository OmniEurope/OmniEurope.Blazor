using System.Diagnostics;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="TrackedJSInterop"/>: a lost circuit is recorded on the current activity then rethrown untouched,
/// every other failure and every result passes through, for each of the seven overloads.
/// </summary>
public sealed class TrackedJSInteropTests
{
    private const string Identifier = "omni.call";

    /// <summary>One overload under test: how it is called on the fake, and whether it carries a cancellation token.</summary>
    private static readonly Dictionary<string, Func<FakeJs, CancellationToken, Task>> Overloads = new()
    {
        ["module void"] = (js, _) => ((IJSObjectReference)js).InvokeVoidTrackedAsync(Identifier, 1, "two").AsTask(),
        ["module void cancellable"] = (js, token) => ((IJSObjectReference)js).InvokeVoidTrackedAsync(Identifier, token, 1, "two").AsTask(),
        ["module result"] = (js, _) => ((IJSObjectReference)js).InvokeTrackedAsync<string>(Identifier, 1, "two").AsTask(),
        ["module result cancellable"] = (js, token) => ((IJSObjectReference)js).InvokeTrackedAsync<string>(Identifier, token, 1, "two").AsTask(),
        ["runtime void"] = (js, _) => ((IJSRuntime)js).InvokeVoidTrackedAsync(Identifier, 1, "two").AsTask(),
        ["runtime result"] = (js, _) => ((IJSRuntime)js).InvokeTrackedAsync<string>(Identifier, 1, "two").AsTask(),
        ["runtime result cancellable"] = (js, token) => ((IJSRuntime)js).InvokeTrackedAsync<string>(Identifier, token, 1, "two").AsTask()
    };

    public static TheoryData<string> OverloadNames { get; } = [.. Overloads.Keys];

    private static Activity StartActivity(out IDisposable listening)
    {
        var source = new ActivitySource("OmniEurope.Blazor.Tests.TrackedJSInterop");
        var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate.Name == source.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        listening = new Disposables(source, listener);
        return source.StartActivity("interop-call") ?? throw new InvalidOperationException("The test activity was not sampled.");
    }

    [Theory]
    [MemberData(nameof(OverloadNames))]
    public async Task LostCircuit_IsRecordedOnTheCurrentActivity_AndRethrownAsTheSameException(string overload)
    {
        var lost = new JSDisconnectedException("circuit lost");
        var js = new FakeJs { Failure = lost };
        using var activity = StartActivity(out var listening);
        using (listening)
        {
            var thrown = await Assert.ThrowsAsync<JSDisconnectedException>(() => Overloads[overload](js, CancellationToken.None));

            Assert.Same(lost, thrown);
            var recorded = Assert.Single(activity.Events);
            Assert.Equal("exception", recorded.Name);
            var tags = recorded.Tags.ToDictionary(tag => tag.Key, tag => tag.Value);
            Assert.Equal(typeof(JSDisconnectedException).FullName, tags["exception.type"]);
            Assert.Equal("circuit lost", tags["exception.message"]);
            Assert.Equal(Identifier, Assert.Single(js.Calls).Identifier);
        }
    }

    [Theory]
    [MemberData(nameof(OverloadNames))]
    public async Task LostCircuit_WithoutAnActivity_IsStillRethrownAsTheSameException(string overload)
    {
        Assert.Null(Activity.Current);
        var lost = new JSDisconnectedException("circuit lost");

        var thrown = await Assert.ThrowsAsync<JSDisconnectedException>(() => Overloads[overload](new FakeJs { Failure = lost }, CancellationToken.None));

        Assert.Same(lost, thrown);
    }

    [Theory]
    [MemberData(nameof(OverloadNames))]
    public async Task AScriptError_PassesUntouched_AndIsNotRecorded(string overload)
    {
        var failure = new JSException("script failed");
        using var activity = StartActivity(out var listening);
        using (listening)
        {
            var thrown = await Assert.ThrowsAsync<JSException>(() => Overloads[overload](new FakeJs { Failure = failure }, CancellationToken.None));

            Assert.Same(failure, thrown);
            Assert.Empty(activity.Events);
        }
    }

    [Theory]
    [MemberData(nameof(OverloadNames))]
    public async Task ACancellation_PassesUntouched_AndIsNotRecorded(string overload)
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var failure = new OperationCanceledException(cancelled.Token);
        using var activity = StartActivity(out var listening);
        using (listening)
        {
            var thrown = await Assert.ThrowsAsync<OperationCanceledException>(() => Overloads[overload](new FakeJs { Failure = failure }, cancelled.Token));

            Assert.Same(failure, thrown);
            Assert.Empty(activity.Events);
        }
    }

    [Theory]
    [MemberData(nameof(OverloadNames))]
    public async Task ASuccessfulCall_ForwardsItsArguments_AndRecordsNothing(string overload)
    {
        using var token = new CancellationTokenSource();
        var js = new FakeJs { Result = "answer" };
        using var activity = StartActivity(out var listening);
        using (listening)
        {
            await Overloads[overload](js, token.Token);

            var call = Assert.Single(js.Calls);
            Assert.Equal(Identifier, call.Identifier);
            Assert.Equal<object?[]>([1, "two"], call.Args!);
            Assert.Equal(overload.EndsWith("cancellable", StringComparison.Ordinal) ? token.Token : CancellationToken.None, call.Token);
            Assert.Empty(activity.Events);
        }
    }

    [Fact]
    public async Task AResultCall_ReturnsWhatTheScriptReturned()
    {
        var js = new FakeJs { Result = "answer" };

        Assert.Equal("answer", await ((IJSObjectReference)js).InvokeTrackedAsync<string>(Identifier));
        Assert.Equal("answer", await ((IJSObjectReference)js).InvokeTrackedAsync<string>(Identifier, CancellationToken.None));
        Assert.Equal("answer", await ((IJSRuntime)js).InvokeTrackedAsync<string>(Identifier));
        Assert.Equal("answer", await ((IJSRuntime)js).InvokeTrackedAsync<string>(Identifier, CancellationToken.None));
    }

    private sealed class Disposables(params IDisposable[] items) : IDisposable
    {
        public void Dispose()
        {
            foreach (var item in items)
            {
                item.Dispose();
            }
        }
    }

    /// <summary>A module and a runtime in one: answers with a result or fails with the exception it was given, asynchronously as a lost circuit does.</summary>
    private sealed class FakeJs : IJSRuntime, IJSObjectReference
    {
        internal Exception? Failure { get; init; }

        internal object? Result { get; init; }

        internal List<(string Identifier, CancellationToken Token, object?[]? Args)> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add((identifier, CancellationToken.None, args));
            return Script.Outcome<TValue>(Failure, Result);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Calls.Add((identifier, cancellationToken, args));
            return Script.Outcome<TValue>(Failure, Result);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    }

    /// <summary>Kept outside the fake, as ManualJSRuntimeScript is, so the interop analyzer sees no unguarded call.</summary>
    private static class Script
    {
        internal static ValueTask<TValue> Outcome<TValue>(Exception? failure, object? result) =>
            failure is not null
                ? ValueTask.FromException<TValue>(failure)
                : ValueTask.FromResult(result is TValue value ? value : default!);
    }
}
