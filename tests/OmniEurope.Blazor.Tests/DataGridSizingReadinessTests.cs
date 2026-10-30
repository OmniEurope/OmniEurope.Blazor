using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>The interactive sizing gate and its explicit, logged failure fallback.</summary>
public sealed class DataGridSizingReadinessTests : OmniBunitContext
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ContentTable_WaitsForSizingOnlyInAnInteractiveRenderer(bool interactive)
    {
        SetRendererInfo(new RendererInfo(interactive ? "Server" : "Static", interactive));
        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.ColumnSizing, OmniDataGridColumnSizing.Content));

        Assert.Equal("content", grid.Find("table").GetAttribute("data-omni-sizing"));
        Assert.Equal(interactive, grid.Find("table").HasAttribute("data-omni-sizing-pending"));
    }

    [Fact]
    public void SizingFailure_ShowsTheStaticTable_LogsTheError_AndDoesNotLoop()
    {
        var error = new JSException("sizing unavailable");
        var logger = new ErrorLogger();
        Services.AddSingleton<ILogger<OmniDataGrid<int>>>(logger);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("attachContentFit", _ => true).SetException(error);

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1, 2])
            .Add(component => component.ColumnSizing, OmniDataGridColumnSizing.Content));

        grid.WaitForAssertion(() => Assert.True(grid.Find("table").HasAttribute("data-omni-sizing-failed")));
        Assert.False(grid.Find("table").HasAttribute("data-omni-sizing"));
        Assert.False(grid.Find("table").HasAttribute("data-omni-sizing-pending"));
        Assert.Equal(2, grid.FindAll("tbody tr[data-omni-row-index]").Count);
        Assert.Same(error, Assert.Single(logger.Errors));
        grid.Render();
        Assert.Single(module.Invocations["attachContentFit"]);
        Assert.Single(logger.Errors);
    }

    [Fact]
    public void DefaultSizing_StillSurfacesScriptErrors()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("attachContentFit", _ => true).SetException(new JSException("script unavailable"));

        Assert.Throws<JSException>(() => Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1])));
    }

    [Fact]
    public void SizingFailure_WithoutALogger_StillShowsItsStaticTable()
    {
        Services.AddSingleton<ILogger<OmniDataGrid<int>>>(_ => null!);
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("attachContentFit", _ => true).SetException(new JSException("sizing unavailable"));

        var grid = Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1])
            .Add(component => component.ColumnSizing, OmniDataGridColumnSizing.Content));

        grid.WaitForAssertion(() => Assert.True(grid.Find("table").HasAttribute("data-omni-sizing-failed")));
        Assert.Single(grid.FindAll("tbody tr[data-omni-row-index]"));
    }

    [Fact]
    public void ContentSizing_StillSurfacesDisconnectedCircuits()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("attachContentFit", _ => true).SetException(new JSDisconnectedException("circuit closed"));

        Assert.Throws<JSDisconnectedException>(() => Render<OmniDataGrid<int>>(parameters => parameters
            .Add(component => component.Items, [1])
            .Add(component => component.ColumnSizing, OmniDataGridColumnSizing.Content)));
    }

    [Fact]
    public void ContentSizing_StillSurfacesErrorsAfterItsObserverAttached()
    {
        var module = JSInterop.SetupModule(OmniModules.Grid);
        module.Mode = JSRuntimeMode.Loose;
        module.SetupVoid("setContentTexts", _ => true).SetException(new JSException("texts unavailable"));

        Assert.Throws<JSException>(() => Render<DataGridColumnSizingTestHost>(parameters => parameters
            .Add(component => component.GridAttributes, new Dictionary<string, object>
            {
                ["ColumnSizing"] = OmniDataGridColumnSizing.Content
            })));
        Assert.Single(module.Invocations["attachContentFit"]);
    }

    private sealed class ErrorLogger : ILogger<OmniDataGrid<int>>
    {
        internal List<Exception?> Errors { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                Errors.Add(exception);
            }
        }
    }
}
