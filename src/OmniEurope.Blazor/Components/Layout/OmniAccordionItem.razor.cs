namespace OmniEurope.Blazor.Components;

/// <summary>
/// One row of an <see cref="OmniAccordion"/>: a header whose toggle button opens and closes a body
/// revealed below it. The header holds a label (<see cref="Title"/> or <see cref="HeaderContent"/>),
/// an optional help mark, controls of its own, a value or badge and a chevron the component draws.
/// Used alone, outside an accordion, it is a disclosure that opens and closes by itself.
/// </summary>
public partial class OmniAccordionItem
{
    private readonly string _generatedId = $"omni-accordion-item-{Guid.NewGuid():N}";
    private bool _open;
    private bool _visited;
    private bool? _expandedParameter;

    [CascadingParameter]
    private OmniAccordionContext? Context { get; set; }

    /// <summary>
    /// The text of the header, the accessible name of the toggle. Replaced by
    /// <see cref="HeaderContent"/> when both are set.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    /// The label area of the header, inside the toggle button: the content may wrap and may hold a
    /// second line, but it has to stay non-interactive (text, an icon, a count), since a button cannot
    /// contain a control. A help mark, a link or a button belongs in <see cref="Help"/> or
    /// <see cref="ActionsContent"/>, which are drawn beside the toggle. It becomes the accessible name
    /// of the toggle and of the body region.
    /// </summary>
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    /// <summary>
    /// A text for the help mark ("?") drawn beside the label: an <see cref="OmniTooltip"/> that opens
    /// on hover and on focus, reached by Tab. Using it never toggles the row. Null or blank, the
    /// default, draws no mark.
    /// </summary>
    [Parameter]
    public string? Help { get; set; }

    /// <summary>
    /// Controls drawn beside the toggle, after the help mark (a link, a menu, a custom help mark): free
    /// content, interactive if need be. A click or a key on one never toggles the row.
    /// </summary>
    [Parameter]
    public RenderFragment? ActionsContent { get; set; }

    /// <summary>
    /// A value or a badge shown at the end of the header, before the chevron. It is not a control: a
    /// click on it toggles the row, and a screen reader reads it as the description of the toggle. It
    /// must be non-interactive.
    /// </summary>
    [Parameter]
    public RenderFragment? SummaryContent { get; set; }

    /// <summary>The body, revealed below the header while the row is open.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether the row is open. It sets the state the row starts in, and every time the host changes
    /// it the row follows; between those changes the reader's own clicks rule, reported through
    /// <see cref="ExpandedChanged"/> for <c>@bind-Expanded</c>. In an accordion that is not
    /// <see cref="OmniAccordion.Multiple"/>, the reader opening this row closes its peers.
    /// </summary>
    [Parameter]
    public bool Expanded { get; set; }

    /// <summary>
    /// Raised with the new state when the reader opens or closes the row, and with <c>false</c> when
    /// the row is closed because a peer opened (single mode). A change the host makes through
    /// <see cref="Expanded"/> is not reported back.
    /// </summary>
    [Parameter]
    public EventCallback<bool> ExpandedChanged { get; set; }

    internal bool IsOpen => _open;

    private string ToggleId => Id ?? _generatedId;
    private string BodyId => $"{ToggleId}-body";
    private string SummaryId => $"{ToggleId}-summary";
    private int HeadingAriaLevel => (int)(Context?.Level ?? OmniHeadingLevel.H3);
    private bool RendersBody => _open || (Context?.KeepContent == true && _visited);
    private RenderFragment? DefaultTitle => string.IsNullOrEmpty(Title) ? null : builder => builder.AddContent(0, Title);

    /// <summary>Joins the accordion, if any, so a peer opening can close this row.</summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Context?.Register(this);
    }

    /// <summary>Follows the host's <see cref="Expanded"/> whenever it changes.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (_expandedParameter != Expanded)
        {
            _expandedParameter = Expanded;
            _open = Expanded;
        }

        _visited |= _open;
    }

    private async Task ToggleAsync()
    {
        _open = !_open;
        _visited |= _open;
        if (_open && Context is not null)
        {
            await Context.ClosePeersAsync(this);
        }

        await ExpandedChanged.InvokeAsync(_open);
    }

    internal async Task CloseFromPeerAsync()
    {
        _open = false;
        await InvokeAsync(StateHasChanged);
        await ExpandedChanged.InvokeAsync(false);
    }

    /// <summary>Leaves the accordion's registry.</summary>
    public void Dispose()
    {
        Context?.Unregister(this);
        GC.SuppressFinalize(this);
    }
}
