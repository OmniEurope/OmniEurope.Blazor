namespace OmniEurope.Blazor.Components;

/// <summary>A breadcrumb trail: a navigation landmark holding the <see cref="OmniBreadcrumbItem"/> of the path to the current page.</summary>
public partial class OmniBreadcrumb
{
    /// <summary>Accessible name of the navigation landmark. Null, the default, is the localized "Breadcrumb".</summary>
    [Parameter]
    public string? Label { get; set; }

    private string EffectiveLabel => LocalizeOr(Label, "BreadcrumbLabel");

    /// <summary>The steps of the path, <see cref="OmniBreadcrumbItem"/> from the root to the current page.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
