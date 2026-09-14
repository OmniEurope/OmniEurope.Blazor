namespace OmniEurope.Blazor.Components;

public partial class OmniProfileMenu
{
    private ElementReference _details;
    private OmniDisclosureDismissal? _dismissal;

    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label)
        ? Localize("ProfileMenuLabel")
        : Label;

    [Parameter, EditorRequired]
    public RenderFragment? Summary { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Closes the open menu when a press lands outside it, which is what a menu is expected to do.
    /// On by default; a page that drives the menu from controls of its own can turn it off, or mark
    /// those controls with <c>data-omni-keep-open</c> so a press on them never counts as outside.
    /// Escape and choosing an item close the menu either way.
    /// </summary>
    [Parameter]
    public bool CloseOnOutsideClick { get; set; } = true;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        _dismissal ??= new OmniDisclosureDismissal(JavaScript);
        return _dismissal.ApplyAsync(_details, CloseOnOutsideClick, closeOnItem: true);
    }

    public async ValueTask DisposeAsync()
    {
        if (_dismissal is not null)
        {
            await _dismissal.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
