using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// An entry of <see cref="OmniOverflowMenu"/>, <see cref="OmniContextMenu"/>, <see cref="OmniSplitButton"/>
/// or <see cref="OmniProfileMenu"/> that unfolds a list of <see cref="OmniMenuItem"/> (and, for a deeper
/// level, other <see cref="OmniSubMenu"/>) beside it. Usable wherever an <see cref="OmniMenuItem"/> is.
/// The entry is a <c>role="menuitem"</c> with <c>aria-haspopup="menu"</c> and <c>aria-expanded</c>, and
/// owns a nested <c>role="menu"</c> named by it. A click, Enter or Space on the entry opens or closes the
/// list and never closes the menu nor raises an action; the arrow toward the inline end opens it with
/// the focus on its first item, the arrow back or Escape closes that list alone, the pointer opens it by
/// hovering the entry; opening one list closes the one that was open at the same level. Choosing an
/// <see cref="OmniMenuItem"/> in a list closes the whole menu, as in the top level.
/// </summary>
public partial class OmniSubMenu
{
    private readonly string _generatedId = $"omni-submenu-{Guid.NewGuid():N}";
    private readonly OmniMenuLevel _entries = new();
    private OmniMenuLevel? _standalone;
    private Task<IJSObjectReference?>? _import;
    private bool _open;
    private bool _openedByPointer;
    private bool _focusFirst;
    private bool _placePending;
    private bool _disposed;

    [CascadingParameter]
    private IOmniMenu? Menu { get; set; }

    [CascadingParameter]
    private OmniMenuLevel? ParentLevel { get; set; }

    /// <summary>The visible text of the entry that opens the list.</summary>
    [Parameter, EditorRequired]
    public string? Label { get; set; }

    /// <summary>
    /// The icon before the label, in general an <see cref="OmniIcon"/>; decorative, the label names the
    /// entry. Without one the label keeps its column, so the labels of a menu stay aligned.
    /// </summary>
    [Parameter]
    public RenderFragment? Icon { get; set; }

    /// <summary>Disables the entry: it stays listed, dimmed, the arrows skip it and its list does not open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The list: <see cref="OmniMenuItem"/> and, for a deeper level, other <see cref="OmniSubMenu"/>.</summary>
    [Parameter, EditorRequired]
    public RenderFragment? ChildContent { get; set; }

    private string EntryId => Id ?? _generatedId;

    private string MenuId => $"{EntryId}-menu";

    /// <summary>The entries beside this one, among which only one list is open.</summary>
    private OmniMenuLevel Siblings => ParentLevel
        ?? (Menu is null ? _standalone ??= new OmniMenuLevel() : OmniMenuLevel.TopOf(Menu));

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Disabled && _open)
        {
            Forget();
        }
    }

    /// <summary>A click with a pointer behind it, or Enter or Space (detail 0), toggles the list.</summary>
    private async Task ToggleAsync(MouseEventArgs args)
    {
        if (Disabled)
        {
            return;
        }

        if (_open && _openedByPointer)
        {
            // The pointer opened it on arrival; the click that follows confirms it rather than undo it.
            _openedByPointer = false;
            _focusFirst = args.Detail == 0;
            _placePending = _focusFirst;
        }
        else if (_open)
        {
            await CloseAsync();
        }
        else
        {
            await OpenAsync(focusFirst: args.Detail == 0, byPointer: false);
        }
    }

    /// <summary>A mouse or a pen over the entry opens its list without taking the focus; touch uses the click.</summary>
    private Task HoverAsync(PointerEventArgs args) => Disabled || _open || args.PointerType is not ("mouse" or "pen")
        ? Task.CompletedTask
        : OpenAsync(focusFirst: false, byPointer: true);

    private async Task OpenAsync(bool focusFirst, bool byPointer)
    {
        await Siblings.OpenAsync(this, CloseAsync);
        _open = true;
        _openedByPointer = byPointer;
        _focusFirst = focusFirst;
        _placePending = true;
    }

    /// <summary>Closes the list, the focus it holds going back to the entry; called by the entry's own click and by a sibling that opens.</summary>
    private async Task CloseAsync()
    {
        if (!_open)
        {
            return;
        }

        await LeaveAsync();
        Forget();
        await InvokeAsync(StateHasChanged);
    }

    private void Forget()
    {
        _open = false;
        _openedByPointer = false;
        _focusFirst = false;
        _placePending = false;
        Siblings.Closed(this);
    }

    private async Task LeaveAsync()
    {
        if (_import is not { IsCompletedSuccessfully: true } loaded || loaded.Result is not { } module)
        {
            return;
        }

        try
        {
            await module.InvokeVoidAsync("leaveSubMenu", MenuId, EntryId);
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_open || !_placePending)
        {
            return;
        }

        _placePending = false;
        var focusFirst = _focusFirst;
        _focusFirst = false;
        _import ??= OmniScriptModule.ImportAsync(JavaScript, OmniModules.Focus, () => _disposed);
        if (await _import is { } module)
        {
            await module.InvokeVoidAsync("openSubMenu", EntryId, MenuId, focusFirst);
        }
    }

    /// <summary>Forgets the open list and releases the script.</summary>
    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        Siblings.Closed(this);
        if (_import is { IsCompletedSuccessfully: true } loaded && loaded.Result is { } module)
        {
            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
