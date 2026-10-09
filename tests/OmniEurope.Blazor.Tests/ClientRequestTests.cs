using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The requests of a client application of 2026-10-08 (PLAN-018): a grid called after its removal
/// (lot 2), the notification region on a narrow screen (lot 3) and the skip link (lot 4). The title
/// tooltips (lot 1) and the focus the skip link moves are the showcase scripts probe's.
/// </summary>
public sealed class ClientRequestTests : OmniBunitContext
{
    private const string InteropModule = "./_content/OmniEurope.Blazor/omniInterop.js";

    public sealed record Row(int Number, string Name);

    [Fact]
    public async Task AGridBeingRemoved_IgnoresTheCallsOfItsHost_AndForgetsItsScript()
    {
        var grid = Render<OmniDataGrid<Row>>(parameters => parameters
            .Add(component => component.Items, [new Row(1, "Alpha"), new Row(2, "Beta")])
            .Add(component => component.Columns, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniDataGridColumn<Row>>(0);
                builder.AddComponentParameter(1, nameof(OmniDataGridColumn<Row>.Key), "name");
                builder.AddComponentParameter(2, nameof(OmniDataGridColumn<Row>.Value), (Func<Row, object?>)(row => row.Name));
                builder.CloseComponent();
            })));
        grid.WaitForAssertion(() => Assert.NotNull(grid.Instance.Script.Module));

        await grid.InvokeAsync(() => grid.Instance.DisposeAsync().AsTask());
        Assert.Null(grid.Instance.Script.Module);
        var calls = JSInterop.Invocations.Count;

        await grid.InvokeAsync(async () =>
        {
            await grid.Instance.SetFiltersAsync(new Dictionary<string, string?> { ["name"] = "Alpha" });
            await grid.Instance.ScrollToIndexAsync(1);
            await grid.Instance.RefreshAsync();
            await grid.Instance.ReloadAsync();
        });

        Assert.Equal(calls, JSInterop.Invocations.Count);
        Assert.Empty(grid.Instance.Query.Filters);
    }

    [Fact]
    public void OnANarrowScreen_TheNotificationsLieAlongTheBottom_AsWideAsTheWindow()
    {
        var styles = StylesheetSource.Read();

        Assert.Matches(@"@media \(max-width: 39\.99rem\) \{\s*\.omni-notification-region\.omni-notification-region \{ inset-block: auto var\(--omni-space-sm\); inset-inline: var\(--omni-space-sm\); justify-items: stretch; justify-self: auto; margin-inline: 0; max-width: none; width: auto; \}\s*\.omni-notification-region\.omni-notification-region \.omni-notification \{ inline-size: auto; \}\s*\}", styles);
    }

    [Fact]
    public void SkipLink_IsTheCurrentPageWithItsTarget_AndMovesTheFocusByScript()
    {
        var module = JSInterop.SetupModule(InteropModule);
        module.Setup<bool>("focusSkipTarget", _ => true).SetResult(true);
        Services.GetRequiredService<NavigationManager>().NavigateTo("composants/grille#ancre");

        var link = Render<OmniSkipLink>(parameters => parameters.Add(component => component.TargetId, "contenu"));

        var anchor = link.Find("a.omni-skip-link");
        Assert.Equal("Aller au contenu principal", anchor.TextContent);
        Assert.Equal("http://localhost/composants/grille#contenu", anchor.GetAttribute("href"));
        anchor.Click();
        Assert.Equal("contenu", Assert.Single(module.Invocations).Arguments[0]);
    }

    [Fact]
    public void SkipLink_WithoutTarget_GoesToTheMainLandmark_AndTakesTheTextOfTheHost()
    {
        var module = JSInterop.SetupModule(InteropModule);
        module.Setup<bool>("focusSkipTarget", _ => true).SetResult(false);

        var link = Render<OmniSkipLink>(parameters => parameters.Add(component => component.Text, "Au contenu"));

        var anchor = link.Find("a.omni-skip-link");
        Assert.Equal("Au contenu", anchor.TextContent);
        Assert.Equal("http://localhost/", anchor.GetAttribute("href"));
        anchor.Click();
        Assert.Null(Assert.Single(module.Invocations).Arguments[0]);
    }

    [Fact]
    public void SkipLink_OnALostCircuit_IsQuiet()
    {
        var module = JSInterop.SetupModule(InteropModule);
        module.Setup<bool>("focusSkipTarget", _ => true).SetException(new Microsoft.JSInterop.JSDisconnectedException("perdu"));

        var link = Render<OmniSkipLink>();
        link.Find("a.omni-skip-link").Click();

        Assert.Single(module.Invocations);
    }
}
