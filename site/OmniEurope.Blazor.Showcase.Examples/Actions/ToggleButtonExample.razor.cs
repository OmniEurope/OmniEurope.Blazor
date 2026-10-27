using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>A toggle button whose pressed state, size, content and states are parameters.</summary>
public partial class ToggleButtonExample
{
    private bool? _lastValue;

    /// <summary>Whether the button is pressed; bind it to share the state with the host.</summary>
    [Parameter]
    public bool Value { get; set; }

    /// <summary>Raised when the button flips.</summary>
    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>The size of the button.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Whether the button shows its icon alone, its name then carried by the accessible label.</summary>
    [Parameter]
    public bool IconOnly { get; set; }

    /// <summary>Whether the button keeps its state but can no longer change it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether a veil breathes over the button and clicks are ignored.</summary>
    [Parameter]
    public bool Busy { get; set; }

    private int ToggleCount { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet() => Count(Value);

    private async Task SetValueAsync(bool value)
    {
        Value = value;
        Count(value);
        await ValueChanged.InvokeAsync(value);
    }

    /// <summary>Counts a flip once, wherever it comes from: the first value seen is not a flip.</summary>
    private void Count(bool value)
    {
        if (_lastValue is { } last && last != value)
        {
            ToggleCount++;
        }

        _lastValue = value;
    }
}
