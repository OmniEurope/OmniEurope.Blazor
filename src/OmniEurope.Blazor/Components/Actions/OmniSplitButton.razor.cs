namespace OmniEurope.Blazor.Components;

public partial class OmniSplitButton
{
    private readonly string _focusKey = $"split-button-{Guid.NewGuid():N}";
    private bool _open;
    private ElementReference _toggle;
    private ElementReference _menu;
    private IJSObjectReference? _focusModule;
    private bool _focusActivated;

    [Parameter, EditorRequired]
    public string Text { get; set; } = string.Empty;

    [Parameter]
    public string MenuLabel { get; set; } = string.Empty;

    private string EffectiveMenuLabel => string.IsNullOrWhiteSpace(MenuLabel)
        ? Localize("SplitButtonMenuLabel")
        : MenuLabel;

    /// <summary>The same three sizes as <see cref="OmniButton"/>, main part and menu part together.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>
    /// What the button means, main part and menu part together, with the fills of
    /// <see cref="OmniButton"/>. <see cref="OmniButtonVariant.Secondary"/> by default: the surface,
    /// border and text colours the split button has always had, rendered without a variant class,
    /// exactly as before.
    /// </summary>
    [Parameter]
    public OmniButtonVariant Variant { get; set; } = OmniButtonVariant.Secondary;

    /// <summary>
    /// Icon drawn before <see cref="Text"/> in the main part. None by default, as before; the menu
    /// part keeps its chevron either way.
    /// </summary>
    [Parameter]
    public OmniIconName? Icon { get; set; }

    private string? VariantClass => Variant == OmniButtonVariant.Secondary
        ? null
        : $"omni-split-button--{Variant.ToString().ToLowerInvariant()}";

    private OmniControlSize IconSize => Size == OmniControlSize.Small ? OmniControlSize.Small : OmniControlSize.Medium;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Busy { get; set; }

    [Parameter]
    public EventCallback OnClick { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private Task InvokeMainAsync() => Disabled || Busy ? Task.CompletedTask : OnClick.InvokeAsync();
    private Task ToggleMenuAsync()
    {
        if (!Disabled && !Busy)
        {
            _open = !_open;
        }

        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _focusModule = await JavaScript.InvokeAsync<IJSObjectReference>("import", "./_content/OmniEurope.Blazor/omni-focus.js");
        }

        if (_focusModule is null)
        {
            return;
        }

        if (_open && !_focusActivated)
        {
            _focusActivated = true;
            await _focusModule.InvokeVoidAsync("activateMenu", _menu, _focusKey);
        }
        else if (!_open && _focusActivated)
        {
            _focusActivated = false;
            await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Escape")
        {
            _open = false;
        }
        else if (args.Key is "ArrowDown" or "Enter" or " ")
        {
            if (!_open)
            {
                // A busy button stays focusable, so the keyboard would otherwise open the menu that disabled used to block.
                _open = !Disabled && !Busy;
            }
            else if (_focusModule is not null)
            {
                await _focusModule.InvokeVoidAsync("moveMenuFocus", _menu, "ArrowDown");
            }
        }
        else if (_open && args.Key is "ArrowUp" or "Home" or "End" && _focusModule is not null)
        {
            await _focusModule.InvokeVoidAsync("moveMenuFocus", _menu, args.Key);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_focusModule is not null)
        {
            try
            {
                await _focusModule.InvokeVoidAsync("restoreFocus", _focusKey);
                await _focusModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
