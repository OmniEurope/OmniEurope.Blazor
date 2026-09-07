namespace OmniEurope.Blazor.Internal;

internal static class OmniUriPolicy
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        Uri.UriSchemeMailto,
        "tel"
    };

    internal static string? EnsureSafe(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var candidate = value.Trim();
        if (candidate.Any(char.IsControl))
        {
            throw CreateException(parameterName);
        }

        // A single RelativeOrAbsolute parse rather than two separate UriKind.Absolute /
        // UriKind.Relative attempts: under the Blazor WebAssembly runtime, Uri.TryCreate with an
        // explicit UriKind.Relative rejects some well-formed relative paths (observed with a plain
        // "/segment" path carrying a query string) that the very same call accepts on desktop .NET,
        // which made every relative Href in a WASM app throw. RelativeOrAbsolute matches how
        // Microsoft.AspNetCore.Components.NavLink itself validates hrefs and does not have that gap.
        if (!Uri.TryCreate(candidate, UriKind.RelativeOrAbsolute, out var uri))
        {
            throw CreateException(parameterName);
        }

        if (uri.IsAbsoluteUri && !AllowedSchemes.Contains(uri.Scheme))
        {
            throw CreateException(parameterName);
        }

        return candidate;
    }

    private static InvalidOperationException CreateException(string parameterName) =>
        new($"'{parameterName}' uses a URI scheme that is not allowed. Use a relative URI or an http, https, mailto, or tel URI.");
}
