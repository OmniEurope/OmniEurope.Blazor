using Bunit;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Review R1-2 (owner decisions of 2026-11-12): the field behind the page is a backdrop of a catalogue,
/// chosen like the palette. Essentiel draws none and offers none; every other theme draws its own (Givre
/// and Trou noir, then the seven of review R1-3, None for the themes whose backdrop does not exist yet) and offers every one; None
/// leaves the plain page; the choice travels with the preference and the theme default comes back on reset.
/// </summary>
public sealed class ThemeBackdropTests : OmniBunitContext
{
    private static OmniThemePreset Theme(string name) => OmniThemePresets.All.Single(theme => theme.Name == name);

    private static OmniThemeBackdrop Named(string name) => OmniThemeBackdrops.All.Single(backdrop => backdrop.Name == name);

    // ---- The catalogue ----

    [Fact]
    public void The_catalogue_holds_the_plain_page_then_the_two_backdrops_that_were_theme_fields_and_the_ones_added_after_them()
    {
        Assert.Equal(
            ["Aucun", "Givre", "Trou noir", "Quadrillage", "Ricochets", "Bokeh", "Horizon", "Aquarelle", "Rayons", "Étoiles"],
            OmniThemeBackdrops.All.Select(backdrop => backdrop.Name));
        Assert.Same(OmniThemeBackdrops.None, OmniThemeBackdrops.All[0]);
        Assert.Empty(OmniThemeBackdrops.None.Shape);
        Assert.Empty(OmniThemeBackdrops.None.DarkShape);
    }

    [Fact]
    public void Essentiel_draws_no_backdrop_and_offers_only_the_plain_page()
    {
        Assert.Same(OmniThemeBackdrops.None, OmniThemePresets.DefaultBackdropFor(null));
        Assert.Same(OmniThemeBackdrops.None, OmniThemePresets.DefaultBackdropFor(OmniThemePresets.All[0]));
        Assert.Equal([OmniThemeBackdrops.None], OmniThemePresets.BackdropsFor(OmniThemePresets.All[0]));
        Assert.Equal([OmniThemeBackdrops.None], OmniThemePresets.BackdropsFor(null));
    }

    [Theory]
    [InlineData("Givre", "Givre")]
    [InlineData("Trou noir", "Trou noir")]
    [InlineData("Ardoise", "Quadrillage")]
    [InlineData("Galet", "Ricochets")]
    [InlineData("Halo", "Bokeh")]
    [InlineData("Papier", "Aquarelle")]
    [InlineData("Rétro", "Rayons")]
    [InlineData("Octet", "Étoiles")]
    [InlineData("Néon", "Horizon")]
    public void Every_other_theme_draws_its_own_backdrop_and_offers_every_one(string theme, string backdrop)
    {
        var preset = Theme(theme);
        Assert.Equal(backdrop, OmniThemePresets.DefaultBackdropFor(preset).Name);
        Assert.Same(OmniThemePresets.DefaultBackdropFor(preset), preset.Backdrop);
        Assert.Equal(OmniThemeBackdrops.All, OmniThemePresets.BackdropsFor(preset));
    }

    [Fact]
    public void A_theme_drawn_over_its_own_backdrop_keeps_every_token_it_had()
    {
        foreach (var preset in OmniThemePresets.All)
        {
            var own = OmniThemePresets.DefaultBackdropFor(preset);
            Assert.Same(preset, preset.With(own));

            // Away and back: the very tokens of the theme of the catalogue, both halves and both shapes.
            var back = preset.With(OmniThemeBackdrops.None).With(own);
            Assert.Equal(preset.Light.OrderBy(token => token.Key, StringComparer.Ordinal), back.Light.OrderBy(token => token.Key, StringComparer.Ordinal));
            Assert.Equal(preset.Dark.OrderBy(token => token.Key, StringComparer.Ordinal), back.Dark.OrderBy(token => token.Key, StringComparer.Ordinal));
            Assert.Equal(preset.Shape.OrderBy(token => token.Key, StringComparer.Ordinal), back.Shape.OrderBy(token => token.Key, StringComparer.Ordinal));
            Assert.Equal(preset.DarkShape.OrderBy(token => token.Key, StringComparer.Ordinal), back.DarkShape.OrderBy(token => token.Key, StringComparer.Ordinal));
        }
    }

    [Fact]
    public void Givre_and_Trou_noir_carry_the_field_they_drew_before_it_became_a_backdrop()
    {
        var givre = Theme("Givre");
        Assert.StartsWith("radial-gradient(circle at calc(14% + 5% * cos(var(--omni-scope-turn)))", givre.Light["--omni-backdrop"], StringComparison.Ordinal);
        Assert.Equal("omni-scope-turn 120s linear infinite", givre.Light["--omni-scope-motion"]);
        Assert.Equal("color-mix(in srgb, var(--omni-color-warning-fill) 36%, var(--omni-color-surface))", givre.Light["--omni-backdrop-glow"]);
        Assert.Equal("color-mix(in srgb, var(--omni-color-danger-fill) 26%, var(--omni-color-surface))", givre.Dark["--omni-backdrop-glow"]);
        // The frost of the cards belongs to the theme: it stays over the plain page.
        Assert.Equal("isolate", givre.With(OmniThemeBackdrops.None).Light["--omni-scope-isolation"]);

        var trouNoir = Theme("Trou noir");
        Assert.Equal("white-hole", trouNoir.Light["--omni-scope-canvas"]);
        Assert.Equal("black-hole", trouNoir.Dark["--omni-scope-canvas"]);
        Assert.StartsWith("radial-gradient(circle at 76% 26%, var(--omni-color-surface) 0 9vmax", trouNoir.Light["--omni-backdrop"], StringComparison.Ordinal);
        // Its deep page belongs to the theme: the plain page of Trou noir is still its page.
        Assert.Equal(trouNoir.Dark["--omni-scope-page"], trouNoir.With(OmniThemeBackdrops.None).Dark["--omni-scope-page"]);
    }

    [Fact]
    public void A_theme_drawn_over_the_plain_page_loses_its_field_and_its_motion_only()
    {
        foreach (var name in new[] { "Givre", "Trou noir" })
        {
            var preset = Theme(name);
            var plain = preset.With(OmniThemeBackdrops.None);
            foreach (var half in new[] { plain.Light, plain.Dark, plain.Shape, plain.DarkShape })
            {
                Assert.DoesNotContain(half.Keys, key => key.StartsWith("--omni-backdrop", StringComparison.Ordinal));
                Assert.False(half.ContainsKey("--omni-scope-motion"), name);
                Assert.False(half.ContainsKey("--omni-scope-canvas"), name);
            }

            var backdropNames = preset.Backdrop.Shape.Keys.Concat(preset.Backdrop.DarkShape.Keys).ToHashSet(StringComparer.Ordinal);
            Assert.All(preset.Light.Where(token => !backdropNames.Contains(token.Key)), token => Assert.Equal(token.Value, plain.Light[token.Key]));
            Assert.Same(OmniThemeBackdrops.None, plain.Backdrop);
        }
    }

    [Fact]
    public void Another_backdrop_replaces_the_tokens_of_the_one_drawn_and_none_of_the_theme()
    {
        var ardoise = Theme("Ardoise");
        var overGivre = ardoise.With(Named("Givre"));
        Assert.Equal(Named("Givre").Shape["--omni-backdrop"], overGivre.Light["--omni-backdrop"]);
        Assert.Equal(Named("Givre").DarkShape["--omni-backdrop-glow"], overGivre.Dark["--omni-backdrop-glow"]);

        var overTrouNoir = overGivre.With(Named("Trou noir"));
        Assert.False(overTrouNoir.Light.ContainsKey("--omni-backdrop-glow"));
        Assert.Equal("black-hole", overTrouNoir.Dark["--omni-scope-canvas"]);

        var back = overTrouNoir.With(OmniThemeBackdrops.None);
        var plain = ardoise.With(OmniThemeBackdrops.None);
        Assert.Equal(plain.Light.OrderBy(token => token.Key, StringComparer.Ordinal), back.Light.OrderBy(token => token.Key, StringComparer.Ordinal));
        Assert.Equal(plain.Dark.OrderBy(token => token.Key, StringComparer.Ordinal), back.Dark.OrderBy(token => token.Key, StringComparer.Ordinal));
    }

    // ---- The scope ----

    private IReadOnlyDictionary<string, string> LightPainted(BunitJSModuleInterop module) =>
        (IReadOnlyDictionary<string, string>)module.Invocations["apply"][^1].Arguments[1]!;

    [Fact]
    public void Choosing_the_plain_page_removes_the_field_from_the_scope()
    {
        var module = JSInterop.SetupModule(OmniModules.Theme);
        module.Mode = JSRuntimeMode.Loose;
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, Theme("Givre"))
            .AddChildContent("page"));
        Assert.True(LightPainted(module).ContainsKey("--omni-backdrop"));

        scope.Render(parameters => parameters.Add(component => component.Backdrop, OmniThemeBackdrops.None));

        Assert.False(LightPainted(module).ContainsKey("--omni-backdrop"));
        Assert.False(LightPainted(module).ContainsKey("--omni-scope-motion"));
        Assert.Equal("isolate", LightPainted(module)["--omni-scope-isolation"]);
    }

    [Fact]
    public void The_plain_page_under_Trou_noir_draws_no_canvas_and_its_backdrop_under_another_theme_does()
    {
        var blackHole = JSInterop.SetupModule(OmniModules.BlackHole);
        blackHole.Mode = JSRuntimeMode.Loose;
        blackHole.Setup<bool>("start", _ => true).SetResult(true);

        var plain = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, Theme("Trou noir"))
            .Add(component => component.Backdrop, OmniThemeBackdrops.None)
            .AddChildContent("page"));
        Assert.Empty(plain.FindAll("canvas"));
        Assert.Empty(blackHole.Invocations["start"]);

        var ardoise = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, Theme("Ardoise"))
            .Add(component => component.Backdrop, Named("Trou noir"))
            .AddChildContent("page"));
        Assert.Single(ardoise.FindAll("canvas.omni-theme-scope__canvas"));
        ardoise.WaitForAssertion(() => Assert.Contains("omni-theme-scope--canvas", ardoise.Find(".omni-theme-scope").ClassList));

        // Back to the plain page: the canvas leaves and its script stops it.
        ardoise.Render(parameters => parameters.Add(component => component.Backdrop, OmniThemeBackdrops.None));
        Assert.Empty(ardoise.FindAll("canvas"));
        Assert.NotEmpty(blackHole.Invocations["sweep"]);
    }

    [Fact]
    public void A_backdrop_handed_to_a_scope_under_Essentiel_is_not_drawn()
    {
        var module = JSInterop.SetupModule(OmniModules.Theme);
        module.Mode = JSRuntimeMode.Loose;

        Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, OmniThemePresets.All[0])
            .Add(component => component.Backdrop, Named("Givre"))
            .AddChildContent("page"));

        Assert.False(LightPainted(module).ContainsKey("--omni-backdrop"));
    }

    [Fact]
    public void The_boot_snapshot_holds_the_backdrop_drawn()
    {
        var module = JSInterop.SetupModule(OmniModules.Theme);
        module.Mode = JSRuntimeMode.Loose;
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, Theme("Givre"))
            .Add(component => component.SnapshotKey, "site:theme")
            .AddChildContent("page"));
        var first = (IReadOnlyDictionary<string, string>)Assert.Single(module.Invocations["snapshot"]).Arguments[6]!;
        Assert.True(first.ContainsKey("--omni-backdrop"));

        scope.Render(parameters => parameters.Add(component => component.Backdrop, OmniThemeBackdrops.None));

        Assert.Equal(2, module.Invocations["snapshot"].Count);
        var plain = (IReadOnlyDictionary<string, string>)module.Invocations["snapshot"][1].Arguments[6]!;
        Assert.False(plain.ContainsKey("--omni-backdrop"));
    }

    // ---- The window ----

    private IRenderedComponent<OmniAppearanceWindow> Window(OmniThemePreset? preset, OmniThemeBackdrop? backdrop, Action<OmniThemeBackdrop?> picked) =>
        Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, preset)
            .Add(component => component.Backdrop, backdrop)
            .Add(component => component.BackdropChanged, picked)
            .Add(component => component.BackdropMotionChanged, _ => { }));

    [Fact]
    public void Under_Essentiel_the_window_offers_the_plain_page_alone_as_the_default()
    {
        var window = Window(null, null, _ => { });

        var option = Assert.Single(window.Find("select[aria-label='Fond']").QuerySelectorAll("option"));
        Assert.Equal("Aucun (défaut)", option.TextContent);
        Assert.Empty(window.FindAll("[role=switch]"));
    }

    [Fact]
    public void Under_Givre_the_window_offers_every_backdrop_its_own_as_the_default()
    {
        OmniThemeBackdrop? picked = Named("Trou noir");
        var window = Window(Theme("Givre"), null, value => picked = value);

        var options = window.Find("select[aria-label='Fond']").QuerySelectorAll("option").Select(option => option.TextContent);
        Assert.Equal(["Aucun", "Givre (défaut)", "Trou noir", "Quadrillage", "Ricochets", "Bokeh", "Horizon", "Aquarelle", "Rayons", "Étoiles"], options);
        // The motion switch sits on the title line of the backdrop row.
        var label = window.Find("#" + window.Find("[role=switch]").GetAttribute("aria-labelledby"));
        Assert.NotNull(label.Closest(".omni-appearance-settings__row")!.QuerySelector("select[aria-label='Fond']"));

        window.Find("select[aria-label='Fond']").Change("0");
        Assert.Same(OmniThemeBackdrops.None, picked);

        // The host stores the plain page; picking the theme's own one then hands back null.
        window.Render(parameters => parameters.Add(component => component.Backdrop, picked));
        window.Find("select[aria-label='Fond']").Change("1");
        Assert.Null(picked);
    }

    [Fact]
    public void The_motion_switch_follows_the_backdrop_drawn_rather_than_the_theme()
    {
        var plainGivre = Window(Theme("Givre"), OmniThemeBackdrops.None, _ => { });
        Assert.Empty(plainGivre.FindAll("[role=switch]"));

        var ardoiseOverGivre = Window(Theme("Ardoise"), Named("Givre"), _ => { });
        Assert.Single(ardoiseOverGivre.FindAll("[role=switch]"));
    }

    [Fact]
    public void Reset_restores_the_backdrop_of_the_theme()
    {
        OmniThemeBackdrop? backdrop = OmniThemeBackdrops.None;
        var window = Window(Theme("Givre"), backdrop, value => backdrop = value);

        var row = window.Find("select[aria-label='Fond']").Closest(".omni-appearance-settings__row")!;
        row.QuerySelectorAll("button").Single(button => button.TextContent.Trim() == "Défaut").Click();
        Assert.Null(backdrop);

        backdrop = OmniThemeBackdrops.None;
        window.Render(parameters => parameters.Add(component => component.Backdrop, backdrop));
        window.Find(".omni-appearance-window__reset").Click();
        Assert.Null(backdrop);
    }

    [Fact]
    public void A_new_theme_drops_the_backdrop_chosen_for_the_previous_one()
    {
        OmniThemePreset? preset = Theme("Givre");
        OmniThemeBackdrop? backdrop = Named("Trou noir");
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, preset)
            .Add(component => component.PresetChanged, value => preset = value)
            .Add(component => component.Backdrop, backdrop)
            .Add(component => component.BackdropChanged, value => backdrop = value));

        window.Find("select[aria-label='Thème']").Change("1");

        Assert.Equal(OmniThemePresets.All[1].Name, preset?.Name);
        Assert.Null(backdrop);
    }

    // ---- The preference ----

    [Fact]
    public void The_choice_survives_the_preference_round_trip()
    {
        var look = AppearanceLook.Default with { Preset = Theme("Givre"), Backdrop = OmniThemeBackdrops.None };

        var preference = look.ToPreference();
        Assert.Equal("Aucun", preference.Backdrop);
        var json = System.Text.Json.JsonSerializer.Serialize(preference, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Contains("\"backdrop\":\"Aucun\"", json, StringComparison.Ordinal);
        Assert.Equal(look, AppearanceLook.Parse(json));

        // An older preference without the member, or a name the catalogue no longer has, keeps the theme's own.
        Assert.Null(AppearanceLook.Parse("{\"version\":1,\"preset\":\"Givre\"}").Backdrop);
        Assert.Null(AppearanceLook.Parse("{\"version\":1,\"preset\":\"Givre\",\"backdrop\":\"Disparu\"}").Backdrop);
    }

    [Fact]
    public async Task The_state_keeps_the_backdrop_and_a_new_theme_drops_it()
    {
        var state = Services.GetRequiredService<OmniAppearanceState>();
        await state.SetPresetAsync(Theme("Givre"));
        await state.SetBackdropAsync(Named("Trou noir"));
        Assert.Same(Named("Trou noir"), state.Backdrop);

        await state.SetPresetAsync(Theme("Trou noir"));
        Assert.Null(state.Backdrop);
    }

    [Fact]
    public async Task A_scope_that_follows_the_state_draws_the_backdrop_chosen_in_it()
    {
        var module = JSInterop.SetupModule(OmniModules.Theme);
        module.Mode = JSRuntimeMode.Loose;
        var state = Services.GetRequiredService<OmniAppearanceState>();
        await state.SetAppearanceAsync(OmniAppearance.Light);
        await state.SetPresetAsync(Theme("Givre"));
        var scope = Render<OmniThemeScope>(parameters => parameters.AddChildContent("page"));
        scope.WaitForAssertion(() => Assert.True(LightPainted(module).ContainsKey("--omni-backdrop")));

        await scope.InvokeAsync(() => state.SetBackdropAsync(OmniThemeBackdrops.None));

        scope.WaitForAssertion(() => Assert.False(LightPainted(module).ContainsKey("--omni-backdrop")));
    }
}
