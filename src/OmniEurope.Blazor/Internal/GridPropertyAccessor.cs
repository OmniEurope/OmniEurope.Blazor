using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Compiles a dotted property path such as <c>Customer.Name</c> into a reusable accessor, so a
/// column can be declared with a property name instead of a lambda. Accessors are cached per item
/// type and path, a null link in the chain yields <c>null</c> instead of throwing, and an unknown
/// path yields no accessor at all so the caller can fall back.
/// </summary>
internal static class GridPropertyAccessor
{
    private static readonly ConcurrentDictionary<(Type Type, string Path), Delegate?> Cache = new();

    internal static Func<TItem, object?>? Create<TItem>(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var accessor = Cache.GetOrAdd((typeof(TItem), path), static key => Compile(key.Type, key.Path));
        return (Func<TItem, object?>?)accessor;
    }

    private static Delegate? Compile(Type itemType, string path)
    {
        var parameter = Expression.Parameter(itemType, "item");
        var body = Access(parameter, path.Split('.', StringSplitOptions.RemoveEmptyEntries), 0);
        if (body is null)
        {
            return null;
        }

        var delegateType = typeof(Func<,>).MakeGenericType(itemType, typeof(object));
        return Expression.Lambda(delegateType, body, parameter).Compile();
    }

    /// <summary>
    /// The rest of the path from <paramref name="current"/>, boxed. A null link returns a null object
    /// rather than the default of the member after it, so <c>Customer.Age</c> of an order without a
    /// customer is null, not 0. Null for an unknown segment.
    /// </summary>
    private static Expression? Access(Expression current, string[] segments, int index)
    {
        if (index == segments.Length)
        {
            return Expression.Convert(current, typeof(object));
        }

        var member = FindMember(current.Type, segments[index]);
        if (member is null)
        {
            return null;
        }

        var rest = Access(Expression.MakeMemberAccess(current, member), segments, index + 1);
        if (rest is null || !CanBeNull(current.Type))
        {
            return rest;
        }

        return Expression.Condition(
            Expression.Equal(current, Expression.Constant(null, current.Type)),
            Expression.Constant(null, typeof(object)),
            rest);
    }


    private static readonly ConcurrentDictionary<(Type Type, string Path), bool> NumericCache = new();

    /// <summary>
    /// Whether the dotted path ends on a number (integer or decimal, nullable or not), so the grid
    /// can align the column at the end in figures of one width. An unknown path is not numeric.
    /// </summary>
    internal static bool IsNumeric<TItem>(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return NumericCache.GetOrAdd((typeof(TItem), path), static key =>
        {
            var current = key.Type;
            foreach (var segment in key.Path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var member = FindMember(current, segment);
                if (member is null)
                {
                    return false;
                }

                current = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
            }

            var type = Nullable.GetUnderlyingType(current) ?? current;
            return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte)
                || type == typeof(decimal) || type == typeof(double) || type == typeof(float);
        });
    }
    /// <summary>
    /// The value type the dotted path ends on, nullable unwrapped, or null for an unknown path. The
    /// filter menu offers the operators that type can be compared with.
    /// </summary>
    internal static Type? ValueType<TItem>(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var current = typeof(TItem);
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var member = FindMember(current, segment);
            if (member is null)
            {
                return null;
            }

            current = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
        }

        return Nullable.GetUnderlyingType(current) ?? current;
    }

    private static readonly ConcurrentDictionary<(Type Type, string Path), Type?> EnumCache = new();

    /// <summary>
    /// The enum type the dotted path ends on (nullable or not), or null. A filter uses it to offer
    /// every member as a candidate, which a remote grid could not derive from the rows it holds.
    /// </summary>
    internal static Type? EnumType<TItem>(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return EnumCache.GetOrAdd((typeof(TItem), path), static key =>
        {
            var current = key.Type;
            foreach (var segment in key.Path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var member = FindMember(current, segment);
                if (member is null)
                {
                    return null;
                }

                current = member is PropertyInfo property ? property.PropertyType : ((FieldInfo)member).FieldType;
            }

            var type = Nullable.GetUnderlyingType(current) ?? current;
            return type.IsEnum ? type : null;
        });
    }

    private static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static MemberInfo? FindMember(Type type, string name) =>
        (MemberInfo?)type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
        ?? type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
}
