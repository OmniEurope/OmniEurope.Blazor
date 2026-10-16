using System.Text.RegularExpressions;
using AngleSharp.Dom;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The blocks of a mail template: the block variables (<see cref="OmniMailRenderOptions.Blocks"/>), whose HTML
/// fragment replaces the paragraph that holds their token alone, and the paragraphs a marker turns into an
/// action button (<c>{{#button name}}</c>) or a banner (<c>{{#banner tone}}</c>).
/// </summary>
/// <remarks>
/// The work is split around the replacement of the text variables, so that no value can make a block: <see cref="Mark"/>
/// runs first, on the template alone, and sets a placeholder where each block variable stands and records each
/// marker's paragraph; <see cref="Build"/> runs after the text variables are written, fills the placeholders with the
/// sanitised fragments (whose text is never read for tokens) and draws the buttons and banners in presentation tables.
/// </remarks>
internal sealed partial class MailBlocks(IReadOnlyDictionary<string, string?> blocks, MailTemplateText text)
{
    /// <summary>Marks an element the blocks drew with its look already written, which the inlining leaves as it is.</summary>
    internal const string Own = "data-omni-mail-own";

    private const string PlaceholderName = "omni-mail-block";
    private static readonly HashSet<string> Replaced = new(StringComparer.Ordinal) { "p", "div", "h1", "h2", "h3", "h4", "h5", "h6" };
    private static readonly HashSet<string> Filled = new(StringComparer.Ordinal) { "li", "td", "th", "blockquote" };
    private static readonly HashSet<string> BlockLevel = new(StringComparer.Ordinal)
    {
        "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "table", "blockquote", "pre", "hr"
    };

    private readonly List<(IElement Placeholder, IElement? Container)> _placeholders = [];
    private readonly List<(IElement Container, string Kind, string? Argument)> _markers = [];

    /// <summary>The names of the variables a template uses, block variables and button addresses included, each once, in order.</summary>
    internal static IReadOnlyList<string> Names(string? template)
    {
        if (template is null)
        {
            return [];
        }

        var tokens = MailTemplateText.Matches(template).Select(match => (match.Index, Name: match.Groups["name"].Value));
        var buttons = Marker().Matches(template)
            .Where(match => match.Groups["kind"].Value.Equals("button", StringComparison.OrdinalIgnoreCase) && match.Groups["arg"].Success)
            .Select(match => (match.Index, Name: match.Groups["arg"].Value));
        return [.. tokens.Concat(buttons).OrderBy(found => found.Index).Select(found => found.Name).Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Sets a placeholder for each block variable and records each marker's paragraph, the marker removed.</summary>
    internal void Mark(IElement body)
    {
        foreach (var node in body.Descendants<IText>().ToArray())
        {
            if (blocks.Count > 0)
            {
                MarkBlockVariables(node, body);
            }
        }

        foreach (var node in body.Descendants<IText>().ToArray())
        {
            var match = Marker().Match(node.Data);
            if (!match.Success)
            {
                continue;
            }

            var container = ContainerOf(node, body) ?? Wrap(node, body);
            node.Data = Marker().Replace(node.Data, string.Empty);
            if (!_markers.Exists(marker => marker.Container == container))
            {
                _markers.Add((container, match.Groups["kind"].Value.ToLowerInvariant(), match.Groups["arg"].Success ? match.Groups["arg"].Value : null));
            }
        }
    }

    /// <summary>Fills the placeholders with the fragments, then draws each recorded button and banner.</summary>
    internal void Build(IElement body, OmniMailTheme theme, string lookAttribute)
    {
        foreach (var (placeholder, container) in _placeholders)
        {
            Fill(placeholder, container, body);
        }

        foreach (var (container, kind, argument) in _markers)
        {
            if (container.Parent is null)
            {
                continue;
            }

            var block = kind == "button"
                ? Button(container, argument, theme, lookAttribute)
                : Banner(container, argument, theme, lookAttribute);
            Put(container, block is null ? [] : [block]);
        }
    }

    private void MarkBlockVariables(IText node, IElement body)
    {
        var data = node.Data;
        var position = 0;
        var parent = node.Parent!;
        foreach (Match match in MailTemplateText.Matches(data))
        {
            var name = match.Groups["name"].Value;
            if (!blocks.ContainsKey(name))
            {
                continue;
            }

            var container = ContainerOf(node, body);
            var alone = container is not null && string.Equals(container.TextContent.Trim(), match.Value, StringComparison.Ordinal);
            parent.InsertBefore(body.Owner!.CreateTextNode(data[position..match.Index]), node);
            var placeholder = body.Owner!.CreateElement(PlaceholderName);
            placeholder.SetAttribute("data-name", name);
            parent.InsertBefore(placeholder, node);
            _placeholders.Add((placeholder, alone ? container : null));
            position = match.Index + match.Length;
        }

        if (position > 0)
        {
            parent.InsertBefore(body.Owner!.CreateTextNode(data[position..]), node);
            parent.RemoveChild(node);
        }
    }

    /// <summary>A placeholder alone in its paragraph takes the paragraph's place with the fragment; one inside a line, the fragment's plain text.</summary>
    private void Fill(IElement placeholder, IElement? container, IElement body)
    {
        var name = placeholder.GetAttribute("data-name")!;
        var value = blocks[name];
        if (value is null)
        {
            text.NoteMissing(name);
        }

        var html = MailBlockSanitizer.Sanitize(value);
        if (container is null)
        {
            var line = MailTemplateText.OneLine(OmniHtmlText.ToPlainText(html));
            placeholder.Parent!.ReplaceChild(body.Owner!.CreateTextNode(line), placeholder);
            return;
        }

        var holder = body.Owner!.CreateElement("div");
        holder.InnerHtml = html;
        Put(container, [.. holder.ChildNodes]);
    }

    /// <summary>
    /// The button of a marked paragraph: its text, on one line, as the label; the address the marker names, kept only
    /// for http, https and mailto (the button losing its link otherwise, as a link does). An empty label draws no button.
    /// </summary>
    private IElement? Button(IElement container, string? argument, OmniMailTheme theme, string lookAttribute)
    {
        var label = MailTemplateText.OneLine(container.TextContent);
        var address = argument is null ? null : text.ValueOf(argument).Trim();
        if (label.Length == 0)
        {
            return null;
        }

        var owner = container.Owner!;
        var table = Element(owner, "table", lookAttribute, "margin:0 0 16px 0;border-collapse:separate",
            ("role", "presentation"), ("cellpadding", "0"), ("cellspacing", "0"), ("border", "0"));
        if (Alignment(container) is { } align)
        {
            table.SetAttribute("align", align);
        }

        var cell = Element(owner, "td", lookAttribute, $"border-radius:{theme.Radius};background-color:{theme.Accent}",
            ("align", "center"), ("bgcolor", theme.Accent));
        var face = $"display:inline-block;padding:12px 24px;font-family:{theme.FontFamily};font-size:16px;font-weight:700;"
            + $"line-height:1.25;color:{theme.OnAccent};text-decoration:none;border:1px solid {theme.Accent};border-radius:{theme.Radius}";
        var link = address is not null && MailDocument.IsSafeAddress(address)
            ? Element(owner, "a", lookAttribute, face, ("href", address), ("target", "_blank"), ("rel", "noopener noreferrer"))
            : Element(owner, "span", lookAttribute, face);
        link.TextContent = label;
        cell.AppendChild(link);
        Nest(owner, table, lookAttribute, cell);
        return table;
    }

    /// <summary>The banner of a marked paragraph: its content, formatting and links kept, on the tone's pale fill with the tone's rule at its start.</summary>
    private static IElement? Banner(IElement container, string? tone, OmniMailTheme theme, string lookAttribute)
    {
        if (container.TextContent.Trim().Length == 0)
        {
            return null;
        }

        var (strong, subtle) = tone?.ToLowerInvariant() switch
        {
            "success" => (theme.Success, theme.SuccessSubtle),
            "warning" => (theme.Warning, theme.WarningSubtle),
            "danger" => (theme.Danger, theme.DangerSubtle),
            _ => (theme.Info, theme.InfoSubtle),
        };
        var owner = container.Owner!;
        var table = Element(owner, "table", lookAttribute, "width:100%;margin:0 0 16px 0;border-collapse:separate",
            ("role", "presentation"), ("width", "100%"), ("cellpadding", "0"), ("cellspacing", "0"), ("border", "0"));
        var alignment = Alignment(container) is { } align ? $";text-align:{align}" : string.Empty;
        var cell = Element(owner, "td", lookAttribute,
            $"padding:12px 16px;background-color:{subtle};border-left:4px solid {strong};border-radius:4px;color:{theme.Text}{alignment}",
            ("bgcolor", subtle));
        while (container.FirstChild is { } child)
        {
            cell.AppendChild(child);
        }

        Nest(owner, table, lookAttribute, cell);
        return table;
    }

    private static void Nest(IDocument owner, IElement table, string lookAttribute, IElement cell)
    {
        var row = Element(owner, "tr", lookAttribute, null);
        row.AppendChild(cell);
        var rows = Element(owner, "tbody", lookAttribute, null);
        rows.AppendChild(row);
        table.AppendChild(rows);
    }

    private static IElement Element(IDocument owner, string name, string lookAttribute, string? look, params (string Name, string Value)[] attributes)
    {
        var element = owner.CreateElement(name);
        element.SetAttribute(Own, string.Empty);
        if (look is not null)
        {
            element.SetAttribute(lookAttribute, look);
        }

        foreach (var (attribute, value) in attributes)
        {
            element.SetAttribute(attribute, value);
        }

        return element;
    }

    /// <summary>The editor's alignment class of the paragraph, as the value of an <c>align</c> attribute.</summary>
    private static string? Alignment(IElement container) =>
        container.ClassList.Contains("omni-align-center") ? "center"
        : container.ClassList.Contains("omni-align-end") ? "right"
        : null;

    /// <summary>Puts the nodes in the paragraph's place, or inside the list item, cell or quotation instead of its content.</summary>
    private static void Put(IElement container, IReadOnlyList<INode> nodes)
    {
        if (Replaced.Contains(container.LocalName))
        {
            foreach (var node in nodes)
            {
                container.Parent!.InsertBefore(node, container);
            }

            container.Parent!.RemoveChild(container);
            return;
        }

        while (container.FirstChild is { } child)
        {
            container.RemoveChild(child);
        }

        foreach (var node in nodes)
        {
            container.AppendChild(node);
        }
    }

    private static IElement? ContainerOf(INode node, IElement body)
    {
        for (var parent = node.ParentElement; parent is not null && parent != body; parent = parent.ParentElement)
        {
            if (Replaced.Contains(parent.LocalName) || Filled.Contains(parent.LocalName))
            {
                return parent;
            }
        }

        return null;
    }

    /// <summary>Text written directly in the body (a template typed by hand): its run of inline nodes is wrapped in a paragraph.</summary>
    private static IElement Wrap(INode node, IElement body)
    {
        var top = node;
        while (top.Parent != body)
        {
            top = top.Parent!;
        }

        var first = top;
        while (first.PreviousSibling is { } previous && IsInline(previous))
        {
            first = previous;
        }

        var paragraph = body.Owner!.CreateElement("p");
        body.InsertBefore(paragraph, first);
        var current = first;
        while (current is not null && (current == top || IsInline(current)))
        {
            var next = current.NextSibling;
            paragraph.AppendChild(current);
            if (current == top)
            {
                top = paragraph;
            }

            current = next;
        }

        return paragraph;
    }

    private static bool IsInline(INode node) => node is not IElement element || !BlockLevel.Contains(element.LocalName);

    [GeneratedRegex(@"\{\{\s*#(?<kind>button|banner)(?:\s+(?<arg>[A-Za-z][A-Za-z0-9_.\-]*))?\s*\}\}", RegexOptions.IgnoreCase)]
    private static partial Regex Marker();
}
