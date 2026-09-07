using System.Reflection;

namespace OmniEurope.Blazor.Showcase.Demos;

/// <summary>
/// Reads the markup of a demo back out of the assembly.
/// </summary>
/// <remarks>
/// The gallery must never show a transcription of a demo, because a transcription drifts the first
/// time the demo is edited and the page then teaches code that is not the code being run. The
/// project embeds each demo file as it compiles it, and this reader addresses the resource by the
/// component type itself, so the text shown is the text executed.
/// </remarks>
public static class DemoSource
{
    /// <summary>
    /// The markup of the file that declares <paramref name="component"/>.
    /// </summary>
    /// <returns>The source text, or null when the resource is missing.</returns>
    public static string? Read(Type component)
    {
        ArgumentNullException.ThrowIfNull(component);
        var assembly = component.Assembly;
        var name = component.FullName + ".razor";
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().TrimEnd();
    }

    /// <summary>
    /// Every embedded demo resource, exposed so a test can prove the catalogue and the embedded
    /// files stay in step.
    /// </summary>
    public static IReadOnlyList<string> EmbeddedNames(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return [.. assembly.GetManifestResourceNames().Where(name => name.EndsWith(".razor", StringComparison.Ordinal))];
    }
}
