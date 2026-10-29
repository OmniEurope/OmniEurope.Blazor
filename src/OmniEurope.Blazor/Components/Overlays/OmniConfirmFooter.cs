namespace OmniEurope.Blazor.Components;

/// <summary>
/// The footer of a confirmation: the action, then cancelling in <see cref="OmniConfirmRequest.CancelVariant"/>,
/// each with an icon and a label. Internal and built in code, so the convention ships with the
/// service rather than as one more public component to place by hand. The action is disabled while the
/// request's <see cref="OmniConfirmRequest.CanConfirm"/> says no, and the footer redraws when the request
/// is refreshed.
/// </summary>
internal sealed class OmniConfirmFooter : OmniComponentBase, IDisposable
{
    private OmniConfirmRequest? _subscribed;

    [Parameter, EditorRequired]
    public OmniConfirmRequest Request { get; set; } = default!;

    [Parameter, EditorRequired]
    public OmniOverlayService Service { get; set; } = default!;

    private string ConfirmText => string.IsNullOrWhiteSpace(Request.ConfirmText) ? Localize("ConfirmAction") : Request.ConfirmText;

    private string CancelText => string.IsNullOrWhiteSpace(Request.CancelText) ? Localize("Cancel") : Request.CancelText;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(_subscribed, Request))
        {
            return;
        }

        Unsubscribe();
        _subscribed = Request;
        _subscribed.Refreshed += Redraw;
    }

    public void Dispose() => Unsubscribe();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Button(builder, 0, Request.ConfirmVariant, Request.ConfirmIcon, ConfirmText, "omni-confirm__action", confirmed: true, Request.ConfirmIconContent);
        Button(builder, 10, Request.CancelVariant, OmniIconName.Close, CancelText, "omni-confirm__cancel", confirmed: false);
    }

    private void Unsubscribe()
    {
        if (_subscribed is not null)
        {
            _subscribed.Refreshed -= Redraw;
            _subscribed = null;
        }
    }

    private void Redraw() => _ = InvokeAsync(StateHasChanged);

    // The action is checked again when it is chosen, not only when it is drawn: a click that reaches the
    // button between a change of the host and the redraw must not answer true on a request that no longer allows it.
    private void Answer(bool confirmed)
    {
        if (confirmed && !Request.IsConfirmAllowed)
        {
            return;
        }

        Service.CloseDialog(confirmed);
    }

    private void Button(RenderTreeBuilder builder, int sequence, OmniButtonVariant variant, OmniIconName icon, string text, string cssClass, bool confirmed, RenderFragment? iconContent = null)
    {
        builder.OpenComponent<OmniButton>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(OmniButton.Variant), variant);
        builder.AddComponentParameter(sequence + 2, nameof(OmniButton.Class), cssClass);
        builder.AddComponentParameter(sequence + 3, nameof(OmniButton.OnClick), EventCallback.Factory.Create<MouseEventArgs>(this, () => Answer(confirmed)));
        builder.AddComponentParameter(sequence + 4, nameof(OmniButton.Icon), (RenderFragment)(iconBuilder =>
        {
            if (iconContent is not null)
            {
                iconBuilder.AddContent(0, iconContent);
            }
            else
            {
                iconBuilder.OpenComponent<OmniIcon>(1);
                iconBuilder.AddComponentParameter(2, nameof(OmniIcon.Name), icon);
                iconBuilder.CloseComponent();
            }
        }));
        builder.AddComponentParameter(sequence + 5, nameof(OmniButton.ChildContent), (RenderFragment)(content =>
        {
            content.OpenElement(0, "span");
            content.AddContent(1, text);
            content.CloseElement();
        }));
        if (confirmed)
        {
            builder.AddComponentParameter(sequence + 6, nameof(OmniButton.Disabled), !Request.IsConfirmAllowed);
        }

        builder.CloseComponent();
    }
}
