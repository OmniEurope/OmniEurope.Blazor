using System.Reflection;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

var arguments = args.ToHashSet(StringComparer.OrdinalIgnoreCase);
var baselineOption = Array.FindIndex(args, value => value.Equals("--baseline", StringComparison.OrdinalIgnoreCase));
var baseline = baselineOption >= 0 && baselineOption + 1 < args.Length
    ? Path.GetFullPath(args[baselineOption + 1])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "public-api.txt"));

ApiSurfaceExtractor.RunSelfTest();
var signatures = ApiSurfaceExtractor.Extract(typeof(OmniButton).Assembly.GetTypes().Where(ApiSurfaceExtractor.IsApiType));
if (arguments.Contains("--update"))
{
    WriteBaselineAtomically(baseline, signatures);
    Console.WriteLine($"Public API baseline updated: {signatures.Count} signatures.");
    return 0;
}

if (!File.Exists(baseline))
{
    Console.Error.WriteLine($"Public API baseline is missing: {baseline}");
    return 1;
}

var expected = File.ReadAllLines(baseline);
var canonicalExpected = expected.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
if (!expected.SequenceEqual(canonicalExpected, StringComparer.Ordinal))
{
    Console.Error.WriteLine("Public API baseline must be sorted and contain unique signatures.");
    return 1;
}
var removed = expected.Except(signatures, StringComparer.Ordinal).ToArray();
var added = signatures.Except(expected, StringComparer.Ordinal).ToArray();
if (!expected.SequenceEqual(signatures, StringComparer.Ordinal))
{
    foreach (var signature in removed) Console.Error.WriteLine($"- {signature}");
    foreach (var signature in added) Console.Error.WriteLine($"+ {signature}");
    return 1;
}

Console.WriteLine($"Public API baseline passed: {signatures.Count} signatures.");
return 0;

static void WriteBaselineAtomically(string path, IReadOnlyList<string> signatures)
{
    var directory = Path.GetDirectoryName(path)!;
    Directory.CreateDirectory(directory);
    var temporary = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
    try
    {
        File.WriteAllLines(temporary, signatures, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        var written = File.ReadAllLines(temporary);
        if (!written.SequenceEqual(signatures, StringComparer.Ordinal))
        {
            throw new IOException("The temporary public API baseline failed verification.");
        }

        File.Move(temporary, path, overwrite: true);
    }
    finally
    {
        if (File.Exists(temporary)) File.Delete(temporary);
    }
}

internal static class ApiSurfaceExtractor
{
    private const BindingFlags ApiDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static readonly NullabilityInfoContext Nullability = new();

    internal static bool IsApiType(Type type)
    {
        if (!(type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem)) return false;
        return type.DeclaringType is null || IsApiType(type.DeclaringType);
    }

    internal static IReadOnlyList<string> Extract(IEnumerable<Type> types)
    {
        var signatures = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in types.OrderBy(TypeName, StringComparer.Ordinal))
        {
            AddType(signatures, type);
        }

        return signatures.ToArray();
    }

    private static void AddType(ISet<string> signatures, Type type)
    {
        var typeName = TypeName(type);
        var kind = type.IsInterface ? "interface"
            : type.IsEnum ? "enum"
            : typeof(MulticastDelegate).IsAssignableFrom(type.BaseType) ? "delegate"
            : type.IsValueType ? "struct"
            : type.IsAbstract && type.IsSealed ? "static-class"
            : type.IsClass ? "class"
            : "type";
        var modifiers = TypeModifiers(type);
        var inheritance = new List<string>();
        if (type.BaseType is not null && type.BaseType != typeof(object) && !type.IsEnum && kind != "delegate")
        {
            inheritance.Add(TypeName(type.BaseType));
        }
        inheritance.AddRange(type.GetInterfaces().Select(TypeName).Order(StringComparer.Ordinal));
        if (type.IsEnum)
        {
            inheritance.Add(TypeName(Enum.GetUnderlyingType(type)));
        }
        signatures.Add($"type {Access(type)} {typeName} [{kind}{modifiers}]{Constraints(type.GetGenericArguments())}{(inheritance.Count == 0 ? string.Empty : " : " + string.Join(", ", inheritance))}");

        if (typeof(IComponent).IsAssignableFrom(type))
        {
            signatures.Add($"component {typeName}");
        }

        foreach (var constructor in type.GetConstructors(ApiDeclared).Where(IsApiMember).OrderBy(MemberKey, StringComparer.Ordinal))
        {
            signatures.Add($"ctor {Access(constructor)} {typeName}({Parameters(constructor.GetParameters())})");
        }

        foreach (var property in type.GetProperties(ApiDeclared).Where(IsApiProperty).OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            var accessors = new[]
                {
                    IsApiMember(property.GetMethod) ? $"get({Access(property.GetMethod!)})" : null,
                    IsApiMember(property.SetMethod) ? $"{(IsInitOnly(property.SetMethod!) ? "init" : "set")}({Access(property.SetMethod!)})" : null
                }
                .Where(value => value is not null);
            var index = property.GetIndexParameters();
            var parameterAttribute = property.GetCustomAttribute<ParameterAttribute>() is null
                ? string.Empty
                : $" [Parameter{(property.GetCustomAttribute<EditorRequiredAttribute>() is null ? string.Empty : ", EditorRequired")}]";
            var required = property.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute") ? " required" : string.Empty;
            var propertyType = TypeName(property.PropertyType, Nullability.Create(property));
            signatures.Add($"property {Access(property)}{MemberModifiers(property.GetMethod ?? property.SetMethod!)}{required} {typeName}.{property.Name}{(index.Length == 0 ? string.Empty : "[" + Parameters(index) + "]")} : {propertyType} {{{string.Join(';', accessors)}}}{parameterAttribute}");
        }

        foreach (var field in type.GetFields(ApiDeclared).Where(field => !field.IsSpecialName && IsApiMember(field)).OrderBy(field => field.Name, StringComparer.Ordinal))
        {
            var modifier = field.IsLiteral ? $" const = {DefaultValue(field.GetRawConstantValue())}" : field.IsInitOnly ? " readonly" : string.Empty;
            signatures.Add($"field {Access(field)}{(field.IsStatic ? " static" : string.Empty)} {typeName}.{field.Name} : {TypeName(field.FieldType, Nullability.Create(field))}{modifier}");
        }

        foreach (var @event in type.GetEvents(ApiDeclared).Where(IsApiEvent).OrderBy(value => value.Name, StringComparer.Ordinal))
        {
            var accessor = @event.AddMethod ?? @event.RemoveMethod!;
            signatures.Add($"event {Access(accessor)}{MemberModifiers(accessor)} {typeName}.{@event.Name} : {TypeName(@event.EventHandlerType!, Nullability.Create(@event))}");
        }

        foreach (var method in type.GetMethods(ApiDeclared)
                     .Where(method => IsApiMember(method) && (!method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal)))
                     .OrderBy(MemberKey, StringComparer.Ordinal))
        {
            var genericArguments = method.IsGenericMethodDefinition ? method.GetGenericArguments() : [];
            signatures.Add($"method {Access(method)}{MemberModifiers(method)} {typeName}.{method.Name}{GenericList(genericArguments)}({Parameters(method.GetParameters())}) : {TypeName(method.ReturnType, Nullability.Create(method.ReturnParameter))}{Constraints(genericArguments)}");
        }
    }

    private static string Parameters(IEnumerable<ParameterInfo> parameters) => string.Join(", ", parameters.Select(parameter =>
    {
        var modifier = parameter.IsOut ? "out " : parameter.IsIn && parameter.ParameterType.IsByRef ? "in " : parameter.ParameterType.IsByRef ? "ref " : parameter.GetCustomAttribute<ParamArrayAttribute>() is null ? string.Empty : "params ";
        var type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;
        var optional = parameter.HasDefaultValue ? $" = {DefaultValue(parameter.DefaultValue)}" : string.Empty;
        return $"{modifier}{TypeName(type, Nullability.Create(parameter))} {parameter.Name}{optional}";
    }));

    private static string DefaultValue(object? value) => value switch
    {
        null => "null",
        string text => $"\"{text}\"",
        char character => $"'{character}'",
        bool boolean => boolean ? "true" : "false",
        Enum enumValue => $"{enumValue.GetType().FullName}.{enumValue}",
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "default"
    };

    private static string Constraints(IEnumerable<Type> arguments)
    {
        var values = new List<string>();
        foreach (var argument in arguments.Where(argument => argument.IsGenericParameter))
        {
            var constraints = new List<string>();
            var attributes = argument.GenericParameterAttributes;
            if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint)) constraints.Add("class");
            if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint)) constraints.Add("struct");
            constraints.AddRange(argument.GetGenericParameterConstraints().Select(TypeName));
            if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)) constraints.Add("new()");
            if (constraints.Count > 0) values.Add($" where {argument.Name} : {string.Join(", ", constraints)}");
        }
        return string.Concat(values);
    }

    private static string GenericList(IEnumerable<Type> arguments)
    {
        var names = arguments.Select(argument => argument.Name).ToArray();
        return names.Length == 0 ? string.Empty : $"<{string.Join(",", names)}>";
    }

    private static string TypeName(Type type, NullabilityInfo? nullability = null)
    {
        if (type.IsByRef) return TypeName(type.GetElementType()!, nullability?.ElementType) + "&";
        if (type.IsArray)
        {
            var arrayName = TypeName(type.GetElementType()!, nullability?.ElementType) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
            return NullableSuffix(type, nullability, arrayName);
        }
        if (type.IsGenericParameter) return type.Name;
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable is not null) return TypeName(nullable) + "?";
        if (!type.IsGenericType) return NullableSuffix(type, nullability, (type.FullName ?? type.Name).Replace('+', '.'));
        var definition = type.GetGenericTypeDefinition();
        var name = (definition.FullName ?? definition.Name).Split('`')[0].Replace('+', '.');
        var arguments = type.GetGenericArguments();
        var nullabilityArguments = nullability?.GenericTypeArguments ?? [];
        var values = arguments.Select((argument, index) => TypeName(argument, index < nullabilityArguments.Length ? nullabilityArguments[index] : null));
        return NullableSuffix(type, nullability, $"{name}<{string.Join(",", values)}>");
    }

    private static string NullableSuffix(Type type, NullabilityInfo? nullability, string value) =>
        !type.IsValueType && nullability?.ReadState == NullabilityState.Nullable ? value + "?" : value;

    private static bool IsApiMember(MethodBase? method) => method is not null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
    private static bool IsApiMember(FieldInfo field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;
    private static bool IsApiProperty(PropertyInfo property) => IsApiMember(property.GetMethod) || IsApiMember(property.SetMethod);
    private static bool IsApiEvent(EventInfo @event) => IsApiMember(@event.AddMethod) || IsApiMember(@event.RemoveMethod);

    private static string Access(Type type) => type.IsPublic || type.IsNestedPublic ? "public" : type.IsNestedFamORAssem ? "protected-internal" : "protected";
    private static string Access(MethodBase method) => method.IsPublic ? "public" : method.IsFamilyOrAssembly ? "protected-internal" : "protected";
    private static string Access(FieldInfo field) => field.IsPublic ? "public" : field.IsFamilyOrAssembly ? "protected-internal" : "protected";
    private static string Access(PropertyInfo property) => Access((MethodBase?)property.GetMethod ?? property.SetMethod!);

    private static string TypeModifiers(Type type)
    {
        if (type.IsAbstract && type.IsSealed) return ", static";
        return (type.IsAbstract ? ", abstract" : string.Empty) + (type.IsSealed ? ", sealed" : string.Empty);
    }

    private static string MemberModifiers(MethodInfo method)
    {
        var values = new List<string>();
        if (method.IsStatic) values.Add("static");
        if (method.IsAbstract) values.Add("abstract");
        else if (method.IsVirtual && method.GetBaseDefinition() != method) values.Add(method.IsFinal ? "sealed-override" : "override");
        else if (method.IsVirtual && !method.IsFinal) values.Add("virtual");
        return values.Count == 0 ? string.Empty : " " + string.Join(' ', values);
    }

    private static bool IsInitOnly(MethodInfo setter) => setter.ReturnParameter.GetRequiredCustomModifiers().Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static string MemberKey(MethodBase method) => $"{method.Name}({Parameters(method.GetParameters())})";

    internal static void RunSelfTest()
    {
        var signatures = Extract([typeof(ApiFixture<>), typeof(IApiFixture), typeof(ApiValue), typeof(ApiDelegate), typeof(ApiMode)]);
        var required = new[]
        {
            "field public static ApiMode.None : ApiMode const = 0",
            "field public static ApiFixture<T>.Version : System.Int32 const = 2",
            "method protected virtual ApiFixture<T>.Transform(System.String? value) : System.String?",
            "property public required ApiFixture<T>.Name : System.String? {get(public);init(public)}",
            "property public ApiFixture<T>.Matrix : System.Int32[,] {get(public);set(public)}"
        };
        foreach (var signature in required)
        {
            if (!signatures.Contains(signature, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Public API extractor self-test failed for exact signature: {signature}");
            }
        }
    }
}

public interface IApiFixture { void Execute(); }
public readonly record struct ApiValue(int Value)
{
    public static ApiValue operator +(ApiValue left, ApiValue right) => new(left.Value + right.Value);
}
public delegate void ApiDelegate(string value);
public sealed class ApiFixture<T> where T : class, new()
{
    public const int Version = 2;
    public ApiFixture(T value) => Value = value;
    public T Value { get; set; }
    public required string? Name { get; init; }
    public int[,] Matrix { get; set; } = new int[0, 0];
    public event EventHandler? Changed;
    public TResult Convert<TResult>(Func<T, TResult> converter) where TResult : struct => converter(Value);
    protected virtual string? Transform(string? value) => value;
    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
public enum ApiMode { None = 0, Active = 4 }
