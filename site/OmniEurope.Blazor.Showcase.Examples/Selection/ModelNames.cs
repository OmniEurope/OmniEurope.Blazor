using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>The short list of model names the autocomplete examples search, and the lookup they search it with.</summary>
public static class ModelNames
{
    /// <summary>The model names, in display order.</summary>
    public static IReadOnlyList<string> All { get; } = ["model-small", "model-medium", "model-large", "model-vision"];

    /// <summary>Stands in for a remote lookup: the component only asks for matches, it does not care where they come from.</summary>
    /// <param name="term">The text typed so far.</param>
    /// <param name="cancellationToken">Cancels the lookup when a newer term replaces it.</param>
    /// <returns>The names that contain the term, case-insensitively.</returns>
    public static Task<IReadOnlyList<OmniOption<string>>> SearchAsync(string term, CancellationToken cancellationToken)
    {
        IReadOnlyList<OmniOption<string>> matches =
        [
            .. All
                .Where(model => model.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(model => new OmniOption<string>(model, model))
        ];

        return Task.FromResult(matches);
    }
}
