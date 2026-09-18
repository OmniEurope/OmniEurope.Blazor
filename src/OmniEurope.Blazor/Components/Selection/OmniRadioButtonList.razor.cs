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

    /// <summary>
    /// A message drawn under the choices with the error glyph, as <see cref="OmniFormField.Error"/>
    /// draws it under a field. While it is set the group carries <c>aria-invalid="true"</c> and is
    /// described by the message (<c>{Id}-error</c>, or <c>{Name}-error</c> without an id), after any
    /// <c>aria-describedby</c> the consumer passed. Null or blank, the default, renders nothing.
    /// </summary>
    [Parameter]
    public string? Error { get; set; }

    private string GroupName => Name ?? Id ?? $"omni-radio-{FieldIdentifier.FieldName}";
    private bool HasError => !string.IsNullOrWhiteSpace(Error);
    private string ErrorId => $"{Id ?? GroupName}-error";

    /// <summary>The aria-invalid the consumer or the edit context put in the attributes, kept when there is no error.</summary>
    private object? InvalidFromAttributes => Attribute("aria-invalid");

    private string? DescribedBy
    {
        get
        {
            var passed = Attribute("aria-describedby")?.ToString();
            if (!HasError)
            {
                return string.IsNullOrWhiteSpace(passed) ? null : passed;
            }

            return string.IsNullOrWhiteSpace(passed) ? ErrorId : $"{passed} {ErrorId}";
        }
    }

    private object? Attribute(string name) =>
        AdditionalAttributes is not null && AdditionalAttributes.TryGetValue(name, out var value) ? value : null;

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
