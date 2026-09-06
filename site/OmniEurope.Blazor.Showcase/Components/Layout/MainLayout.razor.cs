using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Showcase.Resources;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Showcase.Components.Layout;

public partial class MainLayout : IDisposable
{
    private ErrorBoundary? _errorBoundary;

    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    [Inject]
    private ThemeState Theme { get; set; } = default!;

    private OmniAppearance CurrentAppearance => Theme.Mode is ThemeMode.Dark ? OmniAppearance.Dark : OmniAppearance.Light;

    protected override void OnInitialized() => Theme.Changed += OnThemeChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Theme.InitializeAsync().ConfigureAwait(false);
        }
    }

    public void Dispose() => Theme.Changed -= OnThemeChanged;

    private Task ToggleModeAsync() =>
        Theme.SetModeAsync(Theme.Mode is ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark);

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);

    private Task RecoverAsync()
    {
        _errorBoundary?.Recover();
        return Task.CompletedTask;
    }
}
