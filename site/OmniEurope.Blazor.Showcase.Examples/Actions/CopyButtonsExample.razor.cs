using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Actions;

/// <summary>Two copy buttons: one with a fixed text, one computing its text at the click.</summary>
public partial class CopyButtonsExample
{
    /// <summary>The text the first button copies; the second one adds the moment of the click.</summary>
    [Parameter]
    public string Text { get; set; } = "# Error detail\n\n- code: `E-42`\n- when: 2026-10-23T09:30:00Z\n";

    private int Copies { get; set; }

    private Task<string> BuildTextAsync() => Task.FromResult($"{Text}- computed at the click: {DateTimeOffset.UtcNow:O}\n");

    private void OnCopied(bool accepted)
    {
        if (accepted)
        {
            Copies++;
        }
    }
}
