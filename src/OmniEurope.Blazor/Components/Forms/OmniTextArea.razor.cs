namespace OmniEurope.Blazor.Components;

/// <summary>
/// A multi-line text field, with an optional character count. <see cref="OmniInputBase{TValue}.Class"/>
/// goes on the wrapper that holds the field and its count; <see cref="OmniInputBase{TValue}.Id"/> and the
/// additional attributes on the text area.
/// </summary>
public partial class OmniTextArea
{
    /// <summary>The visible number of lines; 4 by default, at least 1.</summary>
    [Parameter]
    public int Rows { get; set; } = 4;

    /// <summary>The longest text accepted (<c>maxlength</c>); null, the default, for no limit.</summary>
    [Parameter]
    public int? MaxLength { get; set; }

    /// <summary>A hint shown while the field is empty.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the value without letting it change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Writes "12 / 200" under the field, politely announced; needs <see cref="MaxLength"/>.</summary>
    [Parameter]
    public bool ShowCount { get; set; }

    /// <summary>The id of the element that describes the field (<c>aria-describedby</c>).</summary>
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
