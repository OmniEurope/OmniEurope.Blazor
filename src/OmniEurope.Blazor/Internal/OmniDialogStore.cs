using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

internal sealed class OmniDialogStore
{
    private readonly List<OmniDialogRequest> _dialogs = [];

    internal OmniDialogRequest? Current => _dialogs.LastOrDefault();
    internal IReadOnlyList<OmniDialogRequest> Items => _dialogs;

    internal void Push(OmniDialogRequest request) => _dialogs.Add(request);

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
