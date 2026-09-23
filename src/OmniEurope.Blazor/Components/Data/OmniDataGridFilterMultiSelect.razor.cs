using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Checkable list of candidate values behind the MultiSelect filter type. The value
/// it reads and writes is the encoded list of <see cref="OmniDataGridFilterValues"/>, so a
/// multi-valued filter travels as the same single string as any other.
/// </summary>
public partial class OmniDataGridFilterMultiSelect
{
    private string _search = string.Empty;

    [Parameter]
    public string Value { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> ValueChanged { get; set; }

    [Parameter]
    public IReadOnlyList<string> Suggestions { get; set; } = [];

    /// <summary>What a candidate reads as (a translated enum name); null shows the value itself.</summary>
    [Parameter]
    public Func<string, string>? TextFor { get; set; }

    /// <summary>Adds a box that narrows the list as it is typed into.</summary>
    [Parameter]
    public bool Searchable { get; set; }

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
            : Suggestions.Where(candidate => Display(candidate).Contains(_search, StringComparison.OrdinalIgnoreCase)))
        .Take(Math.Max(1, MaxSuggestions))
        .ToArray();

    private string Display(string candidate) => TextFor?.Invoke(candidate) ?? candidate;

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

    /// <summary>
    /// Renders an option with the searched fragment wrapped in a mark element, built by hand so the
    /// candidate is never treated as markup.
    /// </summary>
    private RenderFragment Highlighted(string value) => builder =>
    {
        var candidate = Display(value);
        var index = string.IsNullOrEmpty(_search)
            ? -1
            : candidate.IndexOf(_search, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            builder.AddContent(0, candidate);
            return;
        }

        builder.AddContent(1, candidate[..index]);
        builder.OpenElement(2, "mark");
        builder.AddAttribute(3, "class", "omni-combo__match");
        builder.AddContent(4, candidate.Substring(index, _search.Length));
        builder.CloseElement();
        builder.AddContent(5, candidate[(index + _search.Length)..]);
    };
}
