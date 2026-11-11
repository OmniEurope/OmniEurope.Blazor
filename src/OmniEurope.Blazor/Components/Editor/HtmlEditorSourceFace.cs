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

    private readonly HtmlEditorPreview _preview = new();
    private const int QuietMilliseconds = 150;

    /// <summary>The markup under the text area: the value sanitised, again only when it or the policy changed.</summary>
    internal string PreviewOf(string? value, OmniHtmlSanitizerPolicy? policy, bool isSanitized) => _preview.Of(value, policy, isSanitized);

    /// <summary>Has the script raise one input event, with the final text, once the typing paused (see <c>holdInputBursts</c>); idempotent per text area.</summary>
    internal async Task WatchInputAsync()
    {
        // A locked editor takes no input: nothing to keep from the page.
        if (owner.IsLocked)
        {
            return;
        }

        await using var module = await OmniScriptModule.ImportAsync(javaScript, InteropModulePath);
        await module.InvokeVoidTrackedAsync("holdInputBursts", owner.SourceElement, QuietMilliseconds);
    }

    /// <summary>Commits the text a burst of input left held in the script, so whatever acts next acts on what was typed.</summary>
    internal async Task FlushInputAsync()
    {
        if (owner.CurrentMode != OmniHtmlEditorMode.Source || owner.IsLocked)
        {
            return;
        }

        await using var module = await OmniScriptModule.ImportAsync(javaScript, InteropModulePath);
        if (await module.InvokeTrackedAsync<string?>("takeHeldInput", owner.SourceElement) is { } text)
        {
            await owner.CommitAsync(owner.Clean(text));
        }
    }

    private const string TableSource =
        "<table><tbody><tr><td></td><td></td><td></td></tr><tr><td></td><td></td><td></td></tr><tr><td></td><td></td><td></td></tr></tbody></table>";

    /// <summary>The tags each plain action writes around the selection.</summary>
    private static readonly Dictionary<OmniHtmlEditorAction, (string Prefix, string Suffix)> Wraps = new()
    {
        [OmniHtmlEditorAction.Bold] = ("<strong>", "</strong>"),
        [OmniHtmlEditorAction.Italic] = ("<em>", "</em>"),
        [OmniHtmlEditorAction.Underline] = ("<u>", "</u>"),
        [OmniHtmlEditorAction.Strikethrough] = ("<s>", "</s>"),
        [OmniHtmlEditorAction.Highlight] = ("<mark>", "</mark>"),
        [OmniHtmlEditorAction.Subscript] = ("<sub>", "</sub>"),
        [OmniHtmlEditorAction.Superscript] = ("<sup>", "</sup>"),
        [OmniHtmlEditorAction.InlineCode] = ("<code>", "</code>"),
        [OmniHtmlEditorAction.CodeBlock] = ("<pre>", "</pre>"),
        [OmniHtmlEditorAction.Quote] = ("<blockquote>", "</blockquote>"),
        [OmniHtmlEditorAction.Indent] = ("<blockquote>", "</blockquote>"),
        [OmniHtmlEditorAction.BulletList] = ("<ul><li>", "</li></ul>"),
        [OmniHtmlEditorAction.NumberedList] = ("<ol><li>", "</li></ol>"),
        [OmniHtmlEditorAction.AlignLeft] = ("<p>", "</p>"),
        [OmniHtmlEditorAction.AlignCenter] = ("<p class=\"omni-align-center\">", "</p>"),
        [OmniHtmlEditorAction.AlignRight] = ("<p class=\"omni-align-end\">", "</p>"),
        [OmniHtmlEditorAction.AlignJustify] = ("<p class=\"omni-align-justify\">", "</p>"),
        [OmniHtmlEditorAction.InsertTable] = (TableSource, string.Empty),
    };

    /// <summary>Runs a built-in action on the text area; an action the source face cannot write does nothing.</summary>
    internal Task ExecuteAsync(OmniHtmlEditorAction action, string? argument)
    {
        if (Wraps.TryGetValue(action, out var wrap))
        {
            return WrapSelectionAsync(wrap.Prefix, wrap.Suffix);
        }

        return action switch
        {
            OmniHtmlEditorAction.Outdent => ApplyAsync(Unquote),
            OmniHtmlEditorAction.BlockFormat => WrapBlockAsync(argument),
            OmniHtmlEditorAction.FontSize when argument is "small" or "large" or "xlarge" =>
                WrapSelectionAsync($"<span class=\"omni-font-size-{argument}\">", "</span>"),
            OmniHtmlEditorAction.Link when !string.IsNullOrEmpty(argument) =>
                WrapSelectionAsync($"<a href=\"{WebUtility.HtmlEncode(argument)}\">", "</a>"),
            _ => Task.CompletedTask
        };
    }

    /// <summary>A block format the toolbar offers, else a paragraph.</summary>
    private Task WrapBlockAsync(string? argument)
    {
        var tag = HtmlEditorToolbar.BlockFormats.Any(format => format.Value == argument) ? argument! : "p";
        return WrapSelectionAsync($"<{tag}>", $"</{tag}>");
    }

    /// <summary>The text without the quote that wraps all of it, or unchanged.</summary>
    private static string Unquote(string value) =>
        value.StartsWith("<blockquote>", StringComparison.OrdinalIgnoreCase) && value.EndsWith("</blockquote>", StringComparison.OrdinalIgnoreCase)
            ? value[12..^13]
            : value;

    /// <summary>
    /// Writes the prefix and the suffix around the selection of the text area, commits the sanitised
    /// text, then selects again what was selected once the editor has rendered it. Its callers
    /// (RunBuiltInAsync, InsertHtmlAsync and the character panel through RunBuiltInAsync) refuse a locked editor.
    /// </summary>
    internal async Task WrapSelectionAsync(string prefix, string suffix)
    {
        await FlushInputAsync();
        await using var module = await OmniScriptModule.ImportAsync(javaScript, InteropModulePath);
        var result = await module.InvokeTrackedAsync<JsonElement>("wrapTextSelection", owner.SourceElement, prefix, suffix);
        var value = result.GetProperty("value").GetString() ?? string.Empty;
        var start = result.GetProperty("selectionStart").GetInt32();
        var end = result.GetProperty("selectionEnd").GetInt32();
        await owner.CommitAsync(owner.Clean(value));
        await owner.RenderAsync();
        await module.InvokeVoidTrackedAsync("restoreTextSelection", owner.SourceElement, start, end);
    }

    // Reached through ExecuteAsync only, which OmniHtmlEditor.RunBuiltInAsync calls when it is not locked.
    private Task ApplyAsync(Func<string, string> transform) => owner.CommitAsync(owner.Clean(transform(owner.CurrentHtml)));
}
