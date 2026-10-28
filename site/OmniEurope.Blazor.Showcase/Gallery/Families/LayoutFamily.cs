using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Layout family of the gallery: its pages, in menu order.</summary>
internal static class LayoutFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyLayout", OmniIconName.Rows, typeof(LayoutStrings),
    [
        new("stacks", "StacksTitle", "StacksDescription", OmniIconName.Rows,
            ["OmniStack", "OmniDivider"],
            [
                "Layout/StacksExample.razor", "Layout/StacksExample.razor.cs",
                "Layout/StackItem.cs",
                "Layout/LayoutBlock.razor", "Layout/LayoutBlock.razor.cs",
                "Layout/StackGapsExample.razor", "Layout/StackGapsExample.razor.cs",
                "Layout/StackAlignmentsExample.razor", "Layout/StackAlignmentsExample.razor.cs",
                "Layout/StackJustificationsExample.razor", "Layout/StackJustificationsExample.razor.cs",
                "Layout/StackOrientationsExample.razor", "Layout/StackOrientationsExample.razor.cs",
                "Layout/StackOverflowExample.razor", "Layout/StackOverflowExample.razor.cs",
                "Layout/StackDividersExample.razor", "Layout/StackDividersExample.razor.cs"
            ]),
        new("rows-columns", "RowsColumnsTitle", "RowsColumnsDescription", OmniIconName.Layers,
            ["OmniRow", "OmniColumn"],
            [
                "Layout/RowsColumnsExample.razor", "Layout/RowsColumnsExample.razor.cs",
                "Layout/LayoutBlock.razor", "Layout/LayoutBlock.razor.cs",
                "Layout/RowSplitsExample.razor", "Layout/RowSplitsExample.razor.cs",
                "Layout/RowGapsExample.razor", "Layout/RowGapsExample.razor.cs",
                "Layout/RowJustificationsExample.razor", "Layout/RowJustificationsExample.razor.cs",
                "Layout/RowAlignmentsExample.razor", "Layout/RowAlignmentsExample.razor.cs",
                "Layout/RowWrapExample.razor", "Layout/RowWrapExample.razor.cs",
                "Layout/RowResponsiveExample.razor", "Layout/RowResponsiveExample.razor.cs"
            ]),
        new("cards", "CardsTitle", "CardsDescription", OmniIconName.Cards,
            ["OmniCard"],
            [
                "Layout/CardsExample.razor", "Layout/CardsExample.razor.cs",
                "Layout/CardKinds.cs", "Layout/CardSample.cs",
                "Layout/CardsHeader.razor", "Layout/CardsHeader.razor.cs",
                "Layout/CardsBody.razor", "Layout/CardsBody.razor.cs",
                "Layout/CardsFooter.razor", "Layout/CardsFooter.razor.cs",
                "Layout/CardsGalleryExample.razor", "Layout/CardsGalleryExample.razor.cs",
                "Layout/DiscCardsExample.razor", "Layout/DiscCardsExample.razor.cs",
                "Layout/CardDensitiesExample.razor", "Layout/CardDensitiesExample.razor.cs",
                "Layout/OwnDensityExample.razor", "Layout/OwnDensityExample.razor.cs"
            ]),
        new("accordion", "AccordionTitle", "AccordionDescription", OmniIconName.ChevronDown,
            ["OmniAccordion", "OmniAccordionItem"],
            [
                "Layout/AccordionExample.razor", "Layout/AccordionExample.razor.cs",
                "Layout/AccordionAdvancedExample.razor", "Layout/AccordionAdvancedExample.razor.cs"
            ]),
        new("fieldsets", "FieldsetsTitle", "FieldsetsDescription", OmniIconName.Package,
            ["OmniFieldset"],
            [
                "Layout/FieldsetsExample.razor", "Layout/FieldsetsExample.razor.cs",
                "Layout/FieldsetsGalleryExample.razor", "Layout/FieldsetsGalleryExample.razor.cs",
                "Layout/FieldsetBoundExample.razor", "Layout/FieldsetBoundExample.razor.cs"
            ]),
        new("settings-sections", "SettingsSectionsTitle", "SettingsSectionsDescription", OmniIconName.Settings,
            ["OmniSettingsSection", "OmniSettingsTile"],
            [
                "Layout/SettingsSectionsExample.razor", "Layout/SettingsSectionsExample.razor.cs",
                "Layout/SettingsTileContent.cs",
                "Layout/SettingsSampleTile.razor", "Layout/SettingsSampleTile.razor.cs",
                "Layout/SettingsSectionsGalleryExample.razor", "Layout/SettingsSectionsGalleryExample.razor.cs"
            ]),
        new("page-shell", "PageShellTitle", "PageShellDescription", OmniIconName.Layout,
            ["OmniLayout", "OmniHeader", "OmniBody", "OmniMain", "OmniSkipLink", "OmniBootSplash", "OmniThemeScope",
             "OmniSidebar", "OmniSidebarToggle", "OmniPanelMenu", "OmniTabs", "OmniAppMenu", "OmniWindowControls"],
            [
                "Layout/PageShellExample.razor", "Layout/PageShellExample.razor.cs",
                "Layout/PageShellWidths.cs",
                "Layout/PageShellWidthExample.razor", "Layout/PageShellWidthExample.razor.cs",
                "Layout/SidebarShellExample.razor", "Layout/SidebarShellExample.razor.cs",
                "Layout/EndSidebarShellExample.razor", "Layout/EndSidebarShellExample.razor.cs",
                "Layout/OverlayRailShellExample.razor", "Layout/OverlayRailShellExample.razor.cs",
                "Layout/ScrollingTabsExample.razor", "Layout/ScrollingTabsExample.razor.cs",
                "Layout/ApplicationShellExample.razor", "Layout/ApplicationShellExample.razor.cs"
            ]),
        new("details", "DetailsTitle", "DetailsDescription", OmniIconName.Document,
            ["OmniDetailShell", "OmniPageHeader", "OmniDescriptionList", "OmniDescriptionItem", "OmniRelativeTime", "OmniEmptyState"],
            [
                "Layout/DetailsExample.razor", "Layout/DetailsExample.razor.cs",
                "Layout/PinnedActionsExample.razor",
                "Layout/EmptyStatesExample.razor"
            ]),
        new("stat-tiles", "StatTilesTitle", "StatTilesDescription", OmniIconName.ChartBar,
            ["OmniStatTile", "OmniFactTile", "OmniFactTileRow", "OmniTileGrid"],
            [
                "Layout/StatTilesExample.razor", "Layout/StatTilesExample.razor.cs",
                "Layout/StatTilesGalleryExample.razor", "Layout/StatTilesGalleryExample.razor.cs",
                "Layout/StatTilesColumnsExample.razor", "Layout/StatTilesColumnsExample.razor.cs",
                "Layout/FactTilesExample.razor",
                "Layout/FactTilesNarrowExample.razor",
                "Layout/TileGridExample.razor", "Layout/TileGridExample.razor.cs",
                "Layout/TileGridNaturalExample.razor",
                "Layout/TileGridUniformExample.razor", "Layout/TileGridUniformExample.razor.cs"
            ]),
        new("nav-tiles", "NavTilesTitle", "NavTilesDescription", OmniIconName.SquaresFour,
            ["OmniNavTile", "OmniTileGrid"],
            [
                "Layout/NavTilesExample.razor", "Layout/NavTilesExample.razor.cs",
                "Layout/NavTilesTonesExample.razor", "Layout/NavTilesTonesExample.razor.cs",
                "Layout/TileGridFluidExample.razor", "Layout/TileGridFluidExample.razor.cs"
            ])
    ]);
}
