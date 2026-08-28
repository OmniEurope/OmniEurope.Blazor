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
        Expression current = parameter;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var member = FindMember(current.Type, segment);
            if (member is null)
            {
                return null;
            }

            Expression access = Expression.MakeMemberAccess(current, member);
            if (CanBeNull(current.Type))
            {
                access = Expression.Condition(
                    Expression.Equal(current, Expression.Constant(null, current.Type)),
                    Expression.Default(access.Type),
                    access);
            }

            current = access;
        }

        var body = Expression.Convert(current, typeof(object));
        var delegateType = typeof(Func<,>).MakeGenericType(itemType, typeof(object));
        return Expression.Lambda(delegateType, body, parameter).Compile();
    }

    private static bool CanBeNull(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static MemberInfo? FindMember(Type type, string name) =>
        (MemberInfo?)type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
        ?? type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
}
