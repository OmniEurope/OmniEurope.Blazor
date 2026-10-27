using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Selection family of the gallery: its pages, in menu order.</summary>
internal static class SelectionFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilySelection", OmniIconName.CheckCircle, typeof(SelectionStrings),
    [
        new("drop-downs", "DropDownsTitle", "DropDownsDescription", OmniIconName.ChevronDown,
            ["OmniDropDown"],
            [
                "Selection/DropDownExample.razor", "Selection/DropDownExample.razor.cs",
                "Selection/DropDownGalleryExample.razor", "Selection/DropDownGalleryExample.razor.cs"
            ]),
        new("autocomplete", "AutocompleteTitle", "AutocompleteDescription", OmniIconName.Search,
            ["OmniAutocomplete"],
            [
                "Selection/AutocompleteExample.razor", "Selection/AutocompleteExample.razor.cs",
                "Selection/AutocompleteGalleryExample.razor", "Selection/AutocompleteGalleryExample.razor.cs",
                "Selection/AutocompleteCities.cs"
            ]),
        new("multi-select", "MultiSelectTitle", "MultiSelectDescription", OmniIconName.Tag,
            ["OmniMultiSelect", "OmniListBox"],
            [
                "Selection/MultiSelectExample.razor", "Selection/MultiSelectExample.razor.cs",
                "Selection/MultiSelectGalleryExample.razor", "Selection/MultiSelectGalleryExample.razor.cs"
            ]),
        new("lists", "ListsTitle", "ListsDescription", OmniIconName.NumberedList,
            ["OmniListBox", "OmniCheckBoxList"],
            [
                "Selection/ListsExample.razor", "Selection/ListsExample.razor.cs",
                "Selection/ListsGalleryExample.razor", "Selection/ListsGalleryExample.razor.cs"
            ]),
        new("radio-buttons", "RadioButtonsTitle", "RadioButtonsDescription", OmniIconName.CheckCircle,
            ["OmniRadioButtonList", "OmniRating"],
            [
                "Selection/RadioButtonsExample.razor", "Selection/RadioButtonsExample.razor.cs",
                "Selection/RadioButtonsGalleryExample.razor", "Selection/RadioButtonsGalleryExample.razor.cs",
                "Selection/RatingExample.razor", "Selection/RatingExample.razor.cs"
            ])
    ]);
}
