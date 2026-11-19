using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A component removed while the script it needs is still loading must leave nothing behind: no
/// document listener attached after its disposal, no .NET reference left to a listener, and the
/// module released when it arrives (L4-07 of the 2026-10-22 audit).
/// </summary>
public sealed class DisposedWhileLoadingTests : OmniBunitContext
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    private ManualJSRuntime HoldImports()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    private static async Task ReleasedWithoutAttachingAsync(ManualJSRuntime runtime)
    {
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task Popover_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = HoldImports();
        var popover = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtres")
            .Add(component => component.Open, true)
            .AddChildContent("panneau"));

        await popover.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task MediaTile_RemovedWhileItsScriptLoads_AttachesNothing_AndItsDisposalEndsOnceTheScriptIsReleased()
    {
        var runtime = HoldImports();
        var tile = Render<OmniMediaTile>(parameters => parameters.Add(component => component.Stream, new RecordingModule()));

        // The disposal waits for the script that is still loading, so it ends once the script has arrived and been released.
        var disposing = tile.Instance.DisposeAsync().AsTask();
        await ReleasedWithoutAttachingAsync(runtime);

        await disposing.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Sidebar_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = HoldImports();
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.OpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Navigation"));

        await sidebar.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task Dialog_RemovedWhileItsScriptLoads_ActivatesNothing()
    {
        var runtime = HoldImports();
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task Tabs_RemovedWhileTheirScriptLoads_ConfigureNothing()
    {
        var runtime = HoldImports();
        var tabs = Render<OmniTabs>(parameters => parameters
            .Add(component => component.Label, "Sections")
            .AddChildContent<OmniTabsItem>(item => item
                .Add(component => component.Key, "rapports")
                .Add(component => component.Title, "Rapports")
                .AddChildContent("Contenu")));

        await tabs.Instance.DisposeAsync();

        // No strip configured, so no .NET reference made for a listener nothing would release.
        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task SelectBar_RemovedWhileItsScriptLoads_ConfiguresNothing()
    {
        var runtime = HoldImports();
        var choice = "a";
        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "A"), new OmniOption<string>("b", "B")])
            .Add(component => component.Value, choice)
            .Add(component => component.ValueExpression, () => choice));

        await bar.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task Stack_RemovedWhileTheScriptOfItsChevronsLoads_ConfiguresNothing()
    {
        var runtime = HoldImports();
        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.Overflow, OmniStackOverflow.Scroll)
            .AddChildContent("contenu"));

        await stack.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task Fieldset_RemovedWhileItsScriptLoads_ObservesNothing()
    {
        var runtime = HoldImports();
        var fieldset = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, (RenderFragment)(builder => builder.AddContent(0, "Avancé")))
            .Add(component => component.Collapsible, true)
            .Add(component => component.ExpandedChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Champs"));

        await fieldset.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public void Fieldset_WhoseCircuitIsGoneWhileItObserves_StaysRendered()
    {
        var runtime = Install(new ManualJSRuntime());
        runtime.Module.CallFailures["observeFieldsetToggle"] = new JSDisconnectedException("The circuit is gone.");

        var fieldset = Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, (RenderFragment)(builder => builder.AddContent(0, "Avancé")))
            .Add(component => component.Collapsible, true)
            .Add(component => component.ExpandedChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Champs"));

        // The call fails at once, inside the render: a disconnection that escaped would already be reported.
        Assert.Contains("observeFieldsetToggle", runtime.Module.Calls);
        Assert.False(Renderer.UnhandledException.IsCompleted, "The lost circuit escaped OnAfterRenderAsync.");
        Assert.Contains("Champs", fieldset.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autocomplete_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = HoldImports();
        var value = string.Empty;
        var autocomplete = Render<OmniAutocomplete<string>>(parameters => parameters
            .Add(component => component.Value, value)
            .Add(component => component.ValueExpression, () => value)
            .Add(component => component.Search, (_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>([])));

        await autocomplete.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task MenuController_RemovedWhileItsScriptLoads_ReleasesItOnArrival()
    {
        var runtime = HoldImports();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);

        var syncing = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        await controller.DisposeAsync();
        runtime.PendingImport.SetResult(runtime.Module);
        await syncing;

        await runtime.Module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(runtime.Module.Calls);
    }

    [Fact]
    public async Task MenuController_SyncedTwiceWhileItsScriptLoads_ImportsItOnce()
    {
        var runtime = new CountingImportRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);

        var first = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        controller.RequestPlacement(focusLast: false);
        var second = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        runtime.Arrive();
        await Task.WhenAll(first, second);
        await controller.DisposeAsync();

        Assert.Equal(1, runtime.Imports);
        Assert.Equal(1, runtime.Module.Disposals);
    }

    private sealed class RequiredModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string? Name { get; set; }
    }

    private sealed record ExportRow(int Id);

    [Fact]
    public async Task TemplateForm_RemovedWhileTheFocusScriptLoads_FocusesNothing()
    {
        var runtime = HoldImports();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var form = Render<OmniTemplateForm<RequiredModel>>(parameters => parameters
            .Add(component => component.Model, new RequiredModel())
            .Add(component => component.ChildContent, (RenderFragment<Microsoft.AspNetCore.Components.Forms.EditContext>)(_ => builder =>
            {
                builder.OpenComponent<Microsoft.AspNetCore.Components.Forms.DataAnnotationsValidator>(0);
                builder.CloseComponent();
            })));

        // The invalid submit starts loading the focus script, then the form leaves the page.
        form.Find("form").Submit();
        await form.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task ConnectionOverlay_RemovedWhileItsFocusScriptLoads_ActivatesNothing()
    {
        var runtime = HoldImports();
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting));

        await overlay.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task MarkdownExportButton_RemovedWhileItsDownloadScriptLoads_DownloadsNothing()
    {
        var runtime = HoldImports();
        var button = Render<OmniMarkdownExportButton<ExportRow>>(parameters => parameters
            .Add(component => component.Export, () => new OmniTableExport<ExportRow>
            {
                Title = "Erreurs",
                Columns = [new("Id", row => row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                LoadPage = _ => Task.FromResult(new OmniDataGridResult<ExportRow>([new ExportRow(1)], 1))
            }));

        button.Find("button").Click();
        await button.Instance.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
    }

    [Fact]
    public async Task HtmlEditorToolbarFit_RemovedWhileItsScriptLoads_InstallsNothing()
    {
        var html = string.Empty;
        var editor = Render<OmniHtmlEditor>(parameters => parameters
            .Add(component => component.Value, html)
            .Add(component => component.ValueExpression, () => html));
        var runtime = new ManualJSRuntime { HoldImports = true };
        var fit = new HtmlEditorToolbarFit(editor.Instance, runtime);

        var syncing = fit.SyncAsync(default, 1);
        await fit.DisposeAsync();

        await ReleasedWithoutAttachingAsync(runtime);
        await syncing.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task AppearanceDocumentRoot_LeftByEveryScopeWhileItsScriptLoads_WritesNothing()
    {
        var runtime = new ManualJSRuntime { HoldImports = true };
        var root = new AppearanceDocumentRoot(runtime, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        root.Attach();

        var syncing = root.SyncAsync(7, 5);
        await root.DetachAsync();

        // No scope is left to clear the root: the levels must not be written once the script arrives.
        await ReleasedWithoutAttachingAsync(runtime);
        await syncing.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
    }

    private ManualJSRuntime Install(ManualJSRuntime runtime)
    {
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    private static async Task SettledAsync() => await Task.Delay(200);

    [Fact]
    public async Task Sidebar_RemovedWhileItsChevronsAreConfigured_ListensToNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.Module.Gates["configureSidebarOverflow"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var sidebar = Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.OpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Navigation"));

        await sidebar.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();

        Assert.DoesNotContain("attachEscape", runtime.Module.Calls);
        Assert.DoesNotContain("watchNarrow", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RenderedAgainAfterItsDisposal_ActivatesNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));
        await dialog.Instance.DisposeAsync();
        var before = runtime.Module.Calls.Count;

        dialog.Render(parameters => parameters.Add(component => component.Open, true));
        await SettledAsync();

        Assert.Equal(before, runtime.Module.Calls.Count);
    }

    [Fact]
    public async Task Dialog_RemovedWhileTheScriptOfItsWidthLoads_WritesNoWidth()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Width, "30rem")
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();

        Assert.DoesNotContain("setWidth", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RemovedWhileTheScriptOfItsDraggingLoads_AttachesNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Draggable, true)
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();

        Assert.DoesNotContain("attach", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RenderedTwiceWhileItsScriptLoads_WritesTheWidthOnce()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Width, "30rem")
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));
        dialog.Render(parameters => parameters.Add(component => component.Width, "40rem"));

        gate.SetResult();
        await SettledAsync();

        Assert.Contains("setWidth", runtime.Module.Calls);
        Assert.Empty(runtime.Module.Calls.Where(call => call == "setWidth").Skip(2));
    }

    [Fact]
    public async Task Popover_WhoseCircuitIsGoneWhileItAttaches_StaysRendered()
    {
        var runtime = Install(new ManualJSRuntime());
        runtime.Module.CallFailures["attachPopover"] = new JSDisconnectedException("The circuit is gone.");

        var popover = Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtres")
            .Add(component => component.Open, true)
            .AddChildContent("panneau"));
        await SettledAsync();

        Assert.Contains("attachPopover", runtime.Module.Calls);
        Assert.Contains("panneau", popover.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MenuController_WhoseFirstImportFails_ImportsAgainOnTheNextSync()
    {
        var runtime = new FlakyImportRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start));

        await controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);

        Assert.Equal(2, runtime.Imports);
        Assert.Contains("openMenu", runtime.Module.Calls);
        await controller.DisposeAsync();
    }

    [Fact]
    public async Task MenuController_SyncedTwiceThenRemovedWhileItsScriptLoads_ReleasesTheModuleOnce()
    {
        var runtime = new CountingImportRuntime();
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);
        var first = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        controller.RequestPlacement(focusLast: false);
        var second = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);

        await controller.DisposeAsync();
        runtime.Arrive();
        await Task.WhenAll(first, second);

        Assert.Equal(1, runtime.Imports);
        Assert.Equal(1, runtime.Module.Disposals);
    }

    [Fact]
    public async Task MenuController_TwoLoadsEndingTogetherOnTwoThreads_ReleaseTheModuleOnceEveryTime()
    {
        // The two loads above resume on the thread pool, at the same moment: once in a coverage run the
        // later one kept the module again after the first released it, and released it a second time.
        // Many rounds make that interleaving likely; the release must stay single in every one.
        for (var round = 0; round < 500; round++)
        {
            var runtime = new CountingImportRuntime();
            var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
            controller.RequestPlacement(focusLast: false);
            var first = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
            controller.RequestPlacement(focusLast: false);
            var second = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);

            await controller.DisposeAsync();
            runtime.Arrive();
            await Task.WhenAll(first, second);

            Assert.Equal(1, runtime.Module.Disposals);
        }
    }

    /// <summary>A runtime whose first import fails, as a lost network does, and whose later imports answer a recording module.</summary>
    private sealed class FlakyImportRuntime : IJSRuntime
    {
        public RecordingModule Module { get; } = new();

        public int Imports { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "import")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            return ++Imports == 1
                ? ValueTask.FromException<TValue>(new InvalidOperationException("The script could not be loaded."))
                : ValueTask.FromResult((TValue)(object)Module);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    /// <summary>A runtime whose one import is held until <see cref="Arrive"/>, and which counts the imports asked.</summary>
    private sealed class CountingImportRuntime : IJSRuntime
    {
        private readonly TaskCompletionSource<IJSObjectReference> _arrival = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CountedModule Module { get; } = new();

        public int Imports { get; private set; }

        public void Arrive() => _arrival.SetResult(Module);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import")
            {
                Imports++;
                return new(_arrival.Task.ContinueWith(task => (TValue)task.Result, TaskScheduler.Default));
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class CountedModule : IJSObjectReference
    {
        private int _disposals;

        public int Disposals => Volatile.Read(ref _disposals);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposals);
            return ValueTask.CompletedTask;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }
}
