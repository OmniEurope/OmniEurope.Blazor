namespace OmniEurope.Blazor.Showcase.Components.Demos;

/// <summary>
/// The pipeline the vertical form describes, one property per field of the mockup form.
/// </summary>
public sealed class VerticalFormDemoModel
{
    /// <summary>The pipeline name, required.</summary>
    public string Name { get; set; } = "aetheus-deploy-prod";

    /// <summary>The notification address, required; starts without a domain so the error shows.</summary>
    public string Email { get; set; } = "equipe@exemple";

    /// <summary>The access token.</summary>
    public string Token { get; set; } = "motdepasse";

    /// <summary>The number of replicas, between 1 and 12.</summary>
    public int Replicas { get; set; } = 3;

    /// <summary>The target environment.</summary>
    public string Environment { get; set; } = "production";

    /// <summary>The deployment date.</summary>
    public DateOnly? Date { get; set; } = new(2026, 9, 18);

    /// <summary>The deployment time.</summary>
    public TimeOnly? Time { get; set; } = new(14, 30);

    /// <summary>The maintenance window.</summary>
    public DateTime? Moment { get; set; } = new(2026, 9, 18, 22, 0, 0, DateTimeKind.Unspecified);

    /// <summary>What the pipeline does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The deployment strategy, required; none is chosen at first so the error shows.</summary>
    public string? Strategy { get; set; }

    /// <summary>Whether the restore is locked.</summary>
    public bool LockedRestore { get; set; } = true;

    /// <summary>Whether the symbols are published.</summary>
    public bool Symbols { get; set; }

    /// <summary>Whether the package is signed, imposed and therefore disabled.</summary>
    public bool Signing { get; set; } = true;

    /// <summary>Whether a notification is sent at the end.</summary>
    public bool Notify { get; set; }

    /// <summary>Whether deployment is automatic, disabled.</summary>
    public bool AutoDeploy { get; set; }

    /// <summary>The target server, fixed by the environment and disabled.</summary>
    public string Server { get; set; } = "vps2577917";

    /// <summary>The pipeline identifier, read-only.</summary>
    public string Identifier { get; set; } = "pl-2f81c0";

    /// <summary>The build agent, disabled.</summary>
    public string Agent { get; set; } = "shared";
}
