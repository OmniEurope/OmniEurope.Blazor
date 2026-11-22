using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A command in a code block with its copy button, and the outcome of the last copy.</summary>
public partial class CodeBlockExample
{
    /// <summary>Shows the modified command instead of the original.</summary>
    [Parameter]
    public bool Changed { get; set; } = true;

    private bool? LastCopy { get; set; }

    private string Code => CodeReadingSamples.Command(Changed);

    private OmniTone CopyTone => CodeReadingSamples.CopyTone(LastCopy);

    private string CopyText => CodeReadingSamples.CopyText(LastCopy);

    /// <summary>OnCopy reports whether the clipboard accepted the text.</summary>
    private void OnCopied(bool accepted) => LastCopy = accepted;
}
