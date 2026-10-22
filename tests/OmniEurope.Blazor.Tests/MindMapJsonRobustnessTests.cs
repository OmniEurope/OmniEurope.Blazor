using System.Text.Json;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What a stored graph can hold that the format does not promise: a number no double can hold, a node
/// count no map could draw. Reading refuses it with the documented <see cref="JsonException"/>, never
/// with another exception type and never by building a document that breaks the layout.
/// </summary>
public sealed class MindMapJsonRobustnessTests
{
    [Theory]
    [InlineData("""{"nodes":[{"id":"a","x":1e999}]}""")]
    [InlineData("""{"nodes":[{"id":"a","y":-1e999}]}""")]
    [InlineData("""{"nodes":[{"id":"a","fontSize":1e999}]}""")]
    [InlineData("""{"nodes":[{"id":"a","nodeWidth":-1e999}]}""")]
    public void ANumberNoDoubleCanHold_IsRefusedWithAJsonException(string json)
    {
        Assert.IsAssignableFrom<JsonException>(Record.Exception(() => OmniMindMapDocument.FromJson(json)));
    }

    [Fact]
    public void AFontSizeFarOutsideTheIntRange_FallsBackToTheDefault()
    {
        var node = OmniMindMapDocument.FromJson("""{"nodes":[{"id":"a","fontSize":1e30}]}""").Nodes.Single();

        Assert.Equal(OmniMindMapNode.DefaultFontSize, node.FontSize);
    }

    [Fact]
    public void ASizeFarOutsideTheIntRange_FallsBackToAutomatic()
    {
        var node = OmniMindMapDocument.FromJson("""{"nodes":[{"id":"a","nodeWidth":1e30,"nodeHeight":-1e30}]}""").Nodes.Single();

        Assert.Equal(0, node.Width);
        Assert.Equal(0, node.Height);
    }

    [Fact]
    public void AnythingElseThatIsNotJson_StaysAJsonException()
    {
        Assert.IsAssignableFrom<JsonException>(Record.Exception(() => OmniMindMapDocument.FromJson("{")));
        Assert.IsAssignableFrom<JsonException>(Record.Exception(() => OmniMindMapDocument.FromJson("[1]")));
        Assert.IsAssignableFrom<JsonException>(Record.Exception(() => OmniMindMapDocument.FromJson(string.Concat(Enumerable.Repeat("[", 200)))));
    }

    [Fact]
    public void MoreNodesThanTheCap_AreRefused()
    {
        var nodes = string.Join(',', Enumerable.Range(0, MindMapJson.MaximumItems + 1).Select(index => $$"""{"id":"n{{index}}"}"""));

        var failure = Record.Exception(() => OmniMindMapDocument.FromJson($$"""{"nodes":[{{nodes}}]}"""));

        var json = Assert.IsAssignableFrom<JsonException>(failure);
        Assert.Contains("nodes", json.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MoreEdgesOrNotesThanTheCap_AreRefused()
    {
        var edges = string.Join(',', Enumerable.Repeat("""{"from":"a","to":"b"}""", MindMapJson.MaximumItems + 1));
        var notes = string.Join(',', Enumerable.Repeat("""{"attachedTo":"a","text":"t"}""", MindMapJson.MaximumItems + 1));

        Assert.IsAssignableFrom<JsonException>(Record.Exception(() => OmniMindMapDocument.FromJson($$"""{"edges":[{{edges}}]}""")));
        Assert.IsAssignableFrom<JsonException>(Record.Exception(() => OmniMindMapDocument.FromJson($$"""{"notes":[{{notes}}]}""")));
    }

    [Fact]
    public void AMapAtTheCap_IsRead()
    {
        var nodes = string.Join(',', Enumerable.Range(0, MindMapJson.MaximumItems).Select(index => $$"""{"id":"n{{index}}"}"""));

        Assert.Equal(MindMapJson.MaximumItems, OmniMindMapDocument.FromJson($$"""{"nodes":[{{nodes}}]}""").Nodes.Count);
    }
}
