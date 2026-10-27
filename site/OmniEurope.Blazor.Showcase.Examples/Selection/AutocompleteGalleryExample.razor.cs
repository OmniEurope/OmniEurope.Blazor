using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Autocomplete fields with a value set in advance, a strict trigger, and disabled.</summary>
public partial class AutocompleteGalleryExample
{
    /// <summary>Whether a chevron at the end of each field opens the whole list.</summary>
    [Parameter]
    public bool ShowToggle { get; set; } = true;

    /// <summary>The time the fake service takes to answer, in milliseconds.</summary>
    [Parameter]
    public int LatencyMilliseconds { get; set; } = 300;

    /// <summary>Whether the closed cities are listed but cannot be picked.</summary>
    [Parameter]
    public bool DisableClosed { get; set; } = true;

    /// <summary>Whether the search throws, so the fields show their error message.</summary>
    [Parameter]
    public bool FailSearch { get; set; }

    private string? PresetCity { get; set; } = "lux";

    private string? StrictCity { get; set; }

    private string FrozenCity { get; set; } = "vie";

    private Task<IReadOnlyList<OmniOption<string>>> SearchAsync(string text, CancellationToken cancellationToken) =>
        AutocompleteCities.SearchAsync(text, LatencyMilliseconds, DisableClosed, FailSearch, cancellationToken);
}
