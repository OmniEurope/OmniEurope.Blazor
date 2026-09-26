using System.Net;
using System.Runtime.CompilerServices;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal static class OmniHtmlSanitizer
{
    private static readonly string[] AllowedTagNames =
    [
        "p", "br", "div", "strong", "b", "em", "i", "u", "s", "strike", "del", "sub", "sup", "blockquote",
        "ul", "ol", "li", "a", "h1", "h2", "h3", "h4", "h5", "h6", "code", "pre", "span", "hr",
        "table", "thead", "tbody", "tfoot", "tr", "th", "td", "caption"
    ];

    private static readonly string[] AllowedAttributeNames = ["href", "rel", "class", "colspan", "rowspan"];

    /// <summary>
    /// The only classes a value may carry: alignment and text size, which the editor writes as
    /// classes because a style attribute would break the strict CSP. Any other class is dropped,
    /// unless an <see cref="OmniHtmlSanitizerPolicy"/> allows it.
    /// </summary>
    internal static readonly string[] AllowedClassNames =
    [
        "omni-align-left", "omni-align-center", "omni-align-right", "omni-align-justify",
        "omni-font-size-small", "omni-font-size-normal", "omni-font-size-large", "omni-font-size-xlarge"
    ];

    /// <summary>
    /// Presentational containers that are dropped but whose content is kept, so a paste from a word
    /// processor or a web page keeps its text. Anything else that is not allowed goes with its
    /// content: a script, a style element, an embedded document or a form control is never unwrapped.
    /// </summary>
    private static readonly HashSet<string> TransparentTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "font", "section", "article", "header", "footer", "main", "aside", "nav", "address", "center",
        "label", "small", "big", "mark", "abbr", "cite", "dfn", "kbd", "samp", "var", "time", "ins",
        "figure", "figcaption", "dl", "dt", "dd", "q", "bdi", "bdo", "wbr", "o:p"
    };

    /// <summary>Elements no <see cref="OmniHtmlSanitizerPolicy"/> can allow: they run code, load a document or post a form.</summary>
    private static readonly HashSet<string> ForbiddenTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "iframe", "frame", "frameset", "object", "embed", "applet", "base", "link", "meta",
        "noscript", "template", "svg", "math", "form", "input", "button", "textarea", "select", "option"
    };

    /// <summary>Attributes no policy can allow, besides every <c>on*</c> event handler and every namespaced name.</summary>
    private static readonly HashSet<string> ForbiddenAttributeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "style", "srcdoc", "action", "formaction", "http-equiv"
    };

    /// <summary>Attributes whose value is an address, so it is held to the allowed schemes.</summary>
    private static readonly string[] UriAttributeNames = ["href", "src", "cite", "poster", "longdesc"];

    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer(null);
    private static readonly ConditionalWeakTable<OmniHtmlSanitizerPolicy, HtmlSanitizer> PolicySanitizers = new();

    internal static string Sanitize(string? html) => Sanitize(html, null);

    /// <summary>The HTML through the built-in allow-list, widened by <paramref name="policy"/> when there is one.</summary>
    internal static string Sanitize(string? html, OmniHtmlSanitizerPolicy? policy) =>
        string.IsNullOrEmpty(html) ? string.Empty : For(policy).Sanitize(html);

    internal static string SanitizePaste(string? html, string? text) => SanitizePaste(html, text, null);

    /// <summary>
    /// What a paste inserts: the clipboard HTML through the allow-list when there is some, else the
    /// plain text as escaped paragraphs, one per blank-line separated block, with line breaks kept.
    /// </summary>
    internal static string SanitizePaste(string? html, string? text, OmniHtmlSanitizerPolicy? policy)
    {
        if (!string.IsNullOrWhiteSpace(html))
        {
            return Sanitize(html, policy);
        }

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var blocks = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        if (blocks.Length == 1 && !blocks[0].Contains('\n', StringComparison.Ordinal))
        {
            return WebUtility.HtmlEncode(blocks[0]);
        }

        return string.Concat(blocks.Select(block =>
            "<p>" + string.Join("<br>", block.Trim('\n').Split('\n').Select(WebUtility.HtmlEncode)) + "</p>"));
    }

    /// <summary>
    /// Checks a policy before it is used, so a forbidden entry fails where the host set it rather than
    /// being dropped without a word. The sanitiser built from it is kept for the policy's lifetime.
    /// </summary>
    internal static void Validate(OmniHtmlSanitizerPolicy? policy) => _ = For(policy);

    /// <summary>The classes the visual surface keeps while editing, or null when the policy allows every class.</summary>
    internal static IReadOnlyList<string>? ClassesOf(OmniHtmlSanitizerPolicy? policy) =>
        policy?.AllowAnyClass == true ? null : [.. AllowedClassNames, .. (policy?.AdditionalCssClasses ?? []).Select(Normalize)];

    private static HtmlSanitizer For(OmniHtmlSanitizerPolicy? policy) =>
        policy is null ? Sanitizer : PolicySanitizers.GetValue(policy, CreateSanitizer);

    private static HtmlSanitizer CreateSanitizer(OmniHtmlSanitizerPolicy? policy)
    {
        var extraTags = (policy?.AdditionalTags ?? []).Select(Normalize).ToArray();
        var extraAttributes = (policy?.AdditionalAttributes ?? []).Select(Normalize).ToArray();
        var extraClasses = (policy?.AdditionalCssClasses ?? []).Select(Normalize).ToArray();
        var tagAttributes = (policy?.AdditionalTagAttributes ?? new Dictionary<string, IReadOnlyList<string>>())
            .ToDictionary(
                entry => Normalize(entry.Key),
                entry => entry.Value.Select(Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        var forbiddenTag = extraTags.Concat(tagAttributes.Keys).FirstOrDefault(ForbiddenTagNames.Contains);
        if (forbiddenTag is not null)
        {
            throw new ArgumentException($"The element '{forbiddenTag}' can never be allowed by an {nameof(OmniHtmlSanitizerPolicy)}.", nameof(policy));
        }

        var forbiddenAttribute = extraAttributes.Concat(tagAttributes.Values.SelectMany(names => names)).FirstOrDefault(IsForbiddenAttribute);
        if (forbiddenAttribute is not null)
        {
            throw new ArgumentException($"The attribute '{forbiddenAttribute}' can never be allowed by an {nameof(OmniHtmlSanitizerPolicy)}.", nameof(policy));
        }

        var invalidClass = extraClasses.FirstOrDefault(name => name.Any(char.IsWhiteSpace));
        if (invalidClass is not null)
        {
            throw new ArgumentException($"'{invalidClass}' is not a single CSS class name.", nameof(policy));
        }

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(AllowedTagNames);
        sanitizer.AllowedTags.UnionWith(extraTags);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(AllowedAttributeNames);
        sanitizer.AllowedAttributes.UnionWith(extraAttributes);
        sanitizer.AllowedClasses.Clear();
        if (policy?.AllowAnyClass != true)
        {
            // An empty set allows every class in HtmlSanitizer: it is only left empty when the policy says so.
            sanitizer.AllowedClasses.UnionWith(AllowedClassNames);
            sanitizer.AllowedClasses.UnionWith(extraClasses);
        }

        sanitizer.AllowDataAttributes = policy?.AllowDataAttributes == true;
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto", "tel"]);
        sanitizer.UriAttributes.Clear();
        sanitizer.UriAttributes.UnionWith(UriAttributeNames);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedAtRules.Clear();

        // HtmlSanitizer only knows attributes allowed everywhere: one granted to some elements only is
        // allowed everywhere there, then removed below from the elements it was not granted to.
        var globalAttributes = new HashSet<string>(sanitizer.AllowedAttributes, StringComparer.OrdinalIgnoreCase);
        var scopedAttributes = tagAttributes.Values
            .SelectMany(names => names)
            .Where(name => !globalAttributes.Contains(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        sanitizer.AllowedAttributes.UnionWith(scopedAttributes);

        sanitizer.RemovingTag += static (_, eventArgs) =>
        {
            if (eventArgs.Reason == RemoveReason.NotAllowedTag && TransparentTagNames.Contains(eventArgs.Tag.LocalName))
            {
                KeepChildren(eventArgs.Tag);
            }
        };
        sanitizer.PostProcessNode += (_, eventArgs) =>
        {
            if (eventArgs.Node is IHtmlAnchorElement anchor && anchor.HasAttribute("href"))
            {
                anchor.SetAttribute("rel", "noopener noreferrer");
            }

            if (eventArgs.Node is not IElement element)
            {
                return;
            }

            if (element.HasAttribute("class") && string.IsNullOrWhiteSpace(element.GetAttribute("class")))
            {
                element.RemoveAttribute("class");
            }

            if (scopedAttributes.Count > 0)
            {
                tagAttributes.TryGetValue(element.LocalName, out var granted);
                foreach (var name in element.Attributes.Select(attribute => attribute.Name).ToArray())
                {
                    if (scopedAttributes.Contains(name) && granted?.Contains(name) != true)
                    {
                        element.RemoveAttribute(name);
                    }
                }
            }
        };
        return sanitizer;
    }

    private static bool IsForbiddenAttribute(string name) =>
        name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
        || name.Contains(':', StringComparison.Ordinal)
        || ForbiddenAttributeNames.Contains(name);

    private static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException($"An {nameof(OmniHtmlSanitizerPolicy)} entry is empty.", nameof(name));
        }

        return name.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Moves the children of a container about to be removed in front of it. They were collected
    /// with the rest of the document before the removal pass, so they are still sanitised themselves.
    /// </summary>
    private static void KeepChildren(IElement element)
    {
        var parent = element.Parent;
        if (parent is null)
        {
            return;
        }

        while (element.FirstChild is { } child)
        {
            parent.InsertBefore(child, element);
        }
    }
}
