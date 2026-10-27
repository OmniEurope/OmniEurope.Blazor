using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Back.Exceptions;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Components.Preferences;

public sealed class VisitorPreferenceValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
    private static readonly PreferenceLimitsOptions Limits = new() { MaxValueLength = 256 };

    [Fact]
    public void CreatedVisitorIds_AreValid_AndNeverRepeat()
    {
        var ids = Enumerable.Range(0, 200).Select(_ => VisitorPreferenceApi.CreateVisitorId()).ToList();

        Assert.All(ids, id => Assert.True(VisitorPreferenceApi.IsValidVisitorId(id), id));
        Assert.All(ids, id => Assert.Equal(VisitorPreferenceApi.VisitorIdLength, id.Length));
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void VisitorKey_IsTheLowerCaseSha256OfTheId()
    {
        var key = VisitorPreferenceValidator.VisitorKey(new string('A', VisitorPreferenceApi.VisitorIdLength));

        Assert.Equal(64, key.Length);
        Assert.Equal(key.ToLowerInvariant(), key);
        Assert.Equal(key, VisitorPreferenceValidator.VisitorKey(new string('A', VisitorPreferenceApi.VisitorIdLength)));
        Assert.NotEqual(key, VisitorPreferenceValidator.VisitorKey(new string('B', VisitorPreferenceApi.VisitorIdLength)));
    }

    [Theory]
    [InlineData("omni.appearance")]
    [InlineData("omni.grid.showcase-grid-advanced")]
    [InlineData("k_1")]
    public void Key_AcceptsThePackageKeys(string key) => Assert.Equal(key, VisitorPreferenceValidator.Key(key));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".hidden")]
    [InlineData("omni/../appearance")]
    [InlineData("omni appearance")]
    [InlineData("omni.é")]
    [InlineData("omni.appearance'; DROP TABLE \"Visitors\"; --")]
    public void Key_RefusesAnythingElse(string? key) => Assert.Throws<BadRequestException>(() => VisitorPreferenceValidator.Key(key));

    [Fact]
    public void Key_RefusesAKeyOverTheMaximumLength()
    {
        Assert.Equal(VisitorPreferenceApi.MaxKeyLength, VisitorPreferenceValidator.Key(new string('a', VisitorPreferenceApi.MaxKeyLength)).Length);
        Assert.Throws<BadRequestException>(() => VisitorPreferenceValidator.Key(new string('a', VisitorPreferenceApi.MaxKeyLength + 1)));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"a\":")]
    [InlineData("")]
    public void Request_RefusesAValueThatIsNotJson(string value) =>
        Assert.Throws<BadRequestException>(() => VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest(value, Now), Limits, Now));

    [Fact]
    public void Request_RefusesAValueNestedTooDeep()
    {
        var depth = VisitorPreferenceValidator.MaxJsonDepth + 1;
        var value = new string('[', depth) + new string(']', depth);

        Assert.Throws<BadRequestException>(() => VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest(value, Now), Limits, Now));
    }

    [Fact]
    public void Request_RefusesAValueOverTheMaximumLength()
    {
        var atLimit = "\"" + new string('a', Limits.MaxValueLength - 2) + "\"";

        Assert.Equal(Now, VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest(atLimit, Now), Limits, Now));
        Assert.Throws<BadRequestException>(() =>
            VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest(atLimit + " ", Now), Limits, Now));
    }

    [Fact]
    public void Request_RefusesAMissingBodyOrDate()
    {
        Assert.Throws<BadRequestException>(() => VisitorPreferenceValidator.Request(null, Limits, Now));
        Assert.Throws<BadRequestException>(() => VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest("1", default), Limits, Now));
    }

    [Fact]
    public void Request_GivesTheDateInUtc_AndMovesAFutureOneBackToNow()
    {
        var paris = new DateTimeOffset(2026, 10, 27, 13, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(TimeSpan.Zero, VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest(null, paris), Limits, Now).Offset);
        Assert.Equal(Now, VisitorPreferenceValidator.Request(new SaveVisitorPreferenceRequest(null, Now.AddDays(1)), Limits, Now));
    }
}
