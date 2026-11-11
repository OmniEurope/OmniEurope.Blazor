using Bunit;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniScriptModule"/>, the one importer of the package's JavaScript modules: a success is a
/// single import, a <see cref="JSException"/> earns exactly one new attempt, a second one reaches the caller
/// as it came, and a lost circuit or a cancellation is never retried. The failures are scripted over bUnit's
/// own JavaScript runtime, so the successful attempt is a real bUnit module setup; nothing waits on a clock.
/// </summary>
public sealed class ScriptModuleLoaderTests : OmniBunitContext
{
    private const string Path = OmniModules.Interop;

    private ScriptedImports Runtime(params Exception[] failures)
    {
        JSInterop.SetupModule(Path);
        return new ScriptedImports(JSInterop.JSRuntime, failures);
    }

    [Fact]
    public async Task Import_Succeeds_WithASingleAttempt()
    {
        var runtime = Runtime();

        var module = await OmniScriptModule.ImportAsync(runtime, Path, CancellationToken.None);

        Assert.NotNull(module);
        Assert.Equal(1, runtime.Attempts);
        Assert.Equal([Path], runtime.Paths);
    }

    [Fact]
    public async Task Import_AfterAJavaScriptException_TriesOnceMore_AndReturnsTheModule()
    {
        var runtime = Runtime(new JSException("module request failed"));

        var module = await OmniScriptModule.ImportAsync(runtime, Path, CancellationToken.None);

        Assert.NotNull(module);
        Assert.Equal(2, runtime.Attempts);
        Assert.Equal([Path, Path], runtime.Paths);
    }

    [Fact]
    public async Task Import_FailingTwice_SurfacesTheSecondJavaScriptException_AfterTwoAttemptsOnly()
    {
        var second = new JSException("second failure");
        var runtime = Runtime(new JSException("first failure"), second, new JSException("never reached"));

        var thrown = await Assert.ThrowsAsync<JSException>(async () => await OmniScriptModule.ImportAsync(runtime, Path, CancellationToken.None));

        Assert.Same(second, thrown);
        Assert.Equal(2, runtime.Attempts);
    }

    [Fact]
    public async Task Import_AfterALostCircuit_IsNotRetried_AndThrowsAsItCame()
    {
        var lost = new JSDisconnectedException("circuit lost");
        var runtime = Runtime(lost);

        var thrown = await Assert.ThrowsAsync<JSDisconnectedException>(async () => await OmniScriptModule.ImportAsync(runtime, Path, CancellationToken.None));

        Assert.Same(lost, thrown);
        Assert.Equal(1, runtime.Attempts);
    }

    [Fact]
    public async Task Import_AfterACancellation_IsNotRetried_AndThrowsAsItCame()
    {
        var cancelled = new TaskCanceledException("the owner was disposed");
        var runtime = Runtime(cancelled);

        var thrown = await Assert.ThrowsAsync<TaskCanceledException>(async () => await OmniScriptModule.ImportAsync(runtime, Path, CancellationToken.None));

        Assert.Same(cancelled, thrown);
        Assert.Equal(1, runtime.Attempts);
    }

    [Fact]
    public async Task Import_ForwardsAToken_AndANonCancelledOneStillAllowsTheRetry()
    {
        var runtime = Runtime(new JSException("module request failed"));
        using var source = new CancellationTokenSource();

        var module = await OmniScriptModule.ImportAsync(runtime, Path, source.Token);

        Assert.NotNull(module);
        Assert.Equal(2, runtime.Attempts);
        Assert.Equal([source.Token, source.Token], runtime.Tokens);
    }

    [Fact]
    public async Task Import_WithATokenCancelledDuringTheFailure_SurfacesTheExceptionWithoutAnotherAttempt()
    {
        using var source = new CancellationTokenSource();
        var failure = new JSException("module request failed");
        var runtime = Runtime(failure);
        runtime.OnFailure = source.Cancel;

        var thrown = await Assert.ThrowsAsync<JSException>(async () => await OmniScriptModule.ImportAsync(runtime, Path, source.Token));

        Assert.Same(failure, thrown);
        Assert.Equal(1, runtime.Attempts);
    }

    [Fact]
    public async Task Import_OfADisposedOwner_ReleasesTheModule_AndReturnsNull()
    {
        var runtime = Runtime();

        var module = await OmniScriptModule.ImportAsync(runtime, Path, () => true);

        Assert.Null(module);
        Assert.Equal(1, runtime.Attempts);
    }

    [Fact]
    public async Task Import_OfALiveOwner_ReturnsTheModule_EvenAfterARetry()
    {
        var runtime = Runtime(new JSException("module request failed"));

        var module = await OmniScriptModule.ImportAsync(runtime, Path, () => false);

        Assert.NotNull(module);
        Assert.Equal(2, runtime.Attempts);
    }

    /// <summary>The bUnit runtime, failing the first imports with the scripted exceptions then answering for real.</summary>
    private sealed class ScriptedImports(IJSRuntime inner, IReadOnlyList<Exception> failures) : IJSRuntime
    {
        public int Attempts { get; private set; }

        public List<string> Paths { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public Action? OnFailure { get; set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Assert.Equal("import", identifier);
            Paths.Add((string)args![0]!);
            Tokens.Add(cancellationToken);
            if (Attempts < failures.Count)
            {
                var failure = failures[Attempts++];
                OnFailure?.Invoke();
                return ValueTask.FromException<TValue>(failure);
            }

            Attempts++;
            return inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
        }
    }
}
