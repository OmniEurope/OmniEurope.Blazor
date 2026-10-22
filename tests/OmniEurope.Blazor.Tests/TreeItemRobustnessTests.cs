using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>An item a tree opens by itself tells its host without losing what the host's handler throws.</summary>
public sealed class TreeItemRobustnessTests : OmniBunitContext
{
    [Fact]
    public async Task AnItemOpenedByTheTree_ReportsToItsHost_AndAFailingHandlerReachesTheRenderer()
    {
        var reported = new List<bool>();
        var items = new List<string> { "a" };
        var failing = false;
        RenderFragment Content() => builder =>
        {
            var sequence = 0;
            foreach (var name in items)
            {
                builder.OpenComponent<OmniTreeItem<string>>(sequence++);
                builder.SetKey(name);
                builder.AddComponentParameter(sequence++, nameof(OmniTreeItem<string>.Value), name);
                builder.AddComponentParameter(sequence++, nameof(OmniTreeItem<string>.Text), name);
                builder.AddComponentParameter(sequence++, nameof(OmniTreeItem<string>.ExpandedChanged),
                    EventCallback.Factory.Create<bool>(this, opened =>
                    {
                        reported.Add(opened);
                        if (failing)
                        {
                            throw new InvalidOperationException("handler failed");
                        }
                    }));
                builder.AddComponentParameter(sequence++, nameof(OmniTreeItem<string>.ChildContent), (RenderFragment)(child =>
                {
                    child.OpenComponent<OmniTreeItem<string>>(0);
                    child.AddComponentParameter(1, nameof(OmniTreeItem<string>.Value), name + "-child");
                    child.AddComponentParameter(2, nameof(OmniTreeItem<string>.Text), name + "-child");
                    child.CloseComponent();
                }));
                builder.CloseComponent();
            }
        };
        var tree = Render<OmniTree<string>>(parameters => parameters.Add(component => component.ChildContent, Content()));
        await tree.InvokeAsync(tree.Instance.ExpandAllAsync);
        reported.Clear();

        // An item rendered under a tree that was expanded as a whole opens too, and says so to its host.
        items.Add("b");
        tree.Render(parameters => parameters.Add(component => component.ChildContent, Content()));
        await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);
        Assert.Equal([true], reported);

        failing = true;
        items.Add("c");
        var failure = await Record.ExceptionAsync(async () =>
        {
            tree.Render(parameters => parameters.Add(component => component.ChildContent, Content()));
            await Task.Delay(200, Xunit.TestContext.Current.CancellationToken);
            tree.Render(parameters => parameters.Add(component => component.Label, "Arbre"));
        });

        Assert.NotNull(failure);
        Assert.Contains("handler failed", failure.ToString(), StringComparison.Ordinal);
    }
}
