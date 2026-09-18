using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>Lot 8 data compositions: resource list, entity picker, dynamic form and status badge.</summary>
public sealed class DataCompositionTests : OmniBunitContext
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);

    // ---- OmniResourceList ------------------------------------------------------------------------

    [Fact]
    public void ResourceList_ToolbarCarriesSearchFiltersActionsCountAndCreate()
    {
        var host = Render<ResourceListTestHost>();

        var search = host.Find(".omni-resource-list__search");
        Assert.Equal("search", search.GetAttribute("type"));
        Assert.Equal("Rechercher", search.GetAttribute("aria-label"));
        Assert.Single(host.FindAll(".host-filter"));
        Assert.Single(host.FindAll(".host-action"));
        Assert.Equal("3 éléments", host.Find(".omni-resource-list__count").TextContent.Trim());
        Assert.Equal("Serveurs", host.Find("section.omni-resource-list").GetAttribute("aria-label"));
        Assert.Equal(3, host.FindAll("td[data-omni-col='Name']").Count);
        Assert.Empty(host.FindAll(".omni-resource-list__clear"));

        host.Find(".omni-resource-list__create").Click();
        Assert.Equal(1, host.Instance.Created);
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResourceList_SearchFiltersLocallyBindsTheTextAndOffersToClearIt()
    {
        var host = Render<ResourceListTestHost>();

        host.Find(".omni-resource-list__search").Input("ALP");

        Assert.Equal("ALP", host.Instance.Search);
        var cells = host.FindAll("td[data-omni-col='Name']");
        Assert.Equal("alpha", Assert.Single(cells).TextContent.Trim());
        Assert.Equal("1 élément", host.Find(".omni-resource-list__count").TextContent.Trim());

        host.Find(".omni-resource-list__clear").Click();
        Assert.Equal(string.Empty, host.Instance.Search);
        Assert.Equal(3, host.FindAll("td[data-omni-col='Name']").Count);
    }

    [Fact]
    public void ResourceList_SearchWithoutMatch_SaysSoAndClearsFromTheEmptyState()
    {
        var host = Render<ResourceListTestHost>();

        host.Find(".omni-resource-list__search").Input("zzz");

        var noMatch = host.Find(".omni-resource-list__no-match");
        Assert.Equal("Aucun résultat", noMatch.QuerySelector(".omni-empty-state__title")!.TextContent);
        Assert.Contains("« zzz »", noMatch.TextContent, StringComparison.Ordinal);
        Assert.Empty(host.FindAll(".omni-resource-list__empty"));

        host.Find(".omni-resource-list__empty-clear").Click();
        Assert.Equal(3, host.FindAll("td[data-omni-col='Name']").Count);
    }

    [Fact]
    public void ResourceList_EmptyList_ShowsTheEmptyStateWithTheCreateAction()
    {
        var host = Render<ResourceListTestHost>(parameters => parameters.Add(component => component.Servers, []));

        var empty = host.Find(".omni-resource-list__empty");
        Assert.Equal("Aucun serveur", empty.QuerySelector(".omni-empty-state__title")!.TextContent);
        Assert.Equal("Ajoutez le premier serveur.", empty.QuerySelector(".omni-empty-state__description")!.TextContent);

        host.Find(".omni-resource-list__empty-create").Click();
        Assert.Equal(1, host.Instance.Created);
    }

    [Fact]
    public void ResourceList_WithoutTheRight_DisablesCreateAndSaysWhy()
    {
        var host = Render<ResourceListTestHost>(parameters => parameters
            .Add(component => component.Servers, [])
            .Add(component => component.CanCreate, false));

        var create = host.Find(".omni-resource-list__create");
        Assert.True(create.HasAttribute("disabled"));
        Assert.Equal("Droits insuffisants", create.GetAttribute("title"));
        Assert.Empty(host.FindAll(".omni-resource-list__empty-create"));

        create.Click();
        Assert.Equal(0, host.Instance.Created);
    }

    [Fact]
    public async Task ResourceList_ErrorMessage_ReplacesTheGridWithARetryState()
    {
        var host = Render<ResourceListTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Error = "Service injoignable."));

        var alert = host.Find(".omni-resource-list__error [role=alert]");
        Assert.Contains("Service injoignable.", alert.TextContent, StringComparison.Ordinal);
        Assert.Empty(host.FindAll(".omni-data-grid"));

        host.Find(".omni-resource-list__retry").Click();
        Assert.Equal(1, host.Instance.Retries);
        Assert.Single(host.FindAll(".omni-data-grid"));
    }

    [Fact]
    public void ResourceList_RowClick_ReachesTheHost()
    {
        var host = Render<ResourceListTestHost>();

        host.FindAll("td[data-omni-col='Name']")[1].Click();

        Assert.Equal("beta", host.Instance.Clicked?.Name);
    }

    [Fact]
    public void ResourceList_WithLoad_ReloadsFromTheFirstPageWithTheNewSearchAndCountsTheTotal()
    {
        var host = Render<ResourceListTestHost>(parameters => parameters.Add(component => component.UseLoad, true));

        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll("td[data-omni-col='Name']").Count));
        var loads = host.Instance.Loads;
        Assert.Equal("3 éléments", host.Find(".omni-resource-list__count").TextContent.Trim());

        host.Find(".omni-resource-list__search").Input("gam");

        host.WaitForAssertion(() =>
        {
            Assert.Equal("gamma", Assert.Single(host.FindAll("td[data-omni-col='Name']")).TextContent.Trim());
            Assert.Equal("1 élément", host.Find(".omni-resource-list__count").TextContent.Trim());
        });
        Assert.Equal("gam", host.Instance.LastLoadSearch);
        Assert.Equal(1, host.Instance.LastLoadPage);
        Assert.True(host.Instance.Loads > loads);

        // Rendering the host again does not reload: the loader handed to the grid stays the same.
        var settled = host.Instance.Loads;
        host.Render();
        Assert.Equal(settled, host.Instance.Loads);

        host.Find(".omni-resource-list__refresh").Click();
        host.WaitForAssertion(() => Assert.Equal(settled + 1, host.Instance.Loads));
        Assert.Equal(1, host.Instance.Refreshed);
    }

    [Fact]
    public async Task ResourceList_FailingLoad_ShowsTheRetryStateAndRetryLoadsAgain()
    {
        var host = Render<ResourceListTestHost>(parameters => parameters.Add(component => component.UseLoad, true));
        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll("td[data-omni-col='Name']").Count));

        host.Instance.LoadFailure = new InvalidOperationException("boom");
        host.Find(".omni-resource-list__refresh").Click();

        host.WaitForAssertion(() => Assert.Contains("La liste n'a pas pu être chargée.", host.Find(".omni-resource-list__error").TextContent, StringComparison.Ordinal));
        Assert.Empty(host.FindAll(".omni-data-grid"));

        host.Instance.LoadFailure = null;
        var loads = host.Instance.Loads;
        await host.InvokeAsync(() => host.Find(".omni-resource-list__retry").Click());

        host.WaitForAssertion(() => Assert.Equal(3, host.FindAll("td[data-omni-col='Name']").Count));
        Assert.True(host.Instance.Loads > loads);
        Assert.Empty(host.FindAll(".omni-resource-list__error"));
    }

    // ---- OmniEntityPicker ------------------------------------------------------------------------

    private static readonly Role[] Roles =
    [
        new(1, "Lecteur", "Lit tout"), new(2, "Éditeur", "Modifie les fiches"), new(3, "Administrateur", null), new(4, "Auditeur", "Lit les journaux")
    ];

    [Fact]
    public void EntityPicker_ListsTheFirstResultsAndTheChosenChips()
    {
        var searches = new List<string>();
        var picker = RenderPicker(searches, selected: [Roles[0]]);

        picker.WaitForAssertion(() => Assert.Equal(4, picker.FindAll(".omni-entity-picker__option").Count));
        Assert.Equal([string.Empty], searches);
        Assert.Equal("group", picker.Find(".omni-entity-picker").GetAttribute("role"));
        Assert.Equal("1 choisi", picker.Find(".omni-entity-picker__count").TextContent.Trim());
        Assert.Equal("Lecteur", Assert.Single(picker.FindAll(".omni-entity-picker__chip-text")).TextContent);
        Assert.Equal("Retirer Lecteur", picker.Find(".omni-entity-picker__remove").GetAttribute("aria-label"));
        var options = picker.FindAll(".omni-entity-picker__option");
        Assert.Equal("true", options[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", options[1].GetAttribute("aria-pressed"));
        Assert.Equal("Modifie les fiches", options[1].QuerySelector(".omni-entity-picker__option-description")!.TextContent);
    }

    [Fact]
    public void EntityPicker_TogglingAnOption_AddsAndRemovesItWithAnAnnouncement()
    {
        IReadOnlyList<Role>? reported = null;
        var picker = RenderPicker([], selected: [], changed: list => reported = list);
        picker.WaitForAssertion(() => Assert.Equal(4, picker.FindAll(".omni-entity-picker__option").Count));

        picker.FindAll(".omni-entity-picker__option")[2].Click();

        Assert.Equal([3], reported!.Select(role => role.Id));
        Assert.Equal("true", picker.FindAll(".omni-entity-picker__option")[2].GetAttribute("aria-pressed"));
        Assert.Equal("Administrateur ajouté à la sélection", picker.Find(".omni-entity-picker > [role=status]").TextContent);

        picker.FindAll(".omni-entity-picker__option")[2].Click();
        Assert.Empty(reported!);
        Assert.Equal("Administrateur retiré de la sélection", picker.Find(".omni-entity-picker > [role=status]").TextContent);
    }

    [Fact]
    public void EntityPicker_RemovingAChip_RemovesItAndKeepsTheFocusInTheSelection()
    {
        IReadOnlyList<Role>? reported = null;
        var picker = RenderPicker([], selected: [Roles[0], Roles[1]], changed: list => reported = list);

        picker.FindAll(".omni-entity-picker__remove")[0].Click();

        Assert.Equal([2], reported!.Select(role => role.Id));
        Assert.Equal("Éditeur", Assert.Single(picker.FindAll(".omni-entity-picker__chip-text")).TextContent);
        Assert.Contains(JSInterop.Invocations, invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EntityPicker_Search_AsksTheProviderAndMarksTheLetters()
    {
        var searches = new List<string>();
        var picker = RenderPicker(searches, selected: []);
        picker.WaitForAssertion(() => Assert.Equal(4, picker.FindAll(".omni-entity-picker__option").Count));

        picker.Find(".omni-entity-picker__search").Input("edit");

        picker.WaitForAssertion(() => Assert.Single(picker.FindAll(".omni-entity-picker__option")));
        Assert.Equal("edit", searches[^1]);
        Assert.Equal("Édit", picker.Find(".omni-entity-picker__match").TextContent);
    }

    [Fact]
    public void EntityPicker_TwoColumns_OffersOnlyWhatIsNotChosenAndListsTheChosen()
    {
        var picker = RenderPicker([], selected: [Roles[3]], layout: OmniEntityPickerLayout.TwoColumns);
        picker.WaitForAssertion(() => Assert.Equal(3, picker.FindAll(".omni-entity-picker__option").Count));

        Assert.Contains("omni-entity-picker--two-columns", picker.Find(".omni-entity-picker").ClassName, StringComparison.Ordinal);
        Assert.Equal(["Disponibles", "Choisis"], picker.FindAll(".omni-entity-picker__column-title").Select(title => title.TextContent));
        Assert.DoesNotContain(picker.FindAll(".omni-entity-picker__option-text"), option => option.TextContent == "Auditeur");
        Assert.Equal("Auditeur", picker.Find(".omni-entity-picker__chosen .omni-entity-picker__chip-text").TextContent);
    }

    [Fact]
    public void EntityPicker_SingleChoice_ReplacesThePreviousOne()
    {
        IReadOnlyList<Role>? reported = null;
        var picker = RenderPicker([], selected: [Roles[0]], changed: list => reported = list, multiple: false);
        picker.WaitForAssertion(() => Assert.Equal(4, picker.FindAll(".omni-entity-picker__option").Count));

        picker.FindAll(".omni-entity-picker__option")[1].Click();

        Assert.Equal([2], reported!.Select(role => role.Id));
    }

    [Fact]
    public void EntityPicker_FailedSearch_ShowsAnAlertAndRetries()
    {
        var attempts = 0;
        var picker = Render<OmniEntityPicker<Role, int>>(parameters => parameters
            .Add(component => component.Search, (_, _) =>
            {
                attempts++;
                return attempts == 1 ? Task.FromException<IReadOnlyList<Role>>(new InvalidOperationException("down")) : Task.FromResult<IReadOnlyList<Role>>(Roles);
            })
            .Add(component => component.KeySelector, role => role.Id)
            .Add(component => component.TextSelector, role => role.Name));

        picker.WaitForAssertion(() => Assert.Equal("La recherche a échoué.", picker.Find(".omni-entity-picker__state--error span").TextContent));
        picker.Find(".omni-entity-picker__state--error button").Click();

        picker.WaitForAssertion(() => Assert.Equal(4, picker.FindAll(".omni-entity-picker__option").Count));
        Assert.Equal(2, attempts);
    }

    [Fact]
    public void EntityPicker_ChipsBeyondTheLimit_WaitBehindAShowAllButton()
    {
        var picker = RenderPicker([], selected: Roles, maxChips: 2);

        Assert.Equal(2, picker.FindAll(".omni-entity-picker__chip-text").Count);
        var more = picker.Find(".omni-entity-picker__more");
        Assert.Equal("Tout afficher (+2)", more.TextContent);

        more.Click();
        Assert.Equal(4, picker.FindAll(".omni-entity-picker__chip-text").Count);
    }

    private IRenderedComponent<OmniEntityPicker<Role, int>> RenderPicker(
        List<string> searches,
        IReadOnlyList<Role> selected,
        Action<IReadOnlyList<Role>>? changed = null,
        OmniEntityPickerLayout layout = OmniEntityPickerLayout.Stacked,
        bool multiple = true,
        int? maxChips = null)
    {
        IRenderedComponent<OmniEntityPicker<Role, int>>? picker = null;
        picker = Render<OmniEntityPicker<Role, int>>(parameters => parameters
            .Add(component => component.Search, (text, _) =>
            {
                searches.Add(text);
                var compare = CultureInfo.GetCultureInfo("fr-FR").CompareInfo;
                IReadOnlyList<Role> found = [.. Roles.Where(role => compare.IndexOf(role.Name, text, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0)];
                return Task.FromResult(found);
            })
            .Add(component => component.KeySelector, role => role.Id)
            .Add(component => component.TextSelector, role => role.Name)
            .Add(component => component.DescriptionSelector, role => role.Description)
            .Add(component => component.Selected, selected)
            .Add(component => component.SelectedChanged, list =>
            {
                changed?.Invoke(list);
                picker!.Render(p => p.Add(component => component.Selected, list));
            })
            .Add(component => component.Layout, layout)
            .Add(component => component.Multiple, multiple)
            .Add(component => component.MaxVisibleChips, maxChips)
            .Add(component => component.DebounceMilliseconds, 0));
        return picker;
    }

    public sealed record Role(int Id, string Name, string? Description);

    // ---- OmniDynamicForm -------------------------------------------------------------------------

    [Fact]
    public void DynamicForm_DrawsEachKindWithItsLabelMarkerHelpAndDefault()
    {
        var host = Render<DynamicFormTestHost>();

        Assert.Equal("text", host.Find("#form-0-input").GetAttribute("type"));
        Assert.Equal("TEXTAREA", host.Find("#form-1-input").TagName);
        Assert.Equal("3", host.Find("#form-1-input").GetAttribute("rows"));
        var number = host.Find("#form-2-input");
        Assert.Equal("number", number.GetAttribute("type"));
        Assert.Equal("1", number.GetAttribute("min"));
        Assert.Equal("5", number.GetAttribute("max"));
        Assert.Equal("switch", host.Find("#form-3-input").GetAttribute("role"));
        Assert.Equal(["Europe ouest", "Europe centre"], host.FindAll("#form-4-input option").Skip(1).Select(option => option.TextContent));

        // Required marker, help text wired to the control.
        Assert.Single(host.FindAll("label[for='form-0-input'] .omni-label__required"));
        Assert.Equal("true", host.Find("#form-0-input").GetAttribute("aria-required"));
        Assert.Equal("form-0-description", host.Find("#form-0-input").GetAttribute("aria-describedby"));
        Assert.Equal("Le nom affiché.", host.Find("#form-0-description").TextContent);

        // The default is shown and reported to the host once.
        Assert.Equal("2", number.GetAttribute("value"));
        Assert.Equal("2", host.Instance.Values!["replicas"]);
        Assert.False(host.Instance.Values!.ContainsKey("confirm"));
        Assert.DoesNotContain("style=", host.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DynamicForm_Changes_FlowIntoTheBoundValuesAsStrings()
    {
        var host = Render<DynamicFormTestHost>();

        host.Find("#form-0-input").Input("srv-01");
        host.Find("#form-2-input").Change("3.5");
        host.Find("#form-3-input").Click();
        host.Find("#form-4-input").Change("1");

        var values = host.Instance.Values!;
        Assert.Equal("srv-01", values["name"]);
        Assert.Equal("3.5", values["replicas"]);
        Assert.Equal("true", values["confirm"]);
        Assert.Equal("eu-central", values["region"]);
        Assert.False(values.ContainsKey("notes"));
    }

    [Fact]
    public async Task DynamicForm_Validate_ShowsEveryMessageAndMarksTheControlsInvalid()
    {
        var host = Render<DynamicFormTestHost>();

        var valid = true;
        await host.InvokeAsync(() => valid = host.Instance.Form!.Validate());

        Assert.False(valid);
        Assert.Equal("Nom est obligatoire.", host.Find("#form-0-error").TextContent);
        Assert.Equal("true", host.Find("#form-0-input").GetAttribute("aria-invalid"));
        Assert.Equal("form-0-description form-0-error", host.Find("#form-0-input").GetAttribute("aria-describedby"));
        // An unanswered required switch is not answered "no" by default.
        Assert.Equal("Confirmer est obligatoire.", host.Find("#form-3-error").TextContent);
        Assert.Equal("Région est obligatoire.", host.Find("#form-4-error").TextContent);
        Assert.Empty(host.FindAll("#form-2-error"));

        host.Find("#form-0-input").Input("srv");
        host.Find("#form-3-input").Click();
        host.Find("#form-3-input").Click();
        host.Find("#form-4-input").Change("0");
        Assert.Empty(host.FindAll(".omni-form-field__error"));
        Assert.Equal("false", host.Instance.Values!["confirm"]);
        Assert.True(host.Instance.Form!.IsValid);
    }

    [Fact]
    public void DynamicForm_NumberOutsideItsBounds_SaysWhichBound()
    {
        var host = Render<DynamicFormTestHost>();

        host.Find("#form-2-input").Change("9");
        Assert.Equal("Répliques doit valoir au plus 5.", host.Find("#form-2-error").TextContent);

        host.Find("#form-2-input").Change("0");
        Assert.Equal("Répliques doit valoir au moins 1.", host.Find("#form-2-error").TextContent);
    }

    [Fact]
    public void DynamicForm_InsideAnEditForm_BlocksTheSubmitWhileAFieldIsInvalid()
    {
        var host = Render<DynamicFormTestHost>(parameters => parameters.Add(component => component.InEditForm, true));

        host.Find("form").Submit();
        Assert.Equal(0, host.Instance.ValidSubmits);
        Assert.Equal(1, host.Instance.InvalidSubmits);
        Assert.Equal("Nom est obligatoire.", host.Find("#form-0-error").TextContent);

        host.Find("#form-0-input").Input("srv");
        host.Find("#form-3-input").Click();
        host.Find("#form-4-input").Change("0");
        host.Find("form").Submit();
        Assert.Equal(1, host.Instance.ValidSubmits);
    }

    [Fact]
    public async Task DynamicForm_HostErrors_ShowUntilTheFieldChanges()
    {
        var host = Render<DynamicFormTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Errors = new Dictionary<string, string> { ["name"] = "Nom déjà pris." }));
        Assert.Equal("Nom déjà pris.", host.Find("#form-0-error").TextContent);

        host.Find("#form-0-input").Input("autre");
        Assert.Empty(host.FindAll("#form-0-error"));
    }

    [Fact]
    public async Task DynamicForm_ValuesFromTheHost_FillTheControls()
    {
        var host = Render<DynamicFormTestHost>();

        await host.InvokeAsync(() => host.Instance.Update(() => host.Instance.Values = new Dictionary<string, string>
        {
            ["name"] = "db-02", ["replicas"] = "4", ["confirm"] = "true", ["region"] = "eu-west"
        }));

        Assert.Equal("db-02", host.Find("#form-0-input").GetAttribute("value"));
        Assert.Equal("4", host.Find("#form-2-input").GetAttribute("value"));
        Assert.Equal("true", host.Find("#form-3-input").GetAttribute("aria-checked"));
        Assert.True(host.Instance.Form!.IsValid);
    }

    // ---- OmniStatusBadge -------------------------------------------------------------------------

    private enum RunState
    {
        Succeeded,
        Failed,
        Queued,
        Unknown
    }

    private static readonly OmniStatusMap<RunState?> RunStates = new()
    {
        { RunState.Succeeded, OmniBadgeVariant.Success, "Réussi", OmniIconName.CheckCircle },
        { RunState.Failed, new OmniStatus(OmniBadgeVariant.Danger, "Échoué") { Icon = OmniIconName.Error, Description = "Une étape a échoué." } },
        { RunState.Queued, new OmniStatus(OmniBadgeVariant.Neutral, "En file") { Fill = OmniBadgeFill.Outline } }
    };

    [Fact]
    public void StatusBadge_DrawsTheMappedVariantLabelIconAndExplanation()
    {
        var badge = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Failed)
            .Add(component => component.Map, RunStates));

        var inner = badge.Find(".omni-badge");
        Assert.Contains("omni-badge--danger", inner.ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-badge--filled", inner.ClassName, StringComparison.Ordinal);
        Assert.Equal("Échoué", badge.Find(".omni-status-badge__text").TextContent);
        Assert.Single(badge.FindAll(".omni-status-badge__icon"));
        Assert.Equal("Une étape a échoué.", badge.Find(".omni-status-badge").GetAttribute("title"));
        Assert.Empty(badge.FindAll(".omni-status-badge__stale"));
    }

    [Fact]
    public void StatusBadge_FallsBackForAnUnmappedValueAndDrawsADashForNull()
    {
        var unmapped = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Unknown)
            .Add(component => component.Map, RunStates));
        Assert.Equal("Unknown", unmapped.Find(".omni-status-badge__text").TextContent);
        Assert.Contains("omni-badge--neutral", unmapped.Find(".omni-badge").ClassName, StringComparison.Ordinal);

        var missing = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, null)
            .Add(component => component.Map, RunStates));
        Assert.Equal("-", missing.Find(".omni-status-badge__text").TextContent);

        var custom = new OmniStatusMap<string> { Fallback = new OmniStatus(OmniBadgeVariant.Warning, "Autre") };
        Assert.Equal("Autre", custom.Resolve("x").Text);
        Assert.Contains("omni-badge--outline", Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Queued)
            .Add(component => component.Map, RunStates)).Find(".omni-badge").ClassName, StringComparison.Ordinal);
    }

    [Fact]
    public void StatusMap_WithALocalizer_ReadsTheTextsAsResourceKeys()
    {
        var map = new OmniStatusMap<int> { Localizer = new KeyLocalizer() };
        map.Add(1, OmniBadgeVariant.Accent, "State_Running");

        var badge = Render<OmniStatusBadge<int>>(parameters => parameters
            .Add(component => component.Value, 1)
            .Add(component => component.Map, map));

        Assert.Equal("[State_Running]", badge.Find(".omni-status-badge__text").TextContent);
    }

    [Fact]
    public void StatusBadge_OlderThanItsThreshold_ReadsStale()
    {
        var badge = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Succeeded)
            .Add(component => component.Map, RunStates)
            .Add(component => component.Timestamp, Now.AddMinutes(-20))
            .Add(component => component.StaleAfter, TimeSpan.FromMinutes(15))
            .Add(component => component.Now, Now));

        Assert.True(badge.Instance.IsStale);
        Assert.Contains("omni-status-badge--stale", badge.Find(".omni-status-badge").ClassName, StringComparison.Ordinal);
        Assert.Contains("omni-badge--outline", badge.Find(".omni-badge").ClassName, StringComparison.Ordinal);
        Assert.Equal("périmé", badge.Find(".omni-status-badge__stale").TextContent.Trim());
        Assert.StartsWith("Dernière mise à jour : ", badge.Find(".omni-status-badge__stale").GetAttribute("title"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusBadge_TurnsStaleOnItsOwnWhenTheThresholdPasses()
    {
        var clock = new ManualClock(Now);
        var badge = Render<OmniStatusBadge<RunState?>>(parameters => parameters
            .Add(component => component.Value, RunState.Succeeded)
            .Add(component => component.Map, RunStates)
            .Add(component => component.Timestamp, Now.AddMinutes(-10))
            .Add(component => component.StaleAfter, TimeSpan.FromMinutes(15))
            .Add(component => component.StaleText, "ancien")
            .Add(component => component.TimeProvider, clock));

        Assert.Empty(badge.FindAll(".omni-status-badge__stale"));
        Assert.Single(clock.Pending);

        await badge.InvokeAsync(() => clock.Advance(TimeSpan.FromMinutes(6)));

        badge.WaitForAssertion(() => Assert.Equal("ancien", badge.Find(".omni-status-badge__stale").TextContent.Trim()));
        Assert.Empty(clock.Pending);

        badge.Instance.Dispose();
    }

    private sealed class KeyLocalizer : IStringLocalizer
    {
        public LocalizedString this[string name] => new(name, $"[{name}]");

        public LocalizedString this[string name, params object[] arguments] => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    /// <summary>A clock moved by hand that fires the timers falling due.</summary>
    internal sealed class ManualClock(DateTimeOffset now) : TimeProvider
    {
        private readonly List<Timer> _timers = [];
        private DateTimeOffset _now = now;

        public IReadOnlyList<Timer> Pending => [.. _timers.Where(timer => !timer.Disposed)];

        public override DateTimeOffset GetUtcNow() => _now;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new Timer(this, callback, state, _now + dueTime);
            _timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan by)
        {
            _now += by;
            foreach (var timer in _timers.Where(timer => !timer.Disposed && timer.Due <= _now).ToList())
            {
                timer.Fire();
            }
        }

        internal sealed class Timer(ManualClock owner, TimerCallback callback, object? state, DateTimeOffset due) : ITimer
        {
            public DateTimeOffset Due { get; private set; } = due;

            public bool Disposed { get; private set; }

            public void Fire()
            {
                Disposed = true;
                callback(state);
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                Due = owner._now + dueTime;
                Disposed = dueTime == Timeout.InfiniteTimeSpan;
                return true;
            }

            public void Dispose() => Disposed = true;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
