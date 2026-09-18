namespace OmniEurope.Blazor.Components;

/// <summary>The severity of an <see cref="OmniLogLine"/>, from the most verbose to the most severe.</summary>
public enum OmniLogLevel
{
    /// <summary>Step-by-step detail.</summary>
    Trace,

    /// <summary>Diagnostic detail.</summary>
    Debug,

    /// <summary>The normal course of events.</summary>
    Information,

    /// <summary>Something unexpected that did not stop the work; tinted as a warning.</summary>
    Warning,

    /// <summary>A failure; tinted as an error.</summary>
    Error,

    /// <summary>A failure that stops everything; tinted as an error.</summary>
    Critical
}
