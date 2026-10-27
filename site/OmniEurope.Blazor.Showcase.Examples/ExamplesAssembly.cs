using System.Reflection;

namespace OmniEurope.Blazor.Showcase.Examples;

/// <summary>
/// The assembly of the gallery examples, which embeds the source of every example under its path in
/// this project (<c>Actions/ButtonsExample.razor</c>). Not an example itself: it only lets the
/// showcase find the assembly without naming one example in particular.
/// </summary>
public static class ExamplesAssembly
{
    /// <summary>The assembly holding the examples and their embedded sources.</summary>
    public static Assembly Instance { get; } = typeof(ExamplesAssembly).Assembly;
}
