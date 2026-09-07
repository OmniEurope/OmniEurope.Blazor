namespace OmniEurope.Blazor.Components;

public partial class OmniDropDown<TValue>
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    [Parameter]
    public bool AllowEmpty { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    private string EffectivePlaceholder => string.IsNullOrWhiteSpace(Placeholder)
        ? Localize("DropDownPlaceholder")
        : Placeholder;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private IEnumerable<(OmniOption<TValue> Option, int Index)> IndexedOptions =>
        Options.Select((option, index) => (option, index));

    private RenderFragment OptionMarkup(OmniOption<TValue> option, int index) => builder =>
    {
        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AddAttribute(2, "disabled", option.Disabled);
        builder.AddAttribute(3, "selected", EqualityComparer<TValue>.Default.Equals(CurrentValue, option.Value));
        builder.AddContent(4, option.Text);
        builder.CloseElement();
    };

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
