namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The rendered size of one label, read by <c>omni-mindmap.js</c> with <c>getBBox</c>. The key is
/// the <c>data-omni-measure</c> value of the text element.
/// </summary>
internal sealed record MindMapMeasurement(string Key, double Width, double Height);
