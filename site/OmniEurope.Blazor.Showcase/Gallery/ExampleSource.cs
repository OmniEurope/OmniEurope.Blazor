using OmniEurope.Blazor.Showcase.Examples;

namespace OmniEurope.Blazor.Showcase.Gallery;

/// <summary>
/// Reads the source of an example back out of the examples assembly.
/// </summary>
/// <remarks>
/// The Code tab must never show a transcription of an example: it drifts the first time the example
/// is edited, and the page then teaches code that is not the code being run. The examples project
/// embeds each file it compiles under its relative path, and this reader returns that very text.
/// </remarks>
public static class ExampleSource
{
    /// <summary>The source of the file at <paramref name="path"/> in the examples project.</summary>
    /// <param name="path">The path relative to the examples project, with forward slashes
    /// (<c>Actions/ButtonsExample.razor</c>).</param>
    /// <returns>The source text, or <see langword="null"/> when no file is embedded under that path.</returns>
    public static string? Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = ExamplesAssembly.Instance.GetManifestResourceStream(path);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().TrimEnd();
    }

    /// <summary>Every embedded example path, so a test can prove the catalogue and the files stay in step.</summary>
    public static IReadOnlyList<string> EmbeddedPaths { get; } =
        [.. ExamplesAssembly.Instance.GetManifestResourceNames().Order(StringComparer.Ordinal)];

    /// <summary>The file name shown as the tab of a source: the last segment of its path.</summary>
    public static string FileNameOf(string path) => path[(path.LastIndexOf('/') + 1)..];

    /// <summary>The language label of a source, from its extension.</summary>
    public static string LanguageOf(string path) =>
        path.EndsWith(".razor", StringComparison.Ordinal) ? "razor"
        : path.EndsWith(".css", StringComparison.Ordinal) ? "css"
        : "csharp";
}
