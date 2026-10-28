namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>Code viewers with a highlighted line: a whole configuration and an excerpt that keeps its line numbers.</summary>
public partial class CodeViewerExcerptExample
{
    private const string ConfigurationCode = "{\n  \"mode\": \"read\",\n  \"active\": true\n}";

    private const string ExcerptCode = "var total = items.Count;\nvar average = total / count;\nreturn average;";

    private static readonly IReadOnlyList<int> ConfigurationHighlights = [2];

    private static readonly IReadOnlyList<int> ExcerptHighlights = [42];
}
