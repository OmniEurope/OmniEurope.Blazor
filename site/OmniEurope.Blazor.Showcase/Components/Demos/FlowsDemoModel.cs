namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>Ce que l'assistant et la page de connexion de la démonstration font saisir.</summary>
public sealed class FlowsDemoModel
{
    public string Name { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    public bool Backup { get; set; }
}
