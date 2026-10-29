using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The panel menu filter: typing narrows the entries, the groups above a match unfold without the
/// host's Expanded being touched, and clearing the text gives back exactly what was there before. Every
/// test reads the DOM, since "shown" is a matter of the hidden attribute (an element with it is out of
/// the tab order and of the accessibility tree) on the entry or on a group above it.
/// </summary>
public sealed class PanelMenuFilterTests : OmniBunitContext
{
    private const string Open = "omni-panel-menu__group--open";
    private const string Closed = "omni-panel-menu__group--closed";
    private const string ReportsToggle = "#g-reports > .omni-panel-menu__summary > .omni-panel-menu__toggle";

    private static string[] AllIds => ["g-reports", "g-monthly", "l-january", "l-february", "l-summary", "g-settings", "l-general", "l-security", "l-cafe"];

    [Fact]
    public void AMatchingEntryIsShownWithTheGroupsAboveItAndTheOthersAreHidden()
    {
        var host = Render<PanelMenuFilterTestHost>();

        host.Find("input[type=search]").Input("janv");

        Assert.Equal(["g-reports", "g-monthly", "l-january"], Shown(host));
        Assert.True(host.Find("#l-february").HasAttribute("hidden"));
        Assert.True(host.Find("#l-cafe").HasAttribute("hidden"));
    }

    [Fact]
    public void TheGroupsAboveAMatchAreUnfoldedWithoutTheHostBeingTold()
    {
        var host = Render<PanelMenuFilterTestHost>();
        Assert.Contains(Closed, host.Find("#g-reports").ClassName);

        host.Find("input[type=search]").Input("janv");

        Assert.Contains(Open, host.Find("#g-reports").ClassName);
        Assert.Contains(Open, host.Find("#g-monthly").ClassName);
        Assert.False(host.Find("#g-reports > .omni-panel-menu__children").HasAttribute("hidden"));
        Assert.Equal("true", host.Find(ReportsToggle).GetAttribute("aria-expanded"));
        Assert.Empty(host.Instance.ExpandedReports);
        Assert.False(host.Instance.ReportsExpanded);
    }

    [Fact]
    public void ClearingTheTextGivesBackTheStateTheGroupsHadBeforeAndShowsEveryEntry()
    {
        var host = Render<PanelMenuFilterTestHost>();
        host.Find("input[type=search]").Input("janv");

        host.Find("input[type=search]").Input(string.Empty);

        Assert.Equal(AllIds, Shown(host, includeClosedGroups: true));
        Assert.Contains(Closed, host.Find("#g-reports").ClassName);
        Assert.Contains(Closed, host.Find("#g-monthly").ClassName);
        Assert.True(host.Find("#g-reports > .omni-panel-menu__children").HasAttribute("hidden"));
        Assert.Empty(host.Instance.ExpandedReports);
        Assert.DoesNotContain(host.FindAll("[hidden]"), element => !element.ClassList.Contains("omni-panel-menu__children"));
    }

    [Fact]
    public void AGroupTheReaderOpenedByHandIsStillOpenAfterTheFilterWasAppliedAndCleared()
    {
        var host = Render<PanelMenuFilterTestHost>();
        host.Find(ReportsToggle).Click();
        Assert.Equal([true], host.Instance.ExpandedReports);

        host.Find("input[type=search]").Input("param");
        Assert.Contains(Closed, host.Find("#g-monthly").ClassName);
        host.Find("input[type=search]").Input(string.Empty);

        Assert.Contains(Open, host.Find("#g-reports").ClassName);
        Assert.Equal([true], host.Instance.ExpandedReports);
    }

    [Fact]
    public void FoldingAGroupWhileFilteredStaysWithTheFilterAndTheHostKeepsItsOwnState()
    {
        var host = Render<PanelMenuFilterTestHost>();
        host.Find("input[type=search]").Input("janv");
        Assert.Contains(Open, host.Find("#g-reports").ClassName);

        host.Find(ReportsToggle).Click();

        Assert.Contains(Closed, host.Find("#g-reports").ClassName);
        Assert.Empty(host.Instance.ExpandedReports);

        host.Find("input[type=search]").Input(string.Empty);

        Assert.Contains(Closed, host.Find("#g-reports").ClassName);
        Assert.Empty(host.Instance.ExpandedReports);
    }

    [Fact]
    public void ChangingTheTextAgainDropsWhatTheReaderFoldedUnderThePreviousOne()
    {
        var host = Render<PanelMenuFilterTestHost>();
        host.Find("input[type=search]").Input("janv");
        host.Find(ReportsToggle).Click();
        Assert.Contains(Closed, host.Find("#g-reports").ClassName);

        host.Find("input[type=search]").Input("janvi");

        Assert.Contains(Open, host.Find("#g-reports").ClassName);
    }

    [Fact]
    public void AGroupWhoseOwnTextMatchesShowsAllItsEntriesAndNothingElseShows()
    {
        var host = Render<PanelMenuFilterTestHost>();

        host.Find("input[type=search]").Input("param");

        Assert.Equal(["g-settings", "l-general", "l-security"], Shown(host));
        Assert.Contains(Open, host.Find("#g-settings").ClassName);
    }

    [Fact]
    public void ACaseAndAccentInsensitiveTextFindsTheAccentedEntries()
    {
        var host = Render<PanelMenuFilterTestHost>();

        host.Find("input[type=search]").Input("CAFE");
        Assert.Equal(["l-cafe"], Shown(host));

        host.Find("input[type=search]").Input("synthese");
        Assert.Equal(["g-reports", "l-summary"], Shown(host));

        host.Find("input[type=search]").Input("GENERAL");
        Assert.Equal(["g-settings", "l-general"], Shown(host));
    }

    [Fact]
    public void WhenNothingMatchesTheMenuSaysSoInALiveRegionAndShowsNoEntry()
    {
        var host = Render<PanelMenuFilterTestHost>();
        var status = host.Find("p.omni-panel-menu__empty");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal(string.Empty, status.TextContent);

        host.Find("input[type=search]").Input("zzz");

        Assert.Empty(Shown(host));
        Assert.Equal(Localized("PanelMenuNoMatch"), host.Find("p.omni-panel-menu__empty").TextContent);

        host.Find("input[type=search]").Input("cafe");

        Assert.Equal(string.Empty, host.Find("p.omni-panel-menu__empty").TextContent);
    }

    [Fact]
    public void ATextGivenAtTheFirstRenderFiltersAtOnce()
    {
        var host = Render<PanelMenuFilterTestHost>(parameters => parameters.Add(p => p.Filter, "janv"));

        host.WaitForAssertion(() => Assert.Equal(["g-reports", "g-monthly", "l-january"], Shown(host)));
        Assert.Equal(string.Empty, host.Find("p.omni-panel-menu__empty").TextContent);
    }

    [Fact]
    public void TheHostCanDriveTheFilterFromItsOwnFieldWithoutTheBuiltInOne()
    {
        var host = Render<PanelMenuFilterTestHost>(parameters => parameters
            .Add(p => p.ShowFilter, false)
            .Add(p => p.Filter, "cafe"));

        Assert.Empty(host.FindAll("input"));
        Assert.Equal(["l-cafe"], Shown(host));

        host.Render(parameters => parameters.Add(p => p.Filter, null));

        Assert.Equal(AllIds, Shown(host, includeClosedGroups: true));
    }

    [Fact]
    public void TheFieldIsASearchBoxWithAnAccessibleNameAndTheTypedTextIsReportedToTheHost()
    {
        var host = Render<PanelMenuFilterTestHost>();
        var input = host.Find("input");

        Assert.Equal("search", input.GetAttribute("type"));
        Assert.Equal(Localized("PanelMenuFilter"), input.GetAttribute("aria-label"));
        Assert.Equal(Localized("PanelMenuFilter"), input.GetAttribute("placeholder"));

        input.Input("janv");

        Assert.Equal("janv", host.Instance.Filter);
    }

    [Fact]
    public void WithoutShowFilterNoFieldIsRendered()
    {
        var host = Render<PanelMenuFilterTestHost>(parameters => parameters.Add(p => p.ShowFilter, false));

        Assert.Empty(host.FindAll("input"));
        Assert.Empty(host.FindAll("p.omni-panel-menu__empty"));
    }

    [Fact]
    public void DownToItsIconsTheMenuHasNoFieldAndDoesNotFilter()
    {
        var host = Render<PanelMenuFilterTestHost>(parameters => parameters
            .Add(p => p.Display, OmniPanelMenuDisplay.Icon)
            .Add(p => p.Filter, "zzz"));

        Assert.Empty(host.FindAll("input"));
        Assert.DoesNotContain(host.FindAll("[hidden]"), element => !element.ClassList.Contains("omni-panel-menu__children"));
        Assert.Empty(host.FindAll("p.omni-panel-menu__empty"));
    }

    [Fact]
    public void AnEntryAddedWhileFilteredIsJudgedLikeTheOthers()
    {
        var host = Render<PanelMenuFilterTestHost>();
        host.Find("input[type=search]").Input("extra");
        Assert.Equal(Localized("PanelMenuNoMatch"), host.Find("p.omni-panel-menu__empty").TextContent);

        host.Render(parameters => parameters.Add(p => p.ShowExtra, true));

        host.WaitForAssertion(() =>
        {
            Assert.Equal(string.Empty, host.Find("p.omni-panel-menu__empty").TextContent);
            Assert.False(host.Find("#g-settings").HasAttribute("hidden"));
            Assert.False(host.Find("#l-extra").HasAttribute("hidden"));
            Assert.Contains(Open, host.Find("#g-settings").ClassName);
            Assert.True(host.Find("#l-general").HasAttribute("hidden"));
        });
        Assert.Empty(host.Instance.ExpandedReports);
    }

    private string Localized(string key) => Services.GetRequiredService<IStringLocalizer<AppStrings>>()[key].Value;

    // The ids of the entries a reader can reach: not hidden themselves and under no hidden element. A
    // group folded by hand hides its children container without the filter being involved, so those
    // are counted as shown only on request.
    private static string[] Shown(IRenderedComponent<PanelMenuFilterTestHost> host, bool includeClosedGroups = false) =>
        [.. AllIds.Where(id =>
        {
            for (IElement? element = host.Find("#" + id); element is not null; element = element.ParentElement)
            {
                if (element.HasAttribute("hidden") && (!includeClosedGroups || !element.ClassList.Contains("omni-panel-menu__children")))
                {
                    return false;
                }
            }

            return true;
        })];
}
