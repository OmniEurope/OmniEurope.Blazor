using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Named parameter sets for package components, registered once by the host at startup through
/// <c>AddOmniEuropePreset</c>. A component takes its type's default preset, the one named by
/// <see cref="OmniComponentBase.PresetName"/>, or none with <c>PresetName="none"</c>; a parameter written
/// explicitly always wins over the preset. A generic component is matched by its generic type definition,
/// so <c>typeof(OmniDataGrid&lt;&gt;)</c> covers every grid.
/// </summary>
public sealed class OmniPresetRegistry
{
    /// <summary>The <c>PresetName</c> value that opts a component out of its type's default preset.</summary>
    public const string None = "none";

    private readonly Dictionary<Type, Dictionary<string, IReadOnlyDictionary<string, object?>>> _presets = [];
    private readonly Dictionary<Type, string> _defaults = [];
    private readonly ConcurrentDictionary<(Type Component, string Parameter), PropertyInfo> _properties = new();

    /// <summary>
    /// Registers <paramref name="values"/> (parameter name to value) as preset <paramref name="name"/> of
    /// <paramref name="componentType"/>. Throws when the type is not a package component, when the name is
    /// taken or reserved, when a second default is declared, or when a value names no parameter of the
    /// component or has the wrong type: a mistake fails the host at startup, not a page at render.
    /// </summary>
    /// <remarks>
    /// A parameter whose type depends on the component's type parameter (<c>Items</c> of a grid) can only
    /// be checked for its shape at startup: a string for a list fails here, a list of the wrong item type
    /// fails on the first render of the closed component, with a message naming the preset.
    /// </remarks>
    public void Add(Type componentType, string name, IReadOnlyDictionary<string, object?> values, bool isDefault = false)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);
        if (componentType.IsGenericType && !componentType.IsGenericTypeDefinition)
            throw new ArgumentException($"Register the generic type definition ({componentType.Name}<>), not a closed type, so every item type shares the preset.", nameof(componentType));
        if (!IsPackageComponent(componentType))
            throw new ArgumentException($"{componentType.Name} does not derive from OmniComponentBase or OmniInputBase<>.", nameof(componentType));
        if (string.Equals(name, None, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"'{None}' is reserved: it opts a component out of its default preset.", nameof(name));

        foreach (var (parameter, value) in values)
            Validate(componentType, parameter, value);

        if (!_presets.TryGetValue(componentType, out var byName))
            _presets[componentType] = byName = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal);
        if (byName.ContainsKey(name))
            throw new ArgumentException($"{componentType.Name} already has a preset named '{name}'.", nameof(name));
        if (isDefault && _defaults.TryGetValue(componentType, out var existing))
            throw new ArgumentException($"{componentType.Name} already has a default preset ('{existing}').", nameof(isDefault));

        byName[name] = new Dictionary<string, object?>(values, StringComparer.Ordinal);
        if (isDefault) _defaults[componentType] = name;
    }

    /// <summary>
    /// Sets on <paramref name="component"/> every value of its preset that <paramref name="parameters"/>
    /// does not carry. Runs before the explicit parameters are assigned, so they win.
    /// </summary>
    internal void Apply(IComponent component, ParameterView parameters, string? presetName)
    {
        var type = component.GetType();
        var (name, values) = Resolve(type, presetName);
        if (values is null) return;
        foreach (var (parameter, value) in values)
        {
            if (parameters.TryGetValue<object?>(parameter, out _)) continue;
            var property = PropertyOf(type, parameter);
            // Only a parameter typed by the component's type parameter can still be wrong here: the
            // registration checked every other one against its property type.
            if (!Accepts(property.PropertyType, value))
                throw new InvalidOperationException($"Preset '{name}' of {KeyOf(type).Name} gives '{parameter}' {(value is null ? "null" : $"a {value.GetType().Name}")}; this component expects a {property.PropertyType.Name}.");
            property.SetValue(component, value);
        }
    }

    private (string? Name, IReadOnlyDictionary<string, object?>? Values) Resolve(Type type, string? presetName)
    {
        if (string.Equals(presetName, None, StringComparison.OrdinalIgnoreCase)) return (null, null);
        var key = KeyOf(type);
        _presets.TryGetValue(key, out var byName);
        if (presetName is null)
            return _defaults.TryGetValue(key, out var fallback) ? (fallback, byName![fallback]) : (null, null);
        return byName is not null && byName.TryGetValue(presetName, out var named)
            ? (presetName, named)
            : throw new InvalidOperationException($"No preset named '{presetName}' is registered for {key.Name}.");
    }

    /// <summary>Throws the same error as a host with no preset at all when a name is asked for.</summary>
    internal static void ThrowUnregistered(Type type, string presetName)
    {
        if (string.Equals(presetName, None, StringComparison.OrdinalIgnoreCase)) return;
        var key = KeyOf(type);
        throw new InvalidOperationException($"No preset named '{presetName}' is registered for {key.Name}.");
    }

    private PropertyInfo PropertyOf(Type type, string parameter) =>
        _properties.GetOrAdd((type, parameter), static k => k.Component.GetProperty(k.Parameter, BindingFlags.Public | BindingFlags.Instance)!);

    private static void Validate(Type componentType, string parameter, object? value)
    {
        var property = componentType.GetProperty(parameter, BindingFlags.Public | BindingFlags.Instance);
        var attribute = property?.GetCustomAttribute<ParameterAttribute>();
        if (property is null || attribute is null)
            throw new ArgumentException($"{componentType.Name} has no parameter named '{parameter}'.", nameof(parameter));
        if (attribute.CaptureUnmatchedValues || parameter == nameof(OmniComponentBase.PresetName))
            throw new ArgumentException($"'{parameter}' of {componentType.Name} cannot be set by a preset.", nameof(parameter));

        var type = property.PropertyType;
        var accepted = type.ContainsGenericParameters ? MayFit(type, value) : Accepts(type, value);
        if (!accepted)
            throw new ArgumentException($"'{parameter}' of {componentType.Name} is a {type.Name}; the preset gives {(value is null ? "null" : value.GetType().Name)}.", nameof(parameter));
    }

    private static Type KeyOf(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    private static bool Accepts(Type type, object? value) => value is null
        ? !type.IsValueType || Nullable.GetUnderlyingType(type) is not null
        : type.IsInstanceOfType(value);

    /// <summary>
    /// Whether a value can fit a parameter typed by the component's type parameter for some item type: a
    /// list for <c>IReadOnlyList&lt;TItem&gt;</c>. A null value or a bare <c>TValue</c> can fit a closed
    /// type; the first render checks the rest.
    /// </summary>
    private static bool MayFit(Type type, object? value)
    {
        if (value is null || !type.IsGenericType) return true;
        var valueType = value.GetType();
        var definition = type.GetGenericTypeDefinition();
        return SelfAndAncestors(valueType).Concat(valueType.GetInterfaces())
            .Any(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == definition);
    }

    private static IEnumerable<Type> SelfAndAncestors(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
            yield return current;
    }

    private static bool IsPackageComponent(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current == typeof(OmniComponentBase)) return true;
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(OmniInputBase<>)) return true;
        }
        return false;
    }
}
