using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The Left and Right arrows of a tab strip follow what the reader sees: in a right-to-left language the first
/// tab is on the right, so the right arrow goes to the previous tab. Three tabs, so the two directions differ.
/// </summary>
public sealed class TabsDirectionTests : OmniBunitContext
{
    public TabsDirectionTests()
    {
        var module = JSInterop.SetupModule(Internal.OmniModules.Focus);
        module.SetupVoid("configureTabs", _ => true);
        module.SetupVoid("disposeTabs", _ => true);
    }

    [Theory]
    [InlineData("fr-FR", "ArrowRight", "b")]
    [InlineData("fr-FR", "ArrowLeft", "c")]
    [InlineData("ar-SA", "ArrowRight", "c")]
    [InlineData("ar-SA", "ArrowLeft", "b")]
    [InlineData("he-IL", "ArrowRight", "c")]
    public void Arrows_FollowTheReadingDirectionOfTheLanguage(string culture, string key, string expected)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            string? value = "a";
            var tabs = Render<OmniTabs>(p => p
                .Add(t => t.Value, value)
                .Add(t => t.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))
                .Add(t => t.ChildContent, Items));

            tabs.Find(".omni-tabs__viewport").KeyDown(key);

            Assert.Equal(expected, value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void HomeAndEnd_StayTheFirstAndTheLastTabInEveryDirection(string culture)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        try
        {
            string? value = "b";
            var tabs = Render<OmniTabs>(p => p
                .Add(t => t.Value, value)
                .Add(t => t.ValueChanged, EventCallback.Factory.Create<string?>(this, v => value = v))
                .Add(t => t.ChildContent, Items));

            tabs.Find(".omni-tabs__viewport").KeyDown("End");
            Assert.Equal("c", value);
            tabs.Find(".omni-tabs__viewport").KeyDown("Home");
            Assert.Equal("a", value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private static RenderFragment Items => builder =>
    {
        foreach (var key in new[] { "a", "b", "c" })
        {
            builder.OpenComponent<OmniTabsItem>(0);
            builder.AddAttribute(1, nameof(OmniTabsItem.Key), key);
            builder.AddAttribute(2, nameof(OmniTabsItem.Title), key.ToUpperInvariant());
            builder.CloseComponent();
        }
    };
}
