using System.Collections.Immutable;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Analyzers.Tests;

internal static class AnalyzerTestHarness
{
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    /// <summary>
    /// Compiles C# sources against ASP.NET Core Components and the OmniEurope.Blazor assembly, then runs
    /// OE0001 with the given .razor files as additional files, as the Razor SDK passes them.
    /// </summary>
    internal static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        IEnumerable<(string Path, string Text)> sources,
        params (string Path, string Text)[] razorFiles)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            "AnalyzerFixture",
            sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, parseOptions, source.Path)),
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, "The fixture does not compile: " + string.Join(Environment.NewLine, errors.Select(error => error.ToString())));

        var options = new AnalyzerOptions(razorFiles
            .Select(file => (AdditionalText)new InMemoryAdditionalText(file.Path, file.Text))
            .ToImmutableArray());
        var diagnostics = await compilation
            .WithAnalyzers([new UnknownComponentParameterAnalyzer()], options)
            .GetAllDiagnosticsAsync();
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
        return diagnostics.Where(diagnostic => diagnostic.Id == UnknownComponentParameterAnalyzer.DiagnosticId).ToImmutableArray();
    }

    /// <summary>The captured generator output of <c>Fixtures/{name}.razor</c> and the .razor file itself.</summary>
    internal static (string Path, string Text) GeneratedFixture(string name) =>
        ($"{name}_razor.g.cs", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".razor.g.cs.txt")));

    internal static (string Path, string Text) RazorFixture(string name) =>
        ($@"C:\fixture\{name}.razor", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name + ".razor")));

    /// <summary>The code-behind members and the foreign component the captured fixtures use.</summary>
    internal static (string Path, string Text) FixtureCodeBehind() => ("CodeBehind.cs", """
        using System.Collections.Generic;
        using Microsoft.AspNetCore.Components;
        using OmniEurope.Blazor.Components;

        namespace Fixture;

        public partial class Shape
        {
            private Dictionary<string, object> Extra { get; } = new();
            private string Name => "n";
            private void Go() { }
        }

        public partial class Generic
        {
            private IReadOnlyList<OmniOption<string>> Opts { get; } = [];
            private string? Selected { get; set; }
        }

        public sealed class Foreign : ComponentBase
        {
            [Parameter(CaptureUnmatchedValues = true)]
            public IReadOnlyDictionary<string, object>? Attributes { get; set; }
        }
        """);

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        var paths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Concat([
                typeof(ComponentBase).Assembly.Location,
                typeof(OmniBadge).Assembly.Location,
            ])
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return paths.Select(path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text, Encoding.UTF8);
    }
}
