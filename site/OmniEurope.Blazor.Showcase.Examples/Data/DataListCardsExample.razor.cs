using System.Globalization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A paged list of cards and the Markdown export of every file behind it.</summary>
public partial class DataListCardsExample
{
    private static readonly IReadOnlyList<int> Sizes = [2, 4, 8];

    private readonly IReadOnlyList<CaseRow> _rows = [.. CaseData.Rows.Take(6)];

    private int PageNumber { get; set; } = 1;

    private int PageSize { get; set; } = 2;

    private int PageCount => (int)Math.Ceiling(_rows.Count / (double)PageSize);

    private IReadOnlyList<CaseRow> Page => [.. _rows.Skip((PageNumber - 1) * PageSize).Take(PageSize)];

    private void ChangePageSize(int size)
    {
        PageSize = size;
        PageNumber = Math.Clamp(PageNumber, 1, PageCount);
    }

    /// <summary>The export of the card list: every file, read page by page as a remote source would serve it.</summary>
    private OmniTableExport<CaseRow> CreateExport() => new()
    {
        Title = "Open cases",
        Columns =
        [
            new("Reference", row => row.Reference),
            new("Applicant", row => row.Applicant),
            new("Country", row => row.Country),
            new("Amount", row => row.Amount.ToString("C0", CultureInfo.CurrentCulture))
        ],
        Fields = [new("Sort", "reference, ascending")],
        PageSize = 3,
        LoadPage = request => Task.FromResult(new OmniDataGridResult<CaseRow>(
            [.. _rows.OrderBy(row => row.Reference, StringComparer.Ordinal).Skip(request.Skip).Take(request.PageSize)], _rows.Count))
    };
}
