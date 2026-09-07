using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Resources;
using System.Globalization;

namespace OmniEurope.Blazor.Tests;

public sealed class LocalizationTests : OmniBunitContext
{
    [Theory]
    [InlineData("fr-FR", "Le téléversement a échoué.")]
    [InlineData("en-US", "The upload failed.")]
    public void DefaultResourcesResolveInSupportedCultures(string cultureName, string expected)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOmniEuropeBlazor();
            using var provider = services.BuildServiceProvider();
            var localizer = provider.GetRequiredService<IStringLocalizer<AppStrings>>();

            var value = localizer["UploadFailed"];

            Assert.False(value.ResourceNotFound);
            Assert.Equal(expected, value.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
    [Theory]
    [InlineData("fr-FR", "Fil d'Ariane", "Jauge", "Changer l'apparence")]
    [InlineData("en-US", "Breadcrumb", "Gauge", "Change appearance")]
    public void ComponentDefaultsFollowCurrentUiCulture(string cultureName, string breadcrumbLabel, string gaugeLabel, string appearanceLabel)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            var breadcrumb = Render<OmniBreadcrumb>();
            var gauge = Render<OmniArcGauge>();
            var appearance = Render<OmniAppearanceToggle>();

            Assert.Equal(breadcrumbLabel, breadcrumb.Find("nav").GetAttribute("aria-label"));
            Assert.Equal(gaugeLabel, gauge.Find("svg").GetAttribute("aria-label"));
            Assert.Equal(appearanceLabel, appearance.Find("button").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void ComponentsConsultTheHostProvidedStringLocalizer()
    {
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new RecordingLocalizer());
        JSInterop.Mode = JSRuntimeMode.Loose;

        var breadcrumb = Render<OmniBreadcrumb>();
        var host = Render<OmniComponentsHost>();
        var legend = Render<OmniLegend>();

        Assert.Equal("host:BreadcrumbLabel", breadcrumb.Find("nav").GetAttribute("aria-label"));
        Assert.Equal("host:NotificationsRegionLabel", host.Find(".omni-notification-region").GetAttribute("aria-label"));
        Assert.Equal("host:LegendLabel", legend.Find("g").GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData("fr-FR", "Onglets", "La sélection multiple n'est pas valide.")]
    [InlineData("en-US", "Tabs", "The multiple selection is invalid.")]
    public void TabsAndMultiSelectDefaultsFollowCurrentUiCulture(string cultureName, string tabsLabel, string invalidSelection)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            JSInterop.Mode = JSRuntimeMode.Loose;

            var tabs = Render<OmniTabs>();
            var selection = Array.Empty<int>();
            var multiSelect = Render<TestMultiSelect<int>>(parameters => parameters
                .Add(component => component.Value, selection)
                .Add(component => component.ValueExpression, () => selection));

            Assert.Equal(tabsLabel, tabs.Find("[role=tablist]").GetAttribute("aria-label"));
            Assert.Equal(invalidSelection, multiSelect.Instance.GetInvalidMessage());
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private sealed class TestMultiSelect<TValue> : OmniMultiSelect<TValue>
    {
        public string GetInvalidMessage()
        {
            _ = TryParseValueFromString(null, out _, out var message);
            return message;
        }
    }

    [Theory]
    [InlineData("fr-FR", "Les valeurs détaillées sont disponibles au focus ou au survol des séries.", "Aucun élément.", "Vue journalière", "Sélectionner", "Éditeur HTML")]
    [InlineData("en-US", "Detailed values are available when focusing or hovering over the series.", "No items.", "Day view", "Select", "HTML editor")]
    public void Lot09DataAndEditorDefaultsFollowCurrentUiCulture(
        string cultureName,
        string tooltipDescription,
        string emptyList,
        string dayViewLabel,
        string dropDownPlaceholder,
        string editorLabel)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var value = string.Empty;
            var selection = 0;

            var tooltip = Render<OmniChartTooltipOptions>();
            var list = Render<OmniDataList<int>>(parameters => parameters
                .Add(component => component.ItemTemplate, item => builder => builder.AddContent(0, item)));
            var day = Render<OmniDayView>(parameters => parameters.Add(component => component.Date, DateTimeOffset.Now));
            var dropDown = Render<OmniDropDown<int>>(parameters => parameters
                .Add(component => component.Options, [new OmniOption<int>(1, "One")])
                .Add(component => component.AllowEmpty, true)
                .Add(component => component.ValueExpression, () => selection));
            var editor = Render<OmniHtmlEditor>(parameters => parameters
                .Add(component => component.Value, value)
                .Add(component => component.ValueExpression, () => value));

            Assert.Equal(tooltipDescription, tooltip.Find("desc").TextContent);
            Assert.Contains(emptyList, list.Markup, StringComparison.Ordinal);
            Assert.Equal(dayViewLabel, day.Find("section").GetAttribute("aria-label"));
            Assert.Equal(dropDownPlaceholder, dropDown.Find("option").TextContent);
            Assert.Equal(editorLabel, editor.Find("section").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("fr-FR", "Notifications", "Menu contextuel", "Fermer", "Fin du dialogue", "Aucune donnée.")]
    [InlineData("en-US", "Notifications", "Context menu", "Close", "End of dialog", "No data.")]
    public void Lot09OverlayAndGridDefaultsFollowCurrentUiCulture(
        string cultureName,
        string notificationsLabel,
        string menuLabel,
        string closeLabel,
        string dialogEndLabel,
        string emptyGrid)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            JSInterop.Mode = JSRuntimeMode.Loose;

            var host = Render<OmniComponentsHost>();
            var menu = Render<OmniContextMenu>(parameters => parameters
                .Add(component => component.Open, true)
                .Add(component => component.TriggerContent, builder => builder.AddContent(0, "Open")));
            var dialog = Render<OmniDialog>(parameters => parameters
                .Add(component => component.Open, true)
                .Add(component => component.Title, "Title"));
            var grid = Render<OmniDataGrid<int>>();

            Assert.Equal(notificationsLabel, host.Find(".omni-notification-region").GetAttribute("aria-label"));
            Assert.Equal(menuLabel, menu.Find("[role=menu]").GetAttribute("aria-label"));
            Assert.Equal(closeLabel, dialog.Find(".omni-dialog__close").GetAttribute("aria-label"));
            Assert.Equal(dialogEndLabel, dialog.FindAll("[data-focus-sentinel]")[0].GetAttribute("aria-label"));
            Assert.Contains(emptyGrid, grid.Markup, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public async Task Lot09ValidatorDefaultsFollowEnglishUiCulture()
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var form = Render<FormTestHost>();
            form.Find("#name").Input("Alice");
            form.Instance.Model.Email = "invalid";
            form.Instance.Model.Password = "first";
            form.Instance.Model.ConfirmedPassword = "second";

            Assert.False(await form.InvokeAsync(() => form.Instance.EditContext.Validate()));
            var messages = form.Instance.EditContext.GetValidationMessages().ToArray();
            Assert.Contains("The email address is invalid.", messages);
            Assert.Contains("The values do not match.", messages);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("fr-FR", "Légende", "Vue mensuelle", "Ignorer la notification", "État non défini", "Page précédente", "Page 2 sur 4")]
    [InlineData("en-US", "Legend", "Month view", "Dismiss notification", "Indeterminate state", "Previous page", "Page 2 of 4")]
    public void Lot10StructuralDefaultsFollowCurrentUiCulture(
        string cultureName,
        string legendLabel,
        string monthLabel,
        string dismissLabel,
        string indeterminateDescription,
        string previousPage,
        string pageStatus)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            bool? nullable = null;

            var legend = Render<OmniLegend>();
            var month = Render<OmniMonthView>(parameters => parameters.Add(component => component.Date, DateTimeOffset.Now));
            var notification = Render<OmniNotification>(parameters => parameters.Add(component => component.Message, "Message"));
            var nullableSwitch = Render<OmniNullableSwitch>(parameters => parameters
                .Add(component => component.Value, nullable)
                .Add(component => component.ValueExpression, () => nullable));
            var pager = Render<OmniPager>(parameters => parameters
                .Add(component => component.Page, 2)
                .Add(component => component.PageCount, 4));

            Assert.Equal(legendLabel, legend.Find("g").GetAttribute("aria-label"));
            Assert.Equal(monthLabel, month.Find("section").GetAttribute("aria-label"));
            Assert.Equal(dismissLabel, notification.Find("button").GetAttribute("aria-label"));
            Assert.Equal(indeterminateDescription, nullableSwitch.Find("button").GetAttribute("aria-description"));
            Assert.Equal(previousPage, pager.FindAll("button")[0].GetAttribute("aria-label"));
            Assert.Equal(pageStatus, pager.Find(".omni-pager__status").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("fr-FR", "Navigation", "Menu du profil", "Afficher le mot de passe", "Afficher", "Progression", "50 %")]
    [InlineData("en-US", "Navigation", "Profile menu", "Show password", "Show", "Progress", "50%")]
    public void Lot10NavigationFormAndProgressDefaultsFollowCurrentUiCulture(
        string cultureName,
        string navigationLabel,
        string profileLabel,
        string revealLabel,
        string revealText,
        string progressLabel,
        string progressValue)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var passwordValue = string.Empty;

            var panel = Render<OmniPanelMenu>();
            var profile = Render<OmniProfileMenu>(parameters => parameters
                .Add(component => component.Summary, builder => builder.AddContent(0, "User")));
            var password = Render<OmniPassword>(parameters => parameters
                .Add(component => component.Value, passwordValue)
                .Add(component => component.ValueExpression, () => passwordValue));
            var progress = Render<OmniProgressBar>(parameters => parameters
                .Add(component => component.Value, 50)
                .Add(component => component.ShowValue, true));

            Assert.Equal(navigationLabel, panel.Find("nav").GetAttribute("aria-label"));
            Assert.Equal(profileLabel, profile.Find("summary").GetAttribute("aria-label"));
            Assert.Equal(revealLabel, password.Find("button").GetAttribute("aria-label"));
            Assert.Equal(revealText, password.Find("button").TextContent.Trim());
            Assert.Equal(progressLabel, progress.Find("[role=progressbar]").GetAttribute("aria-label"));
            Assert.Equal(progressValue, progress.Find(".omni-progress__label").TextContent);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void DialogRequestDefaultIsLocalizedAtRenderBoundary()
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            JSInterop.Mode = JSRuntimeMode.Loose;
            using var service = new OmniOverlayService();
            var host = Render<OmniComponentsHost>(parameters => parameters.Add(component => component.OverlayService, service));
            service.OpenDialog(new OmniDialogRequest("Title", builder => builder.AddContent(0, "Body")));

            host.WaitForAssertion(() => Assert.Equal("Close", host.Find(".omni-dialog__close").GetAttribute("aria-label")));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("fr-FR", "Période précédente", "Aujourd'hui", "Vue du calendrier", "Mois", "Navigation", "Afficher ou masquer la navigation")]
    [InlineData("en-US", "Previous period", "Today", "Calendar view", "Month", "Navigation", "Show or hide navigation")]
    public void Lot11SchedulerAndSidebarDefaultsFollowConfiguredCulture(
        string cultureName,
        string previousPeriod,
        string today,
        string calendarView,
        string month,
        string sidebarLabel,
        string sidebarToggleLabel)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = culture;
            var scheduler = Render<OmniScheduler>(parameters => parameters
                .Add(component => component.Date, new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero))
                .Add(component => component.Culture, culture)
                .Add(component => component.TimeZone, TimeZoneInfo.Utc));
            var sidebar = Render<OmniSidebar>(parameters => parameters
                .Add(component => component.Open, true)
                .AddChildContent("Menu"));
            var toggle = Render<OmniSidebarToggle>(parameters => parameters.Add(component => component.Controls, "sidebar"));

            Assert.Equal(previousPeriod, scheduler.FindAll("header button")[0].GetAttribute("aria-label"));
            Assert.Equal(today, scheduler.FindAll("header button")[1].TextContent);
            Assert.Equal(calendarView, scheduler.Find("[role=radiogroup]").GetAttribute("aria-label"));
            Assert.Equal(month, scheduler.FindAll("[role=radio]")[2].TextContent);
            Assert.Equal(sidebarLabel, sidebar.Find("aside").GetAttribute("aria-label"));
            Assert.Equal(sidebarToggleLabel, toggle.Find("button").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("fr-FR", "Autres actions", "Étapes", "Chronologie", "Arbre", "Vue hebdomadaire")]
    [InlineData("en-US", "More actions", "Steps", "Timeline", "Tree", "Week view")]
    public void Lot11NavigationAndCollectionDefaultsFollowCurrentUiCulture(
        string cultureName,
        string splitLabel,
        string stepsLabel,
        string timelineLabel,
        string treeLabel,
        string weekLabel)
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            JSInterop.Mode = JSRuntimeMode.Loose;
            var split = Render<OmniSplitButton>(parameters => parameters.Add(component => component.Text, "Save"));
            var steps = Render<OmniSteps>();
            var timeline = Render<OmniTimeline>();
            var tree = Render<OmniTree<int>>();
            var week = Render<OmniWeekView>(parameters => parameters.Add(component => component.Date, DateTimeOffset.Now));

            Assert.Equal(splitLabel, split.Find(".omni-split-button__toggle").GetAttribute("aria-label"));
            Assert.Equal(stepsLabel, steps.Find("[role=list]").GetAttribute("aria-label"));
            Assert.Equal(timelineLabel, timeline.Find("section").GetAttribute("aria-label"));
            Assert.Equal(treeLabel, tree.Find("[role=tree]").GetAttribute("aria-label"));
            Assert.Equal(weekLabel, week.Find("section").GetAttribute("aria-label"));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void Lot11UploadMessagesAndSizesFollowEnglishUiCulture()
    {
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var upload = Render<OmniUpload>();
            upload.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("hello", "note.txt", contentType: "text/plain"));

            Assert.Equal("Selected files", upload.Find(".omni-upload__files").GetAttribute("aria-label"));
            Assert.Contains("5 B", upload.Markup, StringComparison.Ordinal);
            Assert.Contains("1 file selected.", upload.Markup, StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    private sealed class RecordingLocalizer : IStringLocalizer<AppStrings>
    {
        public LocalizedString this[string name] => new(name, $"host:{name}");

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, $"host:{name}:{string.Join(',', arguments)}");

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
