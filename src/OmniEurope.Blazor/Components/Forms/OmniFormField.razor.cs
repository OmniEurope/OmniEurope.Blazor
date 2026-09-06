namespace OmniEurope.Blazor.Components;

public partial class OmniFormField
{
    [Parameter, EditorRequired]
    public string For { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public RenderFragment? Label { get; set; }

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

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
