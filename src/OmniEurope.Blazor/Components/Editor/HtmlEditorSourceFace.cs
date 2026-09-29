using System.Net;
using System.Text.Json;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The source face of an <see cref="OmniHtmlEditor"/>: its built-in actions written as tags around the
/// selection of the text area, through <c>wrapTextSelection</c> and <c>restoreTextSelection</c> of the
/// interop module. Every result goes through the editor's sanitiser and history.
/// </summary>
internal sealed class HtmlEditorSourceFace(OmniHtmlEditor owner, IJSRuntime javaScript)
{
    private const string InteropModulePath = OmniModules.Interop;

    private const string TableSource =
        "<table><tbody><tr><td></td><td></td><td></td></tr><tr><td></td><td></td><td></td></tr><tr><td></td><td></td><td></td></tr></tbody></table>";

    /// <summary>Runs a built-in action on the text area; an action the source face cannot write does nothing.</summary>
    internal Task ExecuteAsync(OmniHtmlEditorAction action, string? argument)
    {
        switch (action)
        {
            case OmniHtmlEditorAction.Bold: return WrapSelectionAsync("<strong>", "</strong>");
            case OmniHtmlEditorAction.Italic: return WrapSelectionAsync("<em>", "</em>");
            case OmniHtmlEditorAction.Underline: return WrapSelectionAsync("<u>", "</u>");
            case OmniHtmlEditorAction.Strikethrough: return WrapSelectionAsync("<s>", "</s>");
            case OmniHtmlEditorAction.Highlight: return WrapSelectionAsync("<mark>", "</mark>");
            case OmniHtmlEditorAction.Subscript: return WrapSelectionAsync("<sub>", "</sub>");
            case OmniHtmlEditorAction.Superscript: return WrapSelectionAsync("<sup>", "</sup>");
            case OmniHtmlEditorAction.InlineCode: return WrapSelectionAsync("<code>", "</code>");
            case OmniHtmlEditorAction.CodeBlock: return WrapSelectionAsync("<pre>", "</pre>");
            case OmniHtmlEditorAction.Quote:
            case OmniHtmlEditorAction.Indent: return WrapSelectionAsync("<blockquote>", "</blockquote>");
            case OmniHtmlEditorAction.Outdent: return ApplyAsync(value => value.StartsWith("<blockquote>", StringComparison.OrdinalIgnoreCase) && value.EndsWith("</blockquote>", StringComparison.OrdinalIgnoreCase) ? value[12..^13] : value);
            case OmniHtmlEditorAction.BulletList: return WrapSelectionAsync("<ul><li>", "</li></ul>");
            case OmniHtmlEditorAction.NumberedList: return WrapSelectionAsync("<ol><li>", "</li></ol>");
            case OmniHtmlEditorAction.BlockFormat:
                var tag = HtmlEditorToolbar.BlockFormats.Any(format => format.Value == argument) ? argument! : "p";
                return WrapSelectionAsync($"<{tag}>", $"</{tag}>");
            case OmniHtmlEditorAction.FontSize:
                return argument is "small" or "large" or "xlarge"
                    ? WrapSelectionAsync($"<span class=\"omni-font-size-{argument}\">", "</span>")
                    : Task.CompletedTask;
            case OmniHtmlEditorAction.AlignLeft: return WrapSelectionAsync("<p>", "</p>");
            case OmniHtmlEditorAction.AlignCenter: return WrapSelectionAsync("<p class=\"omni-align-center\">", "</p>");
            case OmniHtmlEditorAction.AlignRight: return WrapSelectionAsync("<p class=\"omni-align-end\">", "</p>");
            case OmniHtmlEditorAction.AlignJustify: return WrapSelectionAsync("<p class=\"omni-align-justify\">", "</p>");
            case OmniHtmlEditorAction.InsertTable: return WrapSelectionAsync(TableSource, string.Empty);
            case OmniHtmlEditorAction.Link when !string.IsNullOrEmpty(argument):
                return WrapSelectionAsync($"<a href=\"{WebUtility.HtmlEncode(argument)}\">", "</a>");
            default:
                return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Writes the prefix and the suffix around the selection of the text area, commits the sanitised
    /// text, then selects again what was selected once the editor has rendered it. Nothing while locked.
    /// </summary>
    internal async Task WrapSelectionAsync(string prefix, string suffix)
    {
        if (owner.IsLocked)
        {
            return;
        }

        await using var module = await javaScript.InvokeAsync<IJSObjectReference>("import", InteropModulePath);
        var result = await module.InvokeAsync<JsonElement>("wrapTextSelection", owner.SourceElement, prefix, suffix);
        var value = result.GetProperty("value").GetString() ?? string.Empty;
        var start = result.GetProperty("selectionStart").GetInt32();
        var end = result.GetProperty("selectionEnd").GetInt32();
        await owner.CommitAsync(owner.Clean(value));
        await owner.RenderAsync();
        await module.InvokeVoidAsync("restoreTextSelection", owner.SourceElement, start, end);
    }

    private Task ApplyAsync(Func<string, string> transform) => owner.IsLocked
        ? Task.CompletedTask
        : owner.CommitAsync(owner.Clean(transform(owner.CurrentHtml)));
}
