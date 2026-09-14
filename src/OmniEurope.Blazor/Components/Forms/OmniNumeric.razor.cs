namespace OmniEurope.Blazor.Components;

public partial class OmniNumeric<TValue>
{
    [Parameter]
    public string? Minimum { get; set; }

    [Parameter]
    public string? Maximum { get; set; }

    [Parameter]
    public string? Step { get; set; }

    [Parameter]
    public string? Placeholder { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? AriaDescribedBy { get; set; }

    /// <summary>
    /// Brings a committed value outside <see cref="Minimum"/> or <see cref="Maximum"/> back to the
    /// nearest bound: the bound is what <c>ValueChanged</c> receives and what the field then shows.
    /// Off by default, as before: the value is taken as typed and the bounds only guide the
    /// browser. The bounds are read the way the browser reads <c>min</c> and <c>max</c>, in the
    /// invariant culture; one that does not convert to <typeparamref name="TValue"/> is ignored.
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

    protected override void OnAfterRender(bool firstRender)
    {
        if (_typedBeforeClamp is not null)
        {
            _typedBeforeClamp = null;
            StateHasChanged();
        }
    }

    protected override bool TryParseValueFromString(string? value, out TValue result, out string validationErrorMessage)
    {
        if (BindConverter.TryConvertTo<TValue>(value, System.Globalization.CultureInfo.CurrentCulture, out var parsedValue))
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

    private static bool TryReadBound(string? bound, out TValue value)
    {
        if (!string.IsNullOrWhiteSpace(bound)
            && BindConverter.TryConvertTo<TValue>(bound.Trim(), System.Globalization.CultureInfo.InvariantCulture, out var parsed)
            && parsed is not null)
        {
            value = parsed;
            return true;
        }

        value = default!;
        return false;
    }
}
