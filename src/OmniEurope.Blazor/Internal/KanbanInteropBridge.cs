namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The only .NET object <c>omni-kanban.js</c> can call: it forwards the keys pressed on a card to the
/// board. Kept apart from the component so that the callable surface is internal rather than public API.
/// </summary>
internal sealed class KanbanInteropBridge(Func<string, string, Task> onCardKey)
{
    [JSInvokable]
    public Task OnCardKey(string card, string key) => onCardKey(card, key);
}
