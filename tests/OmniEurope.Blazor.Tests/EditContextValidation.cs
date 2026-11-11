#if !NET11_0_OR_GREATER
using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="EditContext"/> gained <c>ValidateAsync</c> in .NET 11, where the synchronous <c>Validate</c>
/// became obsolete (CS0618). .NET 10 only has <c>Validate</c>, the very call a .NET 10 <c>EditForm</c> makes on
/// submit, so the net10.0 run of the suite reaches the package's validators through it. Compiled for net10.0
/// only: on net11.0 the tests call the framework's own <c>ValidateAsync</c>.
/// </summary>
internal static class EditContextValidation
{
    public static Task<bool> ValidateAsync(this EditContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(context.Validate());
    }
}
#endif
