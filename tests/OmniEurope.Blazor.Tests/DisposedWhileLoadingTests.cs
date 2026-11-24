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

    [Fact]
    public async Task Popover_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = this.InstallHeldImports();
        var popover = RenderOpenPopover();

        await popover.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task MediaTile_RemovedWhileItsScriptLoads_AttachesNothing_AndItsDisposalEndsOnceTheScriptIsReleased()
    {
        var runtime = this.InstallHeldImports();
        var tile = Render<OmniMediaTile>(parameters => parameters.Add(component => component.Stream, new RecordingModule()));

        // The disposal waits for the script that is still loading, so it ends once the script has arrived and been released.
        var disposing = tile.Instance.DisposeAsync().AsTask();
        await runtime.ReleasedWithoutAttachingAsync(Patience);

        await disposing.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task BackToTop_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = this.InstallHeldImports();
        var button = Render<OmniBackToTop>();

        await button.Instance.DisposeAsync();

        // The button's tooltip loads its own script through the same runtime: only the back-to-top calls are counted.
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attach", runtime.Module.Calls);
    }

    [Fact]
    public async Task Sidebar_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = this.InstallHeldImports();
        var sidebar = RenderOpenOverlaySidebar();

        await sidebar.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task Dialog_RemovedWhileItsScriptLoads_ActivatesNothing()
    {
        var runtime = this.InstallHeldImports();
        var dialog = Render<OmniDialog>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Title, "Titre")
            .AddChildContent("Contenu"));

        await dialog.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task Tabs_RemovedWhileTheirScriptLoads_ConfigureNothing()
    {
        var runtime = this.InstallHeldImports();
        var tabs = Render<OmniTabs>(parameters => parameters
            .Add(component => component.Label, "Sections")
            .AddChildContent<OmniTabsItem>(item => item
                .Add(component => component.Key, "rapports")
                .Add(component => component.Title, "Rapports")
                .AddChildContent("Contenu")));

        await tabs.Instance.DisposeAsync();

        // No strip configured, so no .NET reference made for a listener nothing would release.
        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task SelectBar_RemovedWhileItsScriptLoads_ConfiguresNothing()
    {
        var runtime = this.InstallHeldImports();
        var choice = "a";
        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Options, [new OmniOption<string>("a", "A"), new OmniOption<string>("b", "B")])
            .Add(component => component.Value, choice)
            .Add(component => component.ValueExpression, () => choice));

        await bar.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task Stack_RemovedWhileTheScriptOfItsChevronsLoads_ConfiguresNothing()
    {
        var runtime = this.InstallHeldImports();
        var stack = Render<OmniStack>(parameters => parameters
            .Add(component => component.Overflow, OmniStackOverflow.Scroll)
            .AddChildContent("contenu"));

        await stack.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task Fieldset_RemovedWhileItsScriptLoads_ObservesNothing()
    {
        var runtime = this.InstallHeldImports();
        var fieldset = RenderCollapsibleFieldset();

        await fieldset.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public void Fieldset_WhoseCircuitIsGoneWhileItObserves_StaysRendered()
    {
        var runtime = Install(new ManualJSRuntime());
        runtime.Module.CallFailures["observeFieldsetToggle"] = new JSDisconnectedException("The circuit is gone.");

        var fieldset = RenderCollapsibleFieldset();

        // The call fails at once, inside the render: a disconnection that escaped would already be reported.
        Assert.Contains("observeFieldsetToggle", runtime.Module.Calls);
        Assert.False(Renderer.UnhandledException.IsCompleted, "The lost circuit escaped OnAfterRenderAsync.");
        Assert.Contains("Champs", fieldset.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autocomplete_RemovedWhileItsScriptLoads_AttachesNothing()
    {
        var runtime = this.InstallHeldImports();
        var autocomplete = this.RenderAutocomplete((_, _) => Task.FromResult<IReadOnlyList<OmniOption<string>>>([]));

        await autocomplete.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task MenuController_RemovedWhileItsScriptLoads_ReleasesItOnArrival()
    {
        var runtime = this.InstallHeldImports();
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
        var runtime = new HeldImportRuntime<DisposalCountingModule>(new());
        var (controller, syncs) = SyncTwice(runtime);
        runtime.Arrive();
        await syncs;
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
        var runtime = this.InstallHeldImports();
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

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task ConnectionOverlay_RemovedWhileItsFocusScriptLoads_ActivatesNothing()
    {
        var runtime = this.InstallHeldImports();
        var overlay = Render<OmniConnectionOverlay>(parameters => parameters
            .Add(component => component.State, OmniConnectionState.Reconnecting));

        await overlay.Instance.DisposeAsync();

        await runtime.ReleasedWithoutAttachingAsync(Patience);
    }

    [Fact]
    public async Task MarkdownExportButton_RemovedWhileItsDownloadScriptLoads_DownloadsNothing()
    {
        var runtime = this.InstallHeldImports();
        var button = Render<OmniMarkdownExportButton<ExportRow>>(parameters => parameters
            .Add(component => component.Export, () => new OmniTableExport<ExportRow>
            {
                Title = "Erreurs",
                Columns = [new("Id", row => row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))],
                LoadPage = _ => Task.FromResult(new OmniDataGridResult<ExportRow>([new ExportRow(1)], 1))
            }));

        button.Find("button").Click();
        await button.Instance.DisposeAsync();

        // The button's tooltip loads its own script through the same runtime: only the back-to-top calls are counted.
        runtime.PendingImport.SetResult(runtime.Module);
        await runtime.Module.Disposal.Task.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
        Assert.DoesNotContain("attach", runtime.Module.Calls);
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

        await runtime.ReleasedWithoutAttachingAsync(Patience);
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
        await runtime.ReleasedWithoutAttachingAsync(Patience);
        await syncing.WaitAsync(Patience, Xunit.TestContext.Current.CancellationToken);
    }

    private ManualJSRuntime Install(ManualJSRuntime runtime)
    {
        Services.AddSingleton<IJSRuntime>(runtime);
        return runtime;
    }

    private static async Task SettledAsync() => await Task.Delay(200);

    /// <summary>Removes the component, then lets the gated script or call go on, and waits for what follows to settle.</summary>
    private static async Task DisposeThenOpenTheGateAsync<TComponent>(IRenderedComponent<TComponent> component, TaskCompletionSource gate)
        where TComponent : IComponent, IAsyncDisposable
    {
        await component.Instance.DisposeAsync();
        gate.SetResult();
        await SettledAsync();
    }

    /// <summary>A runtime installed for the test, whose dialog script is held until the gate opens.</summary>
    private (ManualJSRuntime Runtime, TaskCompletionSource Gate) GateTheDialogScript()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.ImportGates["omni-dialog.js"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return (runtime, gate);
    }

    private IRenderedComponent<OmniPopover> RenderOpenPopover() =>
        Render<OmniPopover>(parameters => parameters
            .Add(component => component.Label, "Filtres")
            .Add(component => component.Open, true)
            .AddChildContent("panneau"));

    private IRenderedComponent<OmniSidebar> RenderOpenOverlaySidebar() =>
        Render<OmniSidebar>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Reveal, OmniSidebarReveal.Overlay)
            .Add(component => component.OpenChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Navigation"));

    private IRenderedComponent<OmniFieldset> RenderCollapsibleFieldset() =>
        Render<OmniFieldset>(parameters => parameters
            .Add(component => component.Legend, (RenderFragment)(builder => builder.AddContent(0, "Avancé")))
            .Add(component => component.Collapsible, true)
            .Add(component => component.ExpandedChanged, EventCallback.Factory.Create<bool>(this, _ => { }))
            .AddChildContent("Champs"));

    /// <summary>An open dialog with a title and a content; <paramref name="extra"/> adds the rest.</summary>
    private IRenderedComponent<OmniDialog> RenderOpenDialog(Action<ComponentParameterCollectionBuilder<OmniDialog>> extra) =>
        Render<OmniDialog>(parameters =>
        {
            parameters
                .Add(component => component.Open, true)
                .Add(component => component.Title, "Titre")
                .AddChildContent("Contenu");
            extra(parameters);
        });

    [Fact]
    public async Task Sidebar_RemovedWhileItsChevronsAreConfigured_ListensToNothing()
    {
        var runtime = Install(new ManualJSRuntime());
        var gate = runtime.Module.Gates["configureSidebarOverflow"] = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var sidebar = RenderOpenOverlaySidebar();

        await DisposeThenOpenTheGateAsync(sidebar, gate);

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
        var (runtime, gate) = GateTheDialogScript();
        var dialog = RenderOpenDialog(parameters => parameters.Add(component => component.Width, "30rem"));

        await DisposeThenOpenTheGateAsync(dialog, gate);

        Assert.DoesNotContain("setWidth", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RemovedWhileTheScriptOfItsDraggingLoads_AttachesNothing()
    {
        var (runtime, gate) = GateTheDialogScript();
        var dialog = RenderOpenDialog(parameters => parameters.Add(component => component.Draggable, true));

        await DisposeThenOpenTheGateAsync(dialog, gate);

        Assert.DoesNotContain("attach", runtime.Module.Calls);
    }

    [Fact]
    public async Task Dialog_RenderedTwiceWhileItsScriptLoads_WritesTheWidthOnce()
    {
        var (runtime, gate) = GateTheDialogScript();
        var dialog = RenderOpenDialog(parameters => parameters.Add(component => component.Width, "30rem"));
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

        var popover = RenderOpenPopover();
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
        var runtime = new HeldImportRuntime<DisposalCountingModule>(new());
        await RemoveWhileTwoSyncsWaitAsync(runtime);

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
            var runtime = new HeldImportRuntime<DisposalCountingModule>(new());
            await RemoveWhileTwoSyncsWaitAsync(runtime);

            Assert.Equal(1, runtime.Module.Disposals);
        }
    }

    /// <summary>A menu controller over <paramref name="runtime"/>, asked twice for its placement and synced each time; the two syncs may still wait.</summary>
    private static (OmniMenuController Controller, Task Syncs) SyncTwice(IJSRuntime runtime)
    {
        var controller = new OmniMenuController(runtime, _ => Task.CompletedTask);
        controller.RequestPlacement(focusLast: false);
        var first = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        controller.RequestPlacement(focusLast: false);
        var second = controller.SyncAsync(true, "menu", default, OmniMenuPlacement.Start);
        return (controller, Task.WhenAll(first, second));
    }

    /// <summary>Synced twice, the controller is removed before its held script arrives, then the script arrives.</summary>
    private static async Task RemoveWhileTwoSyncsWaitAsync(HeldImportRuntime<DisposalCountingModule> runtime)
    {
        var (controller, syncs) = SyncTwice(runtime);
        await controller.DisposeAsync();
        runtime.Arrive();
        await syncs;
    }

    /// <summary>A runtime whose first import fails, as a lost network does, and whose later imports answer a recording module.</summary>
    private sealed class FlakyImportRuntime : ForwardingJSRuntime
    {
        public RecordingModule Module { get; } = new();

        public int Imports { get; private set; }

        public override ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier != "import")
            {
                return ValueTask.FromResult(default(TValue)!);
            }

            return ++Imports == 1
                ? ValueTask.FromException<TValue>(new InvalidOperationException("The script could not be loaded."))
                : ValueTask.FromResult((TValue)(object)Module);
        }
    }
}
