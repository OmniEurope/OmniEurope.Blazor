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
                "Selection/AutocompleteFreeTextExample.razor", "Selection/AutocompleteFreeTextExample.razor.cs",
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
                "Selection/RadioButtonsErrorExample.razor", "Selection/RadioButtonsErrorExample.razor.cs",
                "Selection/RatingExample.razor", "Selection/RatingExample.razor.cs"
            ]),
        new("select-bar", "SelectBarTitle", "SelectBarDescription", OmniIconName.Sliders,
            ["OmniSelectBar"],
            [
                "Selection/SelectBarExample.razor", "Selection/SelectBarExample.razor.cs",
                "Selection/SelectBarIconsExample.razor", "Selection/SelectBarIconsExample.razor.cs",
                "Selection/SelectBarGalleryExample.razor", "Selection/SelectBarGalleryExample.razor.cs"
            ]),
        new("selectable-cards", "SelectableCardsTitle", "SelectableCardsDescription", OmniIconName.Cards,
            ["OmniSelectableCard", "OmniSelectableCardGroup"],
            [
                "Selection/SelectableCardsExample.razor", "Selection/SelectableCardsExample.razor.cs",
                "Selection/SelectableCardsChoices.razor", "Selection/SelectableCardsChoices.razor.cs",
                "Selection/SelectableCardsSingleExample.razor", "Selection/SelectableCardsSingleExample.razor.cs",
                "Selection/SelectableCardsOptionsExample.razor", "Selection/SelectableCardsOptionsExample.razor.cs",
                "Selection/SelectableCardsLayoutExample.razor", "Selection/SelectableCardsLayoutExample.razor.cs"
            ]),
        new("dates", "DatesTitle", "DatesDescription", OmniIconName.Timer,
            ["OmniDatePicker", "OmniDateTimePicker", "OmniTimePicker"],
            [
                "Selection/DatesExample.razor", "Selection/DatesExample.razor.cs",
                "Selection/DatesGalleryExample.razor", "Selection/DatesGalleryExample.razor.cs",
                "Selection/DatesTimeExample.razor", "Selection/DatesTimeExample.razor.cs",
                "Selection/DatesFormExample.razor", "Selection/DatesFormExample.razor.cs",
                "Selection/DatesDialogExample.razor", "Selection/DatesDialogExample.razor.cs"
            ]),
        new("colors", "ColorsTitle", "ColorsDescription", OmniIconName.Palette,
            ["OmniColorPicker"],
            [
                "Selection/ColorsExample.razor", "Selection/ColorsExample.razor.cs",
                "Selection/ColorsGalleryExample.razor", "Selection/ColorsGalleryExample.razor.cs",
                "Selection/ColorPresets.cs"
            ]),
        new("slider", "SliderTitle", "SliderDescription", OmniIconName.Ruler,
            ["OmniSlider"],
            [
                "Selection/SliderExample.razor", "Selection/SliderExample.razor.cs",
                "Selection/SliderGalleryExample.razor", "Selection/SliderGalleryExample.razor.cs",
                "Selection/SliderSelectableMinimumExample.razor", "Selection/SliderSelectableMinimumExample.razor.cs",
                "Selection/SliderFormat.cs"
            ]),
        new("upload", "UploadTitle", "UploadDescription", OmniIconName.Upload,
            ["OmniUpload"],
            [
                "Selection/UploadExample.razor", "Selection/UploadExample.razor.cs",
                "Selection/UploadFormExample.razor", "Selection/UploadFormExample.razor.cs",
                "Selection/UploadEmptyFileExample.razor", "Selection/UploadEmptyFileExample.razor.cs"
            ])
    ]);
}
