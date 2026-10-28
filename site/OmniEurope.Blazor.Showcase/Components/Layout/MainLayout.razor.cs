using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Showcase.Gallery;
using OmniEurope.Blazor.Showcase.Localization;
using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    /// <summary>The id of the sidebar, the one its toggle controls.</summary>
    private const string SidebarId = "showcase-sidebar";

    private ErrorBoundary? _errorBoundary;
    private DotNetObjectReference<MainLayout>? _viewportHandler;
    private bool _sidebarOpen = true;
    private bool _narrowViewport;
    private bool _appearanceWindowOpen;
    private bool _disposed;

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private ThemeState Theme { get; set; } = default!;

    [Inject]
    private ShowcaseLanguageSwitcher Languages { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    /// <summary>The overlay service the pages inject; the host draws its dialogs and notifications.</summary>
    [Inject]
    private OmniOverlayService Overlay { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The mode the scope draws: the one chosen, except under a theme drawn in dark mode only, always dark.</summary>
    private OmniAppearance CurrentAppearance => ToAppearance(Theme.EffectiveMode);

    /// <summary>The mode the menu and the window show as chosen (a dark-only theme fixes it on its own).</summary>
    private OmniAppearance MenuAppearance => ToAppearance(Theme.Mode);

    /// <summary>The theme the menu and the window show: null while it is the first of the catalogue, the shipped one.</summary>
    private OmniThemePreset? LookPreset => ReferenceEquals(Theme.Theme, OmniThemePresets.All[0]) ? null : Theme.Theme;

    /// <summary>The palette the window shows: null while it is the one the theme comes with.</summary>
    private OmniThemePalette? WindowPalette => Theme.HasThemePalette ? null : Theme.Palette;

    /// <summary>The version line of the application menu: the version of the library the showcase presents.</summary>
    private string VersionLine => string.Format(
        System.Globalization.CultureInfo.CurrentCulture,
        Text["LayoutVersion"].Value,
        typeof(OmniButton).Assembly.GetName().Version?.ToString(3));

    /// <summary>
    /// How the menu opens right now: pushed beside the page on a wide screen, overlaid on a phone, where
    /// there is no width to share with the page.
    /// </summary>
    private OmniSidebarReveal EffectiveReveal => _narrowViewport ? OmniSidebarReveal.Overlay : OmniSidebarReveal.Push;

    /// <summary>What a closed menu leaves: a rail of icons on a wide screen, nothing on a phone.</summary>
    private OmniSidebarCollapse EffectiveCollapse => _narrowViewport ? OmniSidebarCollapse.Hidden : OmniSidebarCollapse.Icons;

    /// <summary>Whether the page shown belongs to the gallery, whose menu then takes the place of the site's.</summary>
    private bool InGallery
    {
        get
        {
            var path = Navigation.ToBaseRelativePath(Navigation.Uri);
            var end = path.IndexOfAny(['/', '?', '#']);
            return string.Equals(end < 0 ? path : path[..end], GalleryPage.RoutePrefix, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Called by showcase-layout.js at every crossing of the phone breakpoint.</summary>
    /// <param name="narrow">Whether the window is now a phone's.</param>
    /// <returns>A task that completes once the layout is redrawn.</returns>
    [JSInvokable]
    public Task OnViewportChanged(bool narrow)
    {
        _narrowViewport = narrow;

        // Going to a phone closes the menu: it covers the page there. Going back to a wide screen does not
        // open it again, or a simple resize would undo a closing the visitor chose.
        if (narrow)
        {
            _sidebarOpen = false;
        }

        return InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        Theme.Changed += OnThemeChanged;
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await Theme.InitializeAsync().ConfigureAwait(false);

        // Read once, then followed: the subscription reports only the crossings, not the state a page opens in.
        _narrowViewport = await Js.InvokeAsync<bool>("omniShowcaseLayout.narrow");
        _sidebarOpen = !_narrowViewport;
        _viewportHandler = DotNetObjectReference.Create(this);
        await Js.InvokeVoidAsync("omniShowcaseLayout.watch", _viewportHandler);
        await InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Theme.Changed -= OnThemeChanged;
        Navigation.LocationChanged -= OnLocationChanged;
        if (_viewportHandler is not null)
        {
            try
            {
                await Js.InvokeVoidAsync("omniShowcaseLayout.unwatch");
            }
            catch (Exception exception) when (exception is JSException or JSDisconnectedException)
            {
                // The browser is gone: nothing is left to unsubscribe from.
            }

            _viewportHandler.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private static OmniAppearance ToAppearance(ThemeMode mode) => mode switch
    {
        ThemeMode.Dark => OmniAppearance.Dark,
        ThemeMode.System => OmniAppearance.System,
        _ => OmniAppearance.Light
    };

    private static ThemeMode ToMode(OmniAppearance appearance) => appearance switch
    {
        OmniAppearance.Dark => ThemeMode.Dark,
        OmniAppearance.System => ThemeMode.System,
        _ => ThemeMode.Light
    };

    private void SetSidebarOpen(bool open) => _sidebarOpen = open;

    private void OpenAppearanceWindow() => _appearanceWindowOpen = true;

    private void SetAppearanceWindowOpen(bool open) => _appearanceWindowOpen = open;

    private Task ChangeLanguageAsync(string code) => Languages.SwitchAsync(code);

    private Task SetAppearanceAsync(OmniAppearance appearance) => Theme.SetModeAsync(ToMode(appearance));

    /// <summary>A theme comes whole with its own palette; null is the first theme of the catalogue.</summary>
    private Task SetPresetAsync(OmniThemePreset? preset) => Theme.SelectThemeAsync(preset ?? OmniThemePresets.All[0]);

    /// <summary>A palette repaints the theme; null returns to the palette the theme comes with.</summary>
    private Task SetPaletteAsync(OmniThemePalette? palette) =>
        palette is null ? Theme.ResetPaletteAsync() : Theme.SelectPaletteAsync(palette);

    private Task SetDensityAsync(OmniDensity density) => Theme.SetDensityAsync(density);

    private Task SetBackdropMotionAsync(bool moves) => Theme.SetBackdropMotionAsync(moves);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => InvokeAsync(StateHasChanged);

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private Task RecoverAsync()
    {
        _errorBoundary?.Recover();
        return Task.CompletedTask;
    }
}
