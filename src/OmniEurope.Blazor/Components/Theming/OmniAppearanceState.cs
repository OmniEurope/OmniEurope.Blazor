using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Components;

/// <summary>
/// The user's appearance (mode, theme, palette, font, density, backdrop motion, text size, size of the
/// controls, content width), kept as one user preference through <see cref="OmniPreferences"/>, under
/// <see cref="PreferenceKey"/>. Registered as scoped by <c>AddOmniEuropeBlazor</c>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OmniThemeScope"/>, <see cref="OmniAppearanceWindow"/>, <see cref="OmniAppearanceSettings"/>,
/// <see cref="OmniAppMenu"/> and the outermost <see cref="OmniMain"/> follow this state on their own
/// when the host does not control them (see each component), so an application needs no appearance
/// service of its own: a change made in the window repaints the scope and is kept, and a change that
/// arrives from the host's <see cref="IOmniPreferenceStore"/> (another device) or from another tab
/// repaints the page without any action.
/// </para>
/// <para>
/// The defaults are the parameter defaults of those components: system mode, the shipped theme,
/// comfortable density, a moving backdrop, levels 5 and the host's centred column. The preference is
/// JSON with the enumerations and the catalogue entries (<see cref="OmniThemePresets"/>,
/// <see cref="OmniThemePalettes"/>, <see cref="OmniThemeFonts"/>) by name; a name the catalogue does not
/// hold, or a level out of 1 to 10, keeps that setting's default. A theme, palette or font built by the
/// host holds for the session but reads back as the default.
/// </para>
/// <para>
/// While the page is prerendered, <see cref="OmniPreferences"/> reads the store directly, so a host with a
/// store renders the user's look from the first byte (the attributes of the scope; the tokens follow
/// with the script, and the snapshot of <see cref="OmniThemeScope.SnapshotKey"/> covers the first paint).
/// </para>
/// </remarks>
public sealed class OmniAppearanceState : IDisposable
{
    /// <summary>The key of the preference that holds the appearance.</summary>
    public const string PreferenceKey = "omni.appearance";

    private readonly OmniPreferences _preferences;
    private readonly ILogger<OmniAppearanceState> _logger;
    private readonly Lock _gate = new();
    private AppearanceLook _look = AppearanceLook.Default;
    private Task? _loading;
    private int _localChanges;

    internal OmniAppearanceState(OmniPreferences preferences, IJSRuntime javaScript, ILogger<OmniAppearanceState> logger)
    {
        _preferences = preferences;
        _logger = logger;
        DocumentRoot = new AppearanceDocumentRoot(javaScript, logger);
        _preferences.Changed += OnPreferenceChangedAsync;
    }

    /// <summary>Where the components that follow the state log what they could not apply.</summary>
    internal ILogger Logger => _logger;

    /// <summary>
    /// Raised after any setting changed: set here, loaded, or received from the store or another tab. It
    /// may be raised off the renderer's thread; a component handler marshals its work with <c>InvokeAsync</c>.
    /// </summary>
    public event Action? Changed;

    /// <summary>Light, dark, or following the system; <see cref="OmniAppearance.System"/> by default.</summary>
    public OmniAppearance Appearance => _look.Appearance;

    /// <summary>The theme, or null for the shipped one.</summary>
    public OmniThemePreset? Preset => _look.Preset;

    /// <summary>The palette, or null for the theme's own.</summary>
    public OmniThemePalette? Palette => _look.Palette;

    /// <summary>The font, or null for the one the theme is drawn with.</summary>
    public OmniThemeFont? Font => _look.Font;

    /// <summary>The density; <see cref="OmniDensity.Comfortable"/> by default.</summary>
    public OmniDensity Density => _look.Density;

    /// <summary>Whether a theme that moves its colour field may move it; true by default.</summary>
    public bool BackdropMotion => _look.BackdropMotion;

    /// <summary>Text size, 1 to 10 with 5 as drawn, the default.</summary>
    public int TextSizeLevel => _look.TextSizeLevel;

    /// <summary>Size of the controls, 1 to 10 with 5 as drawn, the default.</summary>
    public int ControlSizeLevel => _look.ControlSizeLevel;

    /// <summary>Whether the page content takes the whole width rather than the host's centred column; false by default.</summary>
    public bool FullWidth => _look.FullWidth;

    /// <summary>The root attributes of the text and control sizes, applied while a scope follows the state.</summary>
    internal AppearanceDocumentRoot DocumentRoot { get; }

    /// <summary>
    /// Reads the preference once per scope (the components that follow the state call it); later calls
    /// return the same task. A preference that cannot be read keeps the defaults, and a change made
    /// before the read ends is not overwritten by it.
    /// </summary>
    /// <param name="cancellationToken">Stops waiting; the read itself goes on.</param>
    /// <returns>A task that completes once the preference is read.</returns>
    public Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // The task is published before the read starts: a component rendered by the change the read
        // raises asks again and waits for the same read instead of starting a second one.
        TaskCompletionSource? started = null;
        Task loading;
        lock (_gate)
        {
            if (_loading is null)
            {
                started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _loading = started.Task;
            }

            loading = _loading;
        }

        if (started is not null)
        {
            _ = CompleteLoadAsync(started);
        }

        return loading.WaitAsync(cancellationToken);
    }

    /// <summary>Sets the mode.</summary>
    /// <param name="appearance">A defined <see cref="OmniAppearance"/>.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetAppearanceAsync(OmniAppearance appearance)
    {
        Defined(appearance);
        return UpdateAsync(look => look with { Appearance = appearance });
    }

    /// <summary>Sets the theme; a new theme drops the palette and the font chosen for the previous one.</summary>
    /// <param name="preset">The theme, or null for the shipped one.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetPresetAsync(OmniThemePreset? preset) =>
        UpdateAsync(look => Equals(look.Preset, preset) ? look : look with { Preset = preset, Palette = null, Font = null });

    /// <summary>Sets the palette.</summary>
    /// <param name="palette">The palette, or null for the theme's own.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetPaletteAsync(OmniThemePalette? palette) => UpdateAsync(look => look with { Palette = palette });

    /// <summary>Sets the font.</summary>
    /// <param name="font">The font, or null for the theme's own.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetFontAsync(OmniThemeFont? font) => UpdateAsync(look => look with { Font = font });

    /// <summary>Sets the density.</summary>
    /// <param name="density">A defined <see cref="OmniDensity"/>.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetDensityAsync(OmniDensity density)
    {
        Defined(density);
        return UpdateAsync(look => look with { Density = density });
    }

    /// <summary>Lets a theme move its colour field, or holds it still.</summary>
    /// <param name="moves">Whether the field may move.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetBackdropMotionAsync(bool moves) => UpdateAsync(look => look with { BackdropMotion = moves });

    /// <summary>Sets the text size.</summary>
    /// <param name="level">The level, brought within 1 to 10.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetTextSizeLevelAsync(int level) => UpdateAsync(look => look with { TextSizeLevel = AppearanceLook.Clamp(level) });

    /// <summary>Sets the size of the controls.</summary>
    /// <param name="level">The level, brought within 1 to 10.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetControlSizeLevelAsync(int level) => UpdateAsync(look => look with { ControlSizeLevel = AppearanceLook.Clamp(level) });

    /// <summary>Sets the content width.</summary>
    /// <param name="fullWidth">True for the whole width, false for the host's centred column.</param>
    /// <returns>A task that completes once the preference is written.</returns>
    public Task SetFullWidthAsync(bool fullWidth) => UpdateAsync(look => look with { FullWidth = fullWidth });

    /// <summary>Stops following the preferences.</summary>
    public void Dispose() => _preferences.Changed -= OnPreferenceChangedAsync;

    private async Task CompleteLoadAsync(TaskCompletionSource started)
    {
        try
        {
            await LoadCoreAsync();
            started.SetResult();
        }
        catch (OperationCanceledException)
        {
            // The scope ended during the read: the defaults stay.
            started.SetResult();
        }
        catch (Exception exception)
        {
            // Anything else is a fault the components that wait for the read must see.
            started.SetException(exception);
        }
    }

    private async Task LoadCoreAsync()
    {
        var changesBefore = Volatile.Read(ref _localChanges);
        AppearancePreference? stored;
        try
        {
            stored = await _preferences.GetAsync<AppearancePreference>(PreferenceKey);
        }
        catch (JSDisconnectedException)
        {
            return;
        }
        catch (Exception exception) when (exception is JSException or ObjectDisposedException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "The appearance preference could not be read; the default appearance is kept.");
            return;
        }

        if (stored is not null && Volatile.Read(ref _localChanges) == changesBefore)
        {
            Apply(AppearanceLook.From(stored));
        }
    }

    private async Task UpdateAsync(Func<AppearanceLook, AppearanceLook> change)
    {
        AppearanceLook after;
        lock (_gate)
        {
            var before = _look;
            after = change(before);
            if (after == before)
            {
                return;
            }

            _look = after;
            _localChanges++;
        }

        Changed?.Invoke();
        try
        {
            await _preferences.SetAsync(PreferenceKey, after.ToPreference());
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone: nobody is left to see the look, and the store was not reached.
        }
    }

    /// <summary>A value written elsewhere: by the store (another device) or by another tab.</summary>
    private Task OnPreferenceChangedAsync(OmniPreferenceChangedEventArgs change)
    {
        // A local write is this state's own, already applied.
        if (change.Key == PreferenceKey && change.Origin != OmniPreferenceChangeOrigin.Local)
        {
            Apply(AppearanceLook.Parse(change.Value));
        }

        return Task.CompletedTask;
    }

    private void Apply(AppearanceLook look)
    {
        lock (_gate)
        {
            if (look == _look)
            {
                return;
            }

            _look = look;
        }

        Changed?.Invoke();
    }

    private static void Defined<T>(T value)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"{typeof(T).Name} has no such value.");
        }
    }
}
