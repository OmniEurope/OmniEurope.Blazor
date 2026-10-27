using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>An integer, a decimal and a nullable integer whose bounds, step and states are parameters.</summary>
public partial class NumbersExample : IDisposable
{
    /// <summary>The lowest value the browser's arrows go to, or <see langword="null"/> for none; the component does not enforce it.</summary>
    [Parameter]
    public int? Minimum { get; set; } = 0;

    /// <summary>The highest value the browser's arrows go to, or <see langword="null"/> for none; the component does not enforce it.</summary>
    [Parameter]
    public int? Maximum { get; set; } = 100;

    /// <summary>What the arrows and the keyboard add or remove on the integer fields.</summary>
    [Parameter]
    public double IntegerStep { get; set; } = 1;

    /// <summary>What the arrows and the keyboard add or remove on the decimal field.</summary>
    [Parameter]
    public double DecimalStep { get; set; } = 0.01;

    /// <summary>Whether the fields show a sample while they are empty.</summary>
    [Parameter]
    public bool ShowPlaceholder { get; set; } = true;

    /// <summary>Whether the fields can be neither focused nor edited.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the fields can be focused and copied but not edited.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// The edit context is kept here rather than created by the form: an unreadable entry does not change
    /// the bound value, so only the validation event tells the component to show the message.
    /// </summary>
    private EditContext Context { get; set; } = null!;

    private int IntegerValue { get; set; } = 12;

    // Whole numbers to start with: the component writes its value in the current culture, and a browser
    // number field only accepts the decimal point.
    private decimal DecimalValue { get; set; } = 20m;

    private int? NullableValue { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Context = new EditContext(this);
        Context.OnValidationStateChanged += OnValidationStateChanged;
    }

    /// <summary>The bounds are only HTML attributes: the component passes them on without enforcing them.</summary>
    private string Echo(decimal value)
    {
        var text = value.ToString(CultureInfo.CurrentCulture);
        var outside = (Minimum is { } minimum && value < minimum) || (Maximum is { } maximum && value > maximum);
        return outside ? $"Bound value: {text}, outside the bounds" : $"Bound value: {text}";
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs args) => StateHasChanged();

    /// <inheritdoc />
    public void Dispose() => Context.OnValidationStateChanged -= OnValidationStateChanged;
}
