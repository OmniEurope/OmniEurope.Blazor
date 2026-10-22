namespace OmniEurope.Blazor.Components;

/// <summary>
/// A list of rows (<see cref="OmniAccordionItem"/>) whose bodies are revealed below their header on
/// click, Enter or Space, one at a time or several together (<see cref="Multiple"/>). It follows the
/// WAI-ARIA accordion pattern: Up, Down, Home and End move focus between the headers.
/// </summary>
public partial class OmniAccordion
{
    private readonly OmniAccordionContext _context = new();
    private bool _pageKeys;

    /// <summary>The rows, <see cref="OmniAccordionItem"/> in the order they are shown.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether several rows can be open at once. False, the default, opens one row at a time:
    /// opening a row closes the one that was open, which reports it through its own
    /// <see cref="OmniAccordionItem.ExpandedChanged"/>. The rows a host sets open through
    /// <see cref="OmniAccordionItem.Expanded"/> are shown as given, even when this is false.
    /// </summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>
    /// Whether a body, once opened, stays rendered (hidden) after its row closes, keeping what is in
    /// it (scroll position, filters of a grid, typed text). False, the default, renders a body only
    /// while its row is open and drops it on closing, so a long list of rows costs only its headers.
    /// </summary>
    [Parameter]
    public bool KeepContent { get; set; }

    /// <summary>
    /// The level of the headings that wrap the toggles (<c>role="heading"</c> and <c>aria-level</c>),
    /// to fit the outline of the page: <see cref="OmniHeadingLevel.H3"/> by default.
    /// </summary>
    [Parameter]
    public OmniHeadingLevel Level { get; set; } = OmniHeadingLevel.H3;

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }

    /// <summary>Hands the settings down to the rows.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _context.Multiple = Multiple;
        _context.KeepContent = KeepContent;
        _context.Level = Level;
    }

    private Task EnablePageKeysAsync()
    {
        if (_pageKeys)
        {
            return Task.CompletedTask;
        }

        _pageKeys = true;
        return Internal.OmniPageKeys.EnableAsync(JavaScript);
    }
}
