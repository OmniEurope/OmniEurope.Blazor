namespace OmniEurope.Blazor.Components;

/// <summary>
/// The placeholder of an empty list, search or section: an icon, a title, a description and the
/// actions that lead out of it.
/// </summary>
public partial class OmniEmptyState
{
    /// <summary>A decorative icon above the title; an empty tray when not given.</summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>What is missing: "No project yet".</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Why, or what to do about it.</summary>
    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Heading level of the title. Null, the default, keeps it a paragraph: an empty state inside a
    /// section does not open a section of its own.
    /// </summary>
    [Parameter]
    public OmniHeadingLevel? Level { get; set; }

    /// <summary>The way out: a button to create the first item, to clear the filters, to go back.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }
}
