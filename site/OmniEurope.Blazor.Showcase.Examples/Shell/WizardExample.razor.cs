using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Shell;

/// <summary>A three-step wizard: a step that waits for its field, a step whose validation may refuse, and a summary.</summary>
public partial class WizardExample
{
    /// <summary>The height of the body of the steps, as a CSS length (<c>20rem</c>, <c>50vh</c>) or <c>auto</c>.</summary>
    [Parameter]
    public string BodyHeight { get; set; } = "20rem";

    private ServerModel Model { get; } = new();

    private int Step { get; set; }

    private bool OptionsRefused { get; set; }

    private bool Finished { get; set; }

    /// <summary>What the wizard ended with; <see langword="null"/> while it is still in progress.</summary>
    private string? Outcome { get; set; }

    /// <summary>The validation of the options step: a backup is required, and the step says so.</summary>
    private Task<bool> ValidateOptionsAsync()
    {
        OptionsRefused = !Model.Backup;
        return Task.FromResult(Model.Backup);
    }

    private void Finish()
    {
        Finished = true;
        Outcome = $"server “{Model.Name}” created";
    }

    private void Reset()
    {
        Model.Name = string.Empty;
        Model.Backup = false;
        OptionsRefused = false;
        Finished = false;
        Step = 0;
        Outcome = "cancelled";
    }

    /// <summary>What the wizard asks for.</summary>
    private sealed class ServerModel
    {
        public string Name { get; set; } = string.Empty;

        public bool Backup { get; set; }
    }
}
