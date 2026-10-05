using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniDataAnnotationsValidator: the package message of every rule, the messages a model chooses, the
/// rules of the model itself, and the edit context it follows, changes and leaves.
/// </summary>
public sealed class DataAnnotationsValidatorTests : OmniBunitContext
{
    public sealed class Rules
    {
        [StringLength(5)]
        public string Short { get; set; } = "trop long";

        [MaxLength(3)]
        public string Capped { get; set; } = "quatre";

        [MinLength(3)]
        public string Long { get; set; } = "ab";

        [Url]
        public string Site { get; set; } = "pas une adresse";

        [RegularExpression("^a")]
        public string Pattern { get; set; } = "b";

        [Phone]
        [DisplayName("Téléphone")]
        public string Phone { get; set; } = "abc";

        [Range(1.5, 2.5)]
        public double Ratio { get; set; } = 9;

        [Compare("Missing")]
        public string Twin { get; set; } = "x";

        [Required(ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.NameRequired))]
        public string? FromResource { get; set; }

        [Required(ErrorMessage = "Clé absente")]
        public string? Custom { get; set; }

        [Required]
        public string Unvalidated => "lecture seule";

        public string this[int index] => index.ToString(System.Globalization.CultureInfo.InvariantCulture);

        public string Free { get; set; } = string.Empty;

        [StringLength(10)]
        public string Fine { get; set; } = "correct";
    }

    public static class Messages
    {
        public static string NameRequired => "Message de ressource";
    }

    [ModelRule(ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = nameof(Messages.NameRequired))]
    [ModelRule(ErrorMessage = "Règle du modèle")]
    [PassingRule]
    [NullMessageRule]
    public sealed class Whole : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        [
            new ValidationResult("Sur deux champs", [nameof(Left), nameof(Right)]),
            new ValidationResult(null)
        ];

        public string Left { get; set; } = string.Empty;

        public string Right { get; set; } = string.Empty;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    private sealed class ModelRuleAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => false;
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class PassingRuleAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value) => true;
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class NullMessageRuleAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => new(null);
    }

    private IRenderedComponent<OmniDataAnnotationsValidator> RenderValidator(EditContext context, IStringLocalizer? localizer = null) =>
        Render<OmniDataAnnotationsValidator>(parameters => parameters
            .AddCascadingValue(context)
            .Add(validator => validator.Localizer, localizer));

    private static string Single(EditContext context, object model, string field) =>
        Assert.Single(context.GetValidationMessages(new FieldIdentifier(model, field)));

    [Fact]
    public void EveryRule_HasItsPackageMessage()
    {
        var model = new Rules();
        var context = new EditContext(model);
        RenderValidator(context);

        context.Validate();

        Assert.Equal("Le champ Short doit contenir au plus 5 caractères.", Single(context, model, nameof(Rules.Short)));
        Assert.Equal("Le champ Capped doit contenir au plus 3 caractères.", Single(context, model, nameof(Rules.Capped)));
        Assert.Equal("Le champ Long doit contenir au moins 3 caractères.", Single(context, model, nameof(Rules.Long)));
        Assert.Equal("Le champ Site n'est pas une adresse web valide.", Single(context, model, nameof(Rules.Site)));
        Assert.Equal("Le champ Pattern n'a pas le format attendu.", Single(context, model, nameof(Rules.Pattern)));
        Assert.Equal("Le champ Téléphone n'est pas valide.", Single(context, model, nameof(Rules.Phone)));
        Assert.Equal("Le champ Ratio doit être compris entre 1,5 et 2,5.", Single(context, model, nameof(Rules.Ratio)));
        Assert.Empty(context.GetValidationMessages(new FieldIdentifier(model, nameof(Rules.Fine))));
        // The compared property does not exist: its name is shown as written.
        Assert.Equal("Twin et Missing ne correspondent pas.", Single(context, model, nameof(Rules.Twin)));
    }

    [Fact]
    public void ModelMessages_FromAResourceOrAMissingKey_AreTheRulesOwnText()
    {
        var model = new Rules();
        var context = new EditContext(model);
        RenderValidator(context);

        context.Validate();

        Assert.Equal("Message de ressource", Single(context, model, nameof(Rules.FromResource)));
        Assert.Equal("Clé absente", Single(context, model, nameof(Rules.Custom)));
    }

    [Fact]
    public void CustomMessageKey_UnknownToTheHostLocalizer_KeepsTheRulesText()
    {
        var model = new Rules();
        var context = new EditContext(model);
        RenderValidator(context, new Texts(new Dictionary<string, string>()));

        context.Validate();

        Assert.Equal("Clé absente", Single(context, model, nameof(Rules.Custom)));
    }

    [Fact]
    public void ModelRules_AndValidatableObject_ReportOnTheirMembers_OrOnTheModel()
    {
        var model = new Whole();
        var context = new EditContext(model);
        RenderValidator(context);

        context.Validate();

        Assert.Equal("Sur deux champs", Single(context, model, nameof(Whole.Left)));
        Assert.Equal("Sur deux champs", Single(context, model, nameof(Whole.Right)));
        // A rule that returns no message gets the framework's own; a validatable result without one stays empty.
        Assert.Equal(["Message de ressource", "Règle du modèle", "The field Whole is invalid.", string.Empty], context.GetValidationMessages(new FieldIdentifier(model, string.Empty)));
    }

    [Fact]
    public void ChangedFieldThatIsNoProperty_ClearsItsMessagesOnly()
    {
        var model = new Rules();
        var context = new EditContext(model);
        RenderValidator(context);
        context.Validate();

        context.NotifyFieldChanged(new FieldIdentifier(model, "NotAProperty"));

        Assert.NotEmpty(context.GetValidationMessages(new FieldIdentifier(model, nameof(Rules.Short))));
    }

    [Fact]
    public void OutsideAForm_TheValidatorRefusesToRender()
    {
        Assert.Throws<InvalidOperationException>(() => Render<OmniDataAnnotationsValidator>());
    }

    [Fact]
    public void NewEditContext_MovesTheValidatorOver_AndTheOldOneIsLeft()
    {
        var first = new Rules();
        var firstContext = new EditContext(first);
        var cascade = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(component => component.Value, firstContext)
            .AddChildContent<OmniDataAnnotationsValidator>());
        // CascadingValue takes its whole parameter set: the child content is passed again each time.
        cascade.Render(parameters => parameters.Add(component => component.Value, firstContext).AddChildContent<OmniDataAnnotationsValidator>());
        firstContext.Validate();
        Assert.NotEmpty(firstContext.GetValidationMessages());

        var second = new Rules();
        var secondContext = new EditContext(second);
        cascade.Render(parameters => parameters.Add(component => component.Value, secondContext).AddChildContent<OmniDataAnnotationsValidator>());

        // The old context lost its messages and its validator; the new one validates.
        Assert.Empty(firstContext.GetValidationMessages());
        firstContext.Validate();
        Assert.Empty(firstContext.GetValidationMessages());
        secondContext.Validate();
        Assert.NotEmpty(secondContext.GetValidationMessages());
    }

    [Fact]
    public void ValidatorDisposedByAnEarlierHandler_IgnoresTheEventItWasStillIn()
    {
        // The edit context invokes the handlers it held when the event started: a handler that
        // disposes the validator first does not stop the validator's own handler from being called.
        var model = new Rules();
        var context = new EditContext(model);
        OmniDataAnnotationsValidator? validator = null;
        context.OnValidationRequested += (_, _) => validator!.Dispose();
        validator = RenderValidator(context).Instance;
        Assert.True(context.Validate());

        var changed = new EditContext(new Rules());
        OmniDataAnnotationsValidator? other = null;
        changed.OnFieldChanged += (_, _) => other!.Dispose();
        other = RenderValidator(changed).Instance;
        changed.NotifyFieldChanged(new FieldIdentifier(changed.Model, nameof(Rules.Short)));

        Assert.Empty(context.GetValidationMessages());
        Assert.Empty(changed.GetValidationMessages());
        // Disposing twice, or a validator never subscribed, is harmless.
        validator.Dispose();
        new OmniDataAnnotationsValidator().Dispose();
    }

    private sealed class Texts(IReadOnlyDictionary<string, string> values) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            values.TryGetValue(name, out var value) ? new(name, value) : new(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
