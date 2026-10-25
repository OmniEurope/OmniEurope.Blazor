using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniTextBox.CopyIconOnly"/> (client application acceptance review R-61): the Copy button of
/// <see cref="OmniTextBox.ShowCopy"/> drawn as its icon alone, still named and titled by its word.
/// </summary>
public sealed class TextBoxCopyIconOnlyTests : OmniBunitContext
{
    private readonly string _text = string.Empty;

    [Fact]
    public void ByDefault_TheCopyButtonKeepsItsWord_AndNoTitle()
    {
        var button = RenderBox(iconOnly: false).Find(".omni-text-box-field__copy");

        Assert.DoesNotContain("omni-button--icon", button.ClassList);
        Assert.False(string.IsNullOrWhiteSpace(button.TextContent));
        Assert.Null(button.GetAttribute("title"));
        Assert.Null(button.GetAttribute("aria-label"));
    }

    [Fact]
    public void IconOnly_DrawsTheSquareIconButton_NamedAndTitledByTheWordItNoLongerShows()
    {
        var word = RenderBox(iconOnly: false).Find(".omni-text-box-field__copy").TextContent.Trim();

        var button = RenderBox(iconOnly: true).Find(".omni-text-box-field__copy");

        Assert.Contains("omni-button--icon", button.ClassList);
        Assert.Equal(string.Empty, button.TextContent.Trim());
        Assert.NotNull(button.QuerySelector("svg.omni-icon"));
        Assert.Equal(word, button.GetAttribute("aria-label"));
        Assert.Equal(word, button.GetAttribute("title"));
    }

    [Fact]
    public async Task IconOnly_StillCopiesTheValue_AndAnnouncesTheOutcome()
    {
        var module = JSInterop.SetupModule(OmniModules.Interop);
        module.Setup<bool>("copyText", _ => true).SetResult(true);
        var box = RenderBox(iconOnly: true);

        await box.Find(".omni-text-box-field__copy").ClickAsync(new());

        Assert.Equal("demo@example.org", Assert.Single(module.Invocations["copyText"]).Arguments[0]);
        box.WaitForAssertion(() => Assert.False(string.IsNullOrWhiteSpace(box.Find("[role=status]").TextContent)));
    }

    private IRenderedComponent<OmniTextBox> RenderBox(bool iconOnly) => Render<OmniTextBox>(parameters => parameters
        .Add(component => component.Value, "demo@example.org")
        .Add(component => component.ReadOnly, true)
        .Add(component => component.ShowCopy, true)
        .Add(component => component.CopyIconOnly, iconOnly)
        .Add(component => component.ValueExpression, () => _text));
}
