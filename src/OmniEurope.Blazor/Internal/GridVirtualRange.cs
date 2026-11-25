namespace OmniEurope.Blazor.Components;

internal readonly record struct GridVirtualRange(int StartIndex, int Count, double TopSpacer, double BottomSpacer)
{
    internal int EndIndex => StartIndex + Count;
}
