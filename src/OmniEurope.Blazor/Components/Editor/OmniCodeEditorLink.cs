namespace OmniEurope.Blazor.Components;

/// <summary>
/// Turns matches of a pattern into Ctrl+click links of <see cref="OmniCodeEditor"/>, reported through
/// <see cref="OmniCodeEditor.LinkActivated"/> rather than opened.
/// </summary>
/// <param name="Name">What the link points to, passed back with the activation.</param>
/// <param name="Pattern">
/// A JavaScript regular expression applied to each line. Its first group is the link and its target;
/// without a group, the whole match is. For example <c>^\s*(?:-\s*)?pipeline:\s*["']?([\w-]+)</c>
/// links the name that follows <c>pipeline:</c> in a YAML file.
/// </param>
public sealed record OmniCodeEditorLink(string Name, string Pattern)
{
    /// <summary>The text shown when the link is hovered. Null uses the localized Ctrl+click hint.</summary>
    public string? Tooltip { get; init; }
}
