namespace OmniEurope.Blazor.Components;

public partial class OmniFormField
{
    [Parameter]
    public string For { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? Label { get; set; }

    /// <summary>Plain-text label shorthand for markup-driven forms.</summary>
    [Parameter]
    public string? Text { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? End { get; set; }

    [Parameter]
    public RenderFragment? Helper { get; set; }

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private string? DescriptionId => string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Description) ? null : $"{Id}-description";
    private string? ErrorId => string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Error) ? null : $"{Id}-error";

    /// <summary>
    /// The density of this component and of what it holds (control heights, paddings, gaps), over
    /// the one it inherits from its theme scope or section. Null, the default, inherits it.
    /// </summary>
    [Parameter]
    public OmniDensity? Density { get; set; }
}
