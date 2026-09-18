namespace OmniEurope.Blazor.Components;

public partial class OmniTextBox
{
    /// <summary>The input type. Defaults to <see cref="OmniTextBoxType.Text"/>.</summary>
    [Parameter]
    public OmniTextBoxType Type { get; set; } = OmniTextBoxType.Text;

    private string TypeAttribute => Type.ToString().ToLowerInvariant();

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

    /// <summary>
    /// A decorative icon laid over the start of the field, muted and click-through, such as the
    /// magnifier of a search field: <c>&lt;Icon&gt;&lt;OmniIcon Name="OmniIconName.Search" /&gt;&lt;/Icon&gt;</c>.
    /// The field keeps its own accessible name. Null, the default, renders the input alone.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    private void HandleInput(ChangeEventArgs args) => CurrentValueAsString = args.Value?.ToString();

    protected override bool TryParseValueFromString(string? value, out string result, out string validationErrorMessage)
    {
        result = value ?? string.Empty;
        validationErrorMessage = null!;
        return true;
    }
}
