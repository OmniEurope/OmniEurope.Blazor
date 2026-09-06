namespace OmniEurope.Blazor.Components;

public partial class OmniTooltip
{
    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public int? TabIndex { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Resolved through the provider rather than injected directly: a host that never called
    /// <c>AddOmniEuropeBlazor</c> would fail to render the component at all under <c>[Inject]</c>,
    /// where here it simply keeps the stylesheet's anchored tooltip instead of the pointer-tracking
    /// one.
    /// </summary>
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private string TooltipId => $"{Id ?? "omni-tooltip"}-content";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // Scoped, so the tooltips of one page share a single import and a single set of listeners
        // however many of them a grid renders.
        if (Services.GetService(typeof(OmniTooltipInterop)) is OmniTooltipInterop interop)
        {
            await interop.EnsureInstalledAsync();
        }
    }
}
