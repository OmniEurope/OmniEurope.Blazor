using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Checkable list of candidate values behind the MultiSelect filter type. The value
/// it reads and writes is the encoded list of <see cref="OmniDataGridFilterValues"/>, so a
/// multi-valued filter travels as the same single string as any other.
/// <see cref="OmniComponentBase.Id"/> goes on the search box when there is one, on the folded summary
/// in the compact presentation, on the list itself otherwise; <see cref="OmniComponentBase.Class"/> and
/// the additional attributes on the outer element.
/// </summary>
public partial class OmniDataGridFilterMultiSelect
{
    private string _search = string.Empty;

    /// <summary>The ticked values, encoded by <see cref="OmniDataGridFilterValues.Join"/>.</summary>
    [Parameter]
    public string Value { get; set; } = string.Empty;

    /// <summary>Raised with the encoded list each time a value is ticked or unticked.</summary>
    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    /// <summary>The candidate values listed.</summary>
    [Parameter]
    public IReadOnlyList<string> Suggestions { get; set; } = [];

    /// <summary>What a candidate reads as (a translated enum name); null shows the value itself.</summary>
    [Parameter]
    public Func<string, string>? FormatValue { get; set; }

    /// <summary>Adds a box that narrows the list as it is typed into, ignoring case and accents.</summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>Placeholder of the search box, and text of the folded summary while nothing is ticked.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Maximum options rendered at once, so a large catalogue stays usable.</summary>
    [Parameter]
    public int MaxSuggestions { get; set; } = 200;

    /// <summary>
    /// <see cref="OmniMultiSelectPresentation.Compact"/>, the default, folds the list behind a
    /// one-line summary so a header filter row keeps the height of one control;
    /// <see cref="OmniMultiSelectPresentation.List"/> shows it open, for a place that is already
    /// on demand such as a filter popover.
    /// </summary>
    [Parameter]
    public OmniMultiSelectPresentation Presentation { get; set; } = OmniMultiSelectPresentation.Compact;

    /// <summary>
    /// Accessible name of the list when nothing else names it: shown open without a search box, it has
    /// neither the box nor the summary to be named by. Null falls back on <see cref="Placeholder"/>.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    private bool NamesItself => !Filterable && Presentation == OmniMultiSelectPresentation.List;

    private string? EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Placeholder : Label;

    /// <summary>The ticked values joined, or the placeholder while none is.</summary>
    private string SummaryText
    {
        get
        {
            var selected = OmniDataGridFilterValues.Split(Value);
            return selected.Count == 0 ? Placeholder ?? string.Empty : string.Join(", ", selected.Select(Display));
        }
    }

    private string SummaryClass => string.IsNullOrEmpty(Value)
        ? "omni-data-grid__multi-text omni-data-grid__multi-text--empty"
        : "omni-data-grid__multi-text";

    private HashSet<string> Selected => [.. OmniDataGridFilterValues.Split(Value)];

    private IReadOnlyList<string> Matches => (string.IsNullOrEmpty(_search)
            ? Suggestions
            : Suggestions.Where(candidate => OmniTextMatch.Contains(Display(candidate), _search)))
        .Take(Math.Max(1, MaxSuggestions))
        .ToArray();

    private string Display(string candidate) => FormatValue?.Invoke(candidate) ?? candidate;

    private void OnSearchInput(ChangeEventArgs args) => _search = args.Value?.ToString() ?? string.Empty;

    private Task ToggleAsync(string candidate, bool selected)
    {
        var values = Selected;
        if (selected)
        {
            values.Add(candidate);
        }
        else
        {
            values.Remove(candidate);
        }

        // Ordered so the persisted configuration of one selection is always written the same way.
        return ValueChanged.InvokeAsync(
            OmniDataGridFilterValues.Join(values.Order(StringComparer.Ordinal)));
    }

}
