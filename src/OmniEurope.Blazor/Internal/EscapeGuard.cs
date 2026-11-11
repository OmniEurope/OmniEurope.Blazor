namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Remembers that an Escape key press closed a list, so that the press stays held back from the handlers
/// around the field (a dialog that closes on Escape) until its key is released. Blazor WebAssembly renders
/// after the handler of the field and before the event moves up, so by then the list is closed and only
/// this flag still says the key was consumed. The owner binds <see cref="ReleaseHandler"/> to <c>onkeyup</c>.
/// </summary>
/// <param name="receiver">The component that receives the key release event.</param>
internal sealed class EscapeGuard(IHandleEvent receiver)
{
    /// <summary>Whether an Escape that closed the list is still waiting for its key to be released.</summary>
    internal bool IsConsumed { get; private set; }

    /// <summary>Marks the current Escape as consumed by the list.</summary>
    internal void Consume() => IsConsumed = true;

    /// <summary>Forgets the consumed Escape (another key was pressed, or the field lost the focus).</summary>
    internal void Reset() => IsConsumed = false;

    /// <summary>The key release handler while an Escape is consumed, otherwise a default callback (no handler).</summary>
    internal EventCallback<KeyboardEventArgs> ReleaseHandler =>
        IsConsumed ? EventCallback.Factory.Create<KeyboardEventArgs>(receiver, _ => Reset()) : default;
}
