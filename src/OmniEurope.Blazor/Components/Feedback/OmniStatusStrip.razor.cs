namespace OmniEurope.Blazor.Components;

/// <summary>
/// A row of statuses: the last runs of a job as coloured points, the slices of an availability window
/// as a bar of segments, a pulsing point for what is still going. Each item carries its own name and
/// tooltip and may lead somewhere.
/// </summary>
/// <remarks>
/// The statuses are the host's own words (<c>success</c>, <c>failed</c>, <c>up</c>...): the strip
/// knows none of them and paints each with the tone <see cref="Tones"/> gives it, so the same strip
/// serves any vocabulary. Items are drawn in the order given; a host showing its latest runs last
/// reverses them itself.
/// </remarks>
public partial class OmniStatusStrip
{
    private static readonly IReadOnlyDictionary<string, OmniBadgeVariant> NoTones =
        new Dictionary<string, OmniBadgeVariant>(StringComparer.Ordinal);

    /// <summary>The items, in the order they are drawn.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniStatusStripItem> Items { get; set; } = [];

    /// <summary>
    /// The tone of each status, by status key, with the comparer the host built the dictionary with.
    /// A status missing from it is drawn <see cref="OmniBadgeVariant.Neutral"/>.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, OmniBadgeVariant> Tones { get; set; } = NoTones;

    /// <summary>
    /// The statuses drawn pulsing, the ones still in progress. The pulse stops for a reader who asks
    /// the system for reduced motion.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<string> Pulsing { get; set; } = [];

    /// <summary>Points side by side, or segments sharing the width.</summary>
    [Parameter]
    public OmniStatusStripShape Shape { get; set; } = OmniStatusStripShape.Dot;

    /// <summary>Accessible name of the strip; the localized "status history" when empty.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>What is written when there is no item; a localized default when empty.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>
    /// Raised when an item without <see cref="OmniStatusStripItem.Href"/> is activated. With a handler,
    /// such items are buttons; without one, they are plain marks.
    /// </summary>
    [Parameter]
    public EventCallback<OmniStatusStripItem> OnItemClick { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("StatusStripLabel") : Label;

    private string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localize("StatusStripEmpty") : EmptyText;

    private string ItemCss(OmniStatusStripItem item) => CssClassBuilder.Combine(
    [
        "omni-status-strip__item",
        $"omni-status-strip__item--{ToneOf(item).ToString().ToLowerInvariant()}",
        Pulsing.Contains(item.Status) ? "omni-status-strip__item--pulse" : null
    ]);

    private OmniBadgeVariant ToneOf(OmniStatusStripItem item) =>
        Tones.TryGetValue(item.Status, out var tone) ? tone : OmniBadgeVariant.Neutral;

    private static string? SafeHref(OmniStatusStripItem item) =>
        string.IsNullOrWhiteSpace(item.Href) ? null : OmniUriPolicy.EnsureSafe(item.Href, nameof(OmniStatusStripItem.Href));
}
