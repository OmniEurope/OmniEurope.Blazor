using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// What an <see cref="Components.OmniFormField"/> cascades so that its control tells it whether the
/// property it is bound to carries <see cref="RequiredAttribute"/>: the field then shows the required
/// mark without <c>Required</c> written by hand. One instance per field, fixed for its lifetime.
/// </summary>
/// <param name="controlId">The id the field's label names (its <c>For</c>), or empty for any control.</param>
/// <param name="changed">Asks the field to render again when the answer changes.</param>
internal sealed class FormFieldRequirement(Func<string> controlId, Action changed)
{
    private static readonly ConcurrentDictionary<(Type Model, string Field), bool> Annotated = new();

    private object? _reporter;

    /// <summary>Whether the control reported a bound property marked <see cref="RequiredAttribute"/>.</summary>
    internal bool IsRequired { get; private set; }

    /// <summary>
    /// Records what a control bound to <paramref name="field"/> found. A control whose id is not the
    /// field's <c>For</c> is ignored, so that a second control of the field (a unit, a button) has no say.
    /// </summary>
    /// <param name="reporter">The control, so that its removal clears only its own answer.</param>
    /// <param name="id">The control's id.</param>
    /// <param name="field">The bound field.</param>
    internal void Report(object reporter, string? id, FieldIdentifier field)
    {
        var expected = controlId();
        if (!string.IsNullOrWhiteSpace(expected) && !string.Equals(expected, id, StringComparison.Ordinal))
        {
            return;
        }

        _reporter = reporter;
        Set(IsAnnotated(field));
    }

    /// <summary>Forgets the answer of a control leaving the field.</summary>
    /// <param name="reporter">The control removed.</param>
    internal void Withdraw(object reporter)
    {
        if (ReferenceEquals(_reporter, reporter))
        {
            _reporter = null;
            Set(false);
        }
    }

    private void Set(bool required)
    {
        if (IsRequired != required)
        {
            IsRequired = required;
            changed();
        }
    }

    private static bool IsAnnotated(FieldIdentifier field) =>
        Annotated.GetOrAdd((field.Model.GetType(), field.FieldName), static key =>
            key.Model.GetProperty(key.Field)?.IsDefined(typeof(RequiredAttribute), inherit: true) == true);
}
