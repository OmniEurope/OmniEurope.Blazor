using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>An autocomplete, a multiple selection and a filterable drop-down inside a dialog.</summary>
public partial class FieldsDialogExample
{
    private static readonly string[] Models = ["model-small", "model-medium", "model-large", "model-vision"];

    private static readonly IReadOnlyList<OmniOption<string>> CountryOptions =
    [
        new("de", "Germany"), new("at", "Austria"), new("be", "Belgium"), new("es", "Spain"),
        new("fr", "France"), new("it", "Italy"), new("nl", "Netherlands"), new("pt", "Portugal")
    ];

    private static readonly IReadOnlyList<OmniOption<string>> SizeOptions =
    [
        new("s", "Small"), new("m", "Medium"), new("l", "Large"), new("xl", "Extra large")
    ];

    private bool Open { get; set; }

    private string? Model { get; set; }

    private IReadOnlyList<string> Countries { get; set; } = [];

    private string? Size { get; set; }

    private string? CountrySearch { get; set; }

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
