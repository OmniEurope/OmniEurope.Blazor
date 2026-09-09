namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The state the form demo edits. Kept out of the demo file so the source shown in the gallery
/// stays the markup a reader would copy.
/// </summary>
public sealed class FormDemoModel
{
    /// <summary>The name field.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The free-text field.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>The e-mail field, typed so the browser offers the right keyboard.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The telephone field.</summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>The web address field.</summary>
    public string Website { get; set; } = string.Empty;

    /// <summary>The search field.</summary>
    public string Search { get; set; } = string.Empty;

    /// <summary>The slider value.</summary>
    public double Budget { get; set; } = 40;

    /// <summary>The checkbox state.</summary>
    public bool Accepted { get; set; }

    /// <summary>The switch state.</summary>
    public bool Alerts { get; set; } = true;
}
