namespace OmniEurope.Blazor.Components;

public partial class OmniProfileMenuItem
{
    [Parameter]
    public string? Href { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Rendered in a round disc before the text, decoratively. A slot rather than a name, so the
    /// consumer keeps its own icon set. Null, the default, renders no disc.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>
    /// A short muted line under the item's text, saying what the entry leads to. It is part of the
    /// item's content, so it is read with the name. Null or blank, the default, renders nothing.
    /// </summary>
    [Parameter]
    public string? Description { get; set; }

    private bool Rich => Icon is not null || !string.IsNullOrWhiteSpace(Description);

    private string ItemCss => Css("omni-profile-menu__item", Rich ? "omni-profile-menu__item--rich" : null);

    private string? SafeHref => OmniUriPolicy.EnsureSafe(Href, nameof(Href));

    /// <summary>
    /// The item's content: the child content alone, as it always was, or the disc, the text and the
    /// description once an icon or a description is set.
    /// </summary>
    private RenderFragment Content => builder =>
    {
        if (!Rich)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        if (Icon is not null)
        {
            builder.OpenElement(1, "span");
            builder.AddAttribute(2, "class", "omni-disc omni-profile-menu__item-icon");
            builder.AddAttribute(3, "aria-hidden", "true");
            builder.AddContent(4, Icon);
            builder.CloseElement();
        }

        builder.OpenElement(5, "span");
        builder.AddAttribute(6, "class", "omni-profile-menu__item-text");
        builder.AddContent(7, ChildContent);
        if (!string.IsNullOrWhiteSpace(Description))
        {
            builder.OpenElement(8, "small");
            builder.AddAttribute(9, "class", "omni-profile-menu__item-description");
            builder.AddContent(10, Description);
            builder.CloseElement();
        }

        builder.CloseElement();
    };
}
