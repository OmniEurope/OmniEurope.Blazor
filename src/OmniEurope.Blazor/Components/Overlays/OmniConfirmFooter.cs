namespace OmniEurope.Blazor.Components;

/// <summary>
/// The footer of a confirmation: the action, then cancelling in <see cref="OmniConfirmRequest.CancelVariant"/>,
/// each with an icon and a label. Internal and built in code, so the convention ships with the
/// service rather than as one more public component to place by hand.
/// </summary>
internal sealed class OmniConfirmFooter : OmniComponentBase
{
    [Parameter, EditorRequired]
    public OmniConfirmRequest Request { get; set; } = default!;

    [Parameter, EditorRequired]
    public OmniOverlayService Service { get; set; } = default!;

    private string ConfirmText => string.IsNullOrWhiteSpace(Request.ConfirmText) ? Localize("ConfirmAction") : Request.ConfirmText;

    private string CancelText => string.IsNullOrWhiteSpace(Request.CancelText) ? Localize("Cancel") : Request.CancelText;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Button(builder, 0, Request.ConfirmVariant, Request.ConfirmIcon, ConfirmText, "omni-confirm__action", confirmed: true);
        Button(builder, 10, Request.CancelVariant, OmniIconName.Close, CancelText, "omni-confirm__cancel", confirmed: false);
    }

    private void Button(RenderTreeBuilder builder, int sequence, OmniButtonVariant variant, OmniIconName icon, string text, string cssClass, bool confirmed)
    {
        builder.OpenComponent<OmniButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniButton.Variant), variant);
        builder.AddComponentParameter(sequence + 2, nameof(OmniButton.Class), cssClass);
        builder.AddComponentParameter(sequence + 3, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => Service.CloseDialog(confirmed)));
        builder.AddComponentParameter(sequence + 4, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
        {
            content.OpenComponent<OmniIcon>(0);
            content.AddComponentParameter(1, nameof(OmniIcon.Name), icon);
            content.CloseComponent();
            content.OpenElement(2, "span");
            content.AddContent(3, text);
            content.CloseElement();
        }));
        builder.CloseComponent();
    }
}
