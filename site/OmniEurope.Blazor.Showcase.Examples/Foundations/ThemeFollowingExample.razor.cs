using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A theme scope, appearance settings and an appearance window that all follow the user's saved look.</summary>
public partial class ThemeFollowingExample : IDisposable
{
    private bool _windowOpen;

    [Inject]
    private OmniAppearanceState Appearance { get; set; } = default!;

    /// <summary>The look the preview follows, in words: the theme and the palette of the state.</summary>
    private string Caption => (Appearance.Preset, Appearance.Palette) switch
    {
        ({ } theme, { } palette) => $"theme {theme.Name}, palette {palette.Name}",
        ({ } theme, null) => $"theme {theme.Name}, its own palette",
        (null, { } palette) => $"palette {palette.Name}, shipped shape",
        _ => "shipped look"
    };

    /// <inheritdoc />
    protected override void OnInitialized() => Appearance.Changed += OnAppearanceChanged;

    private void OnAppearanceChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    public void Dispose()
    {
        Appearance.Changed -= OnAppearanceChanged;
        GC.SuppressFinalize(this);
    }
}
