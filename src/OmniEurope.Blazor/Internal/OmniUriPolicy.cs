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

        if (uri.IsAbsoluteUri ? !AllowedSchemes.Contains(uri.Scheme) : ReadsAsAnotherHost(candidate))
        {
            throw uri.IsAbsoluteUri ? CreateException(parameterName) : CreateHostException(parameterName);
        }

        return candidate;
    }

    // A relative address a browser resolves against another host: "//host", "/\host", "\host" and the same
    // with a percent-encoded slash or backslash. The browser reads a backslash of a link as a slash, and a
    // leading "//" then names the authority. A single leading "/" is a path of the application, and a "//"
    // further in (a path segment, a query, a fragment) stays one.
    private static bool ReadsAsAnotherHost(string candidate)
    {
        var length = ReadSeparator(candidate, 0, out var backslash);
        return length > 0 && (backslash || ReadSeparator(candidate, length, out _) > 0);
    }

    // The length of the slash, backslash or percent-encoded one at the position, 0 when there is none.
    private static int ReadSeparator(string text, int position, out bool backslash)
    {
        backslash = false;
        if (position >= text.Length)
        {
            return 0;
        }

        var rest = text.AsSpan(position);
        if (rest[0] == '/')
        {
            return 1;
        }

        if (rest[0] == '\\')
        {
            backslash = true;
            return 1;
        }

        if (rest.StartsWith("%2f", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        backslash = rest.StartsWith("%5c", StringComparison.OrdinalIgnoreCase);
        return backslash ? 3 : 0;
    }

    private static InvalidOperationException CreateException(string parameterName) =>
        new($"'{parameterName}' uses a URI scheme that is not allowed. Use a relative URI or an http, https, mailto, or tel URI.");

    private static InvalidOperationException CreateHostException(string parameterName) =>
        new($"'{parameterName}' is a relative address that a browser reads as another host (`//host` or `/\\host`). Use a path of the application, or an absolute http or https URI.");
}
