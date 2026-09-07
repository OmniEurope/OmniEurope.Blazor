using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public partial class DynamicDataGridTestHost
{
    public bool ShowNameColumn { get; private set; } = true;
    public List<OmniDataGridLoadRequest> Requests { get; } = [];

    public void RemoveNameColumn()
    {
        ShowNameColumn = false;
        StateHasChanged();
    }

    private Task<OmniDataGridResult<DynamicDataGridRow>> LoadAsync(OmniDataGridLoadRequest request)
    {
        Requests.Add(request);
        return Task.FromResult(new OmniDataGridResult<DynamicDataGridRow>([new(1, "Alice", 30)], 1));
    }
}
