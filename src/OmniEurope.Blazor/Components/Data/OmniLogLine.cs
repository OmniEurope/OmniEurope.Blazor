namespace OmniEurope.Blazor.Components;

/// <summary>One line of an <see cref="OmniLogViewer"/>.</summary>
/// <param name="Text">The text of the line, shown as written: a message template is rendered by the host.</param>
/// <param name="Level">Its severity; warnings and errors are tinted.</param>
/// <param name="Timestamp">When it was written, shown before the text when present.</param>
public sealed record OmniLogLine(string Text, OmniLogLevel Level = OmniLogLevel.Information, DateTimeOffset? Timestamp = null);
