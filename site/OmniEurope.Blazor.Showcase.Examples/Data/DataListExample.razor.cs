using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A paged or virtualized product list fed by items or by a loader, whose source, delay and failure are parameters.</summary>
public partial class DataListExample
{
    private const int LowStock = 5;

    private static readonly IReadOnlyList<int> PagerPageSizes = [3, 6, 12, 24];

    private readonly IReadOnlyList<Product> _products = ProductData.Create();

    private (int Page, int Size, bool Empty, bool Virtualize, bool Fail, double Latency) _loaderKey;
    private Func<CancellationToken, Task<IReadOnlyList<Product>>>? _loader;
    private int _seenPageSize;
    private bool _seenEmpty;
    private int _pageSize = 6;

    /// <summary>Whether the page hands the products to the list or the list loads them.</summary>
    [Parameter]
    public DataListSource Source { get; set; } = DataListSource.Items;

    /// <summary>The delay of the loader, in milliseconds.</summary>
    [Parameter]
    public double Latency { get; set; } = 750;

    /// <summary>Makes the loader fail, to show the error state and its retry button.</summary>
    [Parameter]
    public bool FailLoad { get; set; }

    /// <summary>Empties the catalogue, to show the empty state.</summary>
    [Parameter]
    public bool EmptyData { get; set; }

    /// <summary>Renders only the products in view and scrolls over all of them.</summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>The products per page.</summary>
    [Parameter]
    public int PageSize { get; set; } = 6;

    /// <summary>Raised when the visitor picks another page size in the pager.</summary>
    [Parameter]
    public EventCallback<int> PageSizeChanged { get; set; }

    /// <summary>How many numbered page buttons the pager shows; 0 hides them.</summary>
    [Parameter]
    public int NumericPageCount { get; set; } = 5;

    /// <summary>How the parts of the pager are spread along its row.</summary>
    [Parameter]
    public OmniJustification PagerJustify { get; set; } = OmniJustification.Center;

    /// <summary>Whether the pager has first and last page buttons.</summary>
    [Parameter]
    public bool ShowFirstLast { get; set; } = true;

    /// <summary>Whether the pager offers a choice of page size.</summary>
    [Parameter]
    public bool ShowPageSizeOptions { get; set; } = true;

    /// <summary>Greys the pager out.</summary>
    [Parameter]
    public bool PagerDisabled { get; set; }

    private int CurrentPage { get; set; } = 1;

    private IReadOnlyList<Product> AllProducts => EmptyData ? ProductData.None : _products;

    private int PageCount => Math.Max(1, (int)Math.Ceiling(AllProducts.Count / (double)_pageSize));

    private IReadOnlyList<int> PageSizeChoices => ShowPageSizeOptions ? PagerPageSizes : Array.Empty<int>();

    /// <summary>The page shown, or the whole catalogue when the list virtualizes.</summary>
    private IReadOnlyList<Product> VisibleProducts => Virtualize
        ? AllProducts
        : AllProducts.Skip((CurrentPage - 1) * _pageSize).Take(_pageSize).ToArray();

    private IReadOnlyList<Product> ListItems => Source == DataListSource.Items ? VisibleProducts : ProductData.None;

    /// <summary>
    /// The list reloads only when the delegate changes reference: it is rebuilt at every change of page,
    /// size or setting, and kept as it is between two renders.
    /// </summary>
    private Func<CancellationToken, Task<IReadOnlyList<Product>>>? ListLoader
    {
        get
        {
            if (Source != DataListSource.Load)
            {
                return null;
            }

            var key = (CurrentPage, _pageSize, EmptyData, Virtualize, FailLoad, Latency);
            if (_loader is null || key != _loaderKey)
            {
                _loaderKey = key;
                var items = VisibleProducts;
                var fail = FailLoad;
                var latency = TimeSpan.FromMilliseconds(Latency);
                _loader = cancellationToken => LoadProductsAsync(items, fail, latency, cancellationToken);
            }

            return _loader;
        }
    }

    private string StateEcho => Virtualize
        ? $"All {AllProducts.Count} products, rendered by window."
        : $"Page {CurrentPage} of {PageCount}, {AllProducts.Count} products.";

    protected override void OnParametersSet()
    {
        // Emptying or refilling the catalogue brings the list back to its first page.
        if (EmptyData != _seenEmpty)
        {
            _seenEmpty = EmptyData;
            CurrentPage = 1;
        }

        // A new page size from the settings brings the list back to its first page: the old one may no longer exist.
        if (PageSize != _seenPageSize)
        {
            _seenPageSize = PageSize;
            _pageSize = PageSize;
            CurrentPage = 1;
        }
    }

    private async Task ChangePageSize(int size)
    {
        _pageSize = size;
        _seenPageSize = size;
        CurrentPage = 1;
        await PageSizeChanged.InvokeAsync(size);
    }

    private static string CategoryLabel(ProductCategory category) => category switch
    {
        ProductCategory.Lighting => "Lighting",
        ProductCategory.Audio => "Audio",
        ProductCategory.Furniture => "Furniture",
        _ => "Travel"
    };

    private static string StockLabel(int stock) => stock switch
    {
        0 => "Out of stock",
        <= LowStock => $"Low stock ({stock})",
        _ => $"In stock ({stock})"
    };

    private static OmniTone StockTone(int stock) => stock switch
    {
        0 => OmniTone.Danger,
        <= LowStock => OmniTone.Warning,
        _ => OmniTone.Success
    };

    /// <summary>A slow or failing service, simulated: the delay can be cancelled, the failure throws.</summary>
    private static async Task<IReadOnlyList<Product>> LoadProductsAsync(
        IReadOnlyList<Product> items, bool fail, TimeSpan latency, CancellationToken cancellationToken)
    {
        if (latency > TimeSpan.Zero)
        {
            await Task.Delay(latency, cancellationToken);
        }

        if (fail)
        {
            throw new InvalidOperationException("Simulated load failure.");
        }

        return items;
    }
}
