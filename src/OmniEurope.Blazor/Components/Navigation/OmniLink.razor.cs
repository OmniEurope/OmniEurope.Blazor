namespace OmniEurope.Blazor.Components;

/// <summary>
/// A link of the package's look, its address checked by the URI policy. With <see cref="NewTab"/> it
/// opens in a new tab, says so to assistive technology and draws the external mark; a link that opens
/// a new tab always carries <c>rel="noopener noreferrer"</c>, merged with a <c>rel</c> the host passes.
/// </summary>
public partial class OmniLink
{
    private static readonly string[] ExternalRelTokens = ["noopener", "noreferrer"];

    /// <summary>
    /// The address, checked by the package's URI policy (an unsafe scheme throws). Null or blank, the
    /// default, makes the component an action rather than a link: a <c>button type="button"</c> of the same
    /// look (same classes), keyboard-accessible, that only runs <see cref="OnClick"/>. <see cref="NewTab"/>
    /// has no effect on it.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Keeps the click and the key press on the link from bubbling to an ancestor's handler, so that a link
    /// (or action) in a data grid cell does not also raise the grid's <c>OnRowClick</c> or its row key
    /// handling. False, the default, changes nothing. It is opt-in, not automatic, because the component
    /// cannot know it sits in a grid cell and an ancestor's handler is otherwise wanted (a delegated click).
    /// </summary>
    [Parameter]
    public bool StopPropagation { get; set; }

    /// <summary>The text of the link.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Opens the link in a new tab, announced as such and marked with the external icon.</summary>
    [Parameter]
    public bool NewTab { get; set; }

    /// <summary>The accessible name of the link, when its text alone is not enough. Null, the default, leaves the link named by its text.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Runs when the link is clicked, besides following <see cref="Href"/>: an action that goes with
    /// the navigation (recording a choice, closing a panel). The navigation itself is unchanged.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    private bool IsAction => string.IsNullOrWhiteSpace(Href);

    private bool ShowsExternalIcon => NewTab;

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));

    private string? HostAttribute(string name) =>
        AdditionalAttributes is not null && AdditionalAttributes.TryGetValue(name, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;

    private string? EffectiveTarget => NewTab ? "_blank" : HostAttribute("target");

    /// <summary>
    /// The host's <c>rel</c>, with <c>noopener noreferrer</c> added whenever the link opens another
    /// browsing context (<see cref="NewTab"/>, or a <c>target</c> the host passes other than <c>_self</c>):
    /// a host token such as <c>nofollow</c> is kept, the protection is never dropped.
    /// </summary>
    private string? EffectiveRel
    {
        get
        {
            var host = HostAttribute("rel");
            var target = EffectiveTarget;
            var opensElsewhere = !string.IsNullOrWhiteSpace(target)
                && !string.Equals(target, "_self", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(target, "_parent", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(target, "_top", StringComparison.OrdinalIgnoreCase);
            if (!opensElsewhere)
            {
                return string.IsNullOrWhiteSpace(host) ? null : host;
            }

            var tokens = (host ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            foreach (var token in ExternalRelTokens)
            {
                if (!tokens.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    tokens.Add(token);
                }
            }

            return string.Join(' ', tokens);
        }
    }
}
