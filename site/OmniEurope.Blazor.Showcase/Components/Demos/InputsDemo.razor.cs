namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class InputsDemo
{
    private static readonly DateOnly Floor = new(2020, 1, 1);

    private static readonly DateOnly Ceiling = new(2035, 12, 31);

    private static readonly string[] Cities =
    [
        "Bruxelles", "Bruges", "Brasschaat", "Paris", "Pau", "Perpignan",
        "Luxembourg", "Liège", "Louvain", "Amsterdam", "Anvers", "Arlon"
    ];

    private InputsDemoModel Model { get; } = new();

    /// <summary>
    /// Stands in for a remote lookup: the component only asks for matches, it does not care where
    /// they come from.
    /// </summary>
    private static Task<IReadOnlyList<OmniOption<string>>> SearchCitiesAsync(string term, CancellationToken cancellationToken)
    {
        IReadOnlyList<OmniOption<string>> matches =
        [
            .. Cities
                .Where(city => city.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(city => new OmniOption<string>(city, city))
        ];

        return Task.FromResult(matches);
    }
}
