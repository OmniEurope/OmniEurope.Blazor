using Microsoft.AspNetCore.Components.RenderTree;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// Whether a content fragment is one <see cref="Components.OmniIcon"/> and nothing else: the stylesheet
/// cannot tell, since <c>:only-child</c> ignores the text beside an icon. The fragment is written into a
/// scratch builder and its top-level frames read: whitespace is skipped, a region (a nested fragment) is
/// read through, any other text, markup, element or component means the content is more than an icon.
/// </summary>
internal static class LoneIconContent
{
    internal static bool Is(RenderFragment? content)
    {
        if (content is null)
        {
            return false;
        }

        using var builder = new RenderTreeBuilder();
        content(builder);
        // The frames are the framework's own render tree (BL0006: its types may change between releases),
        // read here and never kept or changed; the button tests pin what is read.
#pragma warning disable BL0006
        var frames = builder.GetFrames();
        var icons = 0;
        var index = 0;
        while (index < frames.Count)
        {
            var frame = frames.Array[index];
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Region:
                    index++;
                    break;
                case RenderTreeFrameType.Text or RenderTreeFrameType.Markup:
                    if (!string.IsNullOrWhiteSpace(frame.FrameType == RenderTreeFrameType.Text ? frame.TextContent : frame.MarkupContent))
                    {
                        return false;
                    }

                    index++;
                    break;
                case RenderTreeFrameType.Component when frame.ComponentType == typeof(Components.OmniIcon):
                    icons++;
                    index += frame.ComponentSubtreeLength;
                    break;
                default:
                    return false;
            }
        }
#pragma warning restore BL0006

        return icons == 1;
    }
}
