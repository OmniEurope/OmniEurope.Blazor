namespace OmniEurope.Blazor.Components;

/// <summary>
/// A choice of several options among check boxes grouped in a fieldset, with an optional legend and error
/// line. It shares its surface with <see cref="OmniRadioButtonList{TValue}"/>: <see cref="Label"/>,
/// <see cref="Disabled"/>, <see cref="AriaDescribedBy"/>, <see cref="Error"/> and the validation state of
/// the form.
/// </summary>
/// <typeparam name="TValue">The value of each option.</typeparam>
public partial class OmniCheckBoxList<TValue>
{
    private readonly string _generatedId = $"omni-checks-{Guid.NewGuid():N}";

    /// <summary>The options, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>
    /// The legend of the group, which names it. Null or blank draws none: the group is then named by the
    /// label of an enclosing <see cref="OmniFormField"/> whose <c>For</c> is
    /// <see cref="OmniInputBase{TValue}.Id"/> (<c>aria-labelledby</c>).
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Whether the whole group is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Ids of elements that describe the group, written in its <c>aria-describedby</c> before the error line.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// A message drawn under the choices with the error glyph, as <see cref="OmniFormField.Error"/>
    /// draws it under a field. While it is set the group carries <c>aria-invalid="true"</c> and is
    /// described by the message (<c>{Id}-error</c>), after <see cref="AriaDescribedBy"/>. Null or blank,
    /// the default, renders nothing. A validation message of the enclosing form also marks the group
    /// invalid, without drawing a line.
    /// </summary>
    [Parameter]
    public string? Error { get; set; }

    private bool HasError => !string.IsNullOrWhiteSpace(Error);
    private bool IsInvalid => HasError || AriaInvalid is not null;
    private string ErrorId => $"{Id ?? _generatedId}-error";
    private string? DescribedBy => OmniChoiceListParts.DescribedBy(AriaDescribedBy, HasError ? ErrorId : null);

    private static string ItemClass(OmniOption<TValue> option) => OmniChoiceListParts.ItemClass(option.Disabled);

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

    /// <summary>Never parses: the list sets its value from the boxes checked, never from text.</summary>
    /// <param name="value">The text, ignored.</param>
    /// <param name="result">Always an empty list.</param>
    /// <param name="validationErrorMessage">The localized "invalid list" message.</param>
    /// <returns>Always false.</returns>
    protected override bool TryParseValueFromString(string? value, out IReadOnlyList<TValue> result, out string validationErrorMessage)
    {
        result = Array.Empty<TValue>();
        validationErrorMessage = Localize("CheckBoxListInvalid");
        return false;
    }
}
