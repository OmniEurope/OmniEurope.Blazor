namespace OmniEurope.Blazor.Components;

/// <summary>
/// A number field (<c>type="number"</c>) bound to a numeric type (<c>int</c>, <c>long</c>,
/// <c>decimal</c>, <c>double</c>... or their nullable forms). The browser reads the bounds and the step
/// in the invariant culture; the field writes the value the same way.
/// </summary>
/// <typeparam name="TValue">The numeric type of the bound value, inferred from <c>@bind-Value</c>.</typeparam>
public partial class OmniNumeric<TValue>
{
    /// <summary>The lowest value (<c>min</c>); null, the default, for none. See <see cref="Clamp"/>.</summary>
    [Parameter]
    public double? Minimum { get; set; }

    /// <summary>The highest value (<c>max</c>); null, the default, for none. See <see cref="Clamp"/>.</summary>
    [Parameter]
    public double? Maximum { get; set; }

    /// <summary>The step of the arrows and of the browser's own validation (<c>step</c>); null for the browser's 1.</summary>
    [Parameter]
    public double? Step { get; set; }

    /// <summary>A hint shown while the field is empty.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Disables the field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Shows the value without letting it change.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>The id of the element that describes the field (<c>aria-describedby</c>).</summary>
    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// Brings a committed value outside <see cref="Minimum"/> or <see cref="Maximum"/> back to the
    /// nearest bound: the bound is what <c>ValueChanged</c> receives and what the field then shows.
    /// Off by default, as before: the value is taken as typed and the bounds only guide the
    /// browser. A bound that does not convert to <typeparamref name="TValue"/> (a fraction for an
    /// integer type, a value out of its range) is ignored.
    /// An empty field on a nullable type stays null.
    /// </summary>
    [Parameter]
    public bool Clamp { get; set; }

    // What the reader typed, rendered once more after a clamp. The input is not bound with @bind, so
    // Blazor does not know the browser's text: when the bound value is already the bound (20 shown,
    // 150 typed), the next render would carry the same "20" and leave "150" on screen. Rendering the
    // typed text first, then the bound, makes the second render a real change. Null outside a clamp,
    // so the field renders exactly its current value as it always did.
    private string? _typedBeforeClamp;
    private bool _clamped;

    private string? DisplayValue => _typedBeforeClamp ?? CurrentValueAsString;

    private void HandleChange(ChangeEventArgs args)
    {
        var typed = args.Value?.ToString();
        _clamped = false;
        CurrentValueAsString = typed;
        if (_clamped)
        {
            _typedBeforeClamp = typed;
        }
    }

    /// <summary>
    /// After an entry <see cref="Clamp"/> brought back within bounds, renders once more so the field
    /// replaces the typed text with the clamped value.
    /// </summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (_typedBeforeClamp is not null)
        {
            _typedBeforeClamp = null;
            StateHasChanged();
        }
    }

    /// <summary>Writes the number in the invariant culture, the only form a number input reads; null leaves the field empty.</summary>
    protected override string? FormatValueAsString(TValue? value) =>
        value is IFormattable number
            ? number.ToString(null, System.Globalization.CultureInfo.InvariantCulture)
            : base.FormatValueAsString(value);
    /// <summary>
    /// Parses the text in the invariant culture when it holds a period, in the current culture otherwise,
    /// then brings it within the bounds when <see cref="Clamp"/> is on. A text that does not parse fails
    /// with the localized "The entered value is invalid.", naming the field when a display name is set.
    /// </summary>
    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        var culture = value?.Contains('.', StringComparison.Ordinal) == true
            ? System.Globalization.CultureInfo.InvariantCulture
            : System.Globalization.CultureInfo.CurrentCulture;
        if (BindConverter.TryConvertTo<TValue>(value, culture, out var parsedValue))
        {
            result = Clamp ? ClampToBounds(parsedValue) : parsedValue;
            _clamped = Clamp && !EqualityComparer<TValue>.Default.Equals(result, parsedValue);
            validationErrorMessage = null!;
            return true;
        }

        result = default!;
        validationErrorMessage = string.IsNullOrWhiteSpace(DisplayName)
            ? Localize("NumericInvalid")
            : Localize("NumericInvalidNamed", DisplayName);
        return false;
    }

    private TValue ClampToBounds(TValue value)
    {
        if (value is null)
        {
            return value;
        }

        // Comparer<T?> orders a nullable by its value, so one path serves int, long, decimal,
        // double and their nullable forms alike.
        var comparer = Comparer<TValue>.Default;
        if (TryReadBound(Minimum, out var minimum) && comparer.Compare(value, minimum) < 0)
        {
            return minimum;
        }

        if (TryReadBound(Maximum, out var maximum) && comparer.Compare(value, maximum) > 0)
        {
            return maximum;
        }

        return value;
    }

    // The bound goes through its invariant text, the one the browser reads, so it converts to the bound
    // type exactly as the field's own value does.
    private static string? Invariant(double? value) => value?.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static bool TryReadBound(double? bound, out TValue value)
    {
        if (Invariant(bound) is { } text
            && BindConverter.TryConvertTo<TValue>(text, System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            && parsed is not null)
        {
            value = parsed;
            return true;
        }

        value = default!;
        return false;
    }
}
