using System.Linq.Expressions;
using System.Text.Json;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>Arranges an HTML editor and its surface script double, for the editor test classes.</summary>
internal static class HtmlEditorTestSetup
{
    /// <summary>Sets up the surface script double, whose <c>read</c> answers nothing.</summary>
    public static BunitJSModuleInterop SetupHtmlEditorSurface(this BunitJSInterop interop)
    {
        var module = interop.SetupModule(OmniModules.HtmlEditor);
        module.Setup<string?>("read", _ => true).SetResult(null);
        return module;
    }

    /// <summary>
    /// Renders an HTML editor bound to <paramref name="value"/> as <c>@bind-Value</c> would, with the
    /// optional change handler, then the parameters <paramref name="more"/> adds.
    /// </summary>
    public static IRenderedComponent<OmniHtmlEditor> RenderHtmlEditor(
        this BunitContext context,
        string value,
        Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>>? more = null,
        Action<string>? changed = null) =>
        context.Render<OmniHtmlEditor>(parameters =>
        {
            parameters.BindValue(() => value, changed);
            more?.Invoke(parameters);
        });

    /// <summary>
    /// Renders an HTML editor holding <paramref name="value"/> and bound to <paramref name="valueExpression"/>, each
    /// change added to <paramref name="changes"/> when given, then the parameters <paramref name="extra"/> adds.
    /// </summary>
    public static IRenderedComponent<OmniHtmlEditor> RenderTrackedHtmlEditor(
        this BunitContext context,
        string? value,
        Expression<Func<string>> valueExpression,
        List<string>? changes = null,
        Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>>? extra = null) =>
        context.Render<OmniHtmlEditor>(parameters =>
        {
            parameters.Add(editor => editor.Value, value).Add(editor => editor.ValueExpression, valueExpression);
            if (changes is not null)
            {
                parameters.Add(editor => editor.ValueChanged, html => changes.Add(html));
            }

            extra?.Invoke(parameters);
        });

    /// <summary>
    /// Renders an HTML editor bound to <paramref name="value"/> whose every change goes to <paramref name="changed"/>,
    /// then the parameters <paramref name="more"/> adds.
    /// </summary>
    public static IRenderedComponent<OmniHtmlEditor> RenderChangingHtmlEditor(
        this BunitContext context,
        string value,
        Action<string> changed,
        Action<ComponentParameterCollectionBuilder<OmniHtmlEditor>>? more = null) =>
        context.RenderHtmlEditor(value, more, changed);

    /// <summary>The options of the one <c>mount</c> call the surface script received.</summary>
    public static JsonElement SingleMountOptions(this BunitJSModuleInterop module) =>
        JsonSerializer.SerializeToElement(Assert.Single(module.Invocations["mount"]).Arguments[3]);

    /// <summary>Renders an editor on <c>&lt;p&gt;A&lt;/p&gt;</c> with the table import command, and opens its panel.</summary>
    public static async Task<IRenderedComponent<OmniHtmlEditor>> OpenTableImportAsync(this BunitContext context, OmniHtmlEditorExtension? extension = null)
    {
        var editor = context.RenderHtmlEditor("<p>A</p>", parameters =>
        {
            parameters.Add(component => component.Commands, [OmniHtmlEditorCommands.ImportTable]);
            if (extension is not null)
            {
                parameters.Add(component => component.Extensions, [extension]);
            }
        });
        await editor.Find("[data-command=import-table]").ClickAsync(new MouseEventArgs());
        return editor;
    }
}
