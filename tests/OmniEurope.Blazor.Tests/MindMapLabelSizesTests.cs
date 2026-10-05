using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The sizes of the mind map texts: a browser measurement is kept for the text it measured, refused for a
/// key that names nothing drawn, and ignored once the text changes.
/// </summary>
public sealed class MindMapLabelSizesTests
{
    private static (MindMapModel Model, MindMapLabelSizes Sizes) Create(OmniMindMapNode? node = null)
    {
        var model = new MindMapModel();
        model.Set(new OmniMindMapDocument
        {
            RootId = "root",
            Nodes = [node ?? new OmniMindMapNode { Id = "root", Label = "Idée" }],
            Notes = [new OmniMindMapNote { AttachedTo = "root", Text = "Une note" }]
        });
        return (model, new MindMapLabelSizes(model));
    }

    [Fact]
    public void Measurement_OfADrawnLabel_SizesItsNode()
    {
        var (model, sizes) = Create();

        Assert.True(sizes.Apply([new MindMapMeasurement(MindMapLabelSizes.NodeKey("root"), 200, 30)]));

        // Text plus padding on each side.
        var (width, height) = sizes.SizeOf(model.Get("root"));
        Assert.Equal(200 + (MindMapGeometry.PaddingX * 2), width);
        Assert.Equal(30 + (MindMapGeometry.PaddingY * 2), height);
    }

    [Fact]
    public void SameMeasurementAgain_ChangesNothing_AndAHalfUnitIsNoChange()
    {
        var (_, sizes) = Create();
        var key = MindMapLabelSizes.NodeKey("root");
        sizes.Apply([new MindMapMeasurement(key, 200, 30)]);

        Assert.False(sizes.Apply([new MindMapMeasurement(key, 200, 30)]));
        Assert.False(sizes.Apply([new MindMapMeasurement(key, 200.4, 30.4)]));
        Assert.True(sizes.Apply([new MindMapMeasurement(key, 201, 30)]));
        Assert.True(sizes.Apply([new MindMapMeasurement(key, 201, 31)]));
    }

    [Fact]
    public void Measurement_IsRefused_ForAKeyThatNamesNothingDrawn_OrAnUnusableSize()
    {
        var (_, sizes) = Create();

        Assert.False(sizes.Apply(
        [
            new MindMapMeasurement(MindMapLabelSizes.NodeKey("gone"), 10, 10),
            new MindMapMeasurement(MindMapLabelSizes.NoteKey(7), 10, 10),
            new MindMapMeasurement("note:-1", 10, 10),
            new MindMapMeasurement("note:x", 10, 10),
            new MindMapMeasurement("edge:0", 10, 10),
            new MindMapMeasurement(MindMapLabelSizes.NodeKey("root"), double.NaN, 10),
            new MindMapMeasurement(MindMapLabelSizes.NodeKey("root"), 10, double.PositiveInfinity)
        ]));
    }

    [Fact]
    public void NoteMeasurement_SizesTheNote_UntilItsTextChanges()
    {
        var (model, sizes) = Create();
        var note = model.NotesOf("root")[0];
        Assert.Equal(MindMapGeometry.EstimateText("Une note", 12, bold: false), sizes.NoteSize(note));

        Assert.True(sizes.Apply([new MindMapMeasurement(MindMapLabelSizes.NoteKey(note.Index), 90, 16)]));
        Assert.Equal((90d, 16d), sizes.NoteSize(note));

        var rewritten = (note.Index, note.Note with { Text = "Une note plus longue" });
        Assert.Equal(MindMapGeometry.EstimateText(rewritten.Item2.Text, 12, bold: false), sizes.NoteSize(rewritten));
    }

    [Fact]
    public void ChangedLabel_FallsBackToTheEstimate_AndAMeasurementOfTheOldTextIsReplaced()
    {
        var (model, sizes) = Create();
        var key = MindMapLabelSizes.NodeKey("root");
        sizes.Apply([new MindMapMeasurement(key, 200, 30)]);

        var bold = model.Get("root") with { Bold = true };
        var (width, _) = sizes.SizeOf(bold);
        var estimate = MindMapGeometry.EstimateText(bold.Label, bold.FontSize, bold: true);
        Assert.Equal(Math.Max(estimate.Width + (MindMapGeometry.PaddingX * 2), MindMapGeometry.MinimumAutoWidth), width);

        // Once the drawn node is bold, the same width measured again is a new measurement.
        model.Set(model.Document with { Nodes = [bold] });
        Assert.True(sizes.Apply([new MindMapMeasurement(key, 200, 30)]));
    }

    [Fact]
    public void FixedSize_WinsOverTheText_AndAShortLabelKeepsTheMinimumWidth()
    {
        var (model, sizes) = Create(new OmniMindMapNode { Id = "root", Label = "A", Width = 300, Height = 90 });
        Assert.Equal((300d, 90d), sizes.SizeOf(model.Get("root")));

        var (shortModel, shortSizes) = Create(new OmniMindMapNode { Id = "root", Label = "A" });
        Assert.Equal(MindMapGeometry.MinimumAutoWidth, shortSizes.SizeOf(shortModel.Get("root")).Width);
    }

    [Fact]
    public void FontSize_FallsBackToTheDefault()
    {
        Assert.Equal(OmniMindMapNode.DefaultFontSize, MindMapLabelSizes.FontSizeOf(new OmniMindMapNode { Id = "n", FontSize = 0 }));
        Assert.Equal(20, MindMapLabelSizes.FontSizeOf(new OmniMindMapNode { Id = "n", FontSize = 20 }));
    }
}
