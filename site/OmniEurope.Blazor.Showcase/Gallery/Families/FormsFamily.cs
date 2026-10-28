using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Gallery.Families;

/// <summary>The Forms family of the gallery: its pages, in menu order.</summary>
internal static class FormsFamily
{
    public static GalleryFamily Family { get; } = new("GalleryFamilyForms", OmniIconName.Edit, typeof(FormsStrings),
    [
        new("text-fields", "TextFieldsTitle", "TextFieldsDescription", OmniIconName.Edit,
            ["OmniTextBox", "OmniTextArea", "OmniPassword", "OmniLabel", "OmniFormField"],
            [
                "Forms/TextFieldsExample.razor", "Forms/TextFieldsExample.razor.cs",
                "Forms/TextFieldTypesExample.razor", "Forms/TextFieldTypesExample.razor.cs",
                "Forms/TextFieldStatesExample.razor", "Forms/TextFieldStatesExample.razor.cs"
            ]),
        new("numbers", "NumbersTitle", "NumbersDescription", OmniIconName.NumberedList,
            ["OmniNumeric", "OmniTemplateForm"],
            [
                "Forms/NumbersExample.razor", "Forms/NumbersExample.razor.cs",
                "Forms/NumberUsesExample.razor", "Forms/NumberUsesExample.razor.cs"
            ]),
        new("checkboxes-switches", "TogglesTitle", "TogglesDescription", OmniIconName.CheckCircle,
            ["OmniCheckBox", "OmniSwitch"],
            [
                "Forms/TogglesExample.razor", "Forms/TogglesExample.razor.cs",
                "Forms/ToggleStatesExample.razor", "Forms/ToggleStatesExample.razor.cs"
            ]),
        new("validation", "ValidationTitle", "ValidationDescription", OmniIconName.ShieldCheck,
            [
                "OmniTemplateForm", "OmniRequiredValidator", "OmniEmailValidator", "OmniLengthValidator",
                "OmniDataAnnotationsValidator", "OmniDynamicForm", "OmniFormActions"
            ],
            [
                "Forms/SignUpValidationExample.razor", "Forms/SignUpValidationExample.razor.cs",
                "Forms/SignUpModel.cs", "Forms/ValidationMessagePlacement.cs", "Forms/ValidationSuccessFeedback.cs",
                "Forms/AnnotationsValidationExample.razor", "Forms/AnnotationsValidationExample.razor.cs",
                "Forms/ContactModel.cs",
                "Forms/DynamicFormExample.razor", "Forms/DynamicFormExample.razor.cs"
            ]),
        new("unsaved-changes", "UnsavedChangesTitle", "UnsavedChangesDescription", OmniIconName.Warning,
            ["OmniUnsavedChangesGuard", "OmniTemplateForm"],
            [
                "Forms/UnsavedChangesGuardExample.razor", "Forms/UnsavedChangesGuardExample.razor.cs",
                "Forms/UnsavedChangesFormExample.razor", "Forms/UnsavedChangesFormExample.razor.cs",
                "Forms/NoteModel.cs"
            ]),
        new("form-layouts", "FormLayoutsTitle", "FormLayoutsDescription", OmniIconName.Rows,
            ["OmniFormField", "OmniFieldset", "OmniFormActions", "OmniPassword", "OmniNumeric", "OmniDropDown",
             "OmniDatePicker", "OmniTimePicker", "OmniDateTimePicker", "OmniRadioButtonList", "OmniUpload"],
            [
                "Forms/VerticalFormExample.razor", "Forms/VerticalFormExample.razor.cs",
                "Forms/VerticalFormModel.cs",
                "Forms/VerticalFormChoicesExample.razor", "Forms/VerticalFormChoicesExample.razor.cs",
                "Forms/VerticalFormChoicesModel.cs"
            ])
    ]);
}
