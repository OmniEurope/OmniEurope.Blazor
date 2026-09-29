namespace OmniEurope.Blazor.Components;

/// <summary>One step of an <see cref="OmniBreadcrumb"/>: a link, or the current page.</summary>
public partial class OmniBreadcrumbItem
{
    /// <summary>The address of the step, checked by the package's URI policy. Null, the default, draws the step as text.</summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>Marks the step as the current page (<c>aria-current="page"</c>), drawn as text.</summary>
    [Parameter]
    public bool Current { get; set; }

    /// <summary>The text of the step.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));
}
