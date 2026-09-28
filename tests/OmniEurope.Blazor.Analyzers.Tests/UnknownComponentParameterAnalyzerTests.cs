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

        // Shape.razor: IconName on the first badge, Text on the button, IconName on the last badge.
        Assert.Equal(
            [
                @"C:\fixture\Shape.razor(1,25): OmniBadge has no parameter 'IconName'",
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

        // Id, Class (nameof) and ChildContent (a literal) are parameters; class, aria-label and data-x
        // are HTML attributes; @attributes is AddMultipleAttributes; Title sits on a div; Bogus goes to
        // a component of another assembly; Text is a parameter of the badge, not of the button.
        Assert.Equal(3, reported.Length);
        foreach (var accepted in new[] { "Id", "Class", "ChildContent", "class", "aria-label", "data-x", "Title", "Bogus" })
        {
            Assert.DoesNotContain(reported, message => message.EndsWith($"'{accepted}'", StringComparison.Ordinal));
        }

        Assert.DoesNotContain("OmniBadge has no parameter 'Text'", reported);
    }

    [Fact]
    public async Task GeneratedCode_LocatesAttributesOfInferredGenericComponentsInTheirTag()
    {
        var diagnostics = await AnalyzeAsync([GeneratedFixture("Generic"), FixtureCodeBehind()], RazorFixture("Generic"));

        // Lines 1 and 3 go through the TypeInference helpers, line 2 names TValue explicitly.
        Assert.Equal(
            [
                @"C:\fixture\Generic.razor(1,53): OmniDropDown has no parameter 'AllowClear'",
                @"C:\fixture\Generic.razor(2,46): OmniDropDown has no parameter 'Presentation'",
                @"C:\fixture\Generic.razor(3,47): OmniDropDown has no parameter 'IconName'",
            ],
            diagnostics.Select(Describe).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GeneratedCode_WithoutTheRazorFile_StillFailsOnTheRazorPath()
    {
        var diagnostics = await AnalyzeAsync(Shape);

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
