using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Page navigation: previous and next (first and last with <see cref="ShowFirstLast"/>), numbered page
/// buttons or a "page n of m" status, and an optional page size selector. Its texts come from the
/// library resources; a host rewords them through <c>AddOmniEuropeTextOverrides</c>.
/// </summary>
public partial class OmniPager
{
    private readonly string _generatedId = $"omni-pager-{Guid.NewGuid():N}";

    /// <summary>The current page, from 1.</summary>
    [Parameter]
    public int Page { get; set; } = 1;

    /// <summary>The number of pages, at least 1.</summary>
    [Parameter]
    public int PageCount { get; set; } = 1;

    /// <summary>Raised with the page the reader asked for; the host updates <see cref="Page"/>.</summary>
    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    /// <summary>Accessible name of the navigation; null uses the localized "pagination".</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Disables every control of the pager.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the first and last page controls next to the previous and next ones.</summary>
    [Parameter]
    public bool ShowFirstLast { get; set; }

    /// <summary>Page sizes the reader can pick from. An empty list hides the selector.</summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

    /// <summary>The page size selected in the selector.</summary>
    [Parameter]
    public int PageSize { get; set; } = 20;

    /// <summary>Raised with the page size the reader picked.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>How many numbered page buttons are rendered around the current page. Zero shows the "page n of m" status instead.</summary>
    [Parameter]
    public int NumericPageCount { get; set; }

    /// <summary>How the controls are aligned along the pager's row; the start by default.</summary>
    [Parameter]
    public OmniJustification HorizontalAlign { get; set; } = OmniJustification.Start;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("PagerLabel")
        : Label;

    private string PagerClass() => Css(
        "omni-pager",
        $"omni-pager--align-{HorizontalAlign.ToString().ToLowerInvariant()}");

    // Two pagers without an Id (a grid's top and bottom bars) never share the id of their size list.
    private string PageSizeId => $"{Id ?? _generatedId}-page-size";

    private bool HasPageSizeOptions => PageSizeOptions.Count > 0;

    private Task SelectAsync(int page) =>
        Disabled || page < 1 || page > PageCount || page == Page ? Task.CompletedTask : PageChanged.InvokeAsync(page);

    private IEnumerable<int> NumericPages
    {
        get
        {
            // Read only when NumericPageCount is positive: the markup draws no numbers otherwise.
            var half = NumericPageCount / 2;
            var start = Math.Max(1, Math.Min(Page - half, Math.Max(1, PageCount - NumericPageCount + 1)));
            var end = Math.Min(PageCount, start + NumericPageCount - 1);
            for (var page = start; page <= end; page++)
            {
                yield return page;
            }
        }
    }

    private static string PageNumber(int page) => page.ToString(System.Globalization.CultureInfo.CurrentCulture);

    private Task ChangePageSizeAsync(string? value) =>
        int.TryParse(value, out var size) && size > 0 ? PageSizeChanged.InvokeAsync(size) : Task.CompletedTask;
}
