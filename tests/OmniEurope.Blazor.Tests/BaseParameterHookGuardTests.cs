using System.Reflection;
using System.Reflection.Emit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniComponentBase"/> refuses the additional attributes the CSP contract forbids in its
/// <c>OnParametersSet</c>: a component that overrides it without calling the base silently lets an inline
/// style or event handler through. The guard reads the compiled IL of every override in the package.
/// </summary>
public sealed class BaseParameterHookGuardTests
{
    private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;

    [Fact]
    public void EveryOnParametersSetOverride_UnderOmniComponentBase_CallsTheBase()
    {
        var overrides = typeof(OmniComponentBase).Assembly.GetTypes()
            .Where(type => type != typeof(OmniComponentBase) && typeof(OmniComponentBase).IsAssignableFrom(type))
            .Select(type => type.GetMethod("OnParametersSet", Declared, Type.EmptyTypes))
            .OfType<MethodInfo>()
            .ToArray();

        var missing = overrides.Where(method => !CallsBase(method)).Select(method => method.DeclaringType!.FullName).Order(StringComparer.Ordinal).ToArray();

        Assert.NotEmpty(overrides);
        Assert.True(missing.Length == 0, $"OnParametersSet sans appel de la base : {string.Join(", ", missing)}");
    }

    [Fact]
    public void TheGuard_FindsAnOverrideThatForgetsTheBase()
    {
        Assert.False(CallsBase(typeof(ForgetsTheBase).GetMethod("OnParametersSet", Declared, Type.EmptyTypes)!));
        Assert.True(CallsBase(typeof(CallsTheBase).GetMethod("OnParametersSet", Declared, Type.EmptyTypes)!));
    }

    // A call or callvirt whose token resolves to an OnParametersSet declared by a base type.
    private static bool CallsBase(MethodInfo method)
    {
        var il = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        for (var index = 0; index + 4 < il.Length; index++)
        {
            if (il[index] != OpCodes.Call.Value && il[index] != OpCodes.Callvirt.Value)
            {
                continue;
            }

            MethodBase? target;
            try
            {
                target = method.Module.ResolveMethod(BitConverter.ToInt32(il, index + 1), method.DeclaringType!.GetGenericArguments(), null);
            }
            catch (ArgumentException)
            {
                continue;
            }

            if (target is { Name: "OnParametersSet" } && target.DeclaringType != method.DeclaringType
                && target.DeclaringType!.IsAssignableFrom(method.DeclaringType!.BaseType))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class ForgetsTheBase : OmniComponentBase
    {
        public int Seen { get; private set; }

        protected override void OnParametersSet() => Seen++;
    }

    private sealed class CallsTheBase : OmniComponentBase
    {
        public int Seen { get; private set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            Seen++;
        }
    }
}
