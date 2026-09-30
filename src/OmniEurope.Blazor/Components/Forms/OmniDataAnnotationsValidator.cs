using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Localization;
using OmniEurope.Blazor.Resources;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// Validates the model of the enclosing form with its DataAnnotations, like Blazor's
/// <see cref="DataAnnotationsValidator"/>, but writes the messages of the standard attributes in the
/// library's cultures instead of the framework's English. A model shared with an API keeps its
/// attributes untouched: the message is chosen from the attribute type, never parsed from text.
/// </summary>
/// <remarks>
/// An attribute with its own <c>ErrorMessage</c> keeps it, looked up first in <see cref="Localizer"/>
/// when one is given (so a bare resource key is translated). An attribute with an
/// <c>ErrorMessageResourceType</c> is already localized by DataAnnotations and passes through.
/// Field names come from <see cref="DisplayAttribute"/> or <see cref="DisplayNameAttribute"/>, then the
/// property name, each looked up in <see cref="Localizer"/>. A <see cref="ValidationAttribute"/> on
/// the model type itself (a <see cref="CustomValidationAttribute"/> comparing two dates, say) and the
/// <see cref="IValidatableObject"/> results are added on the members they name, or on the model when
/// they name none, with their own message.
/// </remarks>
public sealed class OmniDataAnnotationsValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore? _messages;
    private EditContext? _subscribed;

    [Inject]
    private IStringLocalizer<AppStrings> Strings { get; set; } = default!;

    [CascadingParameter]
    private EditContext? CurrentEditContext { get; set; }

    /// <summary>Host resources used for field names and for custom messages written as resource keys.</summary>
    [Parameter]
    public IStringLocalizer? Localizer { get; set; }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> outside an <see cref="EditForm"/>; otherwise follows
    /// the form's edit context, validating the whole model on submit and each field as it changes.
    /// </summary>
    protected override void OnParametersSet()
    {
        if (CurrentEditContext is null)
        {
            throw new InvalidOperationException($"{nameof(OmniDataAnnotationsValidator)} must be placed inside an EditForm.");
        }

        if (ReferenceEquals(_subscribed, CurrentEditContext))
        {
            return;
        }

        Unsubscribe();
        _subscribed = CurrentEditContext;
        _messages = new ValidationMessageStore(CurrentEditContext);
        CurrentEditContext.OnValidationRequested += OnValidationRequested;
        CurrentEditContext.OnFieldChanged += OnFieldChanged;
    }

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        if (_subscribed is null || _messages is null)
        {
            return;
        }

        var model = _subscribed.Model;
        _messages.Clear();
        foreach (var property in ValidatedProperties(model.GetType()))
        {
            AddPropertyErrors(model, property);
        }

        AddModelErrors(model);
        if (model is IValidatableObject validatable)
        {
            foreach (var result in validatable.Validate(new ValidationContext(model)))
            {
                AddResult(model, result, result.ErrorMessage ?? string.Empty);
            }
        }

        _subscribed.NotifyValidationStateChanged();
    }

    private void OnFieldChanged(object? sender, FieldChangedEventArgs args)
    {
        if (_subscribed is null || _messages is null)
        {
            return;
        }

        var field = args.FieldIdentifier;
        var property = field.Model.GetType().GetProperty(field.FieldName, BindingFlags.Public | BindingFlags.Instance);
        _messages.Clear(field);
        if (property is not null)
        {
            AddPropertyErrors(field.Model, property);
        }

        _subscribed.NotifyValidationStateChanged();
    }

    private void AddPropertyErrors(object model, PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes<ValidationAttribute>(inherit: true).ToArray();
        if (attributes.Length == 0 || _messages is null)
        {
            return;
        }

        var value = property.GetValue(model);
        var displayName = FieldName(property);
        var context = new ValidationContext(model) { MemberName = property.Name, DisplayName = displayName };
        var field = new FieldIdentifier(model, property.Name);

        // Same order as System.ComponentModel.DataAnnotations.Validator: a failed Required is the only
        // message of its field, the other rules are not evaluated on a missing value.
        var required = attributes.OfType<RequiredAttribute>().FirstOrDefault();
        if (required is not null)
        {
            var requiredResult = required.GetValidationResult(value, context);
            if (requiredResult is not null && requiredResult != ValidationResult.Success)
            {
                _messages.Add(field, Message(required, requiredResult, displayName, model));
                return;
            }
        }

        foreach (var attribute in attributes.Where(attribute => attribute is not RequiredAttribute))
        {
            var result = attribute.GetValidationResult(value, context);
            if (result is null || result == ValidationResult.Success)
            {
                continue;
            }

            _messages.Add(field, Message(attribute, result, displayName, model));
        }
    }

    /// <summary>The rules carried by the model type rather than by one of its properties.</summary>
    private void AddModelErrors(object model)
    {
        var context = new ValidationContext(model);
        foreach (var attribute in model.GetType().GetCustomAttributes<ValidationAttribute>(inherit: true))
        {
            var result = attribute.GetValidationResult(model, context);
            if (result is null || result == ValidationResult.Success)
            {
                continue;
            }

            // A custom message may be a resource key of the host; otherwise the rule's own text.
            var message = attribute.ErrorMessageResourceType is null && HasCustomMessage(attribute)
                ? Lookup(attribute.ErrorMessage!) ?? result.ErrorMessage
                : result.ErrorMessage;
            AddResult(model, result, message ?? string.Empty);
        }
    }

    private void AddResult(object model, ValidationResult result, string message)
    {
        var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
        foreach (var member in members)
        {
            _messages!.Add(new FieldIdentifier(model, member), message);
        }
    }

    private string Message(ValidationAttribute attribute, ValidationResult result, string displayName, object model)
    {
        if (attribute.ErrorMessageResourceType is not null)
        {
            return result.ErrorMessage ?? string.Empty;
        }

        if (HasCustomMessage(attribute))
        {
            return Lookup(attribute.ErrorMessage!) ?? result.ErrorMessage ?? attribute.ErrorMessage!;
        }

        return attribute switch
        {
            RequiredAttribute => Strings["DataAnnotationsRequired", displayName],
            StringLengthAttribute length when length.MinimumLength > 0 =>
                Counted("DataAnnotationsStringLength", displayName, length.MinimumLength, length.MaximumLength),
            StringLengthAttribute length => Counted("DataAnnotationsMaxLength", displayName, length.MaximumLength),
            MaxLengthAttribute length => Counted("DataAnnotationsMaxLength", displayName, length.Length),
            MinLengthAttribute length => Counted("DataAnnotationsMinLength", displayName, length.Length),
            RangeAttribute range => Strings["DataAnnotationsRange", displayName, Format(range.Minimum), Format(range.Maximum)],
            EmailAddressAttribute => Strings["DataAnnotationsEmail", displayName],
            UrlAttribute => Strings["DataAnnotationsUrl", displayName],
            CompareAttribute compare => Strings["DataAnnotationsCompare", displayName, OtherFieldName(model, compare)],
            RegularExpressionAttribute => Strings["DataAnnotationsPattern", displayName],
            _ => Strings["DataAnnotationsInvalid", displayName],
        };
    }

    // The DataType-based attributes (e-mail, URL, phone, credit card) fill ErrorMessage with their own
    // English default in their constructor, so a non-empty ErrorMessage alone does not mean the model
    // chose a message: it is custom only when it differs from that default.
    private static readonly Dictionary<Type, string?> DefaultMessages = new()
    {
        [typeof(EmailAddressAttribute)] = new EmailAddressAttribute().ErrorMessage,
        [typeof(UrlAttribute)] = new UrlAttribute().ErrorMessage,
        [typeof(PhoneAttribute)] = new PhoneAttribute().ErrorMessage,
        [typeof(CreditCardAttribute)] = new CreditCardAttribute().ErrorMessage,
    };

    private static bool HasCustomMessage(ValidationAttribute attribute) =>
        !string.IsNullOrEmpty(attribute.ErrorMessage)
        && !(DefaultMessages.TryGetValue(attribute.GetType(), out var defaultMessage)
             && string.Equals(defaultMessage, attribute.ErrorMessage, StringComparison.Ordinal));

    private string FieldName(PropertyInfo property)
    {
        var name = property.GetCustomAttribute<DisplayAttribute>()?.GetName()
                   ?? property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                   ?? property.Name;
        return Lookup(name) ?? name;
    }

    private string OtherFieldName(object model, CompareAttribute compare)
    {
        var other = model.GetType().GetProperty(compare.OtherProperty, BindingFlags.Public | BindingFlags.Instance);
        return other is null ? compare.OtherProperty : FieldName(other);
    }

    private string? Lookup(string key)
    {
        if (Localizer is null)
        {
            return null;
        }

        var localized = Localizer[key];
        return localized.ResourceNotFound ? null : localized.Value;
    }

    private static string Format(object? value) =>
        Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;

    /// <summary>A package message whose wording follows a length: "1 character", "2 characters".</summary>
    private string Counted(string name, params object[] arguments) => PluralMessage.Localize(Strings, name, arguments);

    private static IEnumerable<PropertyInfo> ValidatedProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0 && property.GetMethod is not null);

    private void Unsubscribe()
    {
        if (_subscribed is null)
        {
            return;
        }

        _subscribed.OnValidationRequested -= OnValidationRequested;
        _subscribed.OnFieldChanged -= OnFieldChanged;
        _messages?.Clear();
        _subscribed = null;
    }

    /// <summary>Stops following the edit context and clears the messages this validator added to it.</summary>
    public void Dispose() => Unsubscribe();
}
