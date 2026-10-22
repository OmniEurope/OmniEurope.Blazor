namespace OmniEurope.Blazor.Components;

/// <summary>One term and value pair of an <see cref="OmniDescriptionList"/>.</summary>
public partial class OmniDescriptionItem
{
    /// <summary>The term: what the value is, "IP address".</summary>
    [Parameter, EditorRequired]
    public string Term { get; set; } = string.Empty;

    /// <summary>An icon before the term, in general an <see cref="OmniIcon"/>; decorative, the term names the value.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>The value: text, a link, a badge.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Actions on the value, after it: a copy button, an edit link.</summary>
    [Parameter]
    public RenderFragment? ActionsContent { get; set; }
}
