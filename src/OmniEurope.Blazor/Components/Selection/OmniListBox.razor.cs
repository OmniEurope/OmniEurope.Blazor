namespace OmniEurope.Blazor.Components;

public partial class OmniListBox<TValue>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public int VisibleRows { get; set; } = 5;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private void HandleChange(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var index) && index >= 0 && index < Options.Count && !Options[index].Disabled)
        {
            CurrentValue = Options[index].Value;
        }
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (int.TryParse(value, out var index) && index >= 0 && index < Options.Count)
        {
            result = Options[index].Value;
            validationErrorMessage = null!;
            return true;
        }

        result = default!;
        validationErrorMessage = Localize("ListBoxInvalid");
        return false;
    }
}
