namespace OmniEurope.Blazor.Components;

public partial class OmniMultiSelect<TValue>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public int VisibleRows { get; set; } = 5;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool IsSelected(TValue value) =>
        CurrentValue?.Contains(value, EqualityComparer<TValue>.Default) == true;

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
