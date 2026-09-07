using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

public partial class OmniPager
{
    [Parameter]
    public int Page { get; set; } = 1;

    [Parameter]
    public int PageCount { get; set; } = 1;

    [Parameter]
    public EventCallback<int> PageChanged { get; set; }

    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the first and last page controls next to the previous and next ones.</summary>
    [Parameter]
    public bool ShowFirstLast { get; set; }

    /// <summary>Page sizes the reader can pick from. An empty list hides the selector.</summary>
    [Parameter]
    public IReadOnlyList<int> PageSizeOptions { get; set; } = Array.Empty<int>();

    [Parameter]
    public int PageSize { get; set; } = 20;

    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    [Parameter]
    public string? PageSizeText { get; set; }

    [Parameter]
    public string? FirstPageAriaLabel { get; set; }

    [Parameter]
    public string? FirstPageTitle { get; set; }

    [Parameter]
    public string? LastPageAriaLabel { get; set; }

    [Parameter]
    public string? LastPageTitle { get; set; }

    [Parameter]
    public string? PrevPageAriaLabel { get; set; }

    [Parameter]
    public string? PrevPageTitle { get; set; }

    [Parameter]
    public string? NextPageAriaLabel { get; set; }

    [Parameter]
    public string? NextPageTitle { get; set; }

    /// <summary>Composite format receiving the page number, used on the numbered page buttons.</summary>
    [Parameter]
    public string? PageTitleFormat { get; set; }

    /// <summary>Composite format receiving the page number, used as the accessible name.</summary>
    [Parameter]
    public string? PageAriaLabelFormat { get; set; }

    /// <summary>How many numbered page buttons are rendered around the current page.</summary>
    [Parameter]
    public int NumericPageCount { get; set; }

    [Parameter]
    public OmniJustification HorizontalAlign { get; set; } = OmniJustification.Start;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("PagerLabel")
        : Label;

    private string PagerClass() => Css(
        "omni-pager",
        $"omni-pager--align-{HorizontalAlign.ToString().ToLowerInvariant()}");

    private string Text(string? candidate, string key) => string.IsNullOrWhiteSpace(candidate) ? Localize(key) : candidate;

    private string PageSizeId => $"{Id ?? "omni-pager"}-page-size";

    private bool HasPageSizeOptions => PageSizeOptions.Count > 0;

    private Task SelectAsync(int page) =>
        Disabled || page < 1 || page > PageCount || page == Page ? Task.CompletedTask : PageChanged.InvokeAsync(page);

    private IEnumerable<int> NumericPages
    {
        get
        {
            if (NumericPageCount <= 0)
            {
                yield break;
            }

            var half = NumericPageCount / 2;
            var start = Math.Max(1, Math.Min(Page - half, Math.Max(1, PageCount - NumericPageCount + 1)));
            var end = Math.Min(PageCount, start + NumericPageCount - 1);
            for (var page = start; page <= end; page++)
            {
                yield return page;
            }
        }
    }

    private string PageLabel(string? format, int page) => string.IsNullOrWhiteSpace(format)
        ? page.ToString(System.Globalization.CultureInfo.CurrentCulture)
        : string.Format(System.Globalization.CultureInfo.CurrentCulture, format, page);

    private Task ChangePageSizeAsync(string? value) =>
        int.TryParse(value, out var size) && size > 0 ? PageSizeChanged.InvokeAsync(size) : Task.CompletedTask;
}
