using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A theme scope paints itself with a catalogue palette through its script, and only when asked.
/// </summary>
public sealed class ThemeScopePresetTests : OmniBunitContext
{
    private const string ThemeModule = "./_content/OmniEurope.Blazor/omni-theme.js";

    [Fact]
    public void A_scope_without_a_preset_never_loads_the_theme_script()
    {
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .AddChildContent("Contenu"));

        Assert.DoesNotContain(JSInterop.Invocations, invocation => invocation.Identifier == "import");
        Assert.DoesNotContain("style=", scope.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_preset_is_sent_with_both_halves_and_the_appearance_it_follows()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var preset = OmniThemePresets.All.Single(entry => entry.Name == "Néon");

        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.System)
            .Add(component => component.Preset, preset)
            .AddChildContent("Contenu"));

        var invocation = Assert.Single(module.Invocations["apply"]);
        Assert.Same(preset.Light, invocation.Arguments[1]);
        Assert.Same(preset.Dark, invocation.Arguments[2]);
        Assert.Equal("system", invocation.Arguments[3]);
        Assert.DoesNotContain("style=", scope.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dropping_the_preset_clears_the_scope_and_an_unchanged_one_is_not_sent_again()
    {
        var module = JSInterop.SetupModule(ThemeModule);
        var preset = OmniThemePresets.All[0];
        var scope = Render<OmniThemeScope>(parameters => parameters
            .Add(component => component.Preset, preset)
            .AddChildContent("Contenu"));

        scope.Render(parameters => parameters.Add(component => component.Preset, preset));
        Assert.Single(module.Invocations["apply"]);

        scope.Render(parameters => parameters.Add(component => component.Appearance, OmniAppearance.Dark));
        Assert.Equal(2, module.Invocations["apply"].Count);

        scope.Render(parameters => parameters.Add(component => component.Preset, (OmniThemePreset?)null));
        Assert.Single(module.Invocations["clear"]);
    }
}
