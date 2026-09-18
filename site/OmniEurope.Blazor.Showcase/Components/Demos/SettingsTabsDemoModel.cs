namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The settings the tabbed settings page edits.
/// </summary>
public sealed class SettingsTabsDemoModel
{
    /// <summary>Where the builds drop their packages.</summary>
    public string Folder { get; set; } = @"D:\Builds\artefacts";

    /// <summary>Whether the agent starts with the session.</summary>
    public bool StartWithSystem { get; set; } = true;

    /// <summary>The interface language.</summary>
    public string Language { get; set; } = "fr";

    /// <summary>How the mode is chosen.</summary>
    public string Mode { get; set; } = "system";

    /// <summary>Whether the interface is compact.</summary>
    public bool Compact { get; set; }

    /// <summary>Whether each failed run is notified.</summary>
    public bool Failures { get; set; } = true;

    /// <summary>Where the daily digest goes.</summary>
    public string DigestAddress { get; set; } = "equipe@exemple.fr";
}
