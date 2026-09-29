namespace OmniEurope.Blazor.Components;

/// <summary>
/// A choice of one option among radio buttons grouped in a fieldset, with an optional legend and error
/// line. It shares its surface with <see cref="OmniCheckBoxList{TValue}"/>: <see cref="Label"/>,
/// <see cref="Disabled"/>, <see cref="AriaDescribedBy"/>, <see cref="Error"/> and the validation state of
/// the form.
/// </summary>
/// <typeparam name="TValue">The value of each option.</typeparam>
public partial class OmniRadioButtonList<TValue>
{
    private readonly string _generatedName = $"omni-radio-{Guid.NewGuid():N}";

    /// <summary>The options, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>The legend of the group, which names it. Null or blank draws none.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>The <c>name</c> shared by the radio buttons; the <see cref="OmniInputBase{TValue}.Id"/>, or a generated name, when null.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Whether the whole group is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Ids of elements that describe the group, written in its <c>aria-describedby</c> before the error line.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// A message drawn under the choices with the error glyph, as <see cref="OmniFormField.Error"/>
    /// draws it under a field. While it is set the group carries <c>aria-invalid="true"</c> and is
    /// described by the message (<c>{Id}-error</c>, or <c>{Name}-error</c> without an id), after
    /// <see cref="AriaDescribedBy"/>. Null or blank, the default, renders nothing. A validation message
    /// of the enclosing form also marks the group invalid, without drawing a line.
    /// </summary>
    [Parameter]
    public string? Error { get; set; }

    private string GroupName => Name ?? Id ?? _generatedName;
    private bool HasError => !string.IsNullOrWhiteSpace(Error);
    private bool IsInvalid => HasError || AriaInvalid is not null;
    private string ErrorId => $"{Id ?? GroupName}-error";
    private string? DescribedBy => OmniChoiceListParts.DescribedBy(AriaDescribedBy, HasError ? ErrorId : null);

    private static string ItemClass(OmniOption<TValue> option) => OmniChoiceListParts.ItemClass(option.Disabled);

    private bool IsSelected(TValue value) => EqualityComparer<TValue>.Default.Equals(CurrentValue, value);

    private void SelectOption(OmniOption<TValue> option)
    {
        if (!Disabled && !option.Disabled)
        {
            CurrentValue = option.Value;
        }
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        result = default!;
        validationErrorMessage = Localize("RadioButtonListInvalid");
        return false;
    }
}
