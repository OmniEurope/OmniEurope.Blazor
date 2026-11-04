namespace OmniEurope.Blazor.Components;

/// <summary>What an <see cref="OmniDocumentField"/> holds, which decides the control that edits it. Every value travels as a string.</summary>
public enum OmniDocumentFieldKind
{
    /// <summary>Free text, edited in a text box.</summary>
    Text,

    /// <summary>A date written <c>yyyy-MM-dd</c> (ISO 8601), edited in a date picker.</summary>
    Date,

    /// <summary>One of the <see cref="OmniDocumentField.Items"/>, chosen in a list; with <see cref="OmniDocumentField.AllowFreeText"/> any other text too.</summary>
    List,

    /// <summary>A number written in the invariant culture (<c>1234.5</c>), edited in a number field.</summary>
    Number
}
