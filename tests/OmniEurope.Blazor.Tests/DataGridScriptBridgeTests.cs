using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// What the grid asks of its script beyond the first render: the wheel scope and the fill mode followed
/// as they change and undone at disposal, the filter popovers closed after a pick, and a virtual grid
/// released on a lost circuit.
/// </summary>
public sealed class DataGridScriptBridgeTests : OmniBunitContext
{
    private sealed record Row(int Id, string City);

    private static readonly Row[] Rows = [new(1, "Namur"), new(2, "Liège")];

    private static RenderFragment Columns(bool comboFilter = false) => builder =>
    {
        builder.OpenComponent<OmniDataGridColumn<Row>>(0);
        builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Property), nameof(Row.City));
        builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Title), "Ville");
        if (comboFilter)
        {
            builder.AddComponentParameter(3, nameof(OmniDataGridColumn<Row>.Filterable), true);
            builder.AddComponentParameter(4, nameof(OmniDataGridColumn<Row>.FilterType), OmniDataGridColumnFilterType.Combo);
        }

        builder.CloseComponent();
    };

    private static IReadOnlyList<string> Calls(BunitJSModuleInterop module) => [.. module.Invocations.Select(call => call.Identifier)];

    [Fact]
    public async Task WheelScope_IsAttachedPerSelector_DetachedWhenCleared_AndAtDisposal()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.WheelScrollScope, " .page "));

        Assert.Equal(".page", Assert.Single(module.Invocations["attachWheelScope"]).Arguments[1]);
        grid.Render(parameters => parameters.Add(component => component.WheelScrollScope, ".page"));
        Assert.Single(module.Invocations["attachWheelScope"]);

        grid.Render(parameters => parameters.Add(component => component.WheelScrollScope, "  "));
        Assert.Single(module.Invocations["detachWheelScope"]);

        grid.Render(parameters => parameters.Add(component => component.WheelScrollScope, ".autre"));
        await grid.Instance.DisposeAsync();
        Assert.Equal(2, module.Invocations["detachWheelScope"].Count);
    }

    [Fact]
    public async Task FillMode_IsAttached_HandedBack_AndUndoneAtDisposal()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.FillAvailableHeight, true));
        Assert.Single(module.Invocations["attachFill"]);

        grid.Render(parameters => parameters.Add(component => component.FillAvailableHeight, false));
        Assert.Single(module.Invocations["detachFill"]);

        grid.Render(parameters => parameters.Add(component => component.FillAvailableHeight, true));
        await grid.Instance.DisposeAsync();
        Assert.Equal(2, module.Invocations["detachFill"].Count);
        Assert.Equal("uninstallPackageTooltips", Calls(module)[^1]);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void PickInAFilterPopover_ClosesThePopovers_OnlyWhenAsked(bool hide, int closes)
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns(comboFilter: true))
            .Add(component => component.ShowHeaderFilterMenu, true)
            .Add(component => component.HideFilterMenuOnSelect, hide));
        Assert.Single(module.Invocations["attachFilterMenus"]);

        grid.Find(".omni-combo__input").Focus();
        grid.Find(".omni-combo__option").MouseDown();

        Assert.Equal(closes, module.Invocations["closeFilterMenus"].Count);
    }

    [Fact]
    public async Task VirtualGrid_DetachedOnALostCircuit_IsStillReleased()
    {
        var runtime = new ManualJSRuntime();
        runtime.Module.CallFailures["detach"] = new JSDisconnectedException("perdu");
        Services.AddSingleton<IJSRuntime>(runtime);
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, Rows)
            .Add(component => component.Columns, Columns())
            .Add(component => component.ScrollMode, OmniDataGridScrollMode.Virtual));
        Assert.Contains("attach", runtime.Module.Calls);

        await grid.Instance.DisposeAsync();

        Assert.Contains("detach", runtime.Module.Calls);
        Assert.True(runtime.Module.Disposal.Task.IsCompleted);
    }
}
