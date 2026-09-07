using System.Collections.Immutable;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace OmniEurope.Analyzers.Tests;

internal static class AnalyzerTestHarness
{
    private static readonly ImmutableArray<MetadataReference> References = CreateReferences();

    internal static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, params (string Path, string Text)[] additionalFiles)
    {
        var compilation = CSharpCompilation.Create(
            "AnalyzerFixture",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview), "Fixture.cs")],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var options = new AnalyzerOptions(additionalFiles
            .Select(file => (AdditionalText)new InMemoryAdditionalText(file.Path, file.Text))
            .ToImmutableArray());
        return await compilation
            .WithAnalyzers([new OmniEuropeConventionAnalyzer()], options)
            .GetAnalyzerDiagnosticsAsync();
    }

    private static ImmutableArray<MetadataReference> CreateReferences()
    {
        var paths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Concat([
                typeof(ControllerBase).Assembly.Location,
                typeof(ApiControllerAttribute).Assembly.Location,
                typeof(AuthorizeAttribute).Assembly.Location
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
