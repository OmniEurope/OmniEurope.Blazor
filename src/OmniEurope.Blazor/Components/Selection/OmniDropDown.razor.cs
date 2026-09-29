namespace OmniEurope.Blazor.Components;

/// <summary>
/// A choice of one option in a closed list: a native <c>select</c>, or, with <see cref="Filterable"/>,
/// an <see cref="OmniAutocomplete{TValue}"/> that narrows <see cref="Options"/> as the user types.
/// </summary>
/// <typeparam name="TValue">The value of each option.</typeparam>
public partial class OmniDropDown<TValue>
{
    /// <summary>The options, in order; an option with a <see cref="OmniOption{TValue}.Group"/> is listed under that group.</summary>
    [Parameter]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>
    /// Adds an empty first option that clears the value (<c>default</c>, null for a nullable
    /// <typeparamref name="TValue"/>). Off by default.
    /// </summary>
    [Parameter]
    public bool AllowEmpty { get; set; }

    /// <summary>
    /// Lets the user type to narrow the options: the list becomes an editable combobox with the keyboard
    /// of <see cref="OmniAutocomplete{TValue}"/>. Off by default.
    /// </summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>
    /// The text of the empty option (<see cref="AllowEmpty"/>) and the hint of the filter field. Null uses
    /// the localized "Select".
    /// </summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>
    /// The accessible name of the control, written as its <c>aria-label</c>. Null, the default, writes
    /// none, so the <c>label</c> of an enclosing <see cref="OmniFormField"/> (or any <c>label for</c> the
    /// <see cref="OmniInputBase{TValue}.Id"/>) names it.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Whether the control is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Ids of elements that describe the control, written in its <c>aria-describedby</c>.</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private string EffectivePlaceholder => string.IsNullOrWhiteSpace(Placeholder)
        ? Localize("DropDownPlaceholder")
        : Placeholder;

    private IEnumerable<(OmniOption<TValue> Option, int Index)> IndexedOptions =>
        Options.Select((option, index) => (option, index));

    private RenderFragment OptionMarkup(OmniOption<TValue> option, int index) => builder =>
    {
        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", index.ToString(CultureInfo.InvariantCulture));
        builder.AddAttribute(2, "disabled", option.Disabled);
        builder.AddAttribute(3, "selected", EqualityComparer<TValue>.Default.Equals(CurrentValue, option.Value));
        builder.AddContent(4, option.Text);
        builder.CloseElement();
    };

    /// <summary>
    /// The value of the option that shows the current value, or "" for the empty one. Bound on the select
    /// itself: the options' selected attributes alone do not move a selection the user already made, so a
    /// value reset from code (a Clear filters button) left the old choice displayed.
    /// </summary>
    private string SelectedOptionValue
    {
        get
        {
            for (var index = 0; index < Options.Count; index++)
            {
                if (EqualityComparer<TValue>.Default.Equals(CurrentValue, Options[index].Value))
                {
                    return index.ToString(CultureInfo.InvariantCulture);
                }
            }

            return string.Empty;
        }
    }

    private void HandleChange(ChangeEventArgs args)
    {
        var raw = args.Value?.ToString();
        if (string.IsNullOrEmpty(raw) && AllowEmpty)
        {
            CurrentValue = default!;
        }
        else if (int.TryParse(raw, out var index) && index >= 0 && index < Options.Count && !Options[index].Disabled)
        {
            CurrentValue = Options[index].Value;
        }
    }

    private Task<IReadOnlyList<OmniOption<TValue>>> SearchOptionsAsync(string text, CancellationToken _)
    {
        IReadOnlyList<OmniOption<TValue>> result = Options
            .Where(option => option.Text.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
        return Task.FromResult(result);
    }

    private void HandleAutocompleteChange(TValue value) => CurrentValue = value;

    private string FormatValue(TValue value) => Options
        .FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, value))?.Text
        ?? value?.ToString()
        ?? string.Empty;

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (int.TryParse(value, out var index) && index >= 0 && index < Options.Count)
        {
            result = Options[index].Value;
            validationErrorMessage = null!;
            return true;
        }

        result = default!;
        validationErrorMessage = Localize("DropDownInvalid", FieldIdentifier.FieldName);
        return false;
    }
}
