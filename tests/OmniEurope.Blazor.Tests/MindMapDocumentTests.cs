using System.Text.Json;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The stored graph format of the Pronoia mind map editor, read and written back by
/// <see cref="OmniMindMapDocument"/>.
/// </summary>
/// <remarks>
/// The fixtures under <c>TestData/pronoia</c> are copies of Pronoia's seed files
/// (<c>deploy/seed/data/alice/mindmaps.json</c> and <c>bob/mindmaps.json</c>, commit 130bb24): each
/// entry's <c>dataJson</c> is a graph exactly as that editor stored it.
/// </remarks>
public sealed class MindMapDocumentTests
{
    public static TheoryData<string, int> SeedGraphs()
    {
        var data = new TheoryData<string, int>();
        foreach (var file in new[] { "alice-mindmaps.json", "bob-mindmaps.json" })
        {
            using var seed = JsonDocument.Parse(File.ReadAllText(SeedPath(file)));
            for (var index = 0; index < seed.RootElement.GetArrayLength(); index++)
            {
                data.Add(file, index);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(SeedGraphs))]
    public void RealSeedGraph_RoundTripsByteForByte(string file, int index)
    {
        var stored = StoredGraph(file, index);

        var document = OmniMindMapDocument.FromJson(stored);
        var written = document.ToJson();

        Assert.NotEmpty(document.Nodes);
        Assert.Equal(stored, written);
        using var before = JsonDocument.Parse(stored);
        using var after = JsonDocument.Parse(written);
        AssertSameJson(before.RootElement, after.RootElement, "$");
    }

    [Fact]
    public void SeedGraphs_AreThreeRealGraphs()
    {
        var graphs = SeedGraphs().Count;

        Assert.Equal(3, graphs);
    }

    [Fact]
    public void RealSeedGraph_ReadsEveryStoredValue()
    {
        var document = OmniMindMapDocument.FromJson(StoredGraph("alice-mindmaps.json", 0));

        Assert.Equal("node_1", document.RootId);
        var root = document.Nodes[0];
        Assert.Equal("node_1", root.Id);
        Assert.Equal("Déménagement", root.Label);
        Assert.Equal(OmniMindMapGroups.Root, root.Group);
        Assert.Equal(20, root.FontSize);
        Assert.True(root.Bold);
        Assert.False(root.Italic);
        Assert.Contains(document.Nodes, node => node is { Label: "Assurance habitation", Italic: true, Group: OmniMindMapGroups.Blue, X: -460, Y: -60 });
        Assert.All(document.Edges, edge => Assert.Contains(document.Nodes, node => node.Id == edge.From));
    }

    [Fact]
    public void UnknownProperties_SurviveTheRoundTripAtEveryLevel()
    {
        const string stored = """{"rootId":"a","nodes":[{"id":"a","label":"A","group":"teal","x":1.5,"y":-2,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0,"collapsed":true,"meta":{"k":[1,2]}}],"edges":[{"from":"a","to":"b","kind":"dashed"}],"notes":[{"attachedTo":"a","text":"n","pinned":1}],"version":3,"owner":null}""";

        var document = OmniMindMapDocument.FromJson(stored);
        var written = document.ToJson();

        Assert.Equal(stored, written);
        Assert.True(document.Nodes[0].AdditionalProperties["collapsed"].GetBoolean());
        Assert.Equal("dashed", document.Edges[0].AdditionalProperties["kind"].GetString());
        Assert.Equal(3, document.AdditionalProperties["version"].GetInt32());
    }

    [Fact]
    public void MissingValues_TakeTheOriginalEditorDefaults_AndUnidentifiedEntriesAreDropped()
    {
        const string stored = """{"nodes":[{"id":"a"},{"label":"no id"},{"id":7}],"edges":[{"from":"a"},{"from":"a","to":"a"}],"notes":[{"text":"orphan"},{"attachedTo":"a"}]}""";

        var document = OmniMindMapDocument.FromJson(stored);

        Assert.Null(document.RootId);
        var node = Assert.Single(document.Nodes);
        Assert.Equal(string.Empty, node.Label);
        Assert.Equal(OmniMindMapGroups.Root, node.Group);
        Assert.Equal(OmniMindMapNode.DefaultFontSize, node.FontSize);
        Assert.Equal(0, node.X);
        Assert.Single(document.Edges);
        var note = Assert.Single(document.Notes);
        Assert.Equal(string.Empty, note.Text);
        Assert.StartsWith("""{"rootId":null,"nodes":[{"id":"a","label":"","group":"root","x":0,"y":0,"fontSize":14""", document.ToJson(), StringComparison.Ordinal);
    }

    [Fact]
    public void NonObjectText_IsRejected()
    {
        Assert.Throws<JsonException>(() => OmniMindMapDocument.FromJson("[]"));
        Assert.ThrowsAny<JsonException>(() => OmniMindMapDocument.FromJson("{"));
    }

    [Fact]
    public void EmptyDocument_WritesTheFourMembersOfTheFormat()
    {
        Assert.Equal("""{"rootId":null,"nodes":[],"edges":[],"notes":[]}""", OmniMindMapDocument.Empty.ToJson());
    }

    private static string StoredGraph(string file, int index)
    {
        using var seed = JsonDocument.Parse(File.ReadAllText(SeedPath(file)));
        return seed.RootElement[index].GetProperty("dataJson").GetString()!;
    }

    private static string SeedPath(string file)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "tests", "OmniEurope.Blazor.Tests", "TestData", "pronoia", file);
    }

    /// <summary>
    /// Structural equality that also holds the property order and the literal text of every number,
    /// which a plain value comparison would let drift.
    /// </summary>
    private static void AssertSameJson(JsonElement expected, JsonElement actual, string path)
    {
        Assert.True(expected.ValueKind == actual.ValueKind, $"{path}: {expected.ValueKind} became {actual.ValueKind}");
        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProperties = expected.EnumerateObject().ToArray();
                var actualProperties = actual.EnumerateObject().ToArray();
                Assert.Equal(expectedProperties.Select(property => property.Name), actualProperties.Select(property => property.Name));
                for (var index = 0; index < expectedProperties.Length; index++)
                {
                    AssertSameJson(expectedProperties[index].Value, actualProperties[index].Value, $"{path}.{expectedProperties[index].Name}");
                }

                break;
            case JsonValueKind.Array:
                Assert.Equal(expected.GetArrayLength(), actual.GetArrayLength());
                for (var index = 0; index < expected.GetArrayLength(); index++)
                {
                    AssertSameJson(expected[index], actual[index], $"{path}[{index}]");
                }

                break;
            case JsonValueKind.String:
                Assert.Equal(expected.GetString(), actual.GetString());
                break;
            default:
                Assert.Equal(expected.GetRawText(), actual.GetRawText());
                break;
        }
    }
}
