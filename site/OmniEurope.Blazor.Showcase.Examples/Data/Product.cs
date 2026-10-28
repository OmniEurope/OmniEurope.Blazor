using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>Where the products of the list come from.</summary>
public enum DataListSource
{
    /// <summary>The page hands the list the products it shows.</summary>
    Items,

    /// <summary>The list loads its products itself, as from a service.</summary>
    Load
}

/// <summary>The kind of a product.</summary>
public enum ProductCategory
{
    /// <summary>Lamps and lights.</summary>
    Lighting,

    /// <summary>Speakers and headphones.</summary>
    Audio,

    /// <summary>Tables and chairs.</summary>
    Furniture,

    /// <summary>Bags and suitcases.</summary>
    Travel
}

/// <summary>A product of the catalogue the list shows.</summary>
/// <param name="Id">The stable key of the product.</param>
/// <param name="Name">The product name.</param>
/// <param name="Category">The kind of product.</param>
/// <param name="Price">The price.</param>
/// <param name="Stock">The units in stock.</param>
public sealed record Product(int Id, string Name, ProductCategory Category, decimal Price, int Stock);

/// <summary>The sample catalogue of the data list examples.</summary>
public static class ProductData
{
    private const int ProductCount = 60;

    // Fictional range names: data, not interface texts.
    private static readonly string[] Names =
        ["Aurore", "Boréal", "Cassiopée", "Delta", "Éclipse", "Fjord", "Galène", "Horizon", "Iris", "Jade"];

    private static readonly string[] Models = ["S", "M", "L", "Pro", "Max", "Mini"];

    /// <summary>No product, for the empty states.</summary>
    public static IReadOnlyList<Product> None { get; } = [];

    /// <summary>The catalogue, drawn from a fixed seed so the example is the same at every visit.</summary>
    public static IReadOnlyList<Product> Create()
    {
        var random = new Random(60);
        var categories = Enum.GetValues<ProductCategory>();
        var products = new List<Product>(ProductCount);
        for (var index = 0; index < ProductCount; index++)
        {
            var name = string.Create(
                CultureInfo.InvariantCulture,
                $"{Names[index % Names.Length]} {Models[index / Names.Length % Models.Length]}");
            var stock = random.Next(10) == 0 ? 0 : random.Next(1, 40);
            products.Add(new Product(index + 1, name, categories[random.Next(categories.Length)], 19 + (random.Next(80) * 5), stock));
        }

        return products;
    }
}
