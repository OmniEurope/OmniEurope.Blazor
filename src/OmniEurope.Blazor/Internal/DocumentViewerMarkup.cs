using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The HTML pages of <see cref="OmniDocumentViewer"/>: the host's HTML through the package allow-list, then drawn
/// element by element into the render tree rather than written as markup, so that a field can carry its own click and
/// key handlers and no text of the document is ever read as markup by the renderer.
/// </summary>
internal static class DocumentViewerMarkup
{
    /// <summary>The attribute that makes an element a field of the document; its value is the tag the host reads back.</summary>
    internal const string FieldAttribute = "data-oe-field";

    /// <summary>The class of a highlighted field, styled by the package stylesheet.</summary>
    internal const string FieldClass = "omni-document-viewer__field";

    private static readonly HtmlParser Parser = new();

    /// <summary>The attributes a field element gets from the viewer; the document's own value of one of these is dropped.</summary>
    private static readonly HashSet<string> FieldOwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "tabindex", "data-omni-tip", "aria-description"
    };

    /// <summary>What the viewer allows on top of the built-in allow-list: the field attribute.</summary>
    internal static OmniHtmlSanitizerPolicy BasePolicy { get; } = new() { AdditionalAttributes = [FieldAttribute] };

    /// <summary>
    /// The policy a viewer sanitises with: the field attribute, widened by the host's policy when it gives one.
    /// A policy that names something no policy can allow throws <see cref="ArgumentException"/> here, where the host set it.
    /// </summary>
    /// <param name="hostPolicy">The host's policy, or null for the built-in allow-list alone.</param>
    /// <returns>The policy to sanitise with.</returns>
    internal static OmniHtmlSanitizerPolicy PolicyFor(OmniHtmlSanitizerPolicy? hostPolicy)
    {
        var policy = OmniHtmlSanitizerPolicy.Merge(BasePolicy, hostPolicy)!;
        OmniHtmlSanitizer.Validate(policy);
        return policy;
    }

    /// <summary>The HTML of a page through the allow-list of <paramref name="policy"/>, parsed into the body that is drawn.</summary>
    /// <param name="html">The host's HTML.</param>
    /// <param name="policy">The policy, from <see cref="PolicyFor"/>.</param>
    /// <returns>The sanitised body.</returns>
    internal static IElement Parse(string html, OmniHtmlSanitizerPolicy policy) =>
        Parser.ParseDocument("<!DOCTYPE html><html><body>" + OmniHtmlSanitizer.Sanitize(html, policy) + "</body></html>").Body!;

    /// <summary>
    /// Draws the children of a sanitised body. With <paramref name="highlight"/>, an element with a field tag is
    /// highlighted, focusable, tipped with its label and raises <paramref name="onField"/> on click and Enter.
    /// </summary>
    /// <param name="builder">The builder of the page.</param>
    /// <param name="body">The sanitised body.</param>
    /// <param name="highlight">Whether fields are drawn as fields.</param>
    /// <param name="labels">The label of each tag, shown in the tooltip; a tag with none shows itself.</param>
    /// <param name="receiver">The component that owns the handlers.</param>
    /// <param name="onField">Called with the tag of the field that was activated.</param>
    internal static void Render(
        RenderTreeBuilder builder,
        IElement body,
        bool highlight,
        IReadOnlyDictionary<string, string>? labels,
        object receiver,
        Func<string, Task> onField)
    {
        var sequence = 0;
        RenderChildren(builder, body, highlight, labels, receiver, onField, ref sequence);
    }

    /// <summary>The text a field's tooltip shows: the label of its tag, else the tag.</summary>
    /// <param name="tag">The value of <c>data-oe-field</c>.</param>
    /// <param name="labels">The label of each tag.</param>
    /// <returns>The label, or the tag itself.</returns>
    internal static string LabelOf(string tag, IReadOnlyDictionary<string, string>? labels) =>
        labels is not null && labels.TryGetValue(tag, out var label) && !string.IsNullOrWhiteSpace(label) ? label : tag;

    private static void RenderChildren(
        RenderTreeBuilder builder,
        INode parent,
        bool highlight,
        IReadOnlyDictionary<string, string>? labels,
        object receiver,
        Func<string, Task> onField,
        ref int sequence)
    {
        foreach (var node in parent.ChildNodes)
        {
            if (node is IElement element)
            {
                RenderElement(builder, element, highlight, labels, receiver, onField, ref sequence);
            }
            else
            {
                // Text. The allow-list removes comments and processing instructions before this point.
                builder.AddContent(sequence++, node.TextContent);
            }
        }
    }

    private static void RenderElement(
        RenderTreeBuilder builder,
        IElement element,
        bool highlight,
        IReadOnlyDictionary<string, string>? labels,
        object receiver,
        Func<string, Task> onField,
        ref int sequence)
    {
        var tag = highlight ? element.GetAttribute(FieldAttribute) : null;
        var isField = !string.IsNullOrWhiteSpace(tag);
        builder.OpenElement(sequence++, element.LocalName);
        foreach (var attribute in element.Attributes)
        {
            if (!isField || !FieldOwnedAttributes.Contains(attribute.Name))
            {
                builder.AddAttribute(sequence++, attribute.Name, attribute.Value);
            }
        }

        if (isField)
        {
            var fieldTag = tag!;
            var label = LabelOf(fieldTag, labels);
            var existing = element.GetAttribute("class");
            builder.AddAttribute(sequence++, "class", string.IsNullOrWhiteSpace(existing) ? FieldClass : $"{existing} {FieldClass}");
            builder.AddAttribute(sequence++, "tabindex", "0");
            builder.AddAttribute(sequence++, "data-omni-tip", label);
            builder.AddAttribute(sequence++, "aria-description", label);
            builder.AddAttribute(sequence++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(receiver, () => onField(fieldTag)));
            builder.AddEventStopPropagationAttribute(sequence++, "onclick", true);
            builder.AddAttribute(sequence++, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(
                receiver, args => args.Key == "Enter" ? onField(fieldTag) : Task.CompletedTask));
            builder.AddEventStopPropagationAttribute(sequence++, "onkeydown", true);
        }

        RenderChildren(builder, element, highlight, labels, receiver, onField, ref sequence);
        builder.CloseElement();
    }
}
