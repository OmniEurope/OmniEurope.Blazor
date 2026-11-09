using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniInlineFormKeyHost"/>: Ctrl+Enter or Cmd+Enter submits, Escape cancels, a plain Enter is left alone.</summary>
public sealed class InlineFormKeyHostTests : OmniBunitContext
{
    private int _submits;
    private int _cancels;

    private IRenderedComponent<OmniInlineFormKeyHost> Draw(bool withCancel = true)
        => Render<OmniInlineFormKeyHost>(parameters =>
        {
            parameters.Add(component => component.OnSubmit, EventCallback.Factory.Create(this, () => _submits++));
            if (withCancel)
            {
                parameters.Add(component => component.OnCancel, EventCallback.Factory.Create(this, () => _cancels++));
            }

            parameters.AddChildContent("<textarea id=\"notes\"></textarea>");
        });

    [Theory]
    [InlineData("Enter", true, false)]
    [InlineData("NumpadEnter", true, false)]
    [InlineData("Enter", false, true)]
    public void CtrlOrCmdEnter_Submits_AndCancelsNothing(string key, bool ctrl, bool meta)
    {
        var cut = Draw();

        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = ctrl, MetaKey = meta });

        Assert.Equal(1, _submits);
        Assert.Equal(0, _cancels);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    public void PlainEnter_DoesNotSubmit(bool ctrl, bool shift, bool alt)
    {
        var cut = Draw();

        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter", CtrlKey = ctrl, ShiftKey = shift, AltKey = alt });

        Assert.Equal(0, _submits);
        Assert.Equal(0, _cancels);
    }

    [Fact]
    public void Ctrl_WithAnotherKey_DoesNothing()
    {
        var cut = Draw();

        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "a", CtrlKey = true });

        Assert.Equal(0, _submits);
    }

    [Fact]
    public void Escape_Cancels()
    {
        var cut = Draw();

        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(1, _cancels);
        Assert.Equal(0, _submits);
    }

    [Fact]
    public void Escape_WithoutAHandler_DoesNothing()
    {
        var cut = Draw(withCancel: false);

        cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, _cancels);
        Assert.Equal(0, _submits);
    }

    [Fact]
    public void TheChildContent_IsDrawnInsideTheHost()
    {
        var cut = Draw();

        Assert.NotNull(cut.Find("div.omni-inline-form-keys > textarea#notes"));
    }
}
