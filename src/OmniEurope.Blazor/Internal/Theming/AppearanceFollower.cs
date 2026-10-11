using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The link of a component to <see cref="OmniAppearanceState"/> when the host leaves it uncontrolled:
/// the component reads its appearance parameters from the state and writes the changes to it. Whether
/// the host controls the component is decided once, from the parameters of its first render (an
/// appearance value or change callback written in the markup); a host that controls it
/// keeps it exactly as before, and the state is then never read nor written by it.
/// </summary>
internal sealed class AppearanceFollower : IDisposable
{
    private readonly Func<Task> _refresh;

    private AppearanceFollower(OmniAppearanceState state, Func<Task> refresh)
    {
        State = state;
        _refresh = refresh;
        state.Changed += OnChanged;
    }

    internal OmniAppearanceState State { get; }

    /// <summary>
    /// The link, or null when the host controls the component or registered no state (a host that
    /// never called <c>AddOmniEuropeBlazor</c>). <paramref name="refresh"/> is called on every change of
    /// the state, possibly off the renderer's thread: the component marshals it with <c>InvokeAsync</c>.
    /// </summary>
    internal static AppearanceFollower? Start(OmniAppearanceState? state, bool hostControls, Func<Task> refresh) =>
        state is null || hostControls ? null : new AppearanceFollower(state, refresh);

    /// <summary>Whether the host writes any of the parameters <paramref name="names"/>, whatever their value.</summary>
    internal static bool Sets(ParameterView parameters, params string[] names) =>
        names.Any(name => parameters.TryGetValue<object?>(name, out _));

    public void Dispose() => State.Changed -= OnChanged;

    // Raised by the state, which awaits nothing: the refresh is observed here, so its failure is logged
    // instead of lost. A component already disposed, or a page already gone, has nothing left to refresh.
    private void OnChanged() => _ = RefreshAsync();

    internal async Task RefreshAsync()
    {
        try
        {
            await _refresh();
        }
        catch (Exception exception) when (exception is ObjectDisposedException or JSDisconnectedException)
        {
        }
        catch (Exception exception)
        {
            State.Logger.LogError(exception, "A component could not apply the appearance that changed.");
        }
    }
}
