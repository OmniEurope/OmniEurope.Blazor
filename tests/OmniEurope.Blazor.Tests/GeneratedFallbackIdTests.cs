using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Components that derive the ids of their ARIA references from their own id: without one, each
/// instance generates its own, so two of them on a page never point at each other's content. An id
/// given by the consumer is still used as it is.
/// </summary>
public sealed class GeneratedFallbackIdTests : OmniBunitContext
{
    [Fact]
    public void Tooltip_WithoutId_DescribesItsTriggerByItsOwnContent()
    {
        var first = Render<OmniTooltip>(parameters => parameters.Add(component => component.Text, "Un").AddChildContent("A"));
        var second = Render<OmniTooltip>(parameters => parameters.Add(component => component.Text, "Deux").AddChildContent("B"));

        var firstId = first.Find("[role=tooltip]").Id;
        Assert.NotEqual(firstId, second.Find("[role=tooltip]").Id);
        Assert.Equal(firstId, first.Find(".omni-tooltip__trigger").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void Chart_WithoutId_IsLabelledByItsOwnTitleAndDescription()
    {
        var first = Render<OmniChart>(parameters => parameters.Add(component => component.Title, "Ventes").Add(component => component.Description, "Par mois"));
        var second = Render<OmniChart>(parameters => parameters.Add(component => component.Title, "Achats").Add(component => component.Description, "Par mois"));

        var title = first.Find("svg title").Id;
        var description = first.Find("svg desc").Id;
        Assert.NotEqual(title, second.Find("svg title").Id);
        Assert.NotEqual(description, second.Find("svg desc").Id);
        Assert.Equal(title, first.Find("svg").GetAttribute("aria-labelledby"));
        Assert.Equal(description, first.Find("svg").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void StepsItem_WithoutId_ControlsItsOwnPanelInEachGroup()
    {
        var first = Render<OmniSteps>(parameters => parameters.AddChildContent<OmniStepsItem>(Step));
        var second = Render<OmniSteps>(parameters => parameters.AddChildContent<OmniStepsItem>(Step));

        var button = first.Find(".omni-steps__button");
        Assert.NotEqual(button.Id, second.Find(".omni-steps__button").Id);
        Assert.Equal(first.Find(".omni-steps__panel").Id, button.GetAttribute("aria-controls"));
        Assert.Equal(button.Id, first.Find(".omni-steps__panel").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void TabsItem_WithoutId_ControlsItsOwnPanelInEachGroupAcrossBothPasses()
    {
        var first = Render<OmniTabs>(parameters => parameters.Add(component => component.ChildContent, Tab));
        var second = Render<OmniTabs>(parameters => parameters.Add(component => component.ChildContent, Tab));

        var tab = first.Find(".omni-tabs__tab");
        Assert.NotEqual(tab.Id, second.Find(".omni-tabs__tab").Id);
        // The strip and the panels are two renders of the item: both must derive the same ids.
        Assert.Equal(first.Find(".omni-tabs__panel").Id, tab.GetAttribute("aria-controls"));
        Assert.Equal(tab.Id, first.Find(".omni-tabs__panel").GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void TabsItem_WithAnId_KeepsIt()
    {
        var tabs = Render<OmniTabs>(parameters => parameters.Add(component => component.ChildContent, builder =>
        {
            builder.OpenComponent<OmniTabsItem>(0);
            builder.AddAttribute(1, nameof(OmniTabsItem.Id), "general");
            builder.AddAttribute(2, nameof(OmniTabsItem.Title), "Général");
            builder.CloseComponent();
        }));

        Assert.Equal("general-tab", tabs.Find(".omni-tabs__tab").Id);
        Assert.Equal("general-panel", tabs.Find(".omni-tabs__panel").Id);
    }

    [Fact]
    public void RadioButtonList_WithoutNameOrId_DoesNotShareItsGroupWithAnotherBoundToTheSameField()
    {
        var model = new Choice();
        var first = Render<OmniRadioButtonList<string>>(parameters => Radio(parameters, model));
        var second = Render<OmniRadioButtonList<string>>(parameters => Radio(parameters, new Choice()));

        Assert.NotEqual(first.Find("input[type=radio]").GetAttribute("name"), second.Find("input[type=radio]").GetAttribute("name"));
        Assert.Single(first.FindAll("input[type=radio]").Select(radio => radio.GetAttribute("name")).Distinct());
    }

    private static void Step(ComponentParameterCollectionBuilder<OmniStepsItem> step) => step
        .Add(component => component.Index, 0)
        .Add(component => component.Title, "Un");

    private static void Tab(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<OmniTabsItem>(0);
        builder.AddAttribute(1, nameof(OmniTabsItem.Title), "Un");
        builder.CloseComponent();
    }

    private static void Radio(ComponentParameterCollectionBuilder<OmniRadioButtonList<string>> parameters, Choice model) => parameters
        .Add(component => component.Options, [new OmniOption<string>("a", "A"), new OmniOption<string>("b", "B")])
        .Add(component => component.Value, model.Plan)
        .Add(component => component.ValueExpression, () => model.Plan);

    private sealed class Choice
    {
        public string Plan { get; set; } = "a";
    }
}
