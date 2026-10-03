namespace OmniEurope.Blazor.Components;

/// <summary>
/// A block of the visual face given to an <see cref="OmniHtmlEditorProofreader"/>: the text of a paragraph, a heading,
/// a list item or a table cell, its inline markup read through (a bold word is part of its sentence), the elements that
/// are not editable left out.
/// </summary>
/// <param name="Text">The block's text, as the reader sees it.</param>
/// <param name="Language">
/// The language of the block: the <c>lang</c> attribute of its nearest element that has one, inside the surface or
/// around the editor, then the page's; null when none says.
/// </param>
public sealed record OmniHtmlEditorProofreadingText(string Text, string? Language);
