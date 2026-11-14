// Guards for two rules of docs/architecture.md and docs/public-api-conventions.md that had none
// (audit of 2026-11-13, A4 and A5):
// - an adapter of a component, a class that receives a public component at its construction and carries
//   one of its responsibilities, lives in OmniEurope.Blazor.Internal like the Grid* and MindMap* ones; and
//   an Internal class that is not such an adapter, a pure engine, holds no public component either;
// - every public input component carries ReadOnly (bool, false by default), with the two documented
//   exceptions and the one documented default of true.

using System.Reflection;
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

    [Fact]
    public void ComponentAdapters_LiveInInternal()
    {
        var offenders = PackageClasses()
            .Where(type => !type.IsPublic && ReceivesAComponent(type) && type.Namespace != "OmniEurope.Blazor.Internal")
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
            .Where(type => type.Namespace == "OmniEurope.Blazor.Internal" && !ReceivesAComponent(type))
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
