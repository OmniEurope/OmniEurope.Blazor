namespace OmniEurope.Blazor.Components;

public partial class OmniAxisTitle
{
    [Parameter, EditorRequired] public string Text { get; set; } = string.Empty;
    [Parameter] public bool Vertical { get; set; }
    private string X => Vertical ? "50" : "50";
    private string Y => Vertical ? "-1" : "100";
    private string? Transform => Vertical ? "rotate(-90 50 50)" : null;
}
