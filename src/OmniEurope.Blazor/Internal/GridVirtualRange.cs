namespace OmniEurope.Blazor.Internal;

internal readonly record struct GridVirtualRange(int StartIndex, int Count, double TopSpacer, double BottomSpacer)
{
    internal int EndIndex => StartIndex + Count;
}
