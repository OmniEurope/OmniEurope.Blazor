using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A city autocomplete whose timing, wording, highlighting, closed cities, failure and state are parameters.</summary>
public partial class AutocompleteExample
{
    /// <summary>Whether a chevron at the end of the field opens the whole list.</summary>
    [Parameter]
    public bool ShowToggle { get; set; } = true;

    /// <summary>The pause after the last keystroke before the search starts, in milliseconds.</summary>
    [Parameter]
    public int DebounceMilliseconds { get; set; } = 250;

    /// <summary>The number of characters to type before anything is searched.</summary>
    [Parameter]
    public int MinimumLength { get; set; } = 1;

    /// <summary>The time the fake service takes to answer, in milliseconds.</summary>
    [Parameter]
    public int LatencyMilliseconds { get; set; } = 300;

    /// <summary>The text shown in the empty field.</summary>
    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Whether the typed letters are marked in each suggestion.</summary>
    [Parameter]
    public bool HighlightMatches { get; set; } = true;

    /// <summary>Whether the closed cities are listed but cannot be picked.</summary>
    [Parameter]
    public bool DisableClosed { get; set; } = true;

    /// <summary>Whether the search throws, so the field shows its error message.</summary>
    [Parameter]
    public bool FailSearch { get; set; }

    /// <summary>Whether the field is greyed out and ignores input.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private string? City { get; set; }

    private int FailureCount { get; set; }

    private Task<IReadOnlyList<OmniOption<string>>> SearchAsync(string text, CancellationToken cancellationToken) =>
        AutocompleteCities.SearchAsync(text, LatencyMilliseconds, DisableClosed, FailSearch, cancellationToken);

    private void OnSearchFailed(Exception exception) => FailureCount++;
}
