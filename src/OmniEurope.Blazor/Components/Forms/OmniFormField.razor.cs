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

    /// <summary>
    /// A short explanation between the label and the control, in muted text. Given an <see cref="OmniComponentBase.Id"/>,
    /// it has the id <c>{Id}-description</c>, which the control can name in <c>aria-describedby</c>.
    /// Null or blank, the default, renders nothing.
    /// </summary>
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
}
