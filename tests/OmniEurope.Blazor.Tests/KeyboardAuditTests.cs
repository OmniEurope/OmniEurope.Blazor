using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The keyboard paths of the audit of 2026-10-07: a tree item selects once per key (RCL-008), every
/// radio group of buttons keeps one tab stop for the arrows of the page keys (RCL-017), and the parts
/// that load the focus module for those keys only load it at their first focus (RCL-003, RCL-017).
/// What the keys do in the browser is the showcase scripts probe's.
/// </summary>
public sealed class KeyboardAuditTests : OmniBunitContext
{
    private const string FocusModule = "./_content/OmniEurope.Blazor/omni-focus.js";

    private IRenderedComponent<OmniTree<string>> Tree(List<IReadOnlyList<string>> selections) =>
        Render<OmniTree<string>>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.ValueChanged, values => selections.Add(values))
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniTreeItem<string>>(0);
                builder.AddComponentParameter(1, nameof(OmniTreeItem<string>.Value), "bru");
                builder.AddComponentParameter(2, nameof(OmniTreeItem<string>.Text), "Bruxelles");
                builder.AddComponentParameter(3, nameof(OmniTreeItem<string>.ChildContent), (RenderFragment)(child =>
                {
                    child.OpenComponent<OmniTreeItem<string>>(0);
                    child.AddComponentParameter(1, nameof(OmniTreeItem<string>.Value), "ixl");
                    child.AddComponentParameter(2, nameof(OmniTreeItem<string>.Text), "Ixelles");
                    child.CloseComponent();
                }));
                builder.CloseComponent();
            })));

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public void AKeyOnTheSelectButton_SelectsOnce_TheItemStillAnswersItsOwnKeys(string key)
    {
        var selections = new List<IReadOnlyList<string>>();
        var tree = Tree(selections);
        var button = tree.Find(".omni-tree__select");

        // The key reaches the item (an arrow on the button still expands it), then the browser clicks the button.
        button.KeyDown("ArrowRight");
        Assert.Equal("true", tree.Find("[role=treeitem]").GetAttribute("aria-expanded"));
        tree.Find(".omni-tree__select").KeyDown(key);
        tree.Find(".omni-tree__select").Click();
        Assert.Equal([["bru"]], selections.Select(values => values.ToArray()));

        // On the toggle button, the key is the toggle's: it selects nothing.
        tree.Find(".omni-tree__toggle").KeyDown(key);
        tree.Find(".omni-tree__toggle").Click();
        Assert.Single(selections);

        // On the item itself, the key selects, as before.
        tree.Find("[role=treeitem]").KeyDown(key);
        Assert.Equal([["bru"], []], selections.Select(values => values.ToArray()));
    }

    [Fact]
    public void SelectBar_IsOneTabStop_TheChosenOption_ElseTheFirstThatCanBePicked()
    {
        OmniOption<string>[] options = [new("a", "A", Disabled: true), new("b", "B"), new("c", "C")];
        var chosen = string.Empty;
        var bar = Render<OmniSelectBar<string>>(parameters => parameters
            .Add(component => component.Options, options)
            .Add(component => component.Value, chosen)
            .Add(component => component.ValueExpression, () => chosen)
            .Add(component => component.Label, "Vue"));

        Assert.True(bar.Find("[role=radiogroup]").HasAttribute("data-omni-roving"));
        Assert.Equal(["-1", "0", "-1"], TabIndexes(bar.FindAll("[role=radio]")));

        bar.Render(parameters => parameters.Add(component => component.Value, "c"));
        Assert.Equal(["-1", "-1", "0"], TabIndexes(bar.FindAll("[role=radio]")));

        bar.Render(parameters => parameters.Add(component => component.Value, "a"));
        Assert.Equal(["-1", "0", "-1"], TabIndexes(bar.FindAll("[role=radio]")));

        bar.Render(parameters => parameters.Add(component => component.Disabled, true));
        Assert.Equal(["-1", "-1", "-1"], TabIndexes(bar.FindAll("[role=radio]")));
    }

    [Fact]
    public void TheModesOfTheMenuAndOfTheWindow_AreOneTabStop_TheModeInForce()
    {
        var menu = Render<OmniAppMenu>(parameters => parameters
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.AppearanceChanged, _ => { }));
        menu.Find(".omni-app-menu__trigger").Click();
        AssertModes(menu.Find(".omni-app-menu__modes"), "Sombre");

        var window = Render<OmniAppearanceWindow>(parameters => parameters
            .Add(component => component.Open, true)
            .Add(component => component.Appearance, OmniAppearance.Dark)
            .Add(component => component.AppearanceChanged, _ => { }));
        AssertModes(window.Find(".omni-appearance-window__modes"), "Sombre");
    }

    [Fact]
    public void SchedulerViews_AreOneTabStop_AndLoadThePageKeysAtTheirFirstFocus()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.SetupVoid("enablePageKeys").SetVoidResult();
        var scheduler = Render<OmniScheduler>(parameters => parameters
            .Add(component => component.View, OmniCalendarView.Week)
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        var views = scheduler.Find(".omni-scheduler [role=radiogroup]");
        Assert.True(views.HasAttribute("data-omni-roving"));
        Assert.Single(views.QuerySelectorAll("[role=radio][tabindex='0']"));
        Assert.Equal("true", views.QuerySelector("[role=radio][tabindex='0']")!.GetAttribute("aria-checked"));
        Assert.Empty(module.Invocations);

        views.FocusIn();
        scheduler.Find(".omni-scheduler [role=radiogroup]").FocusIn();

        Assert.Single(module.Invocations, invocation => invocation.Identifier == "enablePageKeys");
    }

    [Fact]
    public void ARightLegend_LoadsThePageKeysAtTheFirstFocusOfAnEntry_Once()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.SetupVoid("enablePageKeys").SetVoidResult();
        var chart = Render<OmniChart>(parameters => parameters
            .Add(component => component.Title, "Soldes")
            .AddChildContent(builder =>
            {
                builder.OpenComponent<OmniLineSeries>(0);
                builder.AddAttribute(1, nameof(OmniLineSeries.Title), "Livret");
                builder.AddAttribute(2, nameof(OmniLineSeries.Data), (IReadOnlyList<OmniChartPoint>)[new(0, 1), new(1, 2)]);
                builder.CloseComponent();
                builder.OpenComponent<OmniLegend>(3);
                builder.AddAttribute(4, nameof(OmniLegend.Position), OmniLegendPosition.Right);
                builder.CloseComponent();
            }));
        chart.WaitForAssertion(() => Assert.Single(chart.FindAll("g.omni-chart__legend-entry--toggle")));
        Assert.Empty(module.Invocations);

        chart.Find("g.omni-chart__legend").FocusIn();
        chart.Find("g.omni-chart__legend").FocusIn();

        Assert.Single(module.Invocations, invocation => invocation.Identifier == "enablePageKeys");
    }

    [Fact]
    public void PageKeysOnALostCircuit_AreQuiet()
    {
        var module = JSInterop.SetupModule(FocusModule);
        module.SetupVoid("enablePageKeys").SetException(new JSDisconnectedException("perdu"));
        var scheduler = Render<OmniScheduler>(parameters => parameters.Add(component => component.TimeZone, TimeZoneInfo.Utc));

        scheduler.Find(".omni-scheduler [role=radiogroup]").FocusIn();

        Assert.Single(module.Invocations, invocation => invocation.Identifier == "enablePageKeys");
    }

    private static void AssertModes(AngleSharp.Dom.IElement group, string chosen)
    {
        Assert.True(group.HasAttribute("data-omni-roving"));
        var stops = group.QuerySelectorAll("[role=radio][tabindex='0']");
        Assert.Single(stops);
        Assert.Equal(chosen, stops[0].GetAttribute("aria-label"));
        Assert.Equal(2, group.QuerySelectorAll("[role=radio][tabindex='-1']").Length);
    }

    private static string?[] TabIndexes(IEnumerable<AngleSharp.Dom.IElement> radios) =>
        [.. radios.Select(radio => radio.GetAttribute("tabindex"))];
}
