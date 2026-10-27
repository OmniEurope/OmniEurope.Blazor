using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>An autocomplete over a short list of model names that also accepts a name outside the list.</summary>
public partial class AutocompleteFreeTextExample
{
    private static readonly string[] Models = ["model-small", "model-medium", "model-large", "model-vision"];

    private string? Model { get; set; }

    /// <summary>Stands in for a remote lookup: the component only asks for matches, it does not care where they come from.</summary>
    private static Task<IReadOnlyList<OmniOption<string>>> SearchModelsAsync(string term, CancellationToken cancellationToken)
    {
        IReadOnlyList<OmniOption<string>> matches =
        [
            .. Models
                .Where(model => model.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(model => new OmniOption<string>(model, model))
        ];

        return Task.FromResult(matches);
    }
}
