namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the host asks of the user under the question of a confirmation (<see cref="OmniConfirmRequest.Details"/>):
/// drawn in a block of its own, and drawn again when the request is refreshed. Internal and built in code,
/// like <see cref="OmniConfirmFooter"/>. It reads nothing of the content it holds.
/// </summary>
internal sealed class OmniConfirmDetails : OmniComponentBase, IDisposable
{
    private readonly ConfirmRequestRedraw _redraw;

    public OmniConfirmDetails()
    {
        _redraw = new ConfirmRequestRedraw(Redraw);
    }

    [Parameter, EditorRequired]
    public OmniConfirmRequest Request { get; set; } = default!;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        _redraw.Follow(Request);
    }

    public void Dispose() => _redraw.Dispose();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "omni-confirm__details");
        builder.AddContent(2, Request.Details);
        builder.CloseElement();
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}
