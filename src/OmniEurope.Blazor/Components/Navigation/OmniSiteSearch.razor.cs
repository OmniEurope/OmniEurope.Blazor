namespace OmniEurope.Blazor.Components;

/// <summary>
/// A search field that finds a page of the application among a list the host gives and leads to it:
/// typing lists the matches, grouped, ignoring case and accents, on the text and the keywords of each
/// <see cref="OmniSiteSearchDestination"/>; picking one (a click, or Enter on the highlighted one)
/// navigates to its address and empties the field. It follows the editable combobox pattern of
/// <see cref="OmniAutocomplete{TValue}"/>, whose keyboard it keeps (the arrows, Home and End, Enter,
/// Escape). It sizes itself to the bar it sits in (an <see cref="OmniHeader"/>): it grows up to a
/// readable width, then leaves the rest of the row to the controls after it.
/// </summary>
public partial class OmniSiteSearch
{
    private OmniSiteSearchDestination? _picked;
    private bool _noMatch;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// The pages the search can lead to, in the order the groups and the entries of a group are listed.
    /// Each address is checked by the package's address policy.
    /// </summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniSiteSearchDestination>? Destinations { get; set; }

    /// <summary>The accessible name of the field; the localized "Search the site" when null or blank.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The hint shown in the empty field; the localized "Search…" when null or blank.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>The line shown when nothing matches what was typed; the localized "No results" when null or blank.</summary>
    [Parameter]
    public string? NoMatchText { get; set; }

    private bool HasIcons => Destinations?.Any(destination => destination.Icon is not null) == true;

    /// <summary>Checks every address of <see cref="Destinations"/> against the package's address policy.</summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        foreach (var destination in Destinations ?? [])
        {
            OmniUriPolicy.EnsureSafe(destination.Href, nameof(Destinations));
        }
    }

    // Every word typed must be found in the text or in a keyword of the page; the pages found by their
    // text come before those found only by a keyword. The groups keep the order of their first page, and
    // each is listed once, so the list never repeats a heading.
    private async Task<IReadOnlyList<OmniOption<OmniSiteSearchDestination>>> SearchAsync(string text, CancellationToken cancellation)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var found = new List<(OmniSiteSearchDestination Destination, bool ByText)>();
        foreach (var destination in words.Length == 0 ? [] : Destinations ?? [])
        {
            var byText = true;
            var all = true;
            foreach (var word in words)
            {
                var inText = OmniTextMatch.Contains(destination.Text, word);
                if (!inText && destination.Keywords?.Any(keyword => OmniTextMatch.Contains(keyword, word)) != true)
                {
                    all = false;
                    break;
                }

                byText &= inText;
            }

            if (all)
            {
                found.Add((destination, byText));
            }
        }

        var options = found
            .GroupBy(entry => GroupOf(entry.Destination))
            .SelectMany(group => group.OrderBy(entry => entry.ByText ? 0 : 1))
            .Select(entry => new OmniOption<OmniSiteSearchDestination>(entry.Destination, entry.Destination.Text, Group: GroupOf(entry.Destination)))
            .ToArray();

        var noMatch = words.Length > 0 && options.Length == 0;
        if (noMatch != _noMatch)
        {
            _noMatch = noMatch;
            await InvokeAsync(StateHasChanged);
        }

        return options;
    }

    private static string? GroupOf(OmniSiteSearchDestination destination) =>
        string.IsNullOrWhiteSpace(destination.Group) ? null : destination.Group;

    // The field is bound to a value that is always handed back to "nothing": picking is an event here,
    // not a state, and the field empties because the value it shows is replaced.
    private Task PickAsync(OmniSiteSearchDestination? destination)
    {
        _picked = null;
        _noMatch = false;
        if (destination is not null)
        {
            Navigation.NavigateTo(destination.Href);
        }

        return Task.CompletedTask;
    }

    // The list closes by itself on Escape; the "no match" line closes with it.
    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            _noMatch = false;
        }
    }

    // An emptied field has nothing left to match.
    private void HandleInput(ChangeEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Value?.ToString()))
        {
            _noMatch = false;
        }
    }

    private static RenderFragment<OmniOption<OmniSiteSearchDestination>> OptionIcon => option => builder =>
    {
        if (option.Value?.Icon is { } icon)
        {
            builder.OpenComponent<OmniIcon>(0);
            builder.AddComponentParameter(1, nameof(OmniIcon.Name), icon);
            builder.CloseComponent();
        }
    };
}
