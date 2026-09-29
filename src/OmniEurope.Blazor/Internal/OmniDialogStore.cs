using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class OmniDialogStore
{
    private readonly List<OmniDialogRequest> _dialogs = [];

    internal OmniDialogRequest? Current => _dialogs.LastOrDefault();
    internal IReadOnlyList<OmniDialogRequest> Items => _dialogs;

    internal void Push(OmniDialogRequest request) => _dialogs.Add(request);

    /// <summary>Removes every dialog; true when there was at least one.</summary>
    internal bool Clear()
    {
        var had = _dialogs.Count > 0;
        _dialogs.Clear();
        return had;
    }

    internal bool Pop()
    {
        if (_dialogs.Count == 0)
        {
            return false;
        }

        _dialogs.RemoveAt(_dialogs.Count - 1);
        return true;
    }
}
