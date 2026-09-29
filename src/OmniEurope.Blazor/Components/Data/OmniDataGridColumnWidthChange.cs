namespace OmniEurope.Blazor.Components;

/// <summary>Reports the new CSS width of a resized column.</summary>
/// <param name="Key">Key of the resized column.</param>
/// <param name="Width">Its new width as a CSS pixel length such as <c>240px</c>, never below <c>48px</c>.</param>
public sealed record OmniDataGridColumnWidthChange(string Key, string Width);
