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
    private decimal? _price;

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

    [Theory]
    [InlineData("Enter")]
    [InlineData("NumpadEnter")]
    public void CtrlEnter_InANumericNeverBlurred_SubmitsTheTypedValue(string key)
    {
        decimal? submitted = -1m;
        var cut = Render<OmniInlineFormKeyHost>(parameters => parameters
            .Add(component => component.OnSubmit, EventCallback.Factory.Create(this, () => submitted = _price))
            .AddChildContent<OmniNumeric<decimal?>>(numeric => numeric
                .Add(field => field.Value, _price)
                .Add(field => field.ValueChanged, EventCallback.Factory.Create<decimal?>(this, value => _price = value))
                .Add(field => field.ValueExpression, () => _price)
                .Add(field => field.DisplayName, "Prix")));

        var input = cut.Find("input");
        input.Input("345");
        input.KeyDown(new KeyboardEventArgs { Key = key, CtrlKey = true });

        Assert.Equal(345m, submitted);
    }

    [Fact]
    public void Typing_WithoutEnter_DoesNotCommitBeforeTheChange()
    {
        var commits = 0;
        var cut = Render<OmniNumeric<decimal?>>(parameters => parameters
            .Add(field => field.Value, _price)
            .Add(field => field.ValueChanged, EventCallback.Factory.Create<decimal?>(this, value => { _price = value; commits++; }))
            .Add(field => field.ValueExpression, () => _price)
                .Add(field => field.DisplayName, "Prix"));

        var input = cut.Find("input");
        input.Input("3");
        input.Input("34");
        input.KeyDown(new KeyboardEventArgs { Key = "a" });
        Assert.Equal(0, commits);

        input.Change("34");
        Assert.Equal(34m, _price);
        Assert.Equal(1, commits);
    }

    [Fact]
    public void TheChildContent_IsDrawnInsideTheHost()
    {
        var cut = Draw();

        Assert.NotNull(cut.Find("div.omni-inline-form-keys > textarea#notes"));
    }
}
