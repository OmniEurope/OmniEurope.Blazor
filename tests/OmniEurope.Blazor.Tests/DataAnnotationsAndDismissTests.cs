using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

public sealed class DataAnnotationsAndDismissTests : OmniBunitContext
{
    [Fact]
    public void Validator_WritesTheStandardMessagesInFrench()
    {
        var host = RenderIn("fr-FR");

        InCulture("fr-FR", () => host.Instance.EditContext.Validate());

        // Like DataAnnotations' own Validator, a failed Required is the only message of its field.
        Assert.Equal(["Le champ Nom du projet est obligatoire."], host.Instance.MessagesFor("Name"));
        Assert.Contains("Le champ Mail n'est pas une adresse e-mail valide.", host.Instance.MessagesFor("Mail"));
        Assert.Contains("Le champ Réplicas doit être compris entre 1 et 10.", host.Instance.MessagesFor("Replicas"));
        Assert.Contains("Confirmation et Mail ne correspondent pas.", host.Instance.MessagesFor("MailConfirmation"));
    }

    [Fact]
    public void Validator_WritesTheStandardMessagesInEnglish()
    {
        var host = RenderIn("en-US");

        InCulture("en-US", () => host.Instance.EditContext.Validate());

        Assert.Contains("The Nom du projet field is required.", host.Instance.MessagesFor("Name"));
        Assert.Contains("The Réplicas field must be between 1 and 10.", host.Instance.MessagesFor("Replicas"));
    }

    [Fact]
    public void Validator_ChecksOnlyTheChangedFieldAndReportsTheLengthRange()
    {
        var host = RenderIn("fr-FR");
        host.Instance.Model.Name = "a";

        InCulture("fr-FR", () => host.Instance.EditContext.NotifyFieldChanged(new FieldIdentifier(host.Instance.Model, "Name")));

        Assert.Equal(["Le champ Nom du projet doit contenir entre 2 et 40 caractères."], host.Instance.MessagesFor("Name"));
        Assert.Empty(host.Instance.MessagesFor("Replicas"));
    }

    [Fact]
    public void Validator_TranslatesACustomMessageKeyAndAFieldNameThroughTheHostLocalizer()
    {
        var host = RenderIn("fr-FR", new DictionaryLocalizer(new Dictionary<string, string>
        {
            ["SlugRequired"] = "L'identifiant court est obligatoire.",
            ["Mail"] = "Courriel",
        }));

        InCulture("fr-FR", () => host.Instance.EditContext.Validate());

        Assert.Contains("L'identifiant court est obligatoire.", host.Instance.MessagesFor("Slug"));
        Assert.Contains("Le champ Courriel n'est pas une adresse e-mail valide.", host.Instance.MessagesFor("Mail"));
    }

    [Fact]
    public void Alert_WithoutDismissible_RendersNoCloseButton()
    {
        var alert = Render<OmniAlert>(parameters => parameters
            .AddChildContent("Message")
            .Add(component => component.Title, "Titre"));

        Assert.Empty(alert.FindAll(".omni-alert__dismiss"));
        Assert.Single(alert.FindAll(".omni-alert"));
    }

    [Fact]
    public void Alert_Dismissible_ClosesAndReportsIt()
    {
        var dismissed = 0;
        var alert = Render<OmniAlert>(parameters => parameters
            .AddChildContent("Message")
            .Add(component => component.Dismissible, true)
            .Add(component => component.OnDismiss, () => dismissed++));

        var button = alert.Find("button.omni-alert__dismiss");
        Assert.False(string.IsNullOrWhiteSpace(button.GetAttribute("aria-label")));

        button.Click();

        Assert.Empty(alert.FindAll(".omni-alert"));
        Assert.Equal(1, dismissed);
    }

    private IRenderedComponent<DataAnnotationsValidatorTestHost> RenderIn(string culture, IStringLocalizer? localizer = null)
    {
        IRenderedComponent<DataAnnotationsValidatorTestHost>? host = null;
        InCulture(culture, () => host = Render<DataAnnotationsValidatorTestHost>(parameters => parameters
            .Add(component => component.Localizer, localizer)));
        return host!;
    }

    private static void InCulture(string name, Action action)
    {
        var previous = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private sealed class DictionaryLocalizer(IReadOnlyDictionary<string, string> values) : IStringLocalizer
    {
        public LocalizedString this[string name] =>
            values.TryGetValue(name, out var value)
                ? new LocalizedString(name, value)
                : new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.CurrentCulture, this[name].Value, arguments), this[name].ResourceNotFound);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
            values.Select(pair => new LocalizedString(pair.Key, pair.Value));
    }
}
