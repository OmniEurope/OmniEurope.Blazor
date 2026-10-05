using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Theming and editor components in shapes their suites leave out: a palette picked in the appearance
/// window, a summary that names the control size once the host binds it, and a renamed diff file that
/// lacks one of its two paths.
/// </summary>
public sealed class ThemingAndEditorBranchTests : OmniBunitContext
{
    [Fact]
    public void AppearanceWindow_PaletteChosenInItsList_IsHandedToTheHost()
    {
        var picked = new List<OmniThemePalette?>();
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.PaletteChanged, value => picked.Add(value)));

        window.Find("select[aria-label='Palette']").Change("1");

        Assert.NotNull(Assert.Single(picked));
    }

    [Fact]
    public void AppearanceSummary_NamesTheControlSize_OnceTheHostBindsIt()
    {
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Compact, true)
            .Add(component => component.ControlSizeLevel, 7)
            .Add(component => component.ControlSizeLevelChanged, _ => { }));

        Assert.EndsWith("Contrôles 7", settings.Find(".omni-appearance-settings__summary").TextContent.Trim(), StringComparison.Ordinal);

        var unbound = Render<OmniAppearanceSettings>(parameters => parameters.Add(component => component.Compact, true));
        Assert.DoesNotContain("Contrôles", unbound.Find(".omni-appearance-settings__summary").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void UnifiedDiff_RenamedFileWithoutItsOldPath_ShowsTheOneItHas()
    {
        var diff = Render<OmniUnifiedDiff>(parameters => parameters
            .Add(component => component.Files, [new OmniDiffFile(null, "src/nouveau.cs", OmniDiffFileStatus.Renamed, false, [])]));

        Assert.Contains("src/nouveau.cs", diff.Markup, StringComparison.Ordinal);
    }
}
