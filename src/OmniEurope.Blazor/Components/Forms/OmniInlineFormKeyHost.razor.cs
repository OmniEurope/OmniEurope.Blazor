using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Wraps the editors of an inline creation or edition row and answers two keys for them: Ctrl+Enter
/// (Cmd+Enter on a Mac) submits, Escape cancels. Plain Enter is left alone, in a text area as in a
/// selector that uses it to accept the suggestion it shows. Escape pressed inside an open popup also
/// reaches this host, since the popup closes itself without hiding the key.
/// </summary>
public partial class OmniInlineFormKeyHost
{
    /// <summary>The editors the keys apply to.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Raised on Ctrl+Enter or Cmd+Enter.</summary>
    [Parameter]
    public EventCallback OnSubmit { get; set; }

    /// <summary>Raised on Escape. Without a handler, Escape does nothing here.</summary>
    [Parameter]
    public EventCallback OnCancel { get; set; }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Enter" or "NumpadEnter" && (args.CtrlKey || args.MetaKey))
        {
            await OnSubmit.InvokeAsync();
        }
        else if (args.Key == "Escape")
        {
            await OnCancel.InvokeAsync();
        }
    }
}
