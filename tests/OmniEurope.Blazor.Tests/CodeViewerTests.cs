using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniCodeViewer: the lines it splits, the links its patterns draw, the wrap toggle, the empty state and
/// the side panel.
/// </summary>
public sealed class CodeViewerTests : OmniBunitContext
{
    private static readonly OmniCodeEditorLink FileLink = new("file", @"see (\w+\.cs)");

    private IRenderedComponent<OmniCodeViewer> RenderViewer(string? code, Action<ComponentParameterCollectionBuilder<OmniCodeViewer>>? extra = null) =>
        Render<OmniCodeViewer>(parameters =>
        {
            parameters.Add(viewer => viewer.Code, code);
            extra?.Invoke(parameters);
        });

    private static IReadOnlyList<string> Lines(IRenderedComponent<OmniCodeViewer> viewer) =>
        [.. viewer.FindAll(".omni-code-viewer__text").Select(text => text.TextContent)];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyCode_SaysSo_WithItsDefaultOrOwnText(string? code)
    {
        Assert.Equal("Rien à afficher.", RenderViewer(code).Find(".omni-code-viewer__empty").TextContent);
        Assert.Equal("Vide", RenderViewer(code, parameters => parameters.Add(viewer => viewer.EmptyText, "Vide")).Find(".omni-code-viewer__empty").TextContent);
    }

    [Fact]
    public void Lines_AreSplitOnEveryNewline_AndAFinalNewlineOpensNoEmptyLine()
    {
        Assert.Equal(["a", "b", "c"], Lines(RenderViewer("a\r\nb\nc\n")));
        Assert.Equal(["a", ""], Lines(RenderViewer("a\n\n")));
        Assert.Equal(["", ""], Lines(RenderViewer("\n\n")));
    }

    [Fact]
    public void ChangedCode_IsSplitAgain_AndTheSameCodeIsNot()
    {
        var viewer = RenderViewer("a\nb");
        viewer.Render(parameters => parameters.Add(component => component.Code, "a\nb"));
        Assert.Equal(["a", "b"], Lines(viewer));

        viewer.Render(parameters => parameters.Add(component => component.Code, "c"));
        Assert.Equal(["c"], Lines(viewer));

        viewer.Render(parameters => parameters.Add(component => component.Code, null));
        Assert.Single(viewer.FindAll(".omni-code-viewer__empty"));
    }

    [Fact]
    public void LineNumbers_FollowTheirSwitch()
    {
        Assert.Equal(2, RenderViewer("a\nb").FindAll(".omni-code-viewer__number").Count);
        Assert.Empty(RenderViewer("a\nb", parameters => parameters.Add(viewer => viewer.ShowLineNumbers, false)).FindAll(".omni-code-viewer__number"));
    }

    [Fact]
    public void Title_NamesTheCode_OrTheLocalizedDefault()
    {
        var titled = RenderViewer("a", parameters => parameters.Add(viewer => viewer.Id, "src").Add(viewer => viewer.Title, "Program.cs"));
        Assert.Equal("src-title", titled.Find("section").GetAttribute("aria-labelledby"));
        Assert.Equal("Program.cs", titled.Find("#src-title").TextContent.Trim());

        var untitled = RenderViewer("a");
        var id = untitled.Find("section").GetAttribute("aria-labelledby")!;
        Assert.StartsWith("omni-code-viewer-", id, StringComparison.Ordinal);
        Assert.Equal("Code", untitled.Find($"#{id}").TextContent.Trim());
    }

    [Fact]
    public void Links_DrawTheFirstGroupOfEachMatch_AndReportTheLink()
    {
        OmniCodeEditorLinkEventArgs? followed = null;
        var viewer = RenderViewer("// see Program.cs then see Startup.cs", parameters => parameters
            .Add(viewer => viewer.FirstLineNumber, 40)
            .Add(viewer => viewer.Links, [FileLink])
            .Add(viewer => viewer.LinkActivated, args => followed = args));

        var links = viewer.FindAll(".omni-code-viewer__link");
        Assert.Equal(["Program.cs", "Startup.cs"], links.Select(link => link.TextContent));
        Assert.Equal("Suivre le lien", links[0].GetAttribute("title"));
        Assert.Equal("// see Program.cs then see Startup.cs", viewer.Find(".omni-code-viewer__text").TextContent);

        links[1].Click();
        Assert.Equal(("file", "Startup.cs", 40), (followed!.Name, followed.Target, followed.LineNumber));
    }

    [Fact]
    public void LinkWithoutGroup_TakesTheWholeMatch_AndItsOwnTooltip()
    {
        var viewer = RenderViewer("TODO: rien", parameters => parameters
            .Add(viewer => viewer.Links, [new OmniCodeEditorLink("todo", "TODO") { Tooltip = "Voir la tâche" }]));

        var link = viewer.Find(".omni-code-viewer__link");
        Assert.Equal("TODO", link.TextContent);
        Assert.Equal("Voir la tâche", link.GetAttribute("title"));
    }

    [Fact]
    public void OverlappingMatches_KeepTheEarliest_AndEmptyOrUnmatchedOnesDrawNothing()
    {
        var viewer = RenderViewer("abcdef\nzzz\n\nx", parameters => parameters
            .Add(viewer => viewer.Links,
            [
                new OmniCodeEditorLink("first", "abcd"),
                new OmniCodeEditorLink("second", "cdef"),
                new OmniCodeEditorLink("empty", "(y?)x"),
                new OmniCodeEditorLink("unused", "(q)?z")
            ]));

        Assert.Equal(["abcd", "z", "z", "z"], viewer.FindAll(".omni-code-viewer__link").Select(link => link.TextContent));
        Assert.Equal(["abcdef", "zzz", "", "x"], Lines(viewer));
    }

    [Fact]
    public void SamePatternList_IsNotCompiledAgain_AndANewListIs()
    {
        var links = new[] { FileLink };
        var viewer = RenderViewer("see A.cs", parameters => parameters.Add(viewer => viewer.Links, links));
        viewer.Render(parameters => parameters.Add(component => component.Links, links));
        Assert.Single(viewer.FindAll(".omni-code-viewer__link"));

        viewer.Render(parameters => parameters.Add(component => component.Links, Array.Empty<OmniCodeEditorLink>()));
        Assert.Empty(viewer.FindAll(".omni-code-viewer__link"));
    }

    [Fact]
    public void NullLinks_AreRefused()
    {
        Assert.Throws<ArgumentNullException>(() => RenderViewer("a", parameters => parameters.Add(viewer => viewer.Links, null!)));
    }

    [Fact]
    public void SlowPattern_LeavesTheLineWithoutLinks_InsteadOfFreezing()
    {
        // Nested quantifiers on a line that cannot match: the engine backtracks past the one-second timeout.
        var line = new string('a', 40) + "!";
        var viewer = RenderViewer(line, parameters => parameters
            .Add(viewer => viewer.Links, [new OmniCodeEditorLink("slow", @"^(\w+\s?)*$")]));

        Assert.Empty(viewer.FindAll(".omni-code-viewer__link"));
        Assert.Equal([line], Lines(viewer));
    }

    [Fact]
    public void WrapToggle_SwitchesAndReports_AndTheParameterIsTakenWhenItChanges()
    {
        var reported = new List<bool>();
        var viewer = RenderViewer("a", parameters => parameters.Add(viewer => viewer.WrapChanged, value => reported.Add(value)));
        var toggle = viewer.Find(".omni-code-viewer__wrap");
        Assert.Equal("false", toggle.GetAttribute("aria-pressed"));

        toggle.Click();
        Assert.Contains("omni-code-viewer--wrap", viewer.Find("section").ClassList);
        Assert.Equal("true", viewer.Find(".omni-code-viewer__wrap").GetAttribute("aria-pressed"));
        Assert.Equal([true], reported);

        // The host passing back the value it was told keeps the view wrapped; a new value is taken.
        viewer.Render(parameters => parameters.Add(component => component.Wrap, true));
        Assert.Contains("omni-code-viewer--wrap", viewer.Find("section").ClassList);
        viewer.Render(parameters => parameters.Add(component => component.Wrap, false));
        Assert.DoesNotContain("omni-code-viewer--wrap", viewer.Find("section").ClassList);

        Assert.Empty(RenderViewer("a", parameters => parameters.Add(viewer => viewer.ShowWrapToggle, false)).FindAll(".omni-code-viewer__wrap"));
    }

    [Fact]
    public void SidePanel_IsANamedAsideBesideTheCode()
    {
        var viewer = RenderViewer("a", parameters => parameters
            .Add(viewer => viewer.SidePanel, builder => builder.AddContent(0, "Définitions"))
            .Add(viewer => viewer.SidePanelLabel, "Plan"));

        var panel = viewer.Find("aside.omni-code-viewer__panel");
        Assert.Equal("Plan", panel.GetAttribute("aria-label"));
        Assert.Equal("Définitions", panel.TextContent);
        Assert.Contains("omni-code-viewer--with-panel", viewer.Find("section").ClassList);
        Assert.Empty(RenderViewer("a").FindAll("aside"));
    }

    [Fact]
    public async Task CopyOfANullCode_CopiesNothing()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Interop);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        var viewer = RenderViewer(null);

        Assert.True(await viewer.InvokeAsync(viewer.Instance.CopyAsync));
        Assert.True(await viewer.InvokeAsync(viewer.Instance.CopyAsync));
        Assert.All(module.Invocations["copyText"], call => Assert.Equal(string.Empty, call.Arguments[0]));
        Assert.Equal(2, module.Invocations["copyText"].Count);
    }
}
