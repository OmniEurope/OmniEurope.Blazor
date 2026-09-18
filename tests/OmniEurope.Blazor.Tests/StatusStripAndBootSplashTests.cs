using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniStatusStrip"/>, which paints the host's own statuses with the tones it is given, and
/// <see cref="OmniBootSplash"/>, which asks <c>omniInterop.js</c> to remove the host's boot splash
/// once the application has rendered. The fade itself runs in a browser.
/// </summary>
public sealed class StatusStripAndBootSplashTests : OmniBunitContext
{
    private const string InteropPath = "./_content/OmniEurope.Blazor/omniInterop.js";

    private static readonly IReadOnlyList<OmniStatusStripItem> Runs =
    [
        new() { Status = "success", Label = "Exécution 41 : réussie" },
        new() { Status = "failed", Label = "Exécution 42 : en échec" },
        new() { Status = "running", Label = "Exécution 43 : en cours" },
        new() { Status = "unknown", Label = "Exécution 44 : inconnue" }
    ];

    private static readonly IReadOnlyDictionary<string, OmniBadgeVariant> Tones = new Dictionary<string, OmniBadgeVariant>(StringComparer.Ordinal)
    {
        ["success"] = OmniBadgeVariant.Success,
        ["failed"] = OmniBadgeVariant.Danger,
        ["running"] = OmniBadgeVariant.Accent
    };

    [Fact]
    public void Strip_IsANamedList_OfNamedMarks_PaintedWithTheirTone()
    {
        var strip = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, Runs)
            .Add(component => component.Tones, Tones)
            .Add(component => component.Pulsing, ["running"]));

        var list = strip.Find("ul.omni-status-strip");
        Assert.Equal("list", list.GetAttribute("role"));
        Assert.Equal("Historique des états", list.GetAttribute("aria-label"));
        Assert.Contains("omni-status-strip--dot", list.ClassList);
        var marks = strip.FindAll("li > span.omni-status-strip__item");
        Assert.Equal(4, marks.Count);
        Assert.All(marks, mark => Assert.Equal("img", mark.GetAttribute("role")));
        Assert.Equal(Runs.Select(run => run.Label), marks.Select(mark => mark.GetAttribute("aria-label")));
        Assert.Equal(Runs.Select(run => run.Label), marks.Select(mark => mark.GetAttribute("title")));
        Assert.Equal(Runs.Select(run => run.Status), marks.Select(mark => mark.GetAttribute("data-omni-status")));
        Assert.Contains("omni-status-strip__item--success", marks[0].ClassList);
        Assert.Contains("omni-status-strip__item--danger", marks[1].ClassList);
        Assert.Contains("omni-status-strip__item--accent", marks[2].ClassList);
        Assert.Contains("omni-status-strip__item--neutral", marks[3].ClassList);
        Assert.Equal([false, false, true, false], marks.Select(mark => mark.ClassList.Contains("omni-status-strip__item--pulse")));
        Assert.Empty(strip.FindAll("a, button"));
        Assert.DoesNotContain("style=", strip.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(Variants))]
    public void EveryTone_HasItsClass(OmniBadgeVariant variant)
    {
        var strip = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, [new OmniStatusStripItem { Status = "s", Label = "S" }])
            .Add(component => component.Tones, new Dictionary<string, OmniBadgeVariant> { ["s"] = variant }));

        Assert.Contains($"omni-status-strip__item--{variant.ToString().ToLowerInvariant()}", strip.Find(".omni-status-strip__item").ClassList);
    }

    public static TheoryData<OmniBadgeVariant> Variants() => [.. Enum.GetValues<OmniBadgeVariant>()];

    [Theory]
    [InlineData(OmniStatusStripShape.Dot, "omni-status-strip--dot")]
    [InlineData(OmniStatusStripShape.Segment, "omni-status-strip--segment")]
    public void EveryShape_HasItsClass(OmniStatusStripShape shape, string expected)
    {
        var strip = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, Runs)
            .Add(component => component.Shape, shape));

        Assert.Contains(expected, strip.Find("ul").ClassList);
    }

    [Fact]
    public void AnItemWithAnAddress_IsALink_AndAnUnsafeAddressIsRefused()
    {
        var strip = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, [new OmniStatusStripItem { Status = "success", Label = "Exécution 41", Href = "/runs/41" }]));

        var link = strip.Find("a.omni-status-strip__item");
        Assert.Equal("/runs/41", link.GetAttribute("href"));
        Assert.Equal("Exécution 41", link.GetAttribute("aria-label"));
        Assert.Null(link.GetAttribute("role"));

        Assert.Throws<InvalidOperationException>(() => Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, [new OmniStatusStripItem { Status = "success", Label = "X", Href = "javascript:alert(1)" }])));
    }

    [Fact]
    public void WithAClickHandler_ItemsAreNativeButtons_ThatReportTheirItem()
    {
        OmniStatusStripItem? clicked = null;
        var strip = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, Runs)
            .Add(component => component.OnItemClick, item => clicked = item));

        var buttons = strip.FindAll("button.omni-status-strip__item");
        Assert.Equal(4, buttons.Count);
        Assert.All(buttons, button => Assert.Equal("button", button.GetAttribute("type")));
        Assert.Equal("Exécution 42 : en échec", buttons[1].GetAttribute("aria-label"));

        buttons[1].Click();

        Assert.Same(Runs[1], clicked);
    }

    [Fact]
    public void EmptyStrip_WritesItsText_AndTheParametersReplaceTheDefaults()
    {
        var empty = Render<OmniStatusStrip>(parameters => parameters.Add(component => component.Items, []));
        Assert.Equal("Aucun état à afficher", empty.Find(".omni-status-strip__empty").TextContent);

        var custom = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, [])
            .Add(component => component.EmptyText, "Jamais lancé")
            .Add(component => component.Id, "runs"));
        Assert.Equal("Jamais lancé", custom.Find("#runs").TextContent);

        var named = Render<OmniStatusStrip>(parameters => parameters
            .Add(component => component.Items, Runs)
            .Add(component => component.Label, "Dernières exécutions"));
        Assert.Equal("Dernières exécutions", named.Find("ul").GetAttribute("aria-label"));
    }

    [Fact]
    public void BootSplash_RendersNothing_AndAsksTheScriptToRemoveTheDefaultSplash()
    {
        var module = JSInterop.SetupModule(InteropPath);
        module.Setup<bool>("hideBootSplash", _ => true).SetResult(true);
        bool? hidden = null;

        var splash = Render<OmniBootSplash>(parameters => parameters
            .Add(component => component.OnHidden, EventCallback.Factory.Create<bool>(this, found => hidden = found)));

        Assert.True(string.IsNullOrWhiteSpace(splash.Markup));
        splash.WaitForAssertion(() => Assert.True(hidden));
        Assert.Equal("omni-boot-splash", Assert.Single(module.Invocations["hideBootSplash"]).Arguments[0]);
    }

    [Fact]
    public void BootSplash_ReportsAPageWithoutSplash_AndUsesTheIdItIsGiven()
    {
        var module = JSInterop.SetupModule(InteropPath);
        module.Setup<bool>("hideBootSplash", _ => true).SetResult(false);
        bool? hidden = null;

        var splash = Render<OmniBootSplash>(parameters => parameters
            .Add(component => component.SplashId, "app-splash")
            .Add(component => component.OnHidden, EventCallback.Factory.Create<bool>(this, found => hidden = found)));

        splash.WaitForAssertion(() => Assert.False(hidden));
        Assert.Equal("app-splash", Assert.Single(module.Invocations["hideBootSplash"]).Arguments[0]);

        splash.Render(parameters => parameters.Add(component => component.SplashId, "other"));
        Assert.Single(module.Invocations["hideBootSplash"]);
    }
}
