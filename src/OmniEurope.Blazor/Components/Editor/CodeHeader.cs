namespace OmniEurope.Blazor.Components;

/// <summary>
/// The header shared by <see cref="OmniCodeBlock"/> and <see cref="OmniCodeViewer"/>: the title, the
/// language, the actions and the copy button, which shows a check or a cross while the outcome of the
/// last copy lasts. One implementation for both, the classes taken from <see cref="Block"/>
/// (<c>{Block}__header</c>, <c>{Block}__title</c>, <c>{Block}__language</c>, <c>{Block}__actions</c>,
/// <c>{Block}__copy</c>).
/// </summary>
/// <remarks>
/// Built in C# rather than as a Razor tag: the component is internal, and Razor only finds public
/// components. The owner keeps the clipboard and the live region; this draws what they say.
/// </remarks>
internal sealed class CodeHeader : ComponentBase
{
    /// <summary>The BEM block of the owner: <c>omni-code-block</c> or <c>omni-code-viewer</c>.</summary>
    [Parameter, EditorRequired] public string Block { get; set; } = string.Empty;

    /// <summary>The element drawn: <c>figcaption</c> inside the block's figure, <c>header</c> in the viewer.</summary>
    [Parameter] public string Element { get; set; } = "header";

    /// <summary>The id of the title, so the owner can name its code by it; none when null.</summary>
    [Parameter] public string? TitleId { get; set; }

    /// <summary>The title; nothing is drawn for it when null or blank.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The language, drawn muted after the title; nothing when null or blank.</summary>
    [Parameter] public string? Language { get; set; }

    /// <summary>Buttons drawn before the copy button (the reveal of a secret, the wrap toggle).</summary>
    [Parameter] public RenderFragment? LeadingActions { get; set; }

    /// <summary>Content drawn after the copy button (the host's actions).</summary>
    [Parameter] public RenderFragment? TrailingActions { get; set; }

    /// <summary>Whether the copy button is drawn.</summary>
    [Parameter] public bool ShowCopy { get; set; } = true;

    /// <summary>
    /// Whether the copy button shows its words beside the icon. Off, it is an icon alone, named by the same
    /// words through its accessible name and tooltip.
    /// </summary>
    [Parameter] public bool CopyText { get; set; }

    /// <summary>The outcome of the last copy while its feedback lasts; null when idle.</summary>
    [Parameter] public bool? CopyResult { get; set; }

    /// <summary>What the copy button says: "copy", then "copied" or "copy failed".</summary>
    [Parameter] public string CopyLabel { get; set; } = string.Empty;

    /// <summary>Raised when the copy button is pressed.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnCopy { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Element);
        builder.AddAttribute(1, "class", $"{Block}__header");
        if (!string.IsNullOrWhiteSpace(Title))
        {
            builder.OpenElement(2, "span");
            builder.AddAttribute(3, "id", TitleId);
            builder.AddAttribute(4, "class", $"{Block}__title");
            builder.AddContent(5, Title);
            builder.CloseElement();
        }

        if (!string.IsNullOrWhiteSpace(Language))
        {
            builder.OpenElement(6, "span");
            builder.AddAttribute(7, "class", $"{Block}__language");
            builder.AddContent(8, Language);
            builder.CloseElement();
        }

        builder.OpenElement(9, "span");
        builder.AddAttribute(10, "class", $"{Block}__actions");
        builder.AddContent(11, LeadingActions);
        if (ShowCopy)
        {
            builder.OpenComponent<OmniButton>(12);
            builder.AddComponentParameter(13, nameof(OmniButton.Class), $"{Block}__copy");
            builder.AddComponentParameter(14, nameof(OmniButton.Variant), OmniButtonVariant.Ghost);
            builder.AddComponentParameter(15, nameof(OmniButton.Size), OmniControlSize.Small);
            if (!CopyText)
            {
                builder.AddComponentParameter(16, nameof(OmniButton.Label), CopyLabel);
                builder.AddComponentParameter(17, "title", CopyLabel);
            }

            builder.AddComponentParameter(18, nameof(OmniButton.OnClick), OnCopy);
            builder.AddComponentParameter(19, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
            {
                content.OpenComponent<OmniIcon>(0);
                content.AddComponentParameter(1, nameof(OmniIcon.Name), CopyResult == true ? OmniIconName.Check : OmniIconName.Copy);
                content.AddComponentParameter(2, nameof(OmniIcon.Size), OmniControlSize.Small);
                content.CloseComponent();
                if (CopyText)
                {
                    content.OpenElement(3, "span");
                    content.AddContent(4, CopyLabel);
                    content.CloseElement();
                }
            }));
            builder.CloseComponent();
        }

        builder.AddContent(20, TrailingActions);
        builder.CloseElement();
        builder.CloseElement();
    }
}
