namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Empty and plain lists next to a pager without numbers, a full pager and a disabled one.</summary>
public partial class DataListGalleryExample
{
    private readonly IReadOnlyList<Product> _products = ProductData.Create();

    private IReadOnlyList<Product> TopProducts => [.. _products.Take(4)];

    private int StatusPage { get; set; } = 4;

    private int FullPage { get; set; } = 10;
}
