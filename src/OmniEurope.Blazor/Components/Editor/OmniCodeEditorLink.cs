namespace OmniEurope.Blazor.Components;

/// <summary>
/// Turns matches of a pattern into Ctrl+click links of <see cref="OmniCodeEditor"/>, reported through
/// <see cref="OmniCodeEditor.OnLinkActivate"/> rather than opened.
/// </summary>
/// <param name="Name">What the link points to, passed back with the activation.</param>
/// <param name="Pattern">
/// A regular expression applied to each line. Its first group is the link and its target;
/// without a group, the whole match is. For example <c>^\s*(?:-\s*)?pipeline:\s*["']?([\w-]+)</c>
/// links the name that follows <c>pipeline:</c> in a YAML file. <see cref="OmniCodeEditor"/> reads it as a
/// JavaScript expression (Monaco runs in the browser, with no time limit), <see cref="OmniCodeViewer"/> as a
/// .NET expression (with a time limit of one second per line): write the common subset both read the same way
/// (groups, classes, quantifiers, anchors, alternation, <c>\s</c>, <c>\w</c>, <c>\d</c>), and avoid the syntax of
/// only one (lookbehind, named groups, inline options). A pattern the engine cannot read is ignored by both: its
/// link is not drawn, nothing fails.
/// </param>
public sealed record OmniCodeEditorLink(string Name, string Pattern)
{
    /// <summary>The text shown when the link is hovered. Null uses the localized Ctrl+click hint.</summary>
    public string? Tooltip { get; init; }
}
