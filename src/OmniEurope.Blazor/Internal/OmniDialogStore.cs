using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class OmniDialogStore
{
    private readonly List<OmniDialogRequest> _dialogs = [];
    // One identity per opened dialog, its render key: requests are records, so two dialogs opened with
    // equal values (a button pressed twice) would otherwise share a key and break the render.
    private readonly List<object> _keys = [];

    internal OmniDialogRequest? Current => _dialogs.LastOrDefault();
    internal IReadOnlyList<OmniDialogRequest> Items => _dialogs;

    /// <summary>The render key of the dialog at <paramref name="index"/>, unique to that opening.</summary>
    /// <param name="index">The dialog's place in the stack.</param>
    internal object KeyAt(int index) => _keys[index];

    internal void Push(OmniDialogRequest request)
    {
        _dialogs.Add(request);
        _keys.Add(new object());
    }

    /// <summary>Removes every dialog; true when there was at least one.</summary>
    internal bool Clear()
    {
        var had = _dialogs.Count > 0;
        _dialogs.Clear();
        _keys.Clear();
        return had;
    }

    internal bool Pop()
    {
        if (_dialogs.Count == 0)
        {
            return false;
        }

        _dialogs.RemoveAt(_dialogs.Count - 1);
        _keys.RemoveAt(_keys.Count - 1);
        return true;
    }
}
