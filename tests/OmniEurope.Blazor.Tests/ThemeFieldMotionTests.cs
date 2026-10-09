using System.Text.RegularExpressions;
using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// PLAN-009 lots 2 and 4: a theme may move its colour field, the scope and the system can hold it still,
/// the appearance window offers the setting only where it does something, and a filled alert takes the
/// shape of its theme.
/// </summary>
public sealed class ThemeFieldMotionTests : OmniBunitContext
{
    private const string MotionToken = "--omni-scope-motion";
    private const string Turn = "var(--omni-scope-turn)";
    private const string NoShadow = "0 0 #0000";

    private static string Css => ShippedLookTests.Css;

    // ---- The stylesheet ----

    [Fact]
    public void The_turning_angle_is_registered_so_it_interpolates_and_stays_on_the_scope()
    {
        var angle = ShippedLookTests.Body("@property --omni-scope-turn");
        Assert.Equal("\"<angle>\"", ShippedLookTests.Value(angle, "syntax"));
        Assert.Equal("false", ShippedLookTests.Value(angle, "inherits"));
        Assert.Equal("0deg", ShippedLookTests.Value(angle, "initial-value"));

        // The field reads the angle, so it changes at every step: not inherited either, or every
        // element of the scope would recompute with it.
        Assert.Equal("false", ShippedLookTests.Value(ShippedLookTests.Body("@property --omni-backdrop"), "inherits"));
        Assert.Matches(@"@keyframes omni-scope-turn \{ to \{ --omni-scope-turn: 360deg; \} \}", Css);
    }

    [Fact]
    public void The_scope_moves_its_field_only_when_a_theme_asks_and_nothing_holds_it_still()
    {
        Assert.Equal("var(--omni-scope-motion, none)", ShippedLookTests.Value(ShippedLookTests.Body(".omni-theme-scope"), "animation"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body("[data-omni-theme]"), MotionToken));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-theme-scope[data-omni-backdrop-motion=\"off\"]"), "animation"));
        Assert.Matches(@"@media \(prefers-reduced-motion: reduce\) \{ \.omni-theme-scope \{ animation: none; \} \}", Css);

        // A field drawn on a canvas sits under the content and takes no pointer; while it draws, the CSS
        // field it replaces is dropped.
        var canvas = ShippedLookTests.Body(".omni-theme-scope__canvas");
        Assert.Equal("-1", ShippedLookTests.Value(canvas, "z-index"));
        Assert.Equal("none", ShippedLookTests.Value(canvas, "pointer-events"));
        Assert.Equal("none", ShippedLookTests.Value(ShippedLookTests.Body(".omni-theme-scope--canvas"), "background-image"));
    }

    [Fact]
    public void Trou_noir_draws_its_field_on_a_canvas_and_no_other_theme_has_one()
    {
        // Owner decision of 2026-10-01: the hole is drawn by a shader, no other theme needs one. Owner decision
        // of 2026-10-09: in light mode the same hole is drawn turned over, in ink (white-hole).
        Assert.Equal(["Trou noir"], OmniThemePresets.All.Where(theme => theme.Shape.ContainsKey("--omni-scope-canvas")).Select(theme => theme.Name));
        var preset = OmniThemePresets.All.Single(theme => theme.Name == "Trou noir");
        Assert.Equal("white-hole", preset.Light["--omni-scope-canvas"]);
        Assert.Equal("black-hole", preset.Dark["--omni-scope-canvas"]);

        var trouNoir = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, preset)
            .Add(component => component.BackdropMotion, false)
            .AddChildContent("<p>page</p>"));
        var canvas = trouNoir.Find(".omni-theme-scope > canvas.omni-theme-scope__canvas");
        Assert.Equal("true", canvas.GetAttribute("aria-hidden"));
        var start = Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "start");
        Assert.Equal(false, start.Arguments[2]);

        // Leaving the theme drops the canvas and stops it, still field included: no loop would notice.
        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "sweep");
        trouNoir.Render(parameters => parameters.Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre")));
        Assert.Empty(trouNoir.FindAll("canvas"));
        Assert.Single(JSInterop.Invocations, invocation => invocation.Identifier == "sweep");

        var givre = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
            .AddChildContent("<p>page</p>"));
        Assert.Empty(givre.FindAll("canvas"));
    }

    [Fact]
    public void Trou_noir_paints_its_own_page_in_each_mode_and_no_other_theme_does()
    {
        // Owner decision of 2026-10-02: a deep black page whatever the palette, the surface three quarters
        // of the way to black; owner decision of 2026-10-09: in light mode the surface a quarter of the way
        // to white. Every other scope keeps the palette surface.
        Assert.Equal(["Trou noir"], OmniThemePresets.All.Where(theme => theme.Shape.ContainsKey("--omni-scope-page")).Select(theme => theme.Name));
        var trouNoir = OmniThemePresets.All.Single(theme => theme.Name == "Trou noir");
        Assert.Equal("color-mix(in srgb, var(--omni-color-surface) 25%, rgb(0 0 0 / 100%))", trouNoir.Dark["--omni-scope-page"]);
        Assert.Equal("color-mix(in srgb, var(--omni-color-surface) 75%, rgb(255 255 255 / 100%))", trouNoir.Light["--omni-scope-page"]);

        Assert.Equal("var(--omni-scope-page, var(--omni-color-surface))", ShippedLookTests.Value(ShippedLookTests.Body(".omni-theme-scope"), "background"));
        // initial: the var() fallback, so a scope without the token keeps its surface.
        Assert.Equal("initial", ShippedLookTests.Value(ShippedLookTests.Body("[data-omni-theme]"), "--omni-scope-page"));
    }

    [Fact]
    public void Trou_noir_has_both_halves_and_its_light_one_is_drawn_in_light_mode()
    {
        // Owner decision of 2026-10-09: Trou noir is no longer dark only; no theme of the catalogue is.
        var trouNoir = OmniThemePresets.All.Single(theme => theme.Name == "Trou noir");
        Assert.False(trouNoir.DarkOnly);
        Assert.DoesNotContain(OmniThemePresets.All, theme => theme.DarkOnly);

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, trouNoir)
            .Add(component => component.BackdropMotion, false)
            .AddChildContent("<p>page</p>"));
        Assert.Equal("light", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme"));
    }

    [Fact]
    public void A_dark_only_theme_draws_its_dark_half_whatever_the_mode_and_fixes_the_mode_setting()
    {
        // No theme of the catalogue is dark only any more; the property stays for a host's own theme.
        var darkOnly = OmniThemePresets.All[0] with { DarkOnly = true };

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, darkOnly)
            .AddChildContent("<p>page</p>"));
        Assert.Equal("dark", scope.Find(".omni-theme-scope").GetAttribute("data-omni-theme"));

        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Light)
            .Add(component => component.Preset, darkOnly)
            .Add(component => component.AppearanceChanged, _ => { }));
        var mode = settings.Find(".omni-select-bar");
        Assert.Equal("true", mode.GetAttribute("aria-disabled"));
        Assert.Equal("Ce thème est toujours sombre", mode.GetAttribute("title"));
        Assert.Equal("true", settings.Find(".omni-select-bar__item--selected").GetAttribute("aria-checked"));
        Assert.Contains("Sombre", settings.Find(".omni-select-bar__item--selected").TextContent);
    }

    [Fact]
    public void The_shell_lets_a_theme_field_through_and_is_frosted_by_the_hook_of_the_cards()
    {
        const string fill = "var(--omni-shell-background, var(--omni-color-surface))";
        Assert.Equal(fill, ShippedLookTests.Value(ShippedLookTests.Body(".omni-header"), "background"));
        Assert.Equal(fill, ShippedLookTests.Value(ShippedLookTests.Body(".omni-sidebar__panel"), "background"));
        Assert.Equal("initial", ShippedLookTests.Value(ShippedLookTests.Body("[data-omni-theme]"), "--omni-shell-background"));

        var frost = ShippedLookTests.Body(".omni-header::before,\n.omni-sidebar::before");
        Assert.Equal("var(--omni-card-filter, none)", ShippedLookTests.Value(frost, "backdrop-filter"));
        Assert.Equal("-1", ShippedLookTests.Value(frost, "z-index"));
        Assert.Equal("none", ShippedLookTests.Value(frost, "pointer-events"));

        // A panel floating over the page has no frosted slot under it: it takes the dense pane.
        Assert.Equal(
            "var(--omni-dialog-background, var(--omni-shell-background, var(--omni-color-surface)))",
            ShippedLookTests.Value(ShippedLookTests.Body(".omni-sidebar--overlay.omni-sidebar--open > .omni-sidebar__panel"), "background"));

        // A translucent shell is only readable frosted: a theme that sets one frosts its cards too.
        Assert.All(OmniThemePresets.All.Where(preset => preset.Shape.ContainsKey("--omni-shell-background")), preset =>
            Assert.True(preset.Shape.ContainsKey("--omni-card-filter"), preset.Name));
    }

    [Fact]
    public void A_window_lays_its_fill_over_the_surface_of_the_page()
    {
        // A modeless window has no scrim under it: a dialog fill made for a frosted scrim (Givre's,
        // translucent) would let the page read through, so the fill is stacked on the opaque surface.
        var fill = ShippedLookTests.Value(ShippedLookTests.Body(".omni-window-layer > .omni-dialog"), "background");
        Assert.StartsWith("linear-gradient(var(--omni-dialog-background, ", fill, StringComparison.Ordinal);
        Assert.EndsWith(", var(--omni-color-surface)", fill, StringComparison.Ordinal);
    }

    // ---- The catalogue ----

    [Fact]
    public void A_floating_layer_always_has_an_edge()
    {
        // A menu or a window drawn without a shadow and without a rule cannot be told from the page
        // under it: a theme that removes the floating shadow keeps the card rule around the layer.
        Assert.All(OmniThemePresets.All.Where(preset => preset.Shape.GetValueOrDefault("--omni-overlay-shadow") == NoShadow), preset =>
            Assert.True(preset.Shape.GetValueOrDefault("--omni-card-border-color") is not (null or "transparent"), preset.Name));
    }

    [Fact]
    public void A_theme_moves_its_field_exactly_when_its_field_reads_the_turning_angle()
    {
        var moving = new List<string>();
        foreach (var preset in OmniThemePresets.All)
        {
            // The angle turns the CSS field (Givre); a field drawn on a canvas (Trou noir) moves with its shader.
            var reads = (preset.Shape.TryGetValue("--omni-backdrop", out var drawn) && drawn.Contains(Turn, StringComparison.Ordinal))
                || preset.Shape.ContainsKey("--omni-scope-canvas");
            var moves = preset.Shape.TryGetValue(MotionToken, out var motion);
            Assert.True(reads == moves, $"{preset.Name}: the field reads the angle = {reads}, the theme sets the motion = {moves}.");
            Assert.False(preset.DarkShape.ContainsKey(MotionToken), $"{preset.Name}: the motion is one for both modes.");
            if (moves)
            {
                // One slow turn, at constant speed, for ever: a pace under a minute would draw the eye.
                var match = Regex.Match(motion!, @"^omni-scope-turn (?<seconds>\d+)s linear infinite$");
                Assert.True(match.Success, $"{preset.Name}: {motion}");
                Assert.True(int.Parse(match.Groups["seconds"].Value, System.Globalization.CultureInfo.InvariantCulture) >= 60, $"{preset.Name}: {motion}");
                moving.Add(preset.Name);
            }
        }

        // Owner decision of 2026-09-30: Givre and Trou noir move their field, no other theme does.
        Assert.Equal(["Givre", "Trou noir"], moving);
    }

    [Fact]
    public void A_filled_alert_takes_the_shape_of_its_theme()
    {
        foreach (var preset in OmniThemePresets.All.Skip(1))
        {
            var shape = preset.Shape;
            var cardRadius = shape.GetValueOrDefault("--omni-card-radius", shape.GetValueOrDefault("--omni-radius", string.Empty));
            var rounds = Regex.Match(cardRadius, @"^(?<rem>\d+(?:\.\d+)?)rem") is { Success: true } match
                && double.Parse(match.Groups["rem"].Value, System.Globalization.CultureInfo.InvariantCulture) >= 0.5;
            if (rounds || shape.GetValueOrDefault("--omni-radius") == "0")
            {
                Assert.True(shape.ContainsKey("--omni-alert-radius"), $"{preset.Name}: cards at {cardRadius}, alerts left at the shipped radius.");
            }

            if (shape.GetValueOrDefault("--omni-radius") == "0")
            {
                Assert.Equal("0", shape["--omni-alert-radius"]);
            }

            // A theme that draws its cards without any shadow draws its filled alerts and its buttons
            // without one: left unset, both would keep the shipped elevation.
            if (shape.GetValueOrDefault("--omni-card-shadow") == NoShadow)
            {
                Assert.Equal(NoShadow, shape.GetValueOrDefault("--omni-alert-shadow"));
                Assert.Equal(NoShadow, shape.GetValueOrDefault("--omni-button-shadow"));
            }
        }

        var byName = OmniThemePresets.All.ToDictionary(preset => preset.Name, preset => preset.Shape, StringComparer.Ordinal);
        Assert.Equal(NoShadow, byName["Papier"]["--omni-alert-shadow"]);
        Assert.Equal("0.25rem 0.25rem 0 var(--omni-color-text)", byName["Rétro"]["--omni-alert-shadow"]);
        Assert.Equal(byName["Octet"]["--omni-button-shadow"], byName["Octet"]["--omni-alert-shadow"]);
    }

    // ---- The components ----

    [Theory]
    [InlineData(true, null)]
    [InlineData(false, "off")]
    public void The_scope_holds_the_field_still_through_an_attribute(bool moves, string? expected)
    {
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.BackdropMotion, moves)
            .AddChildContent("Contenu"));

        Assert.Equal(expected, scope.Find(".omni-theme-scope").GetAttribute("data-omni-backdrop-motion"));
    }

    [Theory]
    [InlineData("Givre", true)]
    [InlineData("Trou noir", true)]
    [InlineData("Essentiel", false)]
    [InlineData("Néon", false)]
    public void The_window_offers_the_setting_only_under_a_theme_that_moves_its_field(string themeName, bool offered)
    {
        bool? picked = null;
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == themeName))
            .Add(component => component.BackdropMotion, true)
            .Add(component => component.BackdropMotionChanged, value => picked = value));

        var switches = window.FindAll("[role=switch]");
        if (!offered)
        {
            Assert.Empty(switches);
            return;
        }

        var toggle = Assert.Single(switches);
        Assert.Equal("true", toggle.GetAttribute("aria-checked"));
        Assert.Equal("Fond animé", window.Find("#" + toggle.GetAttribute("aria-labelledby")).TextContent.Trim());
        toggle.Click();
        Assert.False(picked);
    }

    [Fact]
    public void The_window_offers_no_setting_the_host_did_not_bind()
    {
        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
            .Add(component => component.PresetChanged, _ => { }));

        Assert.Empty(window.FindAll("[role=switch]"));
    }

    [Fact]
    public void The_settings_hand_the_choice_of_the_window_to_the_host()
    {
        bool? picked = null;
        var settings = Render<OmniAppearanceSettings>(parameters => parameters
            .Add(component => component.Preset, OmniThemePresets.All.Single(theme => theme.Name == "Givre"))
            .Add(component => component.BackdropMotion, false)
            .Add(component => component.BackdropMotionChanged, value => picked = value));

        settings.Find(".omni-appearance-settings__row--scale button").Click();
        var toggle = settings.Find("[role=switch]");
        Assert.Equal("false", toggle.GetAttribute("aria-checked"));
        toggle.Click();
        Assert.True(picked);
    }
}
