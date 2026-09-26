namespace OmniEurope.Blazor.Components;

/// <summary>
/// A key figure on a tile: an icon, the value, what it measures and an optional detail line. Given
/// <see cref="OnClick"/>, the whole tile becomes one button that opens the figure's detail.
/// </summary>
public partial class OmniStatTile
{
    /// <summary>A decorative icon on a tinted square before the text. None when not given.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The figure itself, already formatted: "12,4 k", "83 %".</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>What the figure measures: "Tokens aujourd'hui".</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>An optional muted line under the label: a reset time, a comparison.</summary>
    [Parameter]
    public string? Detail { get; set; }

    /// <summary>
    /// Makes the tile a button. Its accessible name is <see cref="AriaLabel"/> when set, otherwise the
    /// label followed by the value.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Accessible name of the button form; ignored when the tile is not clickable.</summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private string AccessibleName => !string.IsNullOrWhiteSpace(AriaLabel)
        ? AriaLabel
        : string.IsNullOrWhiteSpace(Value) ? Label : $"{Label} : {Value}";
}
