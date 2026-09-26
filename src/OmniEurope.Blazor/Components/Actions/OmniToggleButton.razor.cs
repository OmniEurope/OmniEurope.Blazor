namespace OmniEurope.Blazor.Components;

public partial class OmniToggleButton
{
    [Parameter]
    public bool Value { get; set; }

    [Parameter]
    public EventCallback<bool> ValueChanged { get; set; }

    /// <summary>The same three sizes as <see cref="OmniButton"/>; with an icon alone the button is square.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The accessible name, needed when the content is an icon alone. An <c>aria-label</c> passed as
    /// an additional attribute still takes precedence, as it did before this parameter existed.
    /// </summary>
    [Parameter]
    public string? AriaLabel { get; set; }

    private Task ToggleAsync() => Disabled || Busy ? Task.CompletedTask : ValueChanged.InvokeAsync(!Value);
}
