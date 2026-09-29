using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace OmniEurope.Blazor.Analyzers;

/// <summary>
/// OE0001: an OmniEurope.Blazor component given an attribute it cannot take. Three cases: a PascalCase
/// attribute it has no parameter for (the components capture unmatched attributes, so a removed or
/// misspelled parameter would compile and silently become an HTML attribute); any attribute it has no
/// parameter for, even lowercase, on a component that captures none (it would only fail at render);
/// and a lowercase <c>class</c> or <c>id</c>, which must be written <c>Class</c> or <c>Id</c>.
/// </summary>
/// <remarks>
/// The rule reads the code the Razor source generator emits for a component tag: a block that opens
/// with <c>OpenComponent&lt;T&gt;</c> and closes with <c>CloseComponent</c>. A parameter the generator
/// recognised is written <c>AddComponentParameter(seq, nameof(T.Name), value)</c>; an attribute it did
/// not recognise keeps its name as a string literal, <c>AddComponentParameter(seq, "Name", value)</c>
/// (render fragments use <c>AddAttribute</c> with a literal of a real parameter). A literal whose name
/// starts with an uppercase ASCII letter, on a component of the OmniEurope.Blazor assembly that has no
/// public <c>[Parameter]</c> property of that name (case-insensitive, as Blazor matches), is reported;
/// on a component with no <c>CaptureUnmatchedValues</c> parameter, any such literal is. Otherwise
/// lowercase names are HTML attributes and <c>@attributes</c> splats are <c>AddMultipleAttributes</c>:
/// neither is reported. The generator leaves that literal in a <c>#line hidden</c> region, so the
/// location is recovered in the <c>.razor</c> file itself.
/// Blazor matches parameter names case-insensitively, so the generator binds a lowercase <c>class</c>
/// or <c>id</c> to the <c>Class</c> or <c>Id</c> parameter and writes <c>nameof(T.Class)</c>, mapped by
/// a <c>#line</c> directive to the attribute as written: the rule reads that span of the <c>.razor</c>
/// file and reports it when it is lowercase. Without the <c>.razor</c> file (the Razor SDK passes it as
/// an additional file) that case cannot be seen; a literal <c>"class"</c> or <c>"id"</c> is reported
/// in any code.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnknownComponentParameterAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "OE0001";

    private const string LibraryAssemblyName = "OmniEurope.Blazor";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Unknown OmniEurope.Blazor component parameter",
        "{0} has no parameter '{1}'",
        "OmniEurope.Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The component captures unmatched attributes, so a parameter it does not have would become an HTML attribute. Remove the attribute or use the parameter that replaced it.");

    private static readonly DiagnosticDescriptor LowercaseRule = new(
        DiagnosticId,
        "Lowercase class or id on an OmniEurope.Blazor component",
        "{0} takes '{2}', not '{1}': write {2}=\"...\"",
        "OmniEurope.Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Class and Id are parameters the component places itself (Class on its outermost element, Id on its focusable control); the runtime guard refuses a lowercase class or id.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, LowercaseRule);

    public override void Initialize(AnalysisContext context)
    {
        // Razor components only exist as generator output: the rule is useless without generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var builder = start.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder");
            var parameter = start.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ParameterAttribute");
            if (builder is null || parameter is null) return;

            var razorFiles = start.Options.AdditionalFiles
                .Where(file => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
                .ToImmutableArray();
            var parameters = new ConcurrentDictionary<INamedTypeSymbol, ComponentShape>(SymbolEqualityComparer.Default);
            start.RegisterSemanticModelAction(model => new TreeAnalysis(model, builder, parameter, razorFiles, parameters).Run());
        });
    }

    private sealed class TreeAnalysis(
        SemanticModelAnalysisContext context,
        INamedTypeSymbol builder,
        INamedTypeSymbol parameterAttribute,
        ImmutableArray<AdditionalText> razorFiles,
        ConcurrentDictionary<INamedTypeSymbol, ComponentShape> parameterCache)
    {
        private readonly SemanticModel _model = context.SemanticModel;
        private readonly CancellationToken _cancellation = context.CancellationToken;

        internal void Run()
        {
            var root = _model.SyntaxTree.GetRoot(_cancellation);
            var findings = new List<Finding>();
            var frames = new Dictionary<string, Stack<INamedTypeSymbol?>>(StringComparer.Ordinal);
            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax access) continue;
                var method = access.Name.Identifier.ValueText;
                if (!IsTracked(method)) continue;
                if (_model.GetSymbolInfo(invocation, _cancellation).Symbol is not IMethodSymbol symbol
                    || !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, builder)) continue;

                // One frame stack per builder variable: a child content lambda writes to __builder2.
                var receiver = access.Expression.ToString();
                if (!frames.TryGetValue(receiver, out var stack)) frames[receiver] = stack = new Stack<INamedTypeSymbol?>();
                switch (method)
                {
                    case "OpenComponent":
                        stack.Push(symbol.IsGenericMethod ? symbol.TypeArguments[0] as INamedTypeSymbol : null);
                        break;
                    case "OpenElement":
                    case "OpenRegion":
                        stack.Push(null);
                        break;
                    case "CloseComponent":
                    case "CloseElement":
                    case "CloseRegion":
                        if (stack.Count > 0) stack.Pop();
                        break;
                    default:
                        if (stack.Count > 0 && stack.Peek() is { } component && Inspect(invocation, component) is { } finding)
                            findings.Add(finding);
                        break;
                }
            }

            if (findings.Count > 0) Report(root, findings);
        }

        private static bool IsTracked(string method) => method switch
        {
            "OpenComponent" or "OpenElement" or "OpenRegion" or "CloseComponent" or "CloseElement" or "CloseRegion"
                or "AddAttribute" or "AddComponentParameter" => true,
            _ => false,
        };

        private Finding? Inspect(InvocationExpressionSyntax invocation, INamedTypeSymbol component)
        {
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count < 2
                || !string.Equals(component.ContainingAssembly?.Name, LibraryAssemblyName, StringComparison.Ordinal)) return null;

            switch (arguments[1].Expression)
            {
                case LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression):
                {
                    var name = literal.Token.ValueText;
                    if (name.Length == 0) return null;

                    var shape = ShapeOf(component);
                    var value = arguments.Count > 2 ? arguments[2].Expression : null;
                    if (name is "class" or "id" && shape.Parameters.Contains(name))
                        return new Finding(literal, component.Name, name, EffectivePosition(invocation, value)) { Replacement = Capitalized(name) };
                    if (shape.Parameters.Contains(name)) return null;
                    return name[0] is >= 'A' and <= 'Z' || !shape.Captures
                        ? new Finding(literal, component.Name, name, EffectivePosition(invocation, value))
                        : null;
                }

                // A parameter the generator bound: nameof(T.Class) or nameof(T.Id), whose #line directive
                // maps to the attribute as the .razor file writes it. Checked against that text in Report.
                case InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } nameOf
                    when nameOf.ArgumentList.Arguments.Count == 1:
                {
                    var token = nameOf.ArgumentList.Arguments[0].Expression.GetLastToken();
                    if (token.ValueText is not ("Class" or "Id")) return null;
                    var mapping = token.LeadingTrivia
                        .Select(trivia => trivia.GetStructure())
                        .OfType<LineSpanDirectiveTriviaSyntax>()
                        .LastOrDefault();
                    return mapping is null
                        ? null
                        : new Finding(null, component.Name, token.ValueText.ToLowerInvariant(), invocation.SpanStart) { Replacement = token.ValueText, Mapping = mapping };
                }

                default:
                    return null;
            }
        }

        private static string Capitalized(string name) => char.ToUpperInvariant(name[0]) + name.Substring(1);

        private ComponentShape ShapeOf(INamedTypeSymbol component) =>
            parameterCache.GetOrAdd(component.OriginalDefinition, type =>
            {
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var captures = false;
                for (var current = type; current is not null; current = current.BaseType)
                {
                    foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (property.DeclaredAccessibility != Accessibility.Public) continue;
                        var attribute = property.GetAttributes().FirstOrDefault(candidate => SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, parameterAttribute));
                        if (attribute is null) continue;
                        names.Add(property.Name);
                        captures |= attribute.NamedArguments.Any(argument => argument.Key == "CaptureUnmatchedValues" && argument.Value.Value is true);
                    }
                }

                return new ComponentShape(names, captures);
            });

        /// <summary>
        /// Where the attribute sits in generator order. A generic component whose type argument is
        /// inferred is written in a <c>TypeInference</c> helper at the end of the file; its attribute is
        /// then placed at the argument that carries its value in the helper call.
        /// </summary>
        private int EffectivePosition(InvocationExpressionSyntax invocation, ExpressionSyntax? value)
        {
            var helper = invocation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (helper?.Parent is not ClassDeclarationSyntax { Identifier.ValueText: "TypeInference" }) return invocation.SpanStart;

            var helperSymbol = _model.GetDeclaredSymbol(helper, _cancellation);
            var ordinal = value is IdentifierNameSyntax && _model.GetSymbolInfo(value, _cancellation).Symbol is IParameterSymbol parameter
                ? parameter.Ordinal
                : -1;
            foreach (var call in helper.SyntaxTree.GetRoot(_cancellation).DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (call.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var callee }
                    || callee != helper.Identifier.ValueText
                    || !SymbolEqualityComparer.Default.Equals((_model.GetSymbolInfo(call, _cancellation).Symbol as IMethodSymbol)?.OriginalDefinition, helperSymbol)) continue;
                var arguments = call.ArgumentList.Arguments;
                // FullSpan: the value's own #line mapping, in its leading trivia, lies after the attribute name.
                return ordinal >= 0 && ordinal < arguments.Count ? arguments[ordinal].FullSpan.Start : call.SpanStart;
            }

            return invocation.SpanStart;
        }

        private void Report(SyntaxNode root, List<Finding> findings)
        {
            var mappings = LineMappings(root, out var razorPath);
            var razorFile = razorPath is null ? null : FindRazorFile(razorPath);
            var razorText = razorFile?.GetText(_cancellation);
            var cursor = 0;
            var texts = new Dictionary<string, SourceText?>(StringComparer.OrdinalIgnoreCase);
            foreach (var finding in findings.OrderBy(finding => finding.Position))
            {
                if (finding.Mapping is { } mapping)
                {
                    if (LowercaseInRazor(mapping, finding.Name, texts) is { } written)
                        context.ReportDiagnostic(Diagnostic.Create(LowercaseRule, written, finding.Component, finding.Name, finding.Replacement));
                    continue;
                }

                Location location;
                if (razorPath is null)
                {
                    location = finding.Literal!.GetLocation();
                }
                else
                {
                    var anchor = mappings.LastOrDefault(mapping => mapping.Position < finding.Position);
                    location = LocateInRazor(razorPath, razorText, anchor, finding.Name, ref cursor);
                }

                context.ReportDiagnostic(finding.Replacement is null
                    ? Diagnostic.Create(Rule, location, finding.Component, finding.Name)
                    : Diagnostic.Create(LowercaseRule, location, finding.Component, finding.Name, finding.Replacement));
            }
        }

        /// <summary>
        /// The location of a bound Class or Id attribute when the .razor file writes it in lowercase
        /// (<paramref name="lowercase"/>), read at the span its #line directive maps to; null when it is
        /// written as the parameter, or when the file is not available.
        /// </summary>
        private Location? LowercaseInRazor(LineSpanDirectiveTriviaSyntax mapping, string lowercase, Dictionary<string, SourceText?> texts)
        {
            if (!int.TryParse(mapping.Start.Line.ValueText, out var line)
                || !int.TryParse(mapping.Start.Character.ValueText, out var character)) return null;
            var path = mapping.File.ValueText;
            if (!texts.TryGetValue(path, out var text)) texts[path] = text = FindRazorFile(path)?.GetText(_cancellation);
            if (text is null || line < 1 || line > text.Lines.Count) return null;

            var start = text.Lines[line - 1].Start + character - 1;
            if (character < 1 || start + lowercase.Length > text.Length
                || !string.Equals(text.ToString(new TextSpan(start, lowercase.Length)), lowercase, StringComparison.Ordinal)) return null;

            var span = new TextSpan(start, lowercase.Length);
            return Location.Create(path, span, text.Lines.GetLinePositionSpan(span));
        }

        /// <summary>
        /// Finds the attribute in the .razor text, searching forward from the last position the
        /// generator mapped before it (the tag, or the attribute before it) and after the previous
        /// finding, since the generator emits attributes in source order.
        /// </summary>
        private static Location LocateInRazor(string razorPath, SourceText? text, LineMapping? anchor, string name, ref int cursor)
        {
            var anchorPosition = anchor is null ? new LinePosition(0, 0) : new LinePosition(anchor.Line, anchor.Character);
            if (text is null) return Location.Create(razorPath, default, new LinePositionSpan(anchorPosition, anchorPosition));

            var anchorOffset = anchor is null || anchor.Line >= text.Lines.Count
                ? 0
                : Math.Min(text.Lines[anchor.Line].Start + anchor.Character, text.Length);
            var content = text.ToString();
            var pattern = new Regex(@"(?<=\s)" + Regex.Escape(name) + @"(?=[\s=/>])", RegexOptions.CultureInvariant);
            var match = pattern.Match(content, Math.Max(anchorOffset, cursor));
            if (!match.Success && cursor > anchorOffset) match = pattern.Match(content, anchorOffset);
            if (!match.Success) return Location.Create(razorPath, default, new LinePositionSpan(anchorPosition, anchorPosition));

            cursor = match.Index + match.Length;
            var span = new TextSpan(match.Index, match.Length);
            return Location.Create(razorPath, span, text.Lines.GetLinePositionSpan(span));
        }

        private AdditionalText? FindRazorFile(string razorPath)
        {
            var target = Normalize(razorPath);
            foreach (var file in razorFiles)
            {
                var candidate = Normalize(file.Path);
                if (string.Equals(candidate, target, StringComparison.OrdinalIgnoreCase)
                    || target.EndsWith("/" + candidate.TrimStart('.', '/'), StringComparison.OrdinalIgnoreCase)) return file;
            }

            return null;
        }

        private static string Normalize(string path) => path.Replace('\\', '/');

        /// <summary>The <c>#line</c> mappings of the generated tree that point into its own .razor file, in order.</summary>
        private List<LineMapping> LineMappings(SyntaxNode root, out string? razorPath)
        {
            razorPath = null;
            var all = new List<LineMapping>();
            foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
            {
                switch (trivia.GetStructure())
                {
                    case PragmaChecksumDirectiveTriviaSyntax checksum when razorPath is null:
                        razorPath = checksum.File.ValueText;
                        break;
                    case LineSpanDirectiveTriviaSyntax span
                        when int.TryParse(span.Start.Line.ValueText, out var line) && int.TryParse(span.Start.Character.ValueText, out var character):
                        all.Add(new LineMapping(trivia.SpanStart, span.File.ValueText, line - 1, character - 1));
                        break;
                    case LineDirectiveTriviaSyntax { File.RawKind: (int)SyntaxKind.StringLiteralToken } directive
                        when int.TryParse(directive.Line.ValueText, out var line):
                        all.Add(new LineMapping(trivia.SpanStart, directive.File.ValueText, line - 1, 0));
                        break;
                }
            }

            razorPath ??= all.FirstOrDefault(mapping => mapping.File.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))?.File;
            if (razorPath is null) return all;
            var path = razorPath;
            return all.Where(mapping => string.Equals(mapping.File, path, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    private sealed class Finding(LiteralExpressionSyntax? literal, string component, string name, int position)
    {
        /// <summary>The literal naming the attribute; null for a parameter the generator bound with nameof.</summary>
        internal LiteralExpressionSyntax? Literal { get; } = literal;
        internal string Component { get; } = component;
        internal string Name { get; } = name;
        internal int Position { get; } = position;

        /// <summary>For a lowercase class or id, the parameter to write instead; null for an unknown attribute.</summary>
        internal string? Replacement { get; set; }

        /// <summary>For a bound Class or Id, the #line directive mapping it into the .razor file.</summary>
        internal LineSpanDirectiveTriviaSyntax? Mapping { get; set; }
    }

    private sealed class ComponentShape(HashSet<string> parameters, bool captures)
    {
        /// <summary>The public parameter names, compared case-insensitively as Blazor does.</summary>
        internal HashSet<string> Parameters { get; } = parameters;

        /// <summary>Whether a parameter captures unmatched attributes.</summary>
        internal bool Captures { get; } = captures;
    }

    private sealed class LineMapping(int position, string file, int line, int character)
    {
        internal int Position { get; } = position;
        internal string File { get; } = file;
        internal int Line { get; } = line;
        internal int Character { get; } = character;
    }
}
