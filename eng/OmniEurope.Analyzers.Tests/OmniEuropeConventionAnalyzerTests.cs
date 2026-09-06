namespace OmniEurope.Analyzers.Tests;

public sealed class OmniEuropeConventionAnalyzerTests
{
    [Fact]
    public async Task Gen001_DetectsDirectContextButAllowsUnitOfWork()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            class AppDbContext { }
            class Service(AppDbContext context) { }
            class UnitOfWork(AppDbContext context) { }
            """);
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "GEN001");
    }

    [Fact]
    public async Task Gen002_DetectsServiceContextButAllowsCanonicalDataTypes()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            interface IUnitOfWork { object Context { get; } }
            class Service { void Run(IUnitOfWork unit) { _ = unit.Context; } }
            class CleanupJob { void Run(IUnitOfWork unit) { _ = unit.Context; } }
            class BlobStorage { void Run(IUnitOfWork unit) { _ = unit.Context; } }
            class AuditLogger { void Run(IUnitOfWork unit) { _ = unit.Context; } }
            """);
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "GEN002");
    }

    [Fact]
    public async Task Gen003_UsesTheBclSymbolAndCoversQualifiedAndAliasedClocks()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            using Clock = System.DateTime;
            class DateTime { public static int UtcNow => 0; }
            class Service
            {
                object A() => System.DateTime.UtcNow;
                object B() => Clock.Now;
                object C() => DateTime.UtcNow;
            }
            """);
        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "GEN003"));
    }

    [Fact]
    public async Task Gen004_RecognizesOnlyRealRazorCodeBlocksOutsideComments()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync(
            "class Fixture { }",
            ("Unsafe.razor", "<p>Text</p>\n@code { private int value; }"),
            ("Safe.razor", "@*\n@code { ignored }\n*@\n<p>@codependent</p>"));
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "GEN004");
        Assert.EndsWith("Unsafe.razor", diagnostics.Single(diagnostic => diagnostic.Id == "GEN004").Location.SourceTree?.FilePath ?? diagnostics.Single(diagnostic => diagnostic.Id == "GEN004").Location.GetLineSpan().Path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Gen005_UsesEfAndLinqSymbolsInsteadOfHomonymousMethods()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            using System;
            using System.Linq;
            using Microsoft.EntityFrameworkCore;
            namespace Microsoft.EntityFrameworkCore
            {
                static class EfExtensions { public static IQueryable<T> Include<T>(this IQueryable<T> source, Func<T, object> path) => source; }
            }
            namespace Other
            {
                static class OtherExtensions { public static IQueryable<T> Include<T>(this IQueryable<T> source, Func<T, object> path) => source; }
            }
            class Item { public int Id { get; set; } }
            class Service
            {
                void Bad(IQueryable<Item> query) { _ = query.OrderBy(item => item.Id).Include(item => item); }
                void Good(IQueryable<Item> query) { _ = Other.OtherExtensions.Include(query.OrderBy(item => item.Id), item => item); }
            }
            """);
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "GEN005");
    }

    [Fact]
    public async Task Gen006_DetectsOnlyUnguardedLinqMaterializationInRepositories()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class ItemRepository
            {
                object Bad(IEnumerable<int> values) => values.ToList();
                object Good(IEnumerable<int> values) => values.Where(value => value > 0).ToList();
            }
            class Service { object Good(IEnumerable<int> values) => values.ToList(); }
            """);
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "GEN006");
    }

    [Fact]
    public async Task Gen007_RejectsMissingAndHomonymousAuthorization()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            using System;
            using Microsoft.AspNetCore.Mvc;
            class AuthorizeAttribute : Attribute { }
            [ApiController] class MissingController : ControllerBase { }
            [Authorize, ApiController] class FakeController : ControllerBase { }
            """);
        Assert.Equal(2, diagnostics.Count(diagnostic => diagnostic.Id == "GEN007"));
    }

    [Fact]
    public async Task Gen007_AcceptsFrameworkAuthorizeAndAllowAnonymous()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            using Microsoft.AspNetCore.Authorization;
            using Microsoft.AspNetCore.Mvc;
            [Authorize, ApiController] class SecuredController : ControllerBase { }
            [AllowAnonymous, ApiController] class PublicController : ControllerBase { }
            """);
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "GEN007");
    }

    [Fact]
    public async Task Gen008_RejectsUserPartialMethodsAndAllowsKnownGenerators()
    {
        var diagnostics = await AnalyzerTestHarness.AnalyzeAsync("""
            using System.Text.RegularExpressions;
            partial class SplitType { partial void Hook(); }
            partial class RegexType
            {
                [GeneratedRegex("a+")]
                private static partial Regex Pattern();
            }
            """);
        Assert.Single(diagnostics, diagnostic => diagnostic.Id == "GEN008");
    }
}
