using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace OmniEurope.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OmniEuropeConventionAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Gen001 = Rule("GEN001", "Do not inject AppDbContext directly", "Constructor parameter '{0}' injects AppDbContext directly; depend on IUnitOfWork instead", "Architecture", DiagnosticSeverity.Error);
    private static readonly DiagnosticDescriptor Gen002 = Rule("GEN002", "Access the database through a repository", "Type '{0}' accesses IUnitOfWork.Context directly; move the query into a repository", "Architecture", DiagnosticSeverity.Warning);
    private static readonly DiagnosticDescriptor Gen003 = Rule("GEN003", "Avoid ambient clocks", "Use injected TimeProvider instead of DateTime.{0}", "Time", DiagnosticSeverity.Warning);
    private static readonly DiagnosticDescriptor Gen004 = Rule("GEN004", "No inline @code in Razor files", "Move @code block to a code-behind .razor.cs file", "Blazor", DiagnosticSeverity.Warning);
    private static readonly DiagnosticDescriptor Gen005 = Rule("GEN005", "Include must precede ordering or pagination", "Move Include before {0} in the query chain", "Data", DiagnosticSeverity.Warning);
    private static readonly DiagnosticDescriptor Gen006 = Rule("GEN006", "Potentially unbounded materialization", "Guard {0} with Where or Take", "Performance", DiagnosticSeverity.Info);
    private static readonly DiagnosticDescriptor Gen007 = Rule("GEN007", "Controller must declare authorization", "Controller '{0}' must declare Authorize or AllowAnonymous", "Security", DiagnosticSeverity.Warning);
    private static readonly DiagnosticDescriptor Gen008 = Rule("GEN008", "Avoid partial types", "Type '{0}' is partial without an allowed generated-code reason", "Structure", DiagnosticSeverity.Warning);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Gen001, Gen002, Gen003, Gen004, Gen005, Gen006, Gen007, Gen008);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterAdditionalFileAction(AnalyzeRazor);
        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzePartialType,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration,
            SyntaxKind.InterfaceDeclaration);
    }

    private static DiagnosticDescriptor Rule(string id, string title, string message, string family, DiagnosticSeverity severity) =>
        new(id, title, message, $"OmniEurope.{family}", severity, isEnabledByDefault: true);

    private static void AnalyzeRazor(AdditionalFileAnalysisContext context)
    {
        if (!context.AdditionalFile.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)) return;
        var normalizedPath = context.AdditionalFile.Path.Replace('\\', '/');
        if (normalizedPath.IndexOf("/tests/", StringComparison.OrdinalIgnoreCase) >= 0) return;
        var text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null) return;

        var inComment = false;
        foreach (var line in text.Lines)
        {
            var value = RemoveRazorComments(line.ToString(), ref inComment).TrimStart();
            if (!value.StartsWith("@code", StringComparison.Ordinal)
                || value.Length > 5 && !char.IsWhiteSpace(value[5]) && value[5] != '{') continue;
            context.ReportDiagnostic(Diagnostic.Create(
                Gen004,
                Location.Create(
                    context.AdditionalFile.Path,
                    TextSpan.FromBounds(line.Start, line.End),
                    new LinePositionSpan(new LinePosition(line.LineNumber, 0), new LinePosition(line.LineNumber, value.Length)))));
            return;
        }
    }

    private static string RemoveRazorComments(string value, ref bool inComment)
    {
        var result = string.Empty;
        var index = 0;
        while (index < value.Length)
        {
            if (inComment)
            {
                var end = value.IndexOf("*@", index, StringComparison.Ordinal);
                if (end < 0) return result;
                inComment = false;
                index = end + 2;
                continue;
            }

            var start = value.IndexOf("@*", index, StringComparison.Ordinal);
            if (start < 0) return result + value.Substring(index);
            result += value.Substring(index, start - index);
            inComment = true;
            index = start + 2;
        }

        return result;
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor || constructor.ContainingType.Name == "UnitOfWork") return;
        foreach (var parameter in constructor.Parameters.Where(parameter => parameter.Type.Name == "AppDbContext"))
        {
            var location = parameter.Locations.FirstOrDefault();
            if (location is not null) context.ReportDiagnostic(Diagnostic.Create(Gen001, location, parameter.Name));
        }
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false } type || !IsController(type)) return;
        if (BaseTypes(type).SelectMany(current => current.GetAttributes()).Any(attribute =>
                IsFrameworkAttribute(attribute, "Microsoft.AspNetCore.Authorization.AuthorizeAttribute", "Microsoft.AspNetCore.Authorization")
                || IsFrameworkAttribute(attribute, "Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute", "Microsoft.AspNetCore.Authorization"))) return;
        var location = type.Locations.FirstOrDefault();
        if (location is not null) context.ReportDiagnostic(Diagnostic.Create(Gen007, location, type.Name));
    }

    private static bool IsController(INamedTypeSymbol type) => BaseTypes(type).Any(current =>
        current.GetAttributes().Any(attribute => IsFrameworkAttribute(attribute, "Microsoft.AspNetCore.Mvc.ApiControllerAttribute", "Microsoft.AspNetCore.Mvc.Core")) ||
        IsFrameworkType(current.BaseType, "Microsoft.AspNetCore.Mvc.ControllerBase", "Microsoft.AspNetCore.Mvc.Core") ||
        IsFrameworkType(current.BaseType, "Microsoft.AspNetCore.Mvc.Controller", "Microsoft.AspNetCore.Mvc.ViewFeatures"));

    private static bool IsFrameworkAttribute(AttributeData attribute, string metadataName, string assemblyName) =>
        IsFrameworkType(attribute.AttributeClass, metadataName, assemblyName);

    private static bool IsFrameworkType(INamedTypeSymbol? type, string metadataName, string assemblyName) =>
        type?.ToDisplayString() == metadataName && type.ContainingAssembly?.Name == assemblyName;

    private static ImmutableArray<INamedTypeSymbol> BaseTypes(INamedTypeSymbol type)
    {
        var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType) builder.Add(current);
        return builder.ToImmutable();
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var access = (MemberAccessExpressionSyntax)context.Node;
        if (access.Name.Identifier.Text is "Now" or "UtcNow"
            && context.SemanticModel.GetSymbolInfo(access, context.CancellationToken).Symbol is IPropertySymbol
            {
                IsStatic: true,
                ContainingType.SpecialType: SpecialType.System_DateTime
            })
        {
            if (access.SyntaxTree.FilePath.IndexOf("Migrations", StringComparison.OrdinalIgnoreCase) < 0)
                context.ReportDiagnostic(Diagnostic.Create(Gen003, access.GetLocation(), access.Name.Identifier.Text));
            return;
        }

        if (access.Name.Identifier.Text != "Context" || context.SemanticModel.GetSymbolInfo(access, context.CancellationToken).Symbol is not IPropertySymbol { ContainingType.Name: "IUnitOfWork" }) return;
        var typeName = access.FirstAncestorOrSelf<TypeDeclarationSyntax>()?.Identifier.Text;
        if (typeName is null || IsDataAccessExemption(typeName)) return;
        context.ReportDiagnostic(Diagnostic.Create(Gen002, access.Name.GetLocation(), typeName));
    }

    private static bool IsDataAccessExemption(string typeName) =>
        typeName.EndsWith("Repository", StringComparison.Ordinal)
        || typeName.EndsWith("Job", StringComparison.Ordinal)
        || typeName.EndsWith("Store", StringComparison.Ordinal)
        || typeName.EndsWith("Storage", StringComparison.Ordinal)
        || typeName is "UnitOfWork" or "AuditLogger";

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax access) return;
        var method = access.Name.Identifier.Text;
        var invokedMethod = GetInvokedMethod(context, invocation);
        if (method is "Include" or "ThenInclude" && IsEfMethod(invokedMethod))
        {
            for (var current = access.Expression; current is InvocationExpressionSyntax parent && parent.Expression is MemberAccessExpressionSyntax member; current = member.Expression)
            {
                var parentMethod = GetInvokedMethod(context, parent);
                if (member.Name.Identifier.Text is "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" or "Skip" or "Take"
                    && IsLinqMethod(parentMethod))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Gen005, access.Name.GetLocation(), member.Name.Identifier.Text));
                    return;
                }
            }
        }

        var containingType = invocation.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        var containingSymbol = containingType is null ? null : context.SemanticModel.GetDeclaredSymbol(containingType, context.CancellationToken);
        if (method is not ("ToList" or "ToListAsync" or "ToArray" or "ToArrayAsync")
            || containingSymbol?.Name.EndsWith("Repository", StringComparison.Ordinal) != true
            || !IsMaterializationMethod(invokedMethod)) return;
        for (var current = access.Expression; current is InvocationExpressionSyntax parent && parent.Expression is MemberAccessExpressionSyntax member; current = member.Expression)
        {
            var parentMethod = GetInvokedMethod(context, parent);
            if (member.Name.Identifier.Text is "Where" or "Take" && IsLinqMethod(parentMethod)
                || member.Name.Identifier.Text is "FirstOrDefaultAsync" or "SingleOrDefaultAsync" && IsEfMethod(parentMethod)) return;
        }
        context.ReportDiagnostic(Diagnostic.Create(Gen006, access.Name.GetLocation(), method));
    }

    private static IMethodSymbol? GetInvokedMethod(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation) =>
        context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;

    private static IMethodSymbol? OriginalMethod(IMethodSymbol? method) => method?.ReducedFrom ?? method;

    private static bool IsEfMethod(IMethodSymbol? method) =>
        OriginalMethod(method)?.ContainingNamespace.ToDisplayString() == "Microsoft.EntityFrameworkCore";

    private static bool IsLinqMethod(IMethodSymbol? method)
    {
        var original = OriginalMethod(method);
        return original?.ContainingNamespace.ToDisplayString() == "System.Linq"
            && original.ContainingType.Name is "Enumerable" or "Queryable";
    }

    private static bool IsMaterializationMethod(IMethodSymbol? method)
    {
        var original = OriginalMethod(method);
        return IsEfMethod(original) || IsLinqMethod(original);
    }

    private static void AnalyzePartialType(SyntaxNodeAnalysisContext context)
    {
        var type = (TypeDeclarationSyntax)context.Node;
        var partial = type.Modifiers.FirstOrDefault(modifier => modifier.IsKind(SyntaxKind.PartialKeyword));
        if (partial == default) return;
        var file = type.SyntaxTree.FilePath;
        if (file.EndsWith(".razor.cs", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase) || file.IndexOf("Migrations", StringComparison.OrdinalIgnoreCase) >= 0) return;
        if (context.SemanticModel.GetDeclaredSymbol(type, context.CancellationToken) is INamedTypeSymbol symbol && HasSupportedGeneratorPairing(symbol)) return;
        context.ReportDiagnostic(Diagnostic.Create(Gen008, partial.GetLocation(), type.Identifier.Text));
    }

    private static bool HasSupportedGeneratorPairing(INamedTypeSymbol type)
    {
        var attributes = type.GetAttributes().Concat(type.GetMembers().SelectMany(member => member.GetAttributes()));
        return attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() is
            "Microsoft.Extensions.Logging.LoggerMessageAttribute"
            or "System.Text.RegularExpressions.GeneratedRegexAttribute"
            or "System.Text.Json.Serialization.JsonSerializableAttribute"
            or "System.Runtime.InteropServices.Marshalling.GeneratedComInterfaceAttribute");
    }
}
