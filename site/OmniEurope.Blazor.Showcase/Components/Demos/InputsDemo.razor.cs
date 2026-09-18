using System.Globalization;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class InputsDemo
{
    private static readonly DateOnly Floor = new(2020, 1, 1);

    private static readonly DateOnly Ceiling = new(2035, 12, 31);

    private static readonly DateTime AppointmentFloor = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private static readonly DateTime AppointmentCeiling = new(2035, 12, 31, 23, 59, 0, DateTimeKind.Unspecified);

    private static readonly string[] Cities =
    [
        "Bruxelles", "Bruges", "Brasschaat", "Paris", "Pau", "Perpignan",
        "Luxembourg", "Liège", "Louvain", "Amsterdam", "Anvers", "Arlon"
    ];

    private InputsDemoModel Model { get; } = new();

    /// <summary>The three bound values, read back from the model so a choice is visibly applied.</summary>
    private string PickedSummary => string.Create(
        CultureInfo.CurrentCulture,
        $"Date : {Model.Date?.ToString("d", CultureInfo.CurrentCulture) ?? "aucune"} ; heure : {Model.Start?.ToString("HH:mm", CultureInfo.InvariantCulture) ?? "aucune"} ; rendez-vous : {Model.Appointment?.ToString("g", CultureInfo.CurrentCulture) ?? "aucun"}");

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
