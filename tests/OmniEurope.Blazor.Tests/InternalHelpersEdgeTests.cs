using System.Globalization;
using System.IO.Compression;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Internal helpers read on their own, at the edges their components rarely reach: a workbook without
/// columns or with characters XML refuses, mind map documents with odd identifiers, dangling links and
/// numbers written as text, Gantt tasks entered backwards, timeline steps without an end, notification
/// holds that ran out, and sanitiser policies with malformed entries.
/// </summary>
public sealed class InternalHelpersEdgeTests
{
    // ---- workbook ---------------------------------------------------------------------------------

    private static string Sheet(OmniTableExportDocument document)
    {
        using var archive = new ZipArchive(new MemoryStream(XlsxWriter.Write(document)), ZipArchiveMode.Read);
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        return reader.ReadToEnd();
    }

    [Fact]
    public void Workbook_WithoutColumns_HasNoWidthsAndNoFilter()
    {
        var sheet = Sheet(new OmniTableExportDocument { Title = "Vide", Columns = [], Rows = [] });

        Assert.DoesNotContain("<cols", sheet, StringComparison.Ordinal);
        Assert.DoesNotContain("autoFilter", sheet, StringComparison.Ordinal);
    }

    [Fact]
    public void Workbook_DropsTheCharactersXmlRefuses_AndKeepsTabsAndLineBreaks()
    {
        var sheet = Sheet(new OmniTableExportDocument
        {
            Title = "Texte",
            Columns = [new("Nom", OmniTableExportValueKind.Text)],
            Rows = [[new("a\u0001b￾c￿d\te")]]
        });

        Assert.Contains("abcd\te", sheet, StringComparison.Ordinal);
        Assert.Contains("autoFilter", sheet, StringComparison.Ordinal);
    }

    // ---- mind map ---------------------------------------------------------------------------------

    [Fact]
    public void MindMapModel_DrawsOnlyUsableIdentifiers_AndLinksAndNotesBetweenDrawnNodes()
    {
        var model = new MindMapModel();
        model.Set(new OmniMindMapDocument
        {
            RootId = "absent",
            Nodes =
            [
                new OmniMindMapNode { Id = "a" },
                new OmniMindMapNode { Id = "" },
                new OmniMindMapNode { Id = new string('x', 65) },
                new OmniMindMapNode { Id = "b" }
            ],
            Edges = [new OmniMindMapEdge { From = "a", To = "absent" }, new OmniMindMapEdge { From = "a", To = "b" }, new OmniMindMapEdge { From = "absent", To = "b" }],
            Notes = [new OmniMindMapNote { AttachedTo = "a", Text = "un" }, new OmniMindMapNote { AttachedTo = "a", Text = "deux" }]
        });

        Assert.Equal(["a", "b"], model.Drawable.Select(node => node.Id));
        // A root that is not drawn gives way to the first drawn node.
        Assert.Equal("a", model.Root!.Id);
        Assert.Equal(1, Assert.Single(model.RenderedEdges).Index);
        Assert.Equal(["un", "deux"], model.NotesOf("a").Select(note => note.Note.Text));
    }

    [Fact]
    public void MindMapJson_ReadsANumberWrittenAsTextAsZero()
    {
        var document = MindMapJson.Read("{\"nodes\":[{\"id\":\"a\",\"x\":\"12\",\"y\":7}]}");

        var node = Assert.Single(document.Nodes);
        Assert.Equal(0, node.X);
        Assert.Equal(7, node.Y);
    }

    [Fact]
    public void MindMapHistory_RecordingTheCurrentDocumentAgain_AddsNoStep()
    {
        var history = new MindMapHistory();
        var start = new OmniMindMapDocument();
        history.Reset(start);

        history.Record(start);

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void MindMapGeometry_ANodeWithoutAFontSize_IsMeasuredAtTheDefaultOne()
    {
        var sized = MindMapGeometry.BoxOf(new OmniMindMapNode { Id = "a", Label = "Texte", FontSize = OmniMindMapNode.DefaultFontSize });

        Assert.Equal(sized, MindMapGeometry.BoxOf(new OmniMindMapNode { Id = "a", Label = "Texte", FontSize = 0 }));
    }

    [Theory]
    [InlineData(double.NaN, 1d)]
    [InlineData(double.PositiveInfinity, 1d)]
    public void MindMapZoom_ThatIsNotANumber_IsOne(double zoom, double expected) =>
        Assert.Equal(expected, MindMapViewport.ClampZoom(zoom));

    // ---- scheduling layouts -----------------------------------------------------------------------

    [Fact]
    public void Gantt_TaskEnteredBackwards_SpansItsTwoDates_AndTodayAfterTheRangeIsNotDrawn()
    {
        OmniGanttTask[] tasks =
        [
            new() { Id = "a", Title = "A", Start = new DateOnly(2026, 9, 10), End = new DateOnly(2026, 9, 1) },
            new() { Id = "b", Title = "B", Start = new DateOnly(2026, 9, 3), End = new DateOnly(2026, 9, 12), DependsOn = ["a"] },
            new() { Id = "c", Title = "C", Start = new DateOnly(2026, 10, 20), End = new DateOnly(2026, 10, 25), DependsOn = ["a"] }
        ];

        var layout = GanttLayout.Build(tasks, OmniCalendarView.Day, grouped: false, today: new DateOnly(2027, 6, 1),
            CultureInfo.InvariantCulture, week => $"S{week}");

        Assert.True(layout.RangeStart <= new DateOnly(2026, 9, 1));
        Assert.True(layout.RangeEnd > new DateOnly(2026, 10, 25));
        Assert.Null(layout.TodayX);
        // B starts before A ends: its link turns back; C starts well after: a plain elbow.
        Assert.Equal(2, layout.Dependencies.Count);
        Assert.Contains(layout.Dependencies, path => path.Split(' ').Count(part => part == "V") == 2);
        Assert.Contains(layout.Dependencies, path => path.Split(' ').Count(part => part == "V") == 1);
    }

    [Fact]
    public void Gantt_TodayBeforeTheRange_IsNotDrawn_AndALinkToARowAboveTurnsUpwards()
    {
        // B is listed before A, which it depends on and overlaps: its link climbs back to B's row.
        OmniGanttTask[] tasks =
        [
            new() { Id = "b", Title = "B", Start = new DateOnly(2026, 9, 3), End = new DateOnly(2026, 9, 12), DependsOn = ["a"] },
            new() { Id = "a", Title = "A", Start = new DateOnly(2026, 9, 1), End = new DateOnly(2026, 9, 10) }
        ];

        var layout = GanttLayout.Build(tasks, OmniCalendarView.Day, grouped: false, today: new DateOnly(2020, 1, 1),
            CultureInfo.InvariantCulture, week => $"S{week}");

        Assert.Null(layout.TodayX);
        var path = Assert.Single(layout.Dependencies);
        var upper = layout.Rows.Single(row => row.Task?.Id == "b").Top;
        Assert.Contains($"V {GanttLayout.N(upper + GanttLayout.RowHeight)} ", path, StringComparison.Ordinal);
    }

    [Fact]
    public void StepTimeline_AFinishedStepWithoutItsEnd_EndsWhereItStarted()
    {
        var start = new DateTimeOffset(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);
        var layout = StepTimelineLayout.Build(
            [
                new OmniStepTimelineStep("build", start, start.AddMinutes(5)),
                new OmniStepTimelineStep("test", start.AddMinutes(1), null, OmniStepTimelineStatus.Failed)
            ],
            start.AddHours(1));

        Assert.Equal(TimeSpan.Zero, layout.Bars.Single(bar => bar.Step.Name == "test").Duration);
        Assert.Equal(TimeSpan.FromMinutes(5), layout.Total);
    }

    // ---- theme colours -----------------------------------------------------------------------------

    public static TheoryData<string?, OmniAppearance> AwkwardPalettes => new()
    {
        { null, OmniAppearance.Dark },
        { "#808080", OmniAppearance.Dark },
        { "#123456", OmniAppearance.Dark },
        { "#123456", OmniAppearance.Light }
    };

    [Theory]
    [MemberData(nameof(AwkwardPalettes))]
    public void ThemeColours_OfAGreyPalette_WalkAsFarFromTheSurfaceAsTheyCan(string? darkAccent, OmniAppearance mode)
    {
        // Everything mid-grey: no colour can reach its ratio on such a surface (white on #808080 is 3.95),
        // so each walk runs its forty steps and stops there, still moved away from the surface.
        var palette = new PaletteDefinition(
            "Gris", "Tout gris", "#808080", "#808080", "#808080", "#808080", "#808080",
            "#808080", "#808080", "#808080", "#808080", darkAccent);

        var colors = ThemePresetFactory.BuildColors(palette, mode);

        Assert.Equal("#808080", colors["--omni-color-surface"]);
        Assert.True(ThemeColor.Contrast(colors["--omni-color-text"], colors["--omni-color-surface"]) > 2);
    }

    // ---- notifications ----------------------------------------------------------------------------

    // A clock whose reading moves without firing anything: its timers are the system's, far longer than the test.
    private sealed class MovableClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 5, 8, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    [Fact]
    public void NotificationHold_TakenAfterTheDeadline_KeepsNoTime_AndResumingANotHeldOneDoesNothing()
    {
        var clock = new MovableClock();
        var changes = 0;
        using var store = new OmniNotificationStore(clock, () => changes++, 5, TimeSpan.FromSeconds(5));
        var held = store.Add("Enregistré", OmniSeverity.Success, null, TimeSpan.FromSeconds(5));
        var free = store.Add("Autre", OmniSeverity.Info, null, TimeSpan.FromSeconds(5));

        // The clock moves past the deadline without its timer firing: the hold keeps zero, not a negative time.
        clock.Now += TimeSpan.FromSeconds(30);
        store.Pause(held);
        store.Resume(free);
        store.Resume(held);

        Assert.True(store.Remove(free, notify: false));
    }

    // ---- pickers, text match, form snapshot --------------------------------------------------------

    [Fact]
    public void PickerFormats_ReadAWrittenOutDate_ThatNoFixedPatternMatches()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");

        Assert.True(PickerFormat.TryParseDate("5 octobre 2026", french, out var date));
        Assert.Equal(new DateOnly(2026, 10, 5), date);
        Assert.True(PickerFormat.TryParseDateTime("5 octobre 2026 10:30", french, out var moment));
        Assert.Equal(new DateTime(2026, 10, 5, 10, 30, 0), moment);
    }

    [Fact]
    public void TextMatch_OfABlankQuery_MatchesEverything()
    {
        Assert.True(OmniTextMatch.Contains("Facture", "   "));
        Assert.Single(OmniTextMatch.Split("Facture", "   "));
    }

    [Fact]
    public void FormSnapshot_FollowsAnObjectOfAnAnonymousType()
    {
        var model = new Holder();
        var snapshot = new FormSnapshot();
        snapshot.Take(model);

        // An anonymous type has no namespace: it is an object of the application, read field by field.
        Assert.False(snapshot.Update(new Microsoft.AspNetCore.Components.Forms.FieldIdentifier(model.Extra, "Inner")));
        Assert.False(snapshot.IsModified);
    }

    public sealed class Holder
    {
        public object Extra { get; set; } = new { Inner = "x" };
    }

    // ---- sanitiser --------------------------------------------------------------------------------

    [Fact]
    public void Sanitizer_RefusesAClassWithASpaceAndAnEmptyEntry()
    {
        Assert.Throws<ArgumentException>(() => OmniHtmlSanitizer.Validate(new OmniHtmlSanitizerPolicy { AdditionalCssClasses = ["deux classes"] }));
        Assert.Throws<ArgumentException>(() => OmniHtmlSanitizer.Validate(new OmniHtmlSanitizerPolicy { AdditionalTags = [" "] }));
    }

    [Fact]
    public void Sanitizer_ClassesOfAPolicy_AreTheBaseOnesAndItsOwn_OrNoneWhenAnyClassGoes()
    {
        Assert.Null(OmniHtmlSanitizer.ClassesOf(new OmniHtmlSanitizerPolicy { AllowAnyClass = true }));
        Assert.Contains("maison", OmniHtmlSanitizer.ClassesOf(new OmniHtmlSanitizerPolicy { AdditionalCssClasses = [" Maison "] })!);
        Assert.NotEmpty(OmniHtmlSanitizer.ClassesOf(null)!);
    }

    [Fact]
    public void SanitizePaste_WithNothingToPaste_IsEmpty() =>
        Assert.Equal(string.Empty, OmniHtmlSanitizer.SanitizePaste(null, null, null));
}
