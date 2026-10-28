using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A read-only code viewer with numbered lines, a highlighted line and the outcome of the last copy.</summary>
public partial class CodeViewerExample
{
    /// <summary>Shows the modified command instead of the original.</summary>
    [Parameter]
    public bool Changed { get; set; } = true;

    /// <summary>Numbers the lines from line 120, as an excerpt of a file is; the highlighted line counts the same way.</summary>
    [Parameter]
    public bool Excerpt { get; set; }

    private bool? LastCopy { get; set; }

    private string Code => CodeReadingSamples.Command(Changed);

    private int FirstLineNumber => Excerpt ? CodeReadingSamples.ExcerptFirstLine : 1;

    private IReadOnlyList<int> HighlightedLines => [FirstLineNumber];

    private OmniTone CopyTone => LastCopy switch
    {
        true => OmniTone.Success,
        false => OmniTone.Danger,
        _ => OmniTone.Neutral
    };

    private string CopyText => LastCopy switch
    {
        true => "accepted by the clipboard",
        false => "refused by the clipboard",
        _ => "none"
    };

    /// <summary>OnCopy reports whether the clipboard accepted the text.</summary>
    private void OnCopied(bool accepted) => LastCopy = accepted;
}
