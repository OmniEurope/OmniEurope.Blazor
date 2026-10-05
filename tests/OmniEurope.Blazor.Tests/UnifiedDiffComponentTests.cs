using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniUnifiedDiff drawing files a host built: every status with its badge, a renamed or untitled path,
/// binary and empty files, every kind of line, folding by hand, files without fold, the switch between
/// given files and a diff text, and an empty diff with or without the host's text.
/// </summary>
public sealed class UnifiedDiffComponentTests : OmniBunitContext
{
    private static readonly OmniDiffHunk Hunk = new("@@ -1,3 +1,3 @@", 1, 3, 1, 3,
    [
        new(OmniDiffLineKind.Context, "garde", 1, 1),
        new(OmniDiffLineKind.Removed, "ancien", 2, null),
        new(OmniDiffLineKind.Added, "nouveau", null, 2),
        new(OmniDiffLineKind.Note, "\\ No newline at end of file", null, null),
    ]);

    private static readonly IReadOnlyList<OmniDiffFile> Files =
    [
        new("a.txt", "a.txt", OmniDiffFileStatus.Modified, false, [Hunk]),
        new(null, "neuf.txt", OmniDiffFileStatus.Added, false, [Hunk]),
        new("vieux.txt", null, OmniDiffFileStatus.Deleted, false, []),
        new("avant.txt", "apres.txt", OmniDiffFileStatus.Renamed, false, []),
        new(null, null, OmniDiffFileStatus.Renamed, true, []),
        new(null, "seul.txt", OmniDiffFileStatus.Renamed, false, []),
    ];

    [Fact]
    public void GivenFiles_ShowTheirStatusPathsAndEveryKindOfLine()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters.Add(component => component.Files, Files).Add(component => component.Id, "revue"));

        Assert.Same(Files, diff.Instance.ShownFiles);
        Assert.Equal(["a.txt", "neuf.txt", "vieux.txt", "avant.txt vers apres.txt", "Modification", "seul.txt"],
            diff.FindAll(".omni-unified-diff__path").Select(path => path.TextContent));
        Assert.Equal(["Ajouté", "Supprimé", "Renommé", "Renommé", "Renommé"], diff.FindAll(".omni-badge").Select(badge => badge.TextContent.Trim()));
        Assert.Equal("revue-file-0", diff.Find(".omni-unified-diff__toggle").Id);
        var rows = diff.FindAll("section")[0].QuerySelectorAll("tbody tr");
        Assert.Equal(
            ["omni-unified-diff__hunk", "omni-unified-diff__line", "omni-unified-diff__line omni-unified-diff__line--removed", "omni-unified-diff__line omni-unified-diff__line--added", "omni-unified-diff__line omni-unified-diff__line--note"],
            rows.Select(row => row.ClassName));
        Assert.Equal("Aucune ligne modifiée.", diff.FindAll(".omni-unified-diff__note")[0].TextContent);
        Assert.Equal("Fichier binaire : aucune ligne à comparer.", diff.FindAll(".omni-unified-diff__note")[^2].TextContent);
    }

    [Fact]
    public void FileFoldedByHand_StaysFolded_UntilTheFilesChange()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters.Add(component => component.Files, Files));

        diff.Find(".omni-unified-diff__toggle").Click();
        Assert.Equal("false", diff.Find(".omni-unified-diff__toggle").GetAttribute("aria-expanded"));
        diff.Render(parameters => parameters.Add(component => component.Files, Files));
        Assert.Equal("false", diff.Find(".omni-unified-diff__toggle").GetAttribute("aria-expanded"));

        diff.Render(parameters => parameters.Add(component => component.Files, [.. Files]));
        Assert.Equal("true", diff.Find(".omni-unified-diff__toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void WithoutFolding_FilesAreTitledOnly_AndAlwaysOpen()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters
            .Add(component => component.Files, Files)
            .Add(component => component.Collapsible, false)
            .Add(component => component.ExpandedByDefault, false));

        Assert.Empty(diff.FindAll("button.omni-unified-diff__toggle"));
        Assert.All(diff.FindAll(".omni-unified-diff__body"), body => Assert.False(body.HasAttribute("hidden")));
    }

    [Fact]
    public void FilesThenText_ParsesTheText_AndAnEmptyDiffSaysSo()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters.Add(component => component.Files, Files));

        diff.Render(parameters => parameters.Add(component => component.Files, null).Add(component => component.Diff, string.Empty));
        Assert.Equal("Aucune modification.", diff.Find(".omni-unified-diff__empty").TextContent);

        diff.Render(parameters => parameters.Add(component => component.EmptyText, "Rien à relire."));
        Assert.Equal("Rien à relire.", diff.Find(".omni-unified-diff__empty").TextContent);
    }
}
