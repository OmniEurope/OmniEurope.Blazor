using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Autocomplete page: the timing, wording and failure settings of a city search, then three more fields.</summary>
public partial class AutocompletePage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private bool ShowToggle { get; set; } = true;

    private double DebounceValue { get; set; } = 250;

    private double MinimumLengthValue { get; set; } = 1;

    private double LatencyValue { get; set; } = 300;

    private string PlaceholderText { get; set; } = string.Empty;

    private bool DisableClosed { get; set; } = true;

    /// <summary>On by default, as in the package: "lie" marks "Lie" in "Liège".</summary>
    private bool HighlightMatches { get; set; } = true;

    private bool FailSearch { get; set; }

    private bool FieldDisabled { get; set; }

    private int DebounceMilliseconds => (int)DebounceValue;

    private int MinimumLength => (int)MinimumLengthValue;

    private int LatencyMilliseconds => (int)LatencyValue;

    private string FormatMilliseconds(double milliseconds) =>
        string.Format(CultureInfo.CurrentCulture, Text["AutocompleteMilliseconds"], milliseconds.ToString("0", CultureInfo.CurrentCulture));

    private string FormatCharacters(double count) =>
        string.Format(CultureInfo.CurrentCulture, Text["AutocompleteCharacters"], count.ToString("0", CultureInfo.CurrentCulture));
}
