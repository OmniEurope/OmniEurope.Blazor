using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OmniEurope.Blazor.Tests;

public sealed partial class ConventionGuardTests
{
    [Fact]
    public void PanelMenuItemLocationSubscriptionGuard()
    {
        var source = Read("src", "OmniEurope.Blazor", "Components", "Navigation", "OmniPanelMenuItem.razor")
            + Read("src", "OmniEurope.Blazor", "Components", "Navigation", "OmniPanelMenuItem.razor.cs");

        Assert.Contains("AbsolutePath", source, StringComparison.Ordinal);
        Assert.Contains("currentUri.Authority", source, StringComparison.Ordinal);
        Assert.Contains("LocationChanged +=", source, StringComparison.Ordinal);
        Assert.Contains("LocationChanged -=", source, StringComparison.Ordinal);
    }

    [Fact]
    public void NativeButtonTypeAttributeGuard()
    {
        var componentRoot = Path.Combine(Root, "src", "OmniEurope.Blazor", "Components");
        var violations = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .SelectMany(file => ButtonTag().Matches(File.ReadAllText(file)).Select(match => (file, tag: match.Value)))
            .Where(candidate => !ButtonType().IsMatch(candidate.tag))
            .Select(candidate => Path.GetRelativePath(Root, candidate.file))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(violations.Length == 0, $"Boutons sans type explicite: {string.Join(", ", violations)}");
    }

    [Fact]
    public void AuditedSampleActions_UseDecorativeOmniIcons()
    {
        var auto = Read("samples", "OmniEurope.Blazor.AutoSmoke.Client", "AutoProbe.razor");
        var hybrid = Read("samples", "OmniEurope.Blazor.HybridSmoke", "HybridSmoke.razor");
        var wasm = Read("samples", "OmniEurope.Blazor.WasmSmoke", "App.razor");
        var catalog = Read("samples", "OmniEurope.Blazor.Catalog", "Components", "Pages", "Home.razor");

        Assert.True(ButtonContainsIcon(auto, "auto-action"));
        Assert.True(ButtonContainsIcon(hybrid, "hybrid-action"));
        Assert.True(ButtonContainsIcon(wasm, "wasm-action"));
        Assert.True(ButtonContainsIcon(catalog, "catalog-open-dialog"));
        Assert.True(ButtonContainsIcon(catalog, "catalog-notify"));
        Assert.False(ButtonContainsIcon("<OmniButton Id=\"auto-action\">Sans icône</OmniButton><OmniIcon />", "auto-action"));
    }

    [Fact]
    public void CatalogDialogRequests_ProvideALocalizedExplicitFooterAction()
    {
        var request = Read("src", "OmniEurope.Blazor", "Components", "Overlays", "OmniDialogRequest.cs");
        var host = Read("src", "OmniEurope.Blazor", "Components", "Overlays", "OmniOverlayHosts.cs");
        var catalog = Read("samples", "OmniEurope.Blazor.Catalog", "Components", "Pages", "Home.razor.cs");

        Assert.Contains("public RenderFragment? Footer { get; init; }", request, StringComparison.Ordinal);
        Assert.Contains("nameof(OmniDialog.Footer), dialog.Footer", host, StringComparison.Ordinal);
        Assert.Contains("Footer = DialogFooter", catalog, StringComparison.Ordinal);
        Assert.Contains("Text[\"Close\"]", catalog, StringComparison.Ordinal);
        Assert.Contains("private void CloseDialog() => Overlays.CloseDialog();", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void EditorConfig_IncludesProjectNeutralConventionsAndPromotedAnalyzers()
    {
        var configuration = Read(".editorconfig");

        Assert.DoesNotContain("\r\n", configuration, StringComparison.Ordinal);
        Assert.Contains("[*.md]\ntrim_trailing_whitespace = false", configuration, StringComparison.Ordinal);
        Assert.Contains("dotnet_sort_system_directives_first = true", configuration, StringComparison.Ordinal);
        Assert.Contains("csharp_style_namespace_declarations = file_scoped:warning", configuration, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.GEN004.severity = error", configuration, StringComparison.Ordinal);
        Assert.Contains("dotnet_diagnostic.GEN008.severity = error", configuration, StringComparison.Ordinal);
        Assert.Contains("[**/Migrations/*.cs]", configuration, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditedInteractiveTargets_MeetTheMinimumTouchSize()
    {
        var styles = Read("src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css");

        Assert.Contains(".omni-notification__dismiss { border: 0; border-radius: var(--omni-radius); font-size: var(--omni-font-size-h4); min-height: 2.75rem; min-width: 2.75rem; }", styles, StringComparison.Ordinal);
        Assert.Contains(".omni-tree__select { background: transparent; border: 0; color: var(--omni-color-text); cursor: pointer; font: inherit; min-height: 2.75rem; }", styles, StringComparison.Ordinal);
        Assert.Contains(".omni-tree__toggle { min-width: 2.75rem; width: 2.75rem; }", styles, StringComparison.Ordinal);
        Assert.Contains(".omni-data-grid__expand { background: transparent; border: 0; color: var(--omni-color-text); cursor: pointer; min-height: 2.75rem; min-width: 2.75rem; }", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void CollapsedStack_HidesLabelsAndKeepsIcons()
    {
        var styles = Read("src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css");

        // Hiding the button's whole content took the icon with the label, leaving empty buttons.
        Assert.DoesNotContain(".omni-stack--overflow-collapse .omni-button > span", styles, StringComparison.Ordinal);
        Assert.Contains(".omni-stack--overflow-collapse :is(.omni-button__content,.omni-select-bar__item) > span:not([aria-hidden=\"true\"],[role=\"img\"])", styles, StringComparison.Ordinal);
        // No host class inside the package: an icon is recognised by what it declares, not by who drew it.
        Assert.DoesNotContain(".oe-icon", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void BusyVeil_IsOneSharedSurfaceColouredRuleAndNoSpinner()
    {
        var styles = Read("src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css");
        var veil = Regex.Match(styles, @"\.omni-busy::after,\s*\.btn-busy::after\s*\{(?<body>[^}]*)\}");

        Assert.True(veil.Success, "La règle partagée du voile .omni-busy::after / .btn-busy::after est absente.");
        Assert.Contains("background: var(--omni-color-surface);", veil.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("animation: omni-busy-veil 1.3s", veil.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("@keyframes omni-busy-veil { 0%, 100% { opacity: 0; } 50% { opacity: 0.45; } }", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("omni-button__busy", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditedTypeContainers_AreSplitIntoOneTopLevelTypePerFile()
    {
        var componentRoot = Path.Combine(Root, "src", "OmniEurope.Blazor", "Components");
        var retiredContainers = new[]
        {
            Path.Combine(componentRoot, "Charts", "OmniChartTypes.cs"),
            Path.Combine(componentRoot, "Data", "OmniDataGridTypes.cs"),
            Path.Combine(componentRoot, "Foundation", "OmniFoundationTypes.cs"),
            Path.Combine(componentRoot, "Navigation", "OmniNavigationTypes.cs"),
            Path.Combine(componentRoot, "Overlays", "OmniOverlayTypes.cs"),
            Path.Combine(componentRoot, "Scheduling", "OmniSchedulerTypes.cs"),
            Path.Combine(componentRoot, "Selection", "OmniSelectionTypes.cs"),
            Path.Combine(componentRoot, "Layout", "OmniStackTypes.cs")
        };
        Assert.All(retiredContainers, path => Assert.False(File.Exists(path), $"Conteneur multi-type encore présent: {path}"));

        var expectedTypes = new[]
        {
            ("Charts", "OmniChartPoint"), ("Charts", "OmniChartSlice"), ("Charts", "OmniChartGeometry"),
            ("Data", "OmniDataGridSelectionMode"), ("Data", "OmniDataGridFilterOperator"), ("Data", "OmniDataGridSort"),
            ("Data", "OmniDataGridFilter"), ("Data", "OmniDataGridColumnWidthChange"), ("Data", "OmniDataGridTextAlign"),
            ("Data", "OmniDataGridLines"), ("Data", "OmniDataGridFilterMode"), ("Data", "OmniDataGridEditMode"),
            ("Data", "OmniDataGridExpandMode"), ("Data", "OmniDataGridPagerPosition"), ("Data", "OmniDataGridSortOrder"),
            ("Data", "OmniDataGridLogicalOperator"), ("Data", "OmniDataGridGroup"), ("Data", "OmniDataGridRowRenderArgs"),
            ("Data", "OmniDataGridFilterCaseSensitivity"),
            ("Data", "OmniDataGridLoadRequest"), ("Data", "OmniDataGridResult"), ("Data", "OmniDataGridColumnDefinition"),
            ("Data", "OmniDataGridContext"), ("Foundation", "OmniTextElement"), ("Foundation", "OmniTextTone"),
            ("Foundation", "OmniHeadingLevel"), ("Foundation", "OmniIconName"), ("Foundation", "OmniBadgeVariant"),
            ("Foundation", "OmniImageLoading"), ("Foundation", "OmniImageFit"), ("Foundation", "OmniSkeletonShape"),
            ("Foundation", "OmniLayoutWidth"), ("Foundation", "OmniProgressVariant"), ("Foundation", "OmniProgressShape"),
            ("Foundation", "OmniSidebarPosition"), ("Foundation", "OmniAppearance"), ("Foundation", "OmniDensity"),
            ("Navigation", "OmniTabsContext"), ("Navigation", "OmniStepsContext"),
            ("Overlays", "OmniNotificationSeverity"), ("Overlays", "OmniDialogRequest"),
            ("Overlays", "OmniNotificationMessage"), ("Overlays", "OmniOverlayService"),
            ("Scheduling", "OmniSchedulerView"), ("Scheduling", "OmniSchedulerAppointment"),
            ("Selection", "OmniOption"), ("Selection", "OmniUploadRequest"),
            ("Layout", "OmniStackOrientation"), ("Layout", "OmniSpacing"),
            ("Layout", "OmniAlignment"), ("Layout", "OmniJustification")
        };

        foreach (var (directory, type) in expectedTypes)
        {
            var path = Path.Combine(componentRoot, directory, $"{type}.cs");
            Assert.True(File.Exists(path), $"Fichier attendu absent: {path}");
            Assert.Single(TopLevelTypeDeclaration().Matches(File.ReadAllText(path)).Cast<Match>());
        }
    }

    [Fact]
    public void EngineeringPowerShellScripts_AreAsciiOnly()
    {
        var engineeringRoot = Path.Combine(Root, "eng");
        var violations = Directory.EnumerateFiles(engineeringRoot, "*.ps1", SearchOption.TopDirectoryOnly)
            .Where(path => File.ReadAllText(path).Any(character => character > 127))
            .Select(path => Path.GetRelativePath(Root, path))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
        Assert.True(File.Exists(Path.Combine(engineeringRoot, "PowerShellMessages.psd1")));
    }

    [Fact]
    public void OfficialLicense_IsTheOnlyEmDashException()
    {
        var roots = new[]
        {
            Path.Combine(Root, "src"), Path.Combine(Root, "samples"), Path.Combine(Root, "tests"),
            Path.Combine(Root, "eng"), Path.Combine(Root, "docs"), Path.Combine(Root, ".github"),
            Path.Combine(Root, "plans")
        };
        var violations = roots
            .SelectMany(path => Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains('\u2014'))
            .Select(path => Path.GetRelativePath(Root, path))
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Empty(violations);

        var license = Read("LICENSE");
        Assert.Equal(2, license.Count(character => character == '\u2014'));
        Assert.Contains("Licence \u2014 for example", license, StringComparison.Ordinal);
        Assert.Contains("Licence \u2014 Reciprocity", license, StringComparison.Ordinal);
    }

    [Fact]
    public void CatalogClaims_ExactlyMatchTheIllustratedComponentMatrix()
    {
        var markup = Read("samples", "OmniEurope.Blazor.Catalog", "Components", "Pages", "Home.razor");
        var resources = Read("samples", "OmniEurope.Blazor.Catalog", "Resources", "CatalogStrings.resx")
            + Read("samples", "OmniEurope.Blazor.Catalog", "Resources", "CatalogStrings.en.resx");
        var illustrated = Regex.Matches(markup, "<\\s*(Omni[A-Z][A-Za-z0-9]*)\\b")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        using var matrix = JsonDocument.Parse(Read("docs", "catalog-scenarios.json"));
        var declared = matrix.RootElement.GetProperty("components").EnumerateArray()
            .Select(item => item.GetProperty("component").GetString()!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(illustrated, declared);
        Assert.True(declared.Length == 37, $"La matrice doit contenir exactement 37 composants, valeur actuelle: {declared.Length}.");
        Assert.All(matrix.RootElement.GetProperty("components").EnumerateArray(), item =>
            Assert.Equal("illustrated", item.GetProperty("evidence").GetString()));
        Assert.Contains("<strong>37</strong>", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("110/110", markup + resources, StringComparison.Ordinal);
        Assert.DoesNotContain("110 capacités", resources, StringComparison.Ordinal);
        Assert.DoesNotContain("110 inventoried", resources, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalJson_PinsTheFeatureBandWithoutAWorkloadVersion()
    {
        using var configuration = JsonDocument.Parse(Read("global.json"));
        var sdk = configuration.RootElement.GetProperty("sdk");

        Assert.Equal("latestPatch", sdk.GetProperty("rollForward").GetString());
        Assert.False(sdk.TryGetProperty("workloadVersion", out _));
    }

    [Fact]
    public void AuthenticityGateWiring_ReferencesExecutableProbesAndCurrentHybridDependencies()
    {
        var catalogHost = Read("eng", "Test-CatalogHost.ps1");
        var editor = Read("src", "OmniEurope.Blazor", "Components", "Editor", "OmniHtmlEditor.razor.cs")
            + Read("src", "OmniEurope.Blazor", "wwwroot", "omniInterop.js");
        var packages = Read("Directory.Packages.props");
        using var hybridLock = JsonDocument.Parse(Read("samples", "OmniEurope.Blazor.HybridSmoke", "packages.lock.json"));

        Assert.Contains("Test-CatalogProbe.mjs", catalogHost, StringComparison.Ordinal);
        Assert.Contains("& node", catalogHost, StringComparison.Ordinal);
        Assert.Contains("CatalogCspAfter", catalogHost, StringComparison.Ordinal);
        Assert.Contains("wrapTextSelection", editor, StringComparison.Ordinal);
        Assert.Contains("restoreTextSelection", editor, StringComparison.Ordinal);
        Assert.Contains("setSelectionRange(6, 10)", Read("eng", "Test-CatalogProbe.mjs"), StringComparison.Ordinal);
        Assert.Contains("Microsoft.Maui.Controls\" Version=\"10.0.90", packages, StringComparison.Ordinal);
        Assert.Contains("Microsoft.AspNetCore.Components.WebView.Maui\" Version=\"10.0.90", packages, StringComparison.Ordinal);
        var hybridDependencies = hybridLock.RootElement.GetProperty("dependencies").EnumerateObject()
            .First(framework => framework.Name.StartsWith("net10.0-windows10.0.19041", StringComparison.Ordinal)).Value;
        Assert.Equal("10.0.90", hybridDependencies.GetProperty("Microsoft.Maui.Controls").GetProperty("resolved").GetString());
        Assert.Equal("10.0.90", hybridDependencies.GetProperty("Microsoft.AspNetCore.Components.WebView.Maui").GetProperty("resolved").GetString());
    }

    [Fact]
    public void ProductionRazorComponents_HaveNoInlineCodeBlocks()
    {
        var roots = new[] { Path.Combine(Root, "src"), Path.Combine(Root, "samples") };
        var violations = roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories))
            .Where(file => Regex.IsMatch(File.ReadAllText(file), "(?m)^\\s*@code\\b"))
            .Select(file => Path.GetRelativePath(Root, file))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
        Assert.True(File.Exists(Path.Combine(Root, "samples", "OmniEurope.Blazor.WasmSmoke", "App.razor.cs")));
    }

    [Fact]
    public void DialogPrimitiveAccessibilityGuard()
    {
        var source = Read("src", "OmniEurope.Blazor", "Components", "Overlays", "OmniDialog.razor")
            + Read("src", "OmniEurope.Blazor", "Components", "Overlays", "OmniDialog.razor.cs");
        var focusModule = Read("src", "OmniEurope.Blazor", "wwwroot", "omni-focus.js");

        Assert.Contains("role=\"dialog\"", source, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", source, StringComparison.Ordinal);
        Assert.Contains("args.Key == \"Escape\"", source, StringComparison.Ordinal);
        Assert.Contains("activateDialog", source, StringComparison.Ordinal);
        Assert.Contains("restoreFocus", source, StringComparison.Ordinal);
        Assert.Contains("focusableElements", focusModule, StringComparison.Ordinal);
        Assert.Contains("event.key !== 'Tab'", focusModule, StringComparison.Ordinal);
    }

    [Fact]
    public void FocusInterop_RespectsReducedMotionPreference()
    {
        var source = Read("src", "OmniEurope.Blazor", "wwwroot", "omniInterop.js");

        Assert.Contains("(prefers-reduced-motion: reduce)", source, StringComparison.Ordinal);
        Assert.Contains("reducedMotion ? 'auto' : 'smooth'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AuditedSampleText_UsesResourcesWithMatchingEnglishKeys()
    {
        var samples = new[]
        {
            (markup: Read("samples", "OmniEurope.Blazor.AutoSmoke.Client", "AutoProbe.razor"), resource: Path.Combine(Root, "samples", "OmniEurope.Blazor.AutoSmoke.Client", "Resources", "AutoSmokeStrings.resx")),
            (markup: Read("samples", "OmniEurope.Blazor.HybridSmoke", "HybridSmoke.razor"), resource: Path.Combine(Root, "samples", "OmniEurope.Blazor.HybridSmoke", "Resources", "HybridSmokeStrings.resx")),
            (markup: Read("samples", "OmniEurope.Blazor.Catalog", "Components", "Pages", "Home.razor") + Read("samples", "OmniEurope.Blazor.Catalog", "Components", "Layout", "MainLayout.razor"), resource: Path.Combine(Root, "samples", "OmniEurope.Blazor.Catalog", "Resources", "CatalogStrings.resx")),
            (markup: Read("samples", "OmniEurope.Blazor.WasmSmoke", "App.razor"), resource: Path.Combine(Root, "samples", "OmniEurope.Blazor.WasmSmoke", "Resources", "WasmSmokeStrings.resx"))
        };

        Assert.All(samples, sample => Assert.Contains("@Text[", sample.markup, StringComparison.Ordinal));
        var combinedMarkup = string.Concat(samples.Select(sample => sample.markup));
        foreach (var auditedLiteral in new[] { "Compteur Auto :", "Action Hybrid", "Catalogue 110/110", "Catalogue des composants", "Dialogue imbriqué", "La bibliothèque est chargée", "Progression du test" })
        {
            Assert.DoesNotContain(auditedLiteral, combinedMarkup, StringComparison.Ordinal);
        }

        foreach (var sample in samples)
        {
            var english = Path.Combine(
                Path.GetDirectoryName(sample.resource)!,
                $"{Path.GetFileNameWithoutExtension(sample.resource)}.en.resx");
            Assert.Equal(ResourceKeys(sample.resource), ResourceKeys(english));
        }
    }

    [Fact]
    public void LibraryLocalization_ExhaustivelyUsesMatchingResourcesAndInjectedLocalizers()
    {
        var resources = Path.Combine(Root, "src", "OmniEurope.Blazor", "Resources", "AppStrings.resx");
        var english = Path.Combine(Root, "src", "OmniEurope.Blazor", "Resources", "AppStrings.en.resx");
        Assert.Equal(ResourceKeys(resources), ResourceKeys(english));

        var componentRoot = Path.Combine(Root, "src", "OmniEurope.Blazor", "Components");
        var sources = Directory.EnumerateFiles(componentRoot, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetExtension(file) is ".cs" or ".razor")
            .Select(file => (file, content: File.ReadAllText(file)))
            .ToArray();
        var directResourceManagerUsage = sources
            .Where(source => source.content.Contains("ResourceManager", StringComparison.Ordinal)
                || source.content.Contains("OmniStrings", StringComparison.Ordinal))
            .Select(source => Path.GetRelativePath(Root, source.file))
            .ToArray();
        var hardCodedFrench = sources
            .Where(source => Regex.IsMatch(source.content, "[àâçéèêëîïôùûüÿœæÀÂÇÉÈÊËÎÏÔÙÛÜŸŒÆ]"))
            .Select(source => Path.GetRelativePath(Root, source.file))
            .ToArray();

        Assert.Empty(directResourceManagerUsage);
        Assert.Empty(hardCodedFrench);
        Assert.Contains(sources, source => source.content.Contains("Localize(", StringComparison.Ordinal));
    }

    [Fact]
    public void RazorImports_DeclareTheWebNamespaceSoEventDirectivesStayDirectives()
    {
        // Without this using, an @onchange or @onclick written inside a template still compiles: Razor
        // stops recognising it as a directive attribute and emits it as a literal HTML attribute, so
        // the handler is silently never wired. Nothing else in the suite catches that.
        var imports = new[] { "tests", "samples" }
            .Select(area => Path.Combine(Root, area))
            .SelectMany(area => Directory.EnumerateFiles(area, "_Imports.razor", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(imports);
        Assert.All(imports, path => Assert.Contains(
            "@using Microsoft.AspNetCore.Components.Web",
            File.ReadAllText(path),
            StringComparison.Ordinal));
    }

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new DirectoryNotFoundException("Racine du dépôt introuvable.");
        }
    }

    private static string Read(params string[] segments) => File.ReadAllText(Path.Combine([Root, .. segments]));

    private static bool ButtonContainsIcon(string source, string id)
    {
        var match = Regex.Match(
            source,
            $"<OmniButton\\b(?=[^>]*\\bId=\\\"{Regex.Escape(id)}\\\")[^>]*>(?<content>.*?)</OmniButton>",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        return match.Success && match.Groups["content"].Value.Contains("<OmniIcon", StringComparison.Ordinal);
    }

    private static string[] ResourceKeys(string path) => XDocument.Load(path)
        .Root!
        .Elements("data")
        .Select(element => (string)element.Attribute("name")!)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [GeneratedRegex("<button\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ButtonTag();

    [GeneratedRegex("\\btype\\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex ButtonType();

    [GeneratedRegex("(?m)^(?:public|internal)\\s+(?:sealed\\s+|static\\s+|partial\\s+)*(?:class|record|enum|interface|struct)\\s+")]
    private static partial Regex TopLevelTypeDeclaration();
}
