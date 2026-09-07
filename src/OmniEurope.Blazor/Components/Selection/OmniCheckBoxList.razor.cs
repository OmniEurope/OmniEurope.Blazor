namespace OmniEurope.Blazor.Components;

public partial class OmniCheckBoxList<TValue>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool IsSelected(TValue value) => CurrentValue?.Contains(value, EqualityComparer<TValue>.Default) == true;

    private void Toggle(OmniOption<TValue> option, ChangeEventArgs args)
    {
        if (Disabled || option.Disabled || args.Value is not bool selected)
        {
            return;
        }

        var values = (CurrentValue ?? Array.Empty<TValue>()).ToList();
        values.RemoveAll(value => EqualityComparer<TValue>.Default.Equals(value, option.Value));
        if (selected)
        {
            values.Add(option.Value);
        }

        CurrentValue = values;
    }

    protected override bool TryParseValueFromString(string? value, out IReadOnlyList<TValue> result, out string validationErrorMessage)
    {
        result = Array.Empty<TValue>();
        validationErrorMessage = Localize("CheckBoxListInvalid");
        return false;
    }
}
