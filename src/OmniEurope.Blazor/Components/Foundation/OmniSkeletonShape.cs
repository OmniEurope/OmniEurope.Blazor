namespace OmniEurope.Blazor.Components;

/// <summary>The outline an <see cref="OmniSkeleton"/> placeholder takes.</summary>
public enum OmniSkeletonShape
{
    /// <summary>Lines of text; the only shape that honours <see cref="OmniSkeleton.LineCount"/>. The default.</summary>
    Text,

    /// <summary>A single block, about two controls tall.</summary>
    Rectangle,

    /// <summary>A single disc.</summary>
    Circle
}
