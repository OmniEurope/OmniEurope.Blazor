namespace OmniEurope.Blazor.Components;

public sealed record OmniHtmlEditorTool(string Name, string Label, Func<string, string> Transform);
