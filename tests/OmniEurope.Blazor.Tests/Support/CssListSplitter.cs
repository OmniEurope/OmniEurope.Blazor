namespace OmniEurope.Blazor.Tests;

/// <summary>Splits a comma-separated CSS list (selectors, shadow or background layers) where no parenthesis is open.</summary>
internal static class CssListSplitter
{
    public static List<string> SplitTopLevel(string list)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var index = 0; index < list.Length; index++)
        {
            switch (list[index])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    parts.Add(list[start..index].Trim());
                    start = index + 1;
                    break;
            }
        }

        parts.Add(list[start..].Trim());
        return parts;
    }
}
