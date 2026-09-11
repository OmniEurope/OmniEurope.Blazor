namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class DiagramDemo
{
    /// <summary>
    /// The map starts from the stored graph format, the text a host keeps in its database, so the
    /// demonstration reads it exactly as a real page would.
    /// </summary>
    private const string StoredGraph = """
        {"rootId":"node_1","nodes":[
        {"id":"node_1","label":"Déménagement","group":"root","x":0,"y":0,"fontSize":20,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_2","label":"Administratif","group":"blue","x":-260,"y":-120,"fontSize":16,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_3","label":"Changer d'adresse","group":"blue","x":-480,"y":-190,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_4","label":"Assurance habitation","group":"blue","x":-480,"y":-60,"fontSize":14,"bold":false,"italic":true,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_5","label":"Cartons","group":"green","x":260,"y":-120,"fontSize":16,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_6","label":"Cuisine","group":"green","x":480,"y":-190,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_7","label":"Livres","group":"green","x":480,"y":-60,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_8","label":"Jour J","group":"orange","x":0,"y":170,"fontSize":16,"bold":true,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_9","label":"Réserver le camion","group":"red","x":-200,"y":280,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0},
        {"id":"node_10","label":"Prévenir les voisins","group":"yellow","x":200,"y":280,"fontSize":14,"bold":false,"italic":false,"nodeWidth":0,"nodeHeight":0}],
        "edges":[{"from":"node_1","to":"node_2"},{"from":"node_2","to":"node_3"},{"from":"node_2","to":"node_4"},
        {"from":"node_1","to":"node_5"},{"from":"node_5","to":"node_6"},{"from":"node_5","to":"node_7"},
        {"from":"node_1","to":"node_8"},{"from":"node_8","to":"node_9"},{"from":"node_8","to":"node_10"}],
        "notes":[{"attachedTo":"node_9","text":"Avant le 15"}]}
        """;

    /// <summary>A host renames one action for its own vocabulary; the others keep the library text.</summary>
    private static readonly OmniMindMapLabels Labels = new() { NewNodeLabel = "Nouvelle idée" };

    private OmniMindMapDocument Map { get; set; } = OmniMindMapDocument.FromJson(StoredGraph);

    private OmniMindMapViewState? View { get; set; }

    private bool ReadOnly { get; set; }

    private string Summary =>
        $"{Map.Nodes.Count} nœuds, {Map.Edges.Count} liens, {Map.ToJson().Length} caractères une fois enregistrée.";
}
