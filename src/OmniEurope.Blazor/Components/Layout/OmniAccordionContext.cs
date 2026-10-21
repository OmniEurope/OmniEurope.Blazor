namespace OmniEurope.Blazor.Components;

/// <summary>
/// What an <see cref="OmniAccordion"/> hands down to its items: its settings, and the registry that
/// lets an item closing its peers (single mode) reach them.
/// </summary>
internal sealed class OmniAccordionContext
{
    private readonly List<OmniAccordionItem> _items = [];

    public bool Multiple { get; set; }
    public bool KeepContent { get; set; }
    public OmniHeadingLevel HeadingLevel { get; set; } = OmniHeadingLevel.H3;

    public void Register(OmniAccordionItem item) => _items.Add(item);

    public void Unregister(OmniAccordionItem item) => _items.Remove(item);

    /// <summary>
    /// In single mode, closes every other open row of the accordion, each reporting its own
    /// <see cref="OmniAccordionItem.ExpandedChanged"/> so a bound value stays true to the screen.
    /// </summary>
    public async Task ClosePeersAsync(OmniAccordionItem opened)
    {
        if (Multiple)
        {
            return;
        }

        foreach (var peer in _items.Where(item => !ReferenceEquals(item, opened) && item.IsOpen).ToArray())
        {
            await peer.CloseFromPeerAsync();
        }
    }
}
