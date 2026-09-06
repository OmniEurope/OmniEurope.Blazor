using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Showcase.Demos;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds the gallery to its promise: every component the library publishes is shown running.
/// </summary>
/// <remarks>
/// A showcase that covers part of a library is worse than no showcase, because a reader cannot tell
/// the untouched components from the missing ones. This test enumerates the public components out of
/// the assembly and fails the moment one of them has no demonstration, so the gap is a build failure
/// rather than something to notice later.
/// </remarks>
public sealed class ShowcaseCoverageTests
{
    /// <summary>
    /// Components that cannot sit in a gallery entry, each for a reason that would not change by
    /// writing more demos. Kept deliberately short: anything else belongs in a demo.
    /// </summary>
    private static readonly HashSet<string> NotDemonstrable = new(StringComparer.Ordinal)
    {
        // Mounted once at the application root to host dialogs and notifications; the showcase
        // itself mounts it in App.razor, and a second instance inside a demo would fight the first.
        "OmniComponentsHost"
    };

    private static IReadOnlyList<Type> PublicComponents =>
    [
        .. typeof(OmniButton).Assembly.GetTypes()
            .Where(type => type is { IsPublic: true, IsAbstract: false }
                && typeof(IComponent).IsAssignableFrom(type))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
    ];

    [Fact]
    public void EveryPublicComponent_IsDemonstratedSomewhere()
    {
        var sources = DemoCatalog.All
            .Select(demo => DemoSource.Read(demo.Component) ?? string.Empty)
            .ToArray();

        var missing = PublicComponents
            .Select(TagName)
            .Where(tag => !NotDemonstrable.Contains(tag))
            .Where(tag => !sources.Any(source => Mentions(source, tag)))
            .ToArray();

        Assert.True(
            missing.Length == 0,
            $"{missing.Length} component(s) have no demonstration: {string.Join(", ", missing)}");
    }

    [Fact]
    public void ExcludedComponents_AreStillRealComponents()
    {
        var known = PublicComponents.Select(TagName).ToHashSet(StringComparer.Ordinal);
        foreach (var excluded in NotDemonstrable)
        {
            Assert.True(known.Contains(excluded), $"The exclusion list names {excluded}, which is not a public component.");
        }
    }

    [Fact]
    public void EveryDemo_IsReachableFromTheGallery()
    {
        Assert.NotEmpty(DemoCatalog.All);
        foreach (var demo in DemoCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(demo.Key));
            Assert.NotEmpty(demo.CapabilityKeys);
        }
    }

    /// <summary>The name a component is written under in markup, generic arity stripped.</summary>
    private static string TagName(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? name : name[..tick];
    }

    /// <summary>
    /// Whether a demo opens that element. The trailing character check keeps <c>OmniText</c> from
    /// matching <c>OmniTextBox</c>.
    /// </summary>
    private static bool Mentions(string source, string tag)
    {
        var index = source.IndexOf('<' + tag, StringComparison.Ordinal);
        while (index >= 0)
        {
            var after = index + 1 + tag.Length;
            if (after >= source.Length || !char.IsLetterOrDigit(source[after]))
            {
                return true;
            }

            index = source.IndexOf('<' + tag, after, StringComparison.Ordinal);
        }

        return false;
    }
}
