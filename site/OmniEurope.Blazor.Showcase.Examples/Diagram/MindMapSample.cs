using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Diagram;

/// <summary>The sample map of the mind map examples, and the labels a host can give the map for its own vocabulary.</summary>
public static class MindMapSample
{
    /// <summary>
    /// A small house-move project (a centre, three branches and a note) in the stored graph format, the text
    /// a host keeps in its database, which the sample reads exactly as a real page would.
    /// </summary>
    private const string StoredGraph = """
        {"rootId":"node_1","nodes":[
        {"id":"node_1","label":"House move","group":"root","x":0,"y":0,"fontSize":20,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_2","label":"Paperwork","group":"blue","x":-260,"y":-120,"fontSize":16,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_3","label":"Change of address","group":"blue","x":-480,"y":-190,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_4","label":"Home insurance","group":"blue","x":-480,"y":-60,"fontSize":14,"bold":false,"italic":true,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_5","label":"Boxes","group":"green","x":260,"y":-120,"fontSize":16,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_6","label":"Kitchen","group":"green","x":480,"y":-190,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_7","label":"Books","group":"green","x":480,"y":-60,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_8","label":"Moving day","group":"orange","x":0,"y":170,"fontSize":16,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_9","label":"Book the van","group":"red","x":-200,"y":280,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_10","label":"Tell the neighbours","group":"yellow","x":200,"y":280,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0}],
        "edges":[{"from":"node_1","to":"node_2"},{"from":"node_2","to":"node_3"},{"from":"node_2","to":"node_4"},
        {"from":"node_1","to":"node_5"},{"from":"node_5","to":"node_6"},{"from":"node_5","to":"node_7"},
        {"from":"node_1","to":"node_8"},{"from":"node_8","to":"node_9"},{"from":"node_8","to":"node_10"}],
        "notes":[{"attachedTo":"node_9","text":"Before the 15th"}]}
        """;

    /// <summary>A host renames three actions for its own vocabulary; the texts left null come from the library.</summary>
    public static OmniMindMapLabels CustomLabels { get; } = new()
    {
        AddNode = "New idea",
        NewNodeLabel = "Idea to name",
        AutoLayout = "Tidy up"
    };

    /// <summary>A fresh copy of the sample map.</summary>
    public static OmniMindMapDocument Create() => OmniMindMapDocument.FromJson(StoredGraph);
}
