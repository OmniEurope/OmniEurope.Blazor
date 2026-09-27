namespace OmniEurope.Blazor.Components;

/// <summary>The content of an <see cref="OmniHtmlEditor"/> on each side of the caret (<see cref="OmniHtmlEditorCommandContext.GetHtmlAroundCaretAsync"/>).</summary>
/// <param name="Before">The sanitised HTML from the start of the value up to the caret.</param>
/// <param name="After">The sanitised HTML from the caret to the end of the value.</param>
public sealed record OmniHtmlCaretSplit(string Before, string After);
