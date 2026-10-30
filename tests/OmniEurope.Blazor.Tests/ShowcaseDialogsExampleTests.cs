using Bunit;
using OmniEurope.Blazor.Internal;
using OmniEurope.Blazor.Showcase.Examples.Overlays;

namespace OmniEurope.Blazor.Tests;

/// <summary>The information example always has an explicit closing action, including when dismissal is refused.</summary>
public sealed class ShowcaseDialogsExampleTests : OmniBunitContext
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Information_ClosesThroughItsOwnAction_WithEitherDismissalSetting(bool dismissible)
    {
        JSInterop.SetupModule(OmniModules.Dialog).Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(OmniModules.Focus).Mode = JSRuntimeMode.Loose;
        var example = Render<DialogsExample>(parameters => parameters.Add(component => component.Dismissible, dismissible));
        example.FindAll("button").Single(button => button.TextContent.Trim() == "Information").Click();
        var dialog = example.Find("section.omni-dialog");
        Assert.Equal(dismissible ? "dialog" : "alertdialog", dialog.GetAttribute("role"));
        Assert.Equal(dismissible ? 1 : 0, dialog.QuerySelectorAll(".omni-dialog__close").Length);

        dialog.QuerySelectorAll("footer button").Single(button => button.TextContent.Trim() == "Close").Click();

        Assert.Empty(example.FindAll("section.omni-dialog"));
    }
}
