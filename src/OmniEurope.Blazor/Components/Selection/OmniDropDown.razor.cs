using System.Collections;
using System.Globalization;
using System.Reflection;

namespace OmniEurope.Blazor.Components;

public partial class OmniDropDown<TValue>
{
    [Parameter]
    public IReadOnlyList<OmniOption<TValue>> Options { get; set; } = Array.Empty<OmniOption<TValue>>();

    /// <summary>
    /// Optional data source for model-backed options. Use <see cref="Options"/> for new code when
    /// the values are already available as <see cref="OmniOption{TValue}"/> instances.
    /// </summary>
    [Parameter]
    public IEnumerable? Data { get; set; }

    [Parameter]
    public string? TextProperty { get; set; }

    [Parameter]
    public string? ValueProperty { get; set; }

    [Parameter]
    public bool AllowEmpty { get; set; }

    [Parameter]
    public bool AllowClear { get; set; }

    [Parameter]
    public EventCallback<TValue> Change { get; set; }

    [Parameter]
    public bool AllowFiltering { get; set; }

    [Parameter]
    public string Placeholder { get; set; } = string.Empty;

    [Parameter]
    public string? AriaLabel { get; set; }

    private string EffectivePlaceholder => string.IsNullOrWhiteSpace(Placeholder)
        ? Localize("DropDownPlaceholder")
        : Placeholder;

    private string EffectiveAriaLabel
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(AriaLabel))
            {
                return AriaLabel;
            }

            if (AdditionalAttributes?.TryGetValue("aria-label", out var label) == true
                && !string.IsNullOrWhiteSpace(label?.ToString()))
            {
                return label.ToString()!;
            }

            return EffectivePlaceholder;
        }
    }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    private IReadOnlyList<OmniOption<TValue>> EffectiveOptions => Data is null
        ? Options
        : Data.Cast<object?>().Select(ToOption).ToArray();

    private bool AllowsEmpty => AllowEmpty || AllowClear;

    private IEnumerable<(OmniOption<TValue> Option, int Index)> IndexedOptions =>
        EffectiveOptions.Select((option, index) => (option, index));

    private RenderFragment OptionMarkup(OmniOption<TValue> option, int index) => builder =>
    {
        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AddAttribute(2, "disabled", option.Disabled);
        builder.AddAttribute(3, "selected", EqualityComparer<TValue>.Default.Equals(CurrentValue, option.Value));
        builder.AddContent(4, option.Text);
        builder.CloseElement();
    };

    private async Task HandleChange(ChangeEventArgs args)
    {
        var raw = args.Value?.ToString();
        if (string.IsNullOrEmpty(raw) && AllowsEmpty)
        {
            CurrentValue = default!;
        }
        else if (int.TryParse(raw, out var index) && index >= 0 && index < EffectiveOptions.Count && !EffectiveOptions[index].Disabled)
        {
            CurrentValue = EffectiveOptions[index].Value;
        }

        if (Change.HasDelegate)
        {
            await Change.InvokeAsync(CurrentValue);
        }
    }

    private Task<IReadOnlyList<OmniOption<TValue>>> SearchOptionsAsync(string text, CancellationToken _)
    {
        IReadOnlyList<OmniOption<TValue>> result = EffectiveOptions
            .Where(option => option.Text.Contains(text, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
        return Task.FromResult(result);
    }

    private async Task HandleAutocompleteChangeAsync(TValue value)
    {
        CurrentValue = value;
        if (Change.HasDelegate)
        {
            await Change.InvokeAsync(value);
        }
    }

    private string FormatValue(TValue value) => EffectiveOptions
        .FirstOrDefault(option => EqualityComparer<TValue>.Default.Equals(option.Value, value))?.Text
        ?? value?.ToString()
        ?? string.Empty;

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (int.TryParse(value, out var index) && index >= 0 && index < EffectiveOptions.Count)
        {
            result = EffectiveOptions[index].Value;
            validationErrorMessage = null!;
            return true;
        }

        result = default!;
        validationErrorMessage = Localize("DropDownInvalid", FieldIdentifier.FieldName);
        return false;
    }

    private OmniOption<TValue> ToOption(object? item)
    {
        var rawValue = Read(item, ValueProperty) ?? item;
        var targetType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
        var value = rawValue is null
            ? default!
            : rawValue is TValue typed
                ? typed
                : targetType.IsEnum
                    ? (TValue)Enum.Parse(targetType, rawValue.ToString()!, ignoreCase: true)
                    : (TValue)Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
        var text = Read(item, TextProperty)?.ToString() ?? item?.ToString() ?? string.Empty;
        return new OmniOption<TValue>(value, text);
    }

    private static object? Read(object? item, string? property)
    {
        if (item is null || string.IsNullOrWhiteSpace(property))
        {
            return item;
        }

        return item.GetType().GetProperty(property, BindingFlags.Instance | BindingFlags.Public)?.GetValue(item);
    }
}
