using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A parent that binds <see cref="OmniDataGrid{TItem}.Load"/> to a method group passes a new
/// delegate on every render: the grid must recognise the same method on the same target and not
/// reload, while a genuinely different loader still reloads.
/// </summary>
public sealed class DataGridLoaderIdentityTests : OmniBunitContext
{
    [Fact]
    public void ParentRerender_WithTheSameMethodAsANewDelegate_DoesNotReload()
    {
        var source = new CountingSource();
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Load, source.LoadAsync));

        Assert.Equal(1, source.Calls);

        // What a parent render does with Load="LoadAsync": a new delegate for the same method.
        grid.Render(parameters => parameters.Add(component => component.Load, new Func<OmniDataGridLoadRequest, Task<OmniDataGridResult<int>>>(source.LoadAsync)));

        Assert.Equal(1, source.Calls);

        var other = new CountingSource();
        grid.Render(parameters => parameters.Add(component => component.Load, other.LoadAsync));

        Assert.Equal(1, other.Calls);
    }

    private sealed class CountingSource
    {
        public int Calls { get; private set; }

        public Task<OmniDataGridResult<int>> LoadAsync(OmniDataGridLoadRequest request)
        {
            Calls++;
            return Task.FromResult(new OmniDataGridResult<int>([1, 2, 3], 3));
        }
    }
}
