namespace OmniEurope.Blazor.Components;

/// <summary>
/// The element of an <see cref="OmniPageVeil"/>, drawn again whenever the veil's parameters change, whoever
/// renders it: the veil itself, or the <see cref="OmniMain"/> that holds the veil, whose own render does not
/// follow the veil's parameters. Internal and built in code, like <see cref="OmniConfirmDetails"/>; it reads
/// everything from the veil, so there is one markup in either place.
/// </summary>
internal sealed class OmniPageVeilSurface : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public OmniPageVeil Veil { get; set; } = default!;

    protected override void OnInitialized() => Veil.Changed += Redraw;

    public void Dispose() => Veil.Changed -= Redraw;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!Veil.Busy)
        {
            return;
        }

        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, Veil.AdditionalAttributes);
        builder.AddAttribute(2, "id", Veil.Id);
        builder.AddAttribute(3, "class", Veil.RootClass);
        builder.AddAttribute(4, "role", "status");
        if (Veil.HasLogo)
        {
            builder.OpenElement(5, "span");
            builder.AddAttribute(6, "class", "omni-page-veil__logo");
            builder.AddAttribute(7, "aria-hidden", "true");
            if (Veil.Logo is not null)
            {
                builder.AddContent(8, Veil.Logo);
            }
            else
            {
                builder.OpenElement(9, "img");
                builder.AddAttribute(10, "class", "omni-page-veil__image");
                builder.AddAttribute(11, "src", Veil.LogoSource);
                builder.AddAttribute(12, "alt", string.Empty);
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        builder.OpenElement(13, "span");
        builder.AddAttribute(14, "class", "omni-visually-hidden");
        builder.AddContent(15, Veil.EffectiveLabel);
        builder.CloseElement();
        builder.CloseElement();
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}
