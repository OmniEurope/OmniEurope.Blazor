using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The values a form's model held when the form started or was last saved, so that a field brought
/// back to its value leaves the form unmodified (Astraia recette R-061). The public readable
/// properties of the model are recorded, and those of the objects it holds a few levels down (an
/// address, a group of settings); a collection is recorded as its items, compared in order. An empty
/// text and no text are the same value, so typing then erasing in an empty field changes nothing. A
/// field the record does not know (on an object created after it was taken) counts as changed once it
/// changes, as before.
/// </summary>
internal sealed class FormSnapshot
{
    private const int Depth = 3;
    private static readonly object Unknown = new();

    private readonly Dictionary<FieldIdentifier, object?> _values = [];
    private readonly HashSet<FieldIdentifier> _changed = [];

    /// <summary>Whether a field differs from the record.</summary>
    internal bool IsModified => _changed.Count > 0;

    /// <summary>Records the values the model holds now and forgets every change.</summary>
    internal void Take(object? model)
    {
        _values.Clear();
        _changed.Clear();
        if (model is not null)
        {
            Record(model, Depth, new HashSet<object>(ReferenceEqualityComparer.Instance));
        }
    }

    /// <summary>Compares a field that changed with its recorded value; true when <see cref="IsModified"/> moved.</summary>
    internal bool Update(FieldIdentifier field)
    {
        var before = IsModified;
        var original = _values.TryGetValue(field, out var recorded) ? recorded : Unknown;
        if (ReferenceEquals(original, Unknown) || !Same(original, Capture(Read(field.Model, field.FieldName))))
        {
            _changed.Add(field);
        }
        else
        {
            _changed.Remove(field);
        }

        return before != IsModified;
    }

    private void Record(object model, int depth, HashSet<object> seen)
    {
        if (!seen.Add(model))
        {
            return;
        }

        foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value;
            try
            {
                value = property.GetValue(model);
            }
            catch (TargetInvocationException)
            {
                continue;
            }

            _values[new FieldIdentifier(model, property.Name)] = Capture(value);
            if (depth > 1 && value is not null && Holds(value.GetType()))
            {
                Record(value, depth - 1, seen);
            }
        }
    }

    // An object whose own fields a form may edit: a class of the application, not text, a collection
    // or a framework type.
    private static bool Holds(Type type) =>
        !type.IsValueType && type != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(type)
        && type.Namespace?.StartsWith("System", StringComparison.Ordinal) != true
        && type.Namespace?.StartsWith("Microsoft", StringComparison.Ordinal) != true;

    private static object? Read(object model, string name)
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var type = model.GetType();
        try
        {
            if (type.GetProperty(name, Flags) is { CanRead: true } property && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(model);
            }

            return type.GetField(name, Flags) is { } field ? field.GetValue(model) : Unknown;
        }
        catch (TargetInvocationException)
        {
            return Unknown;
        }
    }

    // A collection is kept as a copy of its items: a list edited in place must not compare with itself. Bytes (a
    // picture, an attachment) are copied as bytes; a sequence that is no collection (a query, a generator) is never
    // run and is kept as it is; a collection that fails to list its items is unknown, so it counts as changed.
    private static object? Capture(object? value)
    {
        if (value is byte[] bytes)
        {
            return bytes.ToArray();
        }

        if (value is string || value is not IEnumerable items || !IsCollection(value))
        {
            return value;
        }

        try
        {
            return items.Cast<object?>().ToArray();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Unknown;
        }
    }

    private static bool IsCollection(object value) =>
        value is ICollection || value.GetType().GetInterfaces().Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));

    private static bool Same(object? original, object? current)
    {
        if (ReferenceEquals(current, Unknown))
        {
            return false;
        }

        if (original is byte[] bytesBefore && current is byte[] bytesAfter)
        {
            return bytesBefore.AsSpan().SequenceEqual(bytesAfter);
        }

        if (original is object?[] before && current is object?[] after)
        {
            return before.SequenceEqual(after);
        }

        return Equals(Text(original), Text(current));
    }

    private static object? Text(object? value) => value is "" ? null : value;
}
