namespace OmniEurope.Blazor.Components;

public partial class OmniThemeScope
{
    private ElementReference _element;
    private IJSObjectReference? _themeModule;
    private OmniThemePreset? _appliedPreset;
    private OmniAppearance _appliedAppearance;

    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public OmniAppearance Appearance { get; set; }

    [Parameter]
    public OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    /// <summary>
    /// A palette from <see cref="OmniThemePresets"/> to paint this scope with, or null for the
    /// shipped theme. Only the scope and what it contains change; the half used follows
    /// <see cref="Appearance"/>, and <see cref="OmniAppearance.System"/> follows the system setting
    /// as it changes.
    /// </summary>
    /// <remarks>
    /// The values are written through the CSSOM by <c>omni-theme.js</c>, never as a style attribute,
    /// so the strict content security policy holds. A scope that never receives a preset never loads
    /// the script.
    /// </remarks>
    [Parameter]
    public OmniThemePreset? Preset { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ReferenceEquals(Preset, _appliedPreset) && (Preset is null || Appearance == _appliedAppearance))
        {
            return;
        }

        _themeModule ??= await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-theme.js");
        if (Preset is null)
        {
            await _themeModule.InvokeVoidAsync("clear", _element);
        }
        else
        {
            await _themeModule.InvokeVoidAsync("apply", _element, Preset.Light, Preset.Dark, Appearance.ToString().ToLowerInvariant());
        }

        _appliedPreset = Preset;
        _appliedAppearance = Appearance;
    }

    public async ValueTask DisposeAsync()
    {
        if (_themeModule is not null)
        {
            try
            {
                await _themeModule.InvokeVoidAsync("clear", _element);
                await _themeModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
