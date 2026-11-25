// Guards for two rules of docs/architecture.md and docs/public-api-conventions.md that had none
// (audit of 2026-11-13, A4 and A5):
// - an adapter of a component, a class that receives a public component at its construction and carries
//   one of its responsibilities, is declared under the Internal/ folder like the Grid* and MindMap* ones; and
//   an Internal class that is not such an adapter, a pure engine, holds no public component either. The
//   Internal/ folder shares the OmniEurope.Blazor.Components namespace (owner decision of 2026-11-23, which
//   broke the Components <-> Internal namespace cycle), so the tier is read from the source folder and every
//   type declared there stays non-public;
// - every public input component carries ReadOnly (bool, false by default), with the two documented
//   exceptions and the one documented default of true.

using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests.Guards;

public sealed class ComponentArchitectureGuardTests
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Assembly Package = typeof(OmniButton).Assembly;

    private static bool IsPublicComponent(Type type) =>
        type.IsPublic && typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract;

    /// <summary>A public component type, an open generic one (OmniDataGrid&lt;TItem&gt;) included.</summary>
    private static bool IsComponentType(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        return IsPublicComponent(definition);
    }

    private static bool ReceivesAComponent(Type type) =>
        type.GetConstructors(Instance).Any(constructor => constructor.GetParameters().Any(parameter => IsComponentType(parameter.ParameterType)));

    private static IEnumerable<Type> PackageClasses() => Package.GetTypes()
        .Where(type => type.IsClass && !type.IsNested && !type.Name.Contains('<', StringComparison.Ordinal)
            && !typeof(IComponent).IsAssignableFrom(type) && !Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));

    private static readonly Regex TopLevelDeclaration = new(
        @"^(?:(?:public|internal|static|sealed|abstract|partial|readonly|ref|unsafe)\s+)*(?:class|struct|interface|enum|record(?:\s+class|\s+struct)?|delegate\s+\S+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>
    /// The simple names of the top-level types declared under src/OmniEurope.Blazor/Internal, read from the
    /// sources (file-scoped namespaces, so a top-level declaration starts its line). The scan refuses to be empty.
    /// </summary>
    private static readonly Lazy<HashSet<string>> InternalFolderTypeNames = new(() =>
        RepositoryScan.Enumerate(Path.Combine(RepositoryScan.Root, "src", "OmniEurope.Blazor", "Internal"), "*.cs")
            .SelectMany(file => TopLevelDeclaration.Matches(File.ReadAllText(file)).Select(match => match.Groups["name"].Value))
            .ToHashSet(StringComparer.Ordinal));

    private static bool IsDeclaredInInternalFolder(Type type) =>
        !type.IsNested && type.Namespace == "OmniEurope.Blazor.Components" && InternalFolderTypeNames.Value.Contains(StemOf(type));

    [Fact]
    public void InternalFolderTypes_AreFoundInThePackage_AndStayNonPublic()
    {
        var packageTypes = Package.GetTypes().Where(IsDeclaredInInternalFolder).ToArray();
        var found = packageTypes.Select(StemOf).ToHashSet(StringComparer.Ordinal);
        var unresolved = InternalFolderTypeNames.Value.Where(name => !found.Contains(name)).Order(StringComparer.Ordinal).ToArray();

        Assert.True(unresolved.Length == 0,
            "Every type declared under src/OmniEurope.Blazor/Internal is a top-level type of OmniEurope.Blazor.Components in the package; the source scan found names the assembly does not hold:\n"
            + string.Join('\n', unresolved));

        var publicOffenders = packageTypes.Where(type => type.IsPublic).Select(type => type.FullName!).Order(StringComparer.Ordinal).ToArray();
        Assert.True(publicOffenders.Length == 0,
            "A type declared under the Internal/ folder is an implementation detail and stays non-public (docs/architecture.md):\n"
            + string.Join('\n', publicOffenders));
    }

    [Fact]
    public void ComponentAdapters_LiveInInternal()
    {
        var offenders = PackageClasses()
            .Where(type => !type.IsPublic && ReceivesAComponent(type) && !IsDeclaredInInternalFolder(type))
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "An internal class that receives a public component at its construction is an adapter of that component and lives in Internal (docs/architecture.md):\n"
            + string.Join('\n', offenders));
    }

    [Fact]
    public void InternalEngines_HoldNoPublicComponent_UnlessGivenOneAtConstruction()
    {
        var offenders = PackageClasses()
            .Where(type => IsDeclaredInInternalFolder(type) && !ReceivesAComponent(type))
            .SelectMany(type => type.GetFields(Instance)
                .Where(field => IsComponentType(field.FieldType))
                .Select(field => $"{type.FullName}.{field.Name} : {field.FieldType.Name}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "A pure engine of Internal depends on no public component; a class that needs one receives it at its construction, as an adapter (docs/architecture.md):\n"
            + string.Join('\n', offenders));
    }

    [Fact]
    public void InputComponents_CarryReadOnly()
    {
        // Bound by Value and ValueChanged without being an input: the documented exceptions (a form that
        // composes other fields, a command), then the components whose Value is a navigation or a selection
        // (the open tab, step or node, the selected row or task), and the grid's own filter controls,
        // which the grid draws and disables with its filter row.
        string[] withoutReadOnly =
        [
            "OmniDynamicForm", "OmniToggleButton",
            "OmniTabs", "OmniSteps", "OmniWizard", "OmniTree", "OmniDataGrid", "OmniGantt",
            "OmniDataGridFilterCombo", "OmniDataGridFilterDate", "OmniDataGridFilterDateRange", "OmniDataGridFilterMultiSelect",
        ];
        var inputs = Package.GetExportedTypes()
            .Where(IsPublicComponent)
            .Where(type => Parameter(type, "ValueChanged") is not null && Parameter(type, "Value") is not null)
            .ToArray();
        Assert.NotEmpty(inputs);

        var offenders = inputs
            .Where(type => !withoutReadOnly.Contains(StemOf(type), StringComparer.Ordinal))
            .Where(type => Parameter(type, "ReadOnly")?.PropertyType != typeof(bool))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "Every input component carries ReadOnly (bool), docs/public-api-conventions.md \"Lecture seule\":\n"
            + string.Join('\n', offenders));
    }

    private static PropertyInfo? Parameter(Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        return property is not null && Attribute.IsDefined(property, typeof(ParameterAttribute)) ? property : null;
    }

    private static string StemOf(Type type)
    {
        var tick = type.Name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? type.Name : type.Name[..tick];
    }
}
