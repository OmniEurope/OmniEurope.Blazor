namespace OmniEurope.Blazor.Components;

/// <summary>What edits the code of <see cref="OmniCodeEditor"/>.</summary>
public enum OmniCodeEditorEngine
{
    /// <summary>
    /// Monaco, served by the host from <see cref="OmniCodeEditor.MonacoPath"/>. It needs the page policy
    /// to allow <c>style-src 'unsafe-inline'</c>; if its files cannot be loaded, the plain text area is kept.
    /// </summary>
    Monaco,

    /// <summary>A plain text area: no script beyond the package, valid under the strict policy.</summary>
    PlainText
}
