namespace OmniEurope.Blazor.Components;

public partial class OmniRadioButtonList<TValue>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private string GroupName => Name ?? Id ?? $"omni-radio-{FieldIdentifier.FieldName}";
    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private void Select(TValue? value)
    {
        if (!Disabled)
        {
            CurrentValue = value!;
        }
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("RadioButtonListInvalid");
        return false;
    }
}
