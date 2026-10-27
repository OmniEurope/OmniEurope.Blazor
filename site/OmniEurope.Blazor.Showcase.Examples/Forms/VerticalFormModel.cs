namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>The pipeline the vertical form describes, one property per field.</summary>
public sealed class VerticalFormModel
{
    /// <summary>The pipeline name, required.</summary>
    public string Name { get; set; } = "orders-deploy-prod";

    /// <summary>The notification address, required; it starts without a domain so that the error shows.</summary>
    public string Email { get; set; } = "team@example";

    /// <summary>The access token.</summary>
    public string Token { get; set; } = "secret-token";

    /// <summary>The number of replicas, between 1 and 12.</summary>
    public int Replicas { get; set; } = 3;

    /// <summary>What the pipeline does.</summary>
    public string Description { get; set; } = "Builds, tests and deploys the orders service.";

    /// <summary>Whether the restore is locked.</summary>
    public bool LockedRestore { get; set; } = true;

    /// <summary>Whether the symbols are published.</summary>
    public bool Symbols { get; set; }

    /// <summary>Whether the package is signed, imposed and therefore disabled.</summary>
    public bool Signing { get; set; } = true;

    /// <summary>Whether a notification is sent at the end.</summary>
    public bool Notify { get; set; }

    /// <summary>Whether the deployment is automatic, disabled.</summary>
    public bool AutoDeploy { get; set; }

    /// <summary>The target server, fixed by the environment and disabled.</summary>
    public string Server { get; set; } = "srv-01";

    /// <summary>The pipeline identifier, read-only.</summary>
    public string Identifier { get; set; } = "pl-2f81c0";
}
