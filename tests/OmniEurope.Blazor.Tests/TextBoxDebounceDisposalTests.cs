using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniTextBox.Debounce"/>: the text typed during the delay reaches the bound value when
/// the field leaves the page before the delay ends, and removing the parent with it, or the whole
/// renderer, stays silent.
/// </summary>
public sealed class TextBoxDebounceDisposalTests : OmniBunitContext
{
    [Fact]
    public void AFieldRemovedDuringTheDelay_HandsItsTextToTheBoundValue()
    {
        var host = Render<FieldHost>();
        _ = host.Find("input").InputAsync(new ChangeEventArgs { Value = "abc" });
        Assert.Equal(string.Empty, host.Instance.Text);

        host.Render(parameters => parameters.Add(component => component.ShowField, false));

        Assert.Empty(host.FindAll("input"));
        Assert.Equal("abc", host.Instance.Text);
        // The parent rendered again with the value, in the batch that removed the field.
        host.WaitForAssertion(() => Assert.Equal("abc", host.Find("output").TextContent));
    }

    [Fact]
    public void AFieldRemovedWithNothingPending_ChangesNothing()
    {
        var host = Render<FieldHost>();

        host.Render(parameters => parameters.Add(component => component.ShowField, false));

        Assert.Equal(0, host.Instance.Changes);
        Assert.Equal(string.Empty, host.Instance.Text);
    }

    [Fact]
    public void AFieldRemovedWithItsParent_DoesNotThrow()
    {
        var outer = Render<OuterHost>();
        var field = outer.FindComponent<FieldHost>().Instance;
        _ = outer.Find("input").InputAsync(new ChangeEventArgs { Value = "abc" });

        outer.Render(parameters => parameters.Add(component => component.ShowHost, false));

        Assert.Empty(outer.FindAll("input"));
        // The parent is gone as well: its value callback still runs, and its render request is ignored.
        Assert.Equal("abc", field.Text);
    }

    [Fact]
    public async Task AFieldDisposedWithTheRenderer_DoesNotThrow()
    {
        var host = Render<FieldHost>();
        var field = host.Instance;
        _ = host.Find("input").InputAsync(new ChangeEventArgs { Value = "abc" });

        await DisposeComponentsAsync();

        Assert.Equal("abc", field.Text);
    }

    private sealed class OuterHost : ComponentBase
    {
        [Parameter]
        public bool ShowHost { get; set; } = true;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ShowHost)
            {
                builder.OpenComponent<FieldHost>(0);
                builder.CloseComponent();
            }
        }
    }

    private sealed class FieldHost : ComponentBase
    {
        [Parameter]
        public bool ShowField { get; set; } = true;

        public string Text { get; private set; } = string.Empty;

        public int Changes { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "output");
            builder.AddContent(1, Text);
            builder.CloseElement();
            if (ShowField)
            {
                builder.OpenComponent<OmniTextBox>(2);
                builder.AddAttribute(3, nameof(OmniTextBox.Value), Text);
                builder.AddAttribute(4, nameof(OmniTextBox.ValueChanged), EventCallback.Factory.Create<string>(this, value =>
                {
                    Changes++;
                    Text = value;
                }));
                builder.AddAttribute(5, nameof(OmniTextBox.ValueExpression), (Expression<Func<string>>)(() => Text));
                builder.AddAttribute(6, nameof(OmniTextBox.Debounce), TimeSpan.FromHours(1));
                builder.CloseComponent();
            }
        }
    }
}
