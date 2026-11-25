namespace OmniEurope.Blazor.Components;

/// <summary>
/// Runs a callback that comes from the script on the component's dispatcher: nothing once the component
/// is disposed, otherwise the work, then a render. The component passes its own dispatcher, disposed
/// flag and render, so this helper never depends on it.
/// </summary>
internal static class OmniGuardedDispatch
{
    /// <summary>Runs <paramref name="work"/> through <paramref name="invokeAsync"/> unless <paramref name="isDisposed"/>, then calls <paramref name="render"/>.</summary>
    internal static Task RunAsync(Func<Func<Task>, Task> invokeAsync, Func<bool> isDisposed, Action render, Func<Task> work) => invokeAsync(async () =>
    {
        if (isDisposed())
        {
            return;
        }

        await work();
        render();
    });

    /// <summary>Wraps a synchronous <paramref name="work"/> as the task-returning work <see cref="RunAsync"/> takes.</summary>
    internal static Func<Task> Synchronous(Action work) => () =>
    {
        work();
        return Task.CompletedTask;
    };
}
