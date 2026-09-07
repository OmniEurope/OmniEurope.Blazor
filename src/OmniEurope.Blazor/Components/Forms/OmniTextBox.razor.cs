namespace OmniEurope.Blazor.Components;

public partial class OmniTextBox
{
    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public string? Autocomplete { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
