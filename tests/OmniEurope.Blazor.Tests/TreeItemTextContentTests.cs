using Bunit;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniTreeItem{TValue}.TextContent"/>: a rich row, independent of the child items.</summary>
public sealed class TreeItemTextContentTests : OmniBunitContext
{
    [Fact]
    public void TextContent_IsTheRow_TextNamesIt_AndTheRowStillSelects()
    {
        IReadOnlyList<int> selected = [];
        var tree = Render<OmniTree<int>>(parameters => parameters
            .Add(component => component.Label, "Structure")
            .Add(component => component.SelectedValuesChanged, values => selected = values)
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniTreeItem<int>>(0);
                builder.AddComponentParameter(1, nameof(OmniTreeItem<int>.Value), 7);
                builder.AddComponentParameter(2, nameof(OmniTreeItem<int>.Text), "Article 7");
                builder.AddComponentParameter(3, nameof(OmniTreeItem<int>.TextContent), (RenderFragment)(row =>
                {
                    row.OpenElement(0, "strong");
                    row.AddContent(1, "Art. 7");
                    row.CloseElement();
                }));
                builder.CloseComponent();
            })));

        var button = tree.Find(".omni-tree__select");
        Assert.Equal("Article 7", button.GetAttribute("aria-label"));
        Assert.Equal("Art. 7", button.QuerySelector("strong")!.TextContent);
        Assert.Null(tree.Find(".omni-tree__item").GetAttribute("aria-expanded"));

        button.Click();

        Assert.Equal([7], selected);
    }

    [Fact]
    public void WithoutTextContent_TheRowShowsTextWithoutAnAriaLabel()
    {
        var tree = Render<OmniTree<int>>(parameters => parameters
            .Add(component => component.Label, "Structure")
            .Add(component => component.ChildContent, (RenderFragment)(builder =>
            {
                builder.OpenComponent<OmniTreeItem<int>>(0);
                builder.AddComponentParameter(1, nameof(OmniTreeItem<int>.Value), 1);
                builder.AddComponentParameter(2, nameof(OmniTreeItem<int>.Text), "Titre I");
                builder.CloseComponent();
            })));

        var button = tree.Find(".omni-tree__select");
        Assert.Equal("Titre I", button.TextContent);
        Assert.False(button.HasAttribute("aria-label"));
    }
}
