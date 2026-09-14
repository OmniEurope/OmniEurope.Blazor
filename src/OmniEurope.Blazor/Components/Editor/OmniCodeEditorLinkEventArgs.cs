namespace OmniEurope.Blazor.Components;

/// <summary>A link of <see cref="OmniCodeEditor"/> followed with Ctrl+click.</summary>
/// <param name="Name">The <see cref="OmniCodeEditorLink.Name"/> of the pattern that produced the link.</param>
/// <param name="Target">The linked text.</param>
/// <param name="LineNumber">The line of the link, counted from 1.</param>
public sealed record OmniCodeEditorLinkEventArgs(string Name, string Target, int LineNumber);
