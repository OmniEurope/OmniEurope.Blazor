using System.Reflection;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds the gallery to the second half of its promise: not only every component, every variant.
/// </summary>
/// <remarks>
/// Showing a component once proves it exists; it does not show what it can do. A visitor reading the
/// gallery has no way to tell a variant nobody demonstrated from one the library does not have, and
/// a variant nobody demonstrates is a variant nobody notices breaking. This walks the enumerated
/// parameters of every public component and requires each value to appear in some demonstration.
/// </remarks>
public sealed class ShowcaseVariantCoverageTests
{
    /// <summary>
    /// Enumerations the visitor exercises through a component's own interface rather than through a
    /// parameter set in markup. Their values are demonstrated, live, by the advanced grid; writing
    /// them as literals in a demo would add source that teaches nothing.
    /// </summary>
    private static readonly HashSet<string> ChosenAtRuntime = new(StringComparer.Ordinal)
    {
        // The operator, its second condition and the case rule are picked by the visitor in the
        // grid's own filter menu, which the advanced demonstration opens by running in Advanced mode.
        "OmniDataGridFilterOperator",
        "OmniDataGridLogicalOperator",
        "OmniDataGridFilterCaseSensitivity"
    };

    private static readonly string DemoSources = string.Concat(
        Directory.EnumerateFiles(DemoFolder(), "*.razor*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));

    [Fact]
    public void EveryVariantOfEveryComponent_IsDemonstratedSomewhere()
    {
        var missing = new List<string>();
        foreach (var (component, parameter, enumType) in EnumeratedParameters())
        {
            if (ChosenAtRuntime.Contains(enumType.Name))
            {
                continue;
            }

            var shipped = DefaultValueOf(component, parameter);
            foreach (var value in Enum.GetNames(enumType))
            {
                if (string.Equals(value, shipped, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!DemoSources.Contains($"{enumType.Name}.{value}", StringComparison.Ordinal))
                {
                    missing.Add($"{enumType.Name}.{value} ({component.Name}.{parameter.Name})");
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            $"{missing.Count} variant(s) are published but never demonstrated: {string.Join(", ", missing.Distinct())}");
    }

    [Fact]
    public void TheRuntimeExclusions_AreRealEnumerationsOfRealParameters()
    {
        var used = EnumeratedParameters().Select(entry => entry.EnumType.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var excluded in ChosenAtRuntime)
        {
            Assert.True(used.Contains(excluded), $"The exclusion list names {excluded}, which no component parameter uses.");
        }
    }

    [Fact]
    public void TheExclusionsAreOnlyValidBecauseTheAdvancedFilterUiIsDemonstrated()
    {
        // The three excluded enumerations are only reachable through the grid's advanced filter
        // menu. If no demonstration opens that menu, the exclusion stops being justified.
        Assert.Contains("OmniDataGridFilterMode.Advanced", DemoSources, StringComparison.Ordinal);
    }

    private static IEnumerable<(Type Component, PropertyInfo Parameter, Type EnumType)> EnumeratedParameters()
    {
        var assembly = typeof(OmniButton).Assembly;
        foreach (var type in assembly.GetTypes()
            .Where(candidate => candidate is { IsPublic: true, IsAbstract: false }
                && typeof(IComponent).IsAssignableFrom(candidate))
            .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.IsDefined(typeof(ParameterAttribute), inherit: true)))
            {
                var parameterType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (parameterType.IsEnum && parameterType.Assembly == assembly)
                {
                    yield return (type, property, parameterType);
                }
            }
        }
    }

    /// <summary>
    /// The value the component ships with, which the gallery demonstrates by saying nothing. Null
    /// when the component cannot be built here, in which case every value must be shown explicitly.
    /// </summary>
    private static string? DefaultValueOf(Type component, PropertyInfo parameter)
    {
        try
        {
            var closed = component.IsGenericTypeDefinition
                ? component.MakeGenericType([.. component.GetGenericArguments().Select(_ => typeof(object))])
                : component;
            var instance = Activator.CreateInstance(closed);
            return instance is null ? null : parameter.GetValue(instance)?.ToString();
        }
        catch (Exception)
        {
            // A component that cannot be built here (a generic constraint, a required service) tells
            // us nothing about its shipped value. Falling back to null is the strict answer: every
            // value then has to appear in a demonstration.
            return null;
        }
    }

    private static string DemoFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine(directory.FullName, "site", "OmniEurope.Blazor.Showcase", "Components", "Demos");
    }
}
