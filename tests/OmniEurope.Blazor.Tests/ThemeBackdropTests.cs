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
    public void The_catalogue_holds_the_plain_page_then_the_backdrops_drawn()
    {
        Assert.Equal(
            ["Aucun", "Givre", "Trou noir", "Quadrillage", "Ricochets", "Bokeh", "Horizon", "Aquarelle", "Rayons", "Étoiles", "Nénuphar", "Velours", "Relief", "Aplat", "Épure"],
            OmniThemeBackdrops.All.Select(backdrop => backdrop.Name));
        Assert.Same(OmniThemeBackdrops.None, OmniThemeBackdrops.All[0]);
        Assert.Empty(OmniThemeBackdrops.None.Shape);
        Assert.Empty(OmniThemeBackdrops.None.DarkShape);
    }

    /// <summary>The layers of a comma-separated CSS list, split where no parenthesis is open.</summary>
    private static List<string> Layers(string list)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var index = 0; index < list.Length; index++)
        {
            switch (list[index])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(list[start..index].Trim());
                    start = index + 1;
                    break;
            }
        }

        parts.Add(list[start..].Trim());
        return parts;
    }

    /// <summary>
    /// Review R1-3 (owner decisions of 2026-11-12): Nénuphar, Velours, Relief, Aplat and Épure each draw
    /// and move a field in CSS alone, from palette tokens only, in both modes.
    /// </summary>
    [Theory]
    [InlineData("Nénuphar")]
    [InlineData("Velours")]
    [InlineData("Relief")]
    [InlineData("Aplat")]
    [InlineData("Épure")]
    public void An_animated_backdrop_is_drawn_in_css_from_palette_tokens_and_moves_with_the_turning_angle(string name)
    {
        var backdrop = Named(name);
        Assert.Same(backdrop, Theme(name).Backdrop);
        Assert.DoesNotContain("--omni-scope-canvas", backdrop.Shape.Keys);
        Assert.False(string.IsNullOrWhiteSpace(backdrop.Description));
        Assert.Equal("omni-scope-turn 120s linear infinite", backdrop.Shape["--omni-scope-motion"]);
        Assert.DoesNotContain("--omni-scope-motion", backdrop.DarkShape.Keys);

        // The turning angle is read by the field or by the place of its tiles, never by neither.
        var moves = backdrop.Shape["--omni-backdrop"].Contains("var(--omni-scope-turn)", StringComparison.Ordinal)
            || backdrop.Shape.GetValueOrDefault("--omni-scope-backdrop-position", string.Empty).Contains("var(--omni-scope-turn)", StringComparison.Ordinal);
        Assert.True(moves, name);

        // Every colour is drawn from a token of the palette, light and dark: no written colour, no image
        // that a strict CSP or a palette could not follow.
        foreach (var value in backdrop.Shape.Values.Concat(backdrop.DarkShape.Values))
        {
            Assert.DoesNotMatch(@"#[0-9a-fA-F]{3,8}\b|\brgba?\(|\bhsla?\(|url\(|\beval\b", value);
        }

        var stops = backdrop.Shape.Keys.Where(key => key.StartsWith("--omni-backdrop-", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(stops);
        Assert.Equal(stops, backdrop.Shape["--omni-backdrop"].Split("var(--omni-backdrop-").Skip(1).Select(part => "--omni-backdrop-" + part[..part.IndexOf(')', StringComparison.Ordinal)]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal));
        Assert.All(backdrop.DarkShape.Keys, key => Assert.Contains(key, stops));
    }

    [Theory]
    [InlineData("Aplat")]
    [InlineData("Épure")]
    public void A_backdrop_of_shapes_gives_every_layer_its_own_tile(string name)
    {
        var shape = Named(name).Shape;
        var layers = Layers(shape["--omni-backdrop"]).Count;

        Assert.True(layers > 1, name);
        Assert.Equal(layers, Layers(shape["--omni-scope-backdrop-size"]).Count);
        if (shape.TryGetValue("--omni-scope-backdrop-position", out var position))
        {
            Assert.Equal(layers, Layers(position).Count);
        }

        // A shape is drawn once in its tile; a screen repeats its dot.
        Assert.Equal(name == "Aplat" ? "no-repeat" : null, shape.GetValueOrDefault("--omni-scope-backdrop-repeat"));
    }

    [Fact]
    public void A_backdrop_that_names_no_tile_leaves_the_neutral_one_of_the_stylesheet()
    {
        Assert.All(OmniThemeBackdrops.All.Where(backdrop => backdrop.Name is not ("Aplat" or "Épure")), backdrop =>
        {
            Assert.DoesNotContain(backdrop.Shape.Keys, key => key.StartsWith("--omni-scope-backdrop-", StringComparison.Ordinal));
            Assert.DoesNotContain(backdrop.DarkShape.Keys, key => key.StartsWith("--omni-scope-backdrop-", StringComparison.Ordinal));
        });

        var hooks = ShippedLookTests.Body("[data-omni-theme]");
        Assert.Equal("auto", ShippedLookTests.Value(hooks, "--omni-scope-backdrop-size"));
        Assert.Equal("0% 0%", ShippedLookTests.Value(hooks, "--omni-scope-backdrop-position"));
        Assert.Equal("repeat", ShippedLookTests.Value(hooks, "--omni-scope-backdrop-repeat"));
        var scope = ShippedLookTests.Body(".omni-theme-scope");
        Assert.Equal("var(--omni-scope-backdrop-size, auto)", ShippedLookTests.Value(scope, "background-size"));
        Assert.Equal("var(--omni-scope-backdrop-position, 0% 0%)", ShippedLookTests.Value(scope, "background-position"));
        Assert.Equal("var(--omni-scope-backdrop-repeat, repeat)", ShippedLookTests.Value(scope, "background-repeat"));
        Assert.Equal("false", ShippedLookTests.Value(ShippedLookTests.Body("@property --omni-scope-backdrop-position"), "inherits"));
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
    [InlineData("Nénuphar", "Nénuphar")]
    [InlineData("Velours", "Velours")]
    [InlineData("Relief", "Relief")]
    [InlineData("Aplat", "Aplat")]
    [InlineData("Épure", "Épure")]
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
        Assert.Equal(["Aucun", "Givre (défaut)", "Trou noir", "Quadrillage", "Ricochets", "Bokeh", "Horizon", "Aquarelle", "Rayons", "Étoiles", "Nénuphar", "Velours", "Relief", "Aplat", "Épure"], options);
        // The animation switch has a row of its own, right under the backdrop row (review R1-1).
        var label = window.Find("#" + window.Find("[role=switch]").GetAttribute("aria-labelledby"));
        var animation = label.Closest(".omni-appearance-settings__row")!;
        Assert.Null(animation.QuerySelector("select"));
        Assert.Same(window.Find("select[aria-label='Fond']").Closest(".omni-appearance-settings__row"), animation.PreviousElementSibling);

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
        row.QuerySelectorAll("button").Single(button => button.GetAttribute("aria-label") == "Défaut").Click();
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
