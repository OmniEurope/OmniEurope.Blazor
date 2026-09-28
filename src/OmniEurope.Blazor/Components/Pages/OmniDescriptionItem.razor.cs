namespace OmniEurope.Blazor.Components;

/// <summary>One label and value pair of an <see cref="OmniDescriptionList"/>.</summary>
public partial class OmniDescriptionItem
{
    /// <summary>What the value is: "IP address".</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    /// <summary>An icon before the label, in general an <see cref="OmniIcon"/>; decorative, the label names the value.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The value: text, a link, a badge.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Actions on the value, after it: a copy button, an edit link.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }
}
