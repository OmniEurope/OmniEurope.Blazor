using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Localization;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Feedback, foundation, localization and action parts at their edges: an icon titled by its attributes, a
/// width refused, the loading store's repeats and extra ends, a progress bar refused, a status map replaced,
/// read empty or listed, glyph data in every
/// shape, presets asked by name where none exist, overridden texts with arguments, and the split button.
/// </summary>
public sealed class FeedbackFoundationEdgeTests : OmniBunitContext
{
    [Fact]
    public void Icon_TitledByItsTitleAttribute_OrNothing()
    {
        var titled = Render<OmniIcon>(parameters => parameters.Add(component => component.Name, OmniIconName.Info).AddUnmatched("title", "Info"));
        var plain = Render<OmniIcon>(parameters => parameters.Add(component => component.Name, OmniIconName.Info).AddUnmatched("class", "x"));

        Assert.Contains("Info", titled.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("title=", plain.Markup, StringComparison.Ordinal);

        var labelled = Render<OmniIcon>(parameters => parameters.Add(component => component.Name, OmniIconName.Info).Add(component => component.Label, "Aide"));
        var blank = Render<OmniIcon>(parameters => parameters.Add(component => component.Name, OmniIconName.Info).AddUnmatched("title", null));
        Assert.Equal("Aide", labelled.Find("title").TextContent);
        Assert.Empty(blank.FindAll("title"));
    }

    [Fact]
    public void LoadingState_ReportedWhileNobodyListens_IsMeasured()
    {
        var state = new OmniLoadingState();
        Assert.False(state.Unmeasured);

        state.Begin();
        Assert.True(state.Unmeasured);
        state.Report(40);

        Assert.False(state.Unmeasured);
        Assert.Equal(40, state.Value);
    }

    [Fact]
    public void Image_RefusesAWidthThatIsNotPositive() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniImage>(parameters => parameters
            .Add(component => component.Source, "/logo.png")
            .Add(component => component.Width, 0)));

    [Fact]
    public void LoadingState_IgnoresTheSameFigure_AndAnEndWithoutBeginning_AndAScopeEndedTwice()
    {
        var state = new OmniLoadingState();
        var changes = 0;
        state.End();
        state.Begin();
        Assert.True(state.Unmeasured);

        state.Changed += () => changes++;
        state.Report(40);
        state.Report(40.001);
        Assert.False(state.Unmeasured);
        var scope = state.Track();
        scope.Dispose();
        scope.Dispose();
        state.End();
        state.End();

        Assert.Equal(4, changes);
        Assert.False(state.Loading);
    }

    [Theory]
    [InlineData(double.NaN, 1d)]
    [InlineData(1d, double.PositiveInfinity)]
    [InlineData(0d, 1d)]
    public void ProgressBar_RefusesFiguresThatAreNotFinite(double maximum, double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniProgressBar>(parameters => parameters
            .Add(component => component.Maximum, maximum)
            .Add(component => component.Value, value)));

    [Fact]
    public void StatusMap_ReplacesAValue_ListsItsEntries_AndReadsNothingAsEmpty()
    {
        var map = new OmniStatusMap<string>().Add("ok", OmniTone.Success, "OK").Add("ok", OmniTone.Info, "Bon");

        Assert.Single(map);
        Assert.Equal("Bon", map.Resolve("ok").Text);
        Assert.Equal("-", map.Resolve(null).Text);
        Assert.Equal(string.Empty, map.Localize(""));
        Assert.Equal(["ok"], ((System.Collections.IEnumerable)map).Cast<KeyValuePair<string, OmniStatus>>().Select(entry => entry.Key));
        Assert.Equal(string.Empty, new OmniStatusMap<Silent>().Resolve(new Silent()).Text);
    }

    public sealed class Silent
    {
        public override string? ToString() => null;
    }

    [Fact]
    public void IconGlyph_ReadsEveryNumberShape() =>
        Assert.NotNull(new OmniIconGlyph("M1e2 3E-1.5+2,4 Z", 24));

    [Fact]
    public void Presets_AskedByNameWhereNoneExist_AreRefused_AndNoneNamesNothing()
    {
        Services.AddOmniEuropePreset(typeof(OmniTextBox), "compact", new Dictionary<string, object?> { [nameof(OmniTextBox.Placeholder)] = "…" });
        var text = string.Empty;

        Assert.Throws<InvalidOperationException>(() => Render<OmniPassword>(parameters => parameters
            .Add(component => component.PresetName, "compact")
            .Add(component => component.ValueExpression, () => text)));
        OmniPresetRegistry.ThrowUnregistered(typeof(OmniTextBox), "none");
        var registry = new OmniPresetRegistry();
        registry.Add(typeof(OmniTextBox), "vide", new Dictionary<string, object?> { [nameof(OmniTextBox.Placeholder)] = null });
    }

    private sealed class Texts(params (string Name, string Value)[] texts) : IStringLocalizer
    {
        public LocalizedString this[string name] => Find(name, []);

        public LocalizedString this[string name, params object[] arguments] => Find(name, arguments);

        private LocalizedString Find(string name, object[] arguments)
        {
            foreach (var (key, value) in texts)
            {
                if (key == name)
                {
                    return new LocalizedString(name, arguments.Length == 0 ? value : string.Format(System.Globalization.CultureInfo.InvariantCulture, value, arguments));
                }
            }

            return new LocalizedString(name, name, resourceNotFound: true);
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            texts.Select(text => new LocalizedString(text.Name, text.Value));
    }

    [Fact]
    public void TextOverrides_WithArguments_AndListed_TakeTheHostTextFirst()
    {
        var localizer = new OmniTextOverrideLocalizer(
            new Texts(("Count", "{0} lignes"), ("Title", "Titre")),
            new Texts(("Omni_Count", "{0} rangées")),
            "Omni_");

        Assert.Equal("3 rangées", localizer["Count", 3].Value);
        Assert.Equal("Titre", localizer["Title", 3].Value);
        Assert.Equal(["{0} rangées", "Titre"], localizer.GetAllStrings(false).Select(text => text.Value));
    }

    [Fact]
    public void SplitButton_InAHost_OpensInThePortal_AndTheArrowsOpenItOnlyClosedAndUsable()
    {
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        using var service = new OmniOverlayService();
        var opened = new List<bool>();
        var host = Render<OmniComponentsHost>(parameters => parameters
            .Add(component => component.OverlayService, service)
            .AddChildContent<OmniSplitButton>(button => button
                .Add(component => component.Text, "Enregistrer")
                .Add(component => component.OpenChanged, open => opened.Add(open))
                .AddChildContent("menu")));

        host.Find(".omni-split-button__toggle").KeyDown("Enter");
        host.Find(".omni-split-button__toggle").KeyDown("ArrowUp");
        host.WaitForAssertion(() => Assert.NotEmpty(host.FindAll(".omni-overlay-portal__entry--splitbuttonmenu")));
        host.Find(".omni-split-button__toggle").KeyDown("ArrowDown");
        host.Find(".omni-split-button__toggle").Click();
        host.WaitForAssertion(() => Assert.Empty(host.FindAll(".omni-overlay-portal__entry--splitbuttonmenu")));

        var busy = Render<OmniSplitButton>(parameters => parameters
            .Add(component => component.Text, "Enregistrer")
            .Add(component => component.Busy, true)
            .Add(component => component.OpenChanged, open => opened.Add(open))
            .AddChildContent("menu"));
        busy.Find(".omni-split-button__toggle").KeyDown("ArrowDown");

        Assert.Equal([true, false], opened);
    }
}
