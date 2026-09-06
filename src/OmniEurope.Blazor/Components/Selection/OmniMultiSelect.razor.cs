namespace OmniEurope.Blazor.Components;

public partial class OmniMultiSelect<TValue>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public int VisibleRows { get; set; } = 5;

    /// <summary>
    /// Compact keeps the control on a single line and opens its list on demand, which is what a
    /// filter sitting in a toolbar needs. List, the default, stays an always-open native list.
    /// </summary>
    [Parameter]
    public OmniMultiSelectPresentation Presentation { get; set; } = OmniMultiSelectPresentation.List;

    /// <summary>Shown by the compact presentation while nothing is selected.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private int SelectedCount => CurrentValue?.Count ?? 0;

    /// <summary>
    /// One selection is named, several are counted: a control on a single line cannot grow with the
    /// number of entries a filter holds.
    /// </summary>
    private string SummaryText
    {
        get
        {
            if (SelectedCount == 0)
            {
                return string.IsNullOrWhiteSpace(Placeholder) ? Localize("MultiSelectEmpty") : Placeholder;
            }

            if (SelectedCount == 1)
            {
                var only = CurrentValue![0];
                var match = Options.FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, only));
                if (match is not null)
                {
                    return match.Text;
                }
            }

            return Localize("MultiSelectSelected", SelectedCount);
        }
    }

    private bool IsSelected(TValue value) =>
        CurrentValue?.Contains(value, EqualityComparer<TValue>.Default) == true;

    private void Toggle(OmniOption<TValue> option, bool selected)
    {
        if (selected == IsSelected(option.Value))
        {
            return;
        }

        var current = CurrentValue ?? [];
        CurrentValue = selected
            ? [.. current, option.Value]
            : [.. current.Where(value => !EqualityComparer<TValue>.Default.Equals(value, option.Value))];
    }

    private void Clear() => CurrentValue = [];

    private void HandleChange(ChangeEventArgs args)
    {
        var keys = args.Value switch
        {
            string[] values => values,
            IEnumerable<string> values => values.ToArray(),
            string value => [value],
            _ => Array.Empty<string>()
        };

        CurrentValue = keys
            .Select(key => int.TryParse(key, out var index) ? index : -1)
            .Where(index => index >= 0 && index < Options.Count && !Options[index].Disabled)
            .Select(index => Options[index].Value)
            .ToArray();
    }

    protected override bool TryParseValueFromString(string? value, out IReadOnlyList<TValue> result, out string validationErrorMessage)
    {
        result = Array.Empty<TValue>();
        validationErrorMessage = Localize("MultiSelectInvalid");
        return false;
    }
}
