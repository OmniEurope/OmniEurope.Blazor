using Microsoft.CodeAnalysis;
using static OmniEurope.Blazor.Analyzers.Tests.AnalyzerTestHarness;

namespace OmniEurope.Blazor.Analyzers.Tests;

public sealed class UnknownComponentParameterAnalyzerTests
{
    private static readonly (string Path, string Text)[] Shape = [GeneratedFixture("Shape"), FixtureCodeBehind()];

    [Fact]
    public async Task GeneratedCode_ReportsEachUnknownPascalCaseParameterAtItsRazorPosition()
    {
        var diagnostics = await AnalyzeAsync(Shape, RazorFixture("Shape"));

        // Shape.razor: IconName and a lowercase class on the first badge, Text on the button, IconName on
        // the last badge.
        Assert.Equal(
            [
                @"C:\fixture\Shape.razor(1,25): OmniBadge has no parameter 'IconName'",
                @"C:\fixture\Shape.razor(1,38): OmniBadge takes 'Class', not 'class': write Class=""...""",
                @"C:\fixture\Shape.razor(5,13): OmniButton has no parameter 'Text'",
                @"C:\fixture\Shape.razor(8,12): OmniBadge has no parameter 'IconName'",
            ],
            diagnostics.Select(Describe).Order(StringComparer.Ordinal));
        Assert.All(diagnostics, diagnostic => Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity));
    }

    [Fact]
    public async Task GeneratedCode_AcceptsKnownParametersLowercaseAttributesSplatsElementsAndForeignComponents()
    {
        var diagnostics = await AnalyzeAsync(Shape, RazorFixture("Shape"));
        var reported = diagnostics.Select(diagnostic => diagnostic.GetMessage()).ToArray();

        // Tone, Id, Class (nameof, written as the parameters) and ChildContent (a literal) are
        // parameters; aria-label and data-x are HTML attributes; @attributes is AddMultipleAttributes;
        // Title sits on a div; Bogus goes to a component of another assembly. Of the attributes of the
        // badges, only the lowercase class of the first one is reported.
        Assert.Equal(4, reported.Length);
        Assert.Single(reported, message => message.Contains("not 'class'", StringComparison.Ordinal));
        foreach (var accepted in new[] { "Tone", "Id", "Class", "ChildContent", "aria-label", "data-x", "Title", "Bogus" })
        {
            Assert.DoesNotContain(reported, message => message.EndsWith($"'{accepted}'", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task GeneratedCode_LocatesAttributesOfInferredGenericComponentsInTheirTag()
    {
        var diagnostics = await AnalyzeAsync([GeneratedFixture("Generic"), FixtureCodeBehind()], RazorFixture("Generic"));

        // Lines 1 and 3 go through the TypeInference helpers, line 2 names TValue explicitly. The
        // lowercase class of line 3 is bound in the helper too, and still found at its tag.
        Assert.Equal(
            [
                @"C:\fixture\Generic.razor(1,53): OmniDropDown has no parameter 'AllowClear'",
                @"C:\fixture\Generic.razor(2,46): OmniDropDown has no parameter 'Presentation'",
                @"C:\fixture\Generic.razor(3,47): OmniDropDown has no parameter 'IconName'",
                @"C:\fixture\Generic.razor(3,68): OmniDropDown takes 'Class', not 'class': write Class=""...""",
            ],
            diagnostics.Select(Describe).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GeneratedCode_WithoutTheRazorFile_StillFailsOnTheRazorPath()
    {
        var diagnostics = await AnalyzeAsync(Shape);

        // A lowercase class is read in the .razor text, so it cannot be seen without it; the three
        // unknown parameters still fail.
        Assert.Equal(3, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal(@"C:\fixture\Shape.razor", diagnostic.Location.GetLineSpan().Path));
    }

    [Fact]
    public async Task HandWrittenBuilderCode_ChecksInheritedParametersCaseInsensitivelyAndReportsAtTheLiteral()
    {
        var diagnostics = await AnalyzeAsync([("Handwritten.cs", """
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using OmniEurope.Blazor.Components;

            namespace Fixture;

            public sealed class Handwritten : ComponentBase
            {
                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<OmniBadge>(0);
                    builder.AddComponentParameter(1, "Class", "x");
                    builder.AddComponentParameter(2, "PRESETNAME", "none");
                    builder.AddComponentParameter(3, "Glyph", "x");
                    builder.CloseComponent();
                    builder.OpenComponent<OmniCheckBox<bool>>(4);
                    builder.AddComponentParameter(5, "DisplayName", "n");
                    builder.AddComponentParameter(6, "AdditionalAttributes", null);
                    builder.CloseComponent();
                    builder.OpenElement(7, "div");
                    builder.AddAttribute(8, "Title", "t");
                    builder.OpenComponent<OmniBadge>(9);
                    builder.AddAttribute(10, "Nested", "n");
                    builder.CloseComponent();
                    builder.AddAttribute(11, "Label", "t");
                    builder.CloseElement();
                }
            }
            """)]);

        // Class and PresetName come from OmniComponentBase, DisplayName and AdditionalAttributes from
        // InputBase; Title and Label sit on the div, after the nested badge closed.
        Assert.Equal(
            ["Handwritten.cs(14,42): OmniBadge has no parameter 'Glyph'", "Handwritten.cs(23,34): OmniBadge has no parameter 'Nested'"],
            diagnostics.Select(Describe).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task ComponentWithoutCapture_ReportsEveryUnknownAttributeEvenLowercase()
    {
        var diagnostics = await AnalyzeAsync([("NoCapture.cs", """
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using OmniEurope.Blazor.Components;

            namespace Fixture;

            public sealed class NoCapture : ComponentBase
            {
                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<OmniUnsavedChangesGuard>(0);
                    builder.AddComponentParameter(1, "HasChanges", true);
                    builder.AddComponentParameter(2, "class", "x");
                    builder.AddComponentParameter(3, "data-step", "2");
                    builder.CloseComponent();
                    builder.OpenComponent<OmniBootSplash>(4);
                    builder.AddComponentParameter(5, "onclick", "x");
                    builder.AddComponentParameter(6, "Class", "x");
                    builder.CloseComponent();
                    builder.OpenComponent<OmniBadge>(7);
                    builder.AddComponentParameter(8, "data-step", "2");
                    builder.CloseComponent();
                }
            }
            """)]);

        // The guard and the splash capture no attribute: a lowercase one would only fail at render.
        // The badge captures them, so its data-step is an HTML attribute.
        Assert.Equal(
            [
                "NoCapture.cs(13,42): OmniUnsavedChangesGuard has no parameter 'class'",
                "NoCapture.cs(14,42): OmniUnsavedChangesGuard has no parameter 'data-step'",
                "NoCapture.cs(17,42): OmniBootSplash has no parameter 'onclick'",
                "NoCapture.cs(18,42): OmniBootSplash has no parameter 'Class'",
            ],
            diagnostics.Select(Describe).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task LiteralLowercaseClassOrId_OnAComponentWithTheParameter_IsReported()
    {
        var diagnostics = await AnalyzeAsync([("Lowercase.cs", """
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;
            using OmniEurope.Blazor.Components;

            namespace Fixture;

            public sealed class Lowercase : ComponentBase
            {
                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<OmniBadge>(0);
                    builder.AddComponentParameter(1, "class", "x");
                    builder.AddComponentParameter(2, "id", "b");
                    builder.AddComponentParameter(3, "Class", "y");
                    builder.CloseComponent();
                }
            }
            """)]);

        Assert.Equal(
            [
                "Lowercase.cs(12,42): OmniBadge takes 'Class', not 'class': write Class=\"...\"",
                "Lowercase.cs(13,42): OmniBadge takes 'Id', not 'id': write Id=\"...\"",
            ],
            diagnostics.Select(Describe).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task BoundIdWrittenLowercaseInTheRazorFile_IsReportedThere_AndTheParameterSpellingIsNot()
    {
        // What the generator writes for <OmniBadge id="a" /> then <OmniBadge Id="b" />: both bound to
        // Id with nameof, each mapped by its #line directive to the attribute as written.
        const string generated = """
            #pragma checksum "C:\fixture\Ids.razor" "{8829d00f-11b8-4213-878b-770e8597ac16}" "00"
            namespace Fixture
            {
                public partial class Ids : global::Microsoft.AspNetCore.Components.ComponentBase
                {
                    protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                    {
                        __builder.OpenComponent<global::OmniEurope.Blazor.Components.OmniBadge>(0);
                        __builder.AddComponentParameter(1, nameof(global::OmniEurope.Blazor.Components.OmniBadge.
            #line (1,12)-(1,14) "C:\fixture\Ids.razor"
            Id
            #line default
            #line hidden
                        ), "a");
                        __builder.CloseComponent();
                        __builder.OpenComponent<global::OmniEurope.Blazor.Components.OmniBadge>(2);
                        __builder.AddComponentParameter(3, nameof(global::OmniEurope.Blazor.Components.OmniBadge.
            #line (2,12)-(2,14) "C:\fixture\Ids.razor"
            Id
            #line default
            #line hidden
                        ), "b");
                        __builder.CloseComponent();
                    }
                }
            }
            """;
        const string razor = "<OmniBadge id=\"a\" />\n<OmniBadge Id=\"b\" />\n";

        var diagnostics = await AnalyzeAsync([("Ids_razor.g.cs", generated)], (@"C:\fixture\Ids.razor", razor));

        Assert.Equal(
            [@"C:\fixture\Ids.razor(1,12): OmniBadge takes 'Id', not 'id': write Id=""..."""],
            diagnostics.Select(Describe));
    }

    [Fact]
    public async Task ComponentsOfAnotherAssembly_AreNotAnalyzed()
    {
        var diagnostics = await AnalyzeAsync([("Foreign.cs", """
            using System.Collections.Generic;
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace Fixture;

            public sealed class Local : ComponentBase
            {
                [Parameter(CaptureUnmatchedValues = true)]
                public IReadOnlyDictionary<string, object>? Attributes { get; set; }
            }

            public sealed class Host : ComponentBase
            {
                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<Local>(0);
                    builder.AddComponentParameter(1, "IconName", "x");
                    builder.CloseComponent();
                }
            }
            """)]);

        Assert.Empty(diagnostics);
    }

    private static string Describe(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetLineSpan();
        return $"{span.Path}({span.StartLinePosition.Line + 1},{span.StartLinePosition.Character + 1}): {diagnostic.GetMessage()}";
    }
}
