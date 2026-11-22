using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>An autocomplete, a multiple selection, a filterable drop-down and the three pickers inside a dialog.</summary>
public partial class FieldsDialogExample
{
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

    private DateOnly? Date { get; set; }

    private DateTime? Moment { get; set; }

    private TimeOnly? Time { get; set; }
}
