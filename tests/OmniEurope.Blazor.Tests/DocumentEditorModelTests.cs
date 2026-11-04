using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The model of <see cref="OmniDocumentEditor"/>: what <see cref="DocumentEditorIndex"/> accepts and what it refuses with a
/// clear <see cref="ArgumentException"/>, the way values are compared, and the sizes a drawing is exported at.
/// </summary>
public sealed class DocumentEditorModelTests
{
    private sealed record Foreign(string Id) : OmniDocumentInline(Id);

    private static OmniDocumentRun Run(string id = "r1", string text = "text") => new(id, text);

    private static OmniDocumentField Field(
        string id = "f1",
        OmniDocumentFieldKind kind = OmniDocumentFieldKind.Text,
        string? value = null,
        IReadOnlyList<string>? items = null,
        bool free = false,
        string tag = "tag",
        string label = "Label") => new(id, tag, label, kind, value, items, free);

    private static OmniDocument Doc(params OmniDocumentInline[] inlines) => new([new OmniDocumentBlock("b1", inlines)]);

    private static DocumentEditorIndex Build(OmniDocument document) => DocumentEditorIndex.Build(document, "Document");

    private static OmniDocumentDrawing Drawing(double width = 400, double height = 150, IReadOnlyList<OmniInkStroke>? strokes = null) => new(strokes ?? [], width, height);

    [Fact]
    public void AValidDocument_IsIndexedByIdentifier()
    {
        var document = new OmniDocument(
        [
            new OmniDocumentBlock("b1", [Run("r1"), Field("f1", OmniDocumentFieldKind.Date, "2026-11-04")], Drawing()),
            new OmniDocumentBlock("b2", [Run("r2", string.Empty), Field("f2", OmniDocumentFieldKind.Number, "-12.50"), Field("f3", OmniDocumentFieldKind.List, "B", ["A", "B"]), Field("f4", OmniDocumentFieldKind.List, "free", free: true)])
        ]);

        var index = Build(document);

        Assert.Equal(["r1", "r2"], index.Runs.Keys.Order());
        Assert.Equal(["f1", "f2", "f3", "f4"], index.Fields.Keys.Order());
        Assert.Equal(["b1", "b2"], index.Blocks.Keys.Order());
    }

    [Fact]
    public void AnEmptyDocument_AndEmptyValues_AreAccepted()
    {
        var index = Build(new OmniDocument([new OmniDocumentBlock("b1", [Field("d", OmniDocumentFieldKind.Date, string.Empty), Field("n", OmniDocumentFieldKind.Number), Field("l", OmniDocumentFieldKind.List, null, ["A"])])]));

        Assert.Equal(3, index.Fields.Count);
        Assert.Empty(Build(new OmniDocument([])).Blocks);
    }

    public static TheoryData<string, OmniDocument> Refused() => new()
    {
        { "Blocks is null", new OmniDocument(null!) },
        { "null block", new OmniDocument([null!]) },
        { "empty id", new OmniDocument([new OmniDocumentBlock(" ", [])]) },
        { "The block id 'b' is used twice", new OmniDocument([new OmniDocumentBlock("b", []), new OmniDocumentBlock("b", [])]) },
        { "has no inlines", new OmniDocument([new OmniDocumentBlock("b", null!)]) },
        { "a null inline", new OmniDocument([new OmniDocumentBlock("b1", [null!])]) },
        { "of type Foreign", Doc(new Foreign("x")) },
        { "has an empty id", Doc(Run(string.Empty)) },
        { "inline id 'r1' is used twice", Doc(Run("r1"), Run("r1")) },
        { "inline id 'x' is used twice", Doc(Run("x"), Field("x")) },
        { "inline id 'x' is used twice", Doc(Field("x"), Field("x")) },
        { "has no text", Doc(Run("r1", null!)) },
        { "empty tag", Doc(Field(tag: " ")) },
        { "empty label", Doc(Field(label: string.Empty)) },
        { "unknown kind (9)", Doc(Field(kind: (OmniDocumentFieldKind)9)) },
        { "is not a list", Doc(Field(items: ["A"])) },
        { "is not a list", Doc(Field(kind: OmniDocumentFieldKind.Date, free: true)) },
        { "no items and does not allow free text", Doc(Field(kind: OmniDocumentFieldKind.List)) },
        { "no items and does not allow free text", Doc(Field(kind: OmniDocumentFieldKind.List, items: [])) },
        { "has an empty item", Doc(Field(kind: OmniDocumentFieldKind.List, items: ["A", string.Empty])) },
        { "lists the item 'A' twice", Doc(Field(kind: OmniDocumentFieldKind.List, items: ["A", "A"])) },
        { "not a date written yyyy-MM-dd", Doc(Field(kind: OmniDocumentFieldKind.Date, value: "04/11/2026")) },
        { "not a date written yyyy-MM-dd", Doc(Field(kind: OmniDocumentFieldKind.Date, value: "2026-13-01")) },
        { "not a number in the invariant culture", Doc(Field(kind: OmniDocumentFieldKind.Number, value: "1,5")) },
        { "not a number in the invariant culture", Doc(Field(kind: OmniDocumentFieldKind.Number, value: "abc")) },
        { "not one of its items", Doc(Field(kind: OmniDocumentFieldKind.List, value: "C", items: ["A", "B"])) },
        { "no strokes list or holds a null stroke", new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing(null!, 100, 100))]) },
        { "no strokes list or holds a null stroke", new OmniDocument([new OmniDocumentBlock("b", [], new OmniDocumentDrawing([null!], 100, 100))]) },
        { "finite size above zero", new OmniDocument([new OmniDocumentBlock("b", [], Drawing(0, 100))]) },
        { "finite size above zero", new OmniDocument([new OmniDocumentBlock("b", [], Drawing(100, -1))]) },
        { "finite size above zero", new OmniDocument([new OmniDocumentBlock("b", [], Drawing(double.NaN, 100))]) },
        { "finite size above zero", new OmniDocument([new OmniDocumentBlock("b", [], Drawing(100, double.PositiveInfinity))]) },
        { "finite size above zero", new OmniDocument([new OmniDocumentBlock("b", [], Drawing(6145, 100))]) }
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public void ADocumentThatBreaksTheModel_IsRefused_WithAClearArgumentException(string expected, OmniDocument document)
    {
        var failure = Assert.Throws<ArgumentException>(() => Build(document));

        Assert.Contains(expected, failure.Message, StringComparison.Ordinal);
        Assert.Equal("Document", failure.ParamName);
    }

    [Fact]
    public void AListWithoutItems_IsFineWhenItAllowsFreeText_AndTheLargestDrawingIsAccepted()
    {
        Build(Doc(Field(kind: OmniDocumentFieldKind.List, free: true)));
        Build(new OmniDocument([new OmniDocumentBlock("b", [], Drawing(6144, 6144))]));
    }

    [Theory]
    [InlineData(400, 533)]
    [InlineData(150, 200)]
    [InlineData(72, 96)]
    [InlineData(0.1, 1)]
    [InlineData(1000.5, 1334)]
    public void Pixels_AreThePointsAtNinetySixDpi_RoundedAndAtLeastOne(double points, int pixels)
        => Assert.Equal(pixels, DocumentEditorIndex.Pixels(points));

    [Fact]
    public void Normalize_TurnsAnEmptyStringIntoNoValue()
    {
        Assert.Null(DocumentEditorIndex.Normalize(null));
        Assert.Null(DocumentEditorIndex.Normalize(string.Empty));
        Assert.Equal(" ", DocumentEditorIndex.Normalize(" "));
    }

    [Fact]
    public void SameValue_ComparesNumbersByNumber_AndTheRestByText()
    {
        Assert.True(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Text, null, null));
        Assert.False(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Text, "a", null));
        Assert.False(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Text, null, "a"));
        Assert.True(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Text, "a", "a"));
        Assert.False(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Text, "a", "A"));
        Assert.True(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Number, "12.50", "12.5"));
        Assert.False(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Number, "12.5", "12.6"));
        Assert.False(DocumentEditorIndex.SameValue(OmniDocumentFieldKind.Date, "2026-11-04", "2026-11-05"));
    }

    [Fact]
    public void TheChangeSet_IsEmptyOnlyWithoutAnyChange()
    {
        Assert.True(new OmniDocumentChangeSet([], [], []).IsEmpty);
        Assert.False(new OmniDocumentChangeSet([new OmniRunTextChange("r", "t")], [], []).IsEmpty);
        Assert.False(new OmniDocumentChangeSet([], [new OmniFieldValueChange("f", "t", null)], []).IsEmpty);
        Assert.False(new OmniDocumentChangeSet([], [], [new OmniDrawingChange("b", Drawing(), [1])]).IsEmpty);
    }
}
