using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Editor;

/// <summary>A code editor: language, line numbers, wrapping, links reported to the page, height, and the text it binds.</summary>
public partial class CodeEditorExample
{
    private const string NoLink = "No link followed: Ctrl+click a pipeline name.";

    private static readonly IReadOnlyList<OmniCodeEditorLink> PipelineLinks =
    [
        new("pipeline", "^\\s*(?:-\\s*)?pipeline:\\s*[\"']?([\\w-]+)")
    ];

    /// <summary>The language of the highlighting, such as <c>yaml</c> or <c>csharp</c>.</summary>
    [Parameter]
    public string Language { get; set; } = "yaml";

    /// <summary>The code the editor binds.</summary>
    [Parameter]
    public string Code { get; set; } = CodeSamples.Of("yaml");

    /// <summary>Raised with the code while typing.</summary>
    [Parameter]
    public EventCallback<string> CodeChanged { get; set; }

    /// <summary>The code can be read and selected but not changed.</summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>Shows the gutter of line numbers.</summary>
    [Parameter]
    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Wraps long lines instead of scrolling.</summary>
    [Parameter]
    public bool WordWrap { get; set; }

    /// <summary>Makes the name after <c>pipeline:</c> a Ctrl+click link, reported to the page.</summary>
    [Parameter]
    public bool ShowLinks { get; set; } = true;

    /// <summary>The height of the editor, in rem.</summary>
    [Parameter]
    public int HeightRem { get; set; } = 20;

    private string Height => $"{HeightRem}rem";

    private string LinkState { get; set; } = NoLink;

    private IReadOnlyList<OmniCodeEditorLink> ActiveLinks => ShowLinks ? PipelineLinks : [];

    private Task NotifyCodeAsync() => CodeChanged.InvokeAsync(Code);

    private void FollowLink(OmniCodeEditorLinkEventArgs link) =>
        LinkState = string.Format(CultureInfo.CurrentCulture, "Link followed: {0}, line {1}.", link.Target, link.LineNumber);

    private async Task ResetCodeAsync()
    {
        Code = CodeSamples.Of(Language);
        await CodeChanged.InvokeAsync(Code);
    }
}
