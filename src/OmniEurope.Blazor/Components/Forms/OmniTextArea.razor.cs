namespace OmniEurope.Blazor.Components;

public partial class OmniTextArea
{
    [Parameter]
    public int Rows { get; set; } = 4;

    [Parameter]
    public int? MaxLength { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool ShowCount { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private int CurrentLength => CurrentValueAsString?.Length ?? 0;
    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (Rows < 1 || MaxLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Rows), "Rows and MaxLength must be positive when provided.");
        }
    }
}
