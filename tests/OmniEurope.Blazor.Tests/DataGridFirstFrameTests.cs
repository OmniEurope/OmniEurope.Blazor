using Bunit;
using OmniEurope.Blazor.Components;
using Item = OmniEurope.Blazor.Tests.DataGridFirstFrameTestHost.Item;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The column components of a grid register only after its first render. That render used to draw the items
/// through the implicit column, their raw text, replaced a moment later by the real cells: a visible flash of
/// wrong content on every page opening. A grid that declares columns now shows its loading row until they
/// registered; a columns fragment that declares none still falls back to the implicit column.
/// </summary>
public sealed class DataGridFirstFrameTests : OmniBunitContext
{
    private static readonly Item[] Items = [new(1, "Alpha"), new(2, "Bravo")];

    [Theory]
    [InlineData(OmniDataGridScrollMode.All)]
    [InlineData(OmniDataGridScrollMode.Paged)]
    [InlineData(OmniDataGridScrollMode.Virtual)]
    public void Declared_columns_are_never_preceded_by_the_raw_items(OmniDataGridScrollMode mode)
    {
        Item.RawTextReads = 0;

        var grid = Render<DataGridFirstFrameTestHost>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.ScrollMode, mode));

        grid.WaitForAssertion(() => Assert.Contains("Alpha", grid.Find("tbody").TextContent, StringComparison.Ordinal));
        Assert.Equal(0, Item.RawTextReads);
        Assert.DoesNotContain("RAW", grid.Markup, StringComparison.Ordinal);
        Assert.Empty(grid.FindAll("tbody .omni-data-grid__pending"));
    }

    [Fact]
    public void A_columns_fragment_that_declares_none_still_shows_the_implicit_column()
    {
        var grid = Render<DataGridFirstFrameTestHost>(parameters => parameters
            .Add(component => component.Items, Items)
            .Add(component => component.DeclareColumn, false));

        grid.WaitForAssertion(() => Assert.Contains("RAW Alpha", grid.Find("tbody").TextContent, StringComparison.Ordinal));
        Assert.Empty(grid.FindAll("tbody .omni-data-grid__pending"));
    }
}
