using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class ValidatorRulesTests : OmniBunitContext
{
    private static string[] MessagesFor(IRenderedComponent<ValidatorRulesTestHost> host, string field)
    {
        host.InvokeAsync(() => host.Instance.EditContext.Validate()).GetAwaiter().GetResult();
        return host.Instance.EditContext.GetValidationMessages(new FieldIdentifier(host.Instance.Model, field)).ToArray();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a@b.fr")]
    [InlineData("  jean.dupont@exemple.fr  ")]
    [InlineData("jean.dupont+agenda@cabinet-durand.co.uk")]
    [InlineData("élodie.müller@exemple.fr")]
    [InlineData("o'brien@exemple.ie")]
    [InlineData("a@xn--exemple-9ua.fr")]
    public void Email_AcceptsAnAddressOrNothing(string value)
    {
        var host = Render<ValidatorRulesTestHost>();
        host.Instance.Model.Mail = value;

        Assert.Empty(MessagesFor(host, "Mail"));
    }

    [Theory]
    [InlineData("jean")]
    [InlineData("jean@")]
    [InlineData("@exemple.fr")]
    [InlineData("jean@exemple")]
    [InlineData("jean@@exemple.fr")]
    [InlineData("jean@dupont@exemple.fr")]
    [InlineData("jean dupont@exemple.fr")]
    [InlineData(".jean@exemple.fr")]
    [InlineData("jean.@exemple.fr")]
    [InlineData("jean..dupont@exemple.fr")]
    [InlineData("jean@-exemple.fr")]
    [InlineData("jean@exemple-.fr")]
    [InlineData("jean@exemple..fr")]
    [InlineData("jean@exemple.fr.")]
    [InlineData("jean@192.168.0.1")]
    [InlineData("Jean <jean@exemple.fr>")]
    [InlineData("\"jean\"@exemple.fr")]
    public void Email_RefusesWhatIsNotAnAddress(string value)
    {
        var host = Render<ValidatorRulesTestHost>();
        host.Instance.Model.Mail = value;

        Assert.Equal(["Ce champ ne contient pas une adresse e-mail valide."], MessagesFor(host, "Mail"));
    }

    [Fact]
    public void Email_RefusesAnOverlongLocalPart_AndTakesTheHostMessage()
    {
        var host = Render<ValidatorRulesTestHost>(parameters => parameters.Add(component => component.MailMessage, "Adresse du client invalide."));
        host.Instance.Model.Mail = new string('a', 65) + "@exemple.fr";

        Assert.Equal(["Adresse du client invalide."], MessagesFor(host, "Mail"));
    }

    [Fact]
    public void Email_ShowsItsMessageInALiveRegionWhenTheFieldChanges()
    {
        var host = Render<ValidatorRulesTestHost>();
        host.Instance.Model.Mail = "jean@";

        host.InvokeAsync(() => host.Instance.EditContext.NotifyFieldChanged(new FieldIdentifier(host.Instance.Model, "Mail")));

        host.WaitForAssertion(() => Assert.Contains(
            host.FindAll(".omni-validation-message"),
            message => message.TextContent == "Ce champ ne contient pas une adresse e-mail valide." && message.GetAttribute("aria-live") == "polite"));
    }

    [Theory]
    [InlineData(null, 4, null)]
    [InlineData(null, 4, "")]
    [InlineData(null, 4, "abcd")]
    [InlineData(2, 4, "ab")]
    [InlineData(2, null, "abcdefgh")]
    [InlineData(3, 3, "abc")]
    public void Length_AcceptsATextWithinItsBoundsOrNothing(int? min, int? max, string? value)
    {
        var host = Render<ValidatorRulesTestHost>(parameters => parameters
            .Add(component => component.Min, min)
            .Add(component => component.Max, max));
        host.Instance.Model.Code = value;

        Assert.Empty(MessagesFor(host, "Code"));
    }

    [Theory]
    [InlineData(null, 4, "abcde", "Ce champ doit contenir au plus 4 caractères.")]
    [InlineData(3, null, "ab", "Ce champ doit contenir au moins 3 caractères.")]
    [InlineData(1, null, " ", null)]
    [InlineData(2, 10, "a", "Ce champ doit contenir entre 2 et 10 caractères.")]
    [InlineData(4, 4, "abc", "Ce champ doit contenir exactement 4 caractères.")]
    [InlineData(null, 1, "ab", "Ce champ doit contenir au plus 1 caractère.")]
    public void Length_WritesItsBoundsInTheMessage(int? min, int? max, string value, string? expected)
    {
        var host = Render<ValidatorRulesTestHost>(parameters => parameters
            .Add(component => component.Min, min)
            .Add(component => component.Max, max));
        host.Instance.Model.Code = value;

        // A space counts: one space meets a minimum of one.
        Assert.Equal(expected is null ? Array.Empty<string>() : new[] { expected }, MessagesFor(host, "Code"));
    }

    [Fact]
    public void Length_TakesTheHostMessage()
    {
        var host = Render<ValidatorRulesTestHost>(parameters => parameters.Add(component => component.CodeMessage, "Code trop long."));
        host.Instance.Model.Code = "abcdef";

        Assert.Equal(["Code trop long."], MessagesFor(host, "Code"));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(-1, null)]
    [InlineData(null, -2)]
    [InlineData(5, 4)]
    public void Length_RefusesBoundsThatCannotBeMet(int? min, int? max)
    {
        Assert.Throws<ArgumentException>(() => Render<ValidatorRulesTestHost>(parameters => parameters
            .Add(component => component.Min, min)
            .Add(component => component.Max, max)));
    }

    [Theory]
    [InlineData("en-US", "This field does not contain a valid email address.", "This field must be between 2 and 4 characters long.")]
    [InlineData("de-DE", "Dieses Feld enthält keine gültige E-Mail-Adresse.", "Dieses Feld muss zwischen 2 und 4 Zeichen lang sein.")]
    [InlineData("hr-HR", "Ovo polje ne sadrži valjanu adresu e-pošte.", "Ovo polje mora sadržavati od 2 do 4 znaka.")]
    public void DefaultMessages_FollowTheUiCulture(string culture, string mail, string code)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var host = Render<ValidatorRulesTestHost>(parameters => parameters.Add(component => component.Min, 2));
            host.Instance.Model.Mail = "jean";
            host.Instance.Model.Code = "a";

            Assert.Equal([mail], MessagesFor(host, "Mail"));
            Assert.Equal([code], MessagesFor(host, "Code"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
