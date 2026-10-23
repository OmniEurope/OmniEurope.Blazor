namespace OmniEurope.Blazor.Components;

/// <summary>
/// A file field: a drop zone (or a field with a Browse button) that checks the chosen files against
/// its limits, hands them to <see cref="Upload"/> with progress and cancellation, and lists the files
/// it holds. Messages are localized.
/// </summary>
public partial class OmniUpload
{
    private readonly string _generatedId = $"omni-upload-{Guid.NewGuid():N}";
    private CancellationTokenSource? _uploadCancellation;
    private IReadOnlyList<IBrowserFile> _files = Array.Empty<IBrowserFile>();
    private bool _uploading;
    private bool _canRetry;
    private bool _dragging;
    private double _progress;
    private string? _message;
    private bool _hasError;
    private bool _showAll;
    private bool _disposed;

    /// <summary>Whether several files can be chosen at once and added to the list; off by default, a new file then replaces the list.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Whether the field is disabled: no file can be chosen, dropped or removed. Off by default.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the files held can be read and the field focused but not changed: it lists them and draws no
    /// file control, drop zone or remove button, so no file can be chosen, dropped or removed. Not dimmed.
    /// Off by default.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// The most files a selection may hold, 10 by default; bound and <see cref="Multiple"/>, the files
    /// already listed count too. A selection over it is refused whole with a localized error.
    /// </summary>
    [Parameter]
    public int MaximumFiles { get; set; } = 10;

    /// <summary>
    /// The largest size of one file, in bytes; 10 MiB by default. A selection holding a larger file is
    /// refused whole, and <see cref="OmniUploadRequest.OpenReadStream"/> reads no more than this.
    /// </summary>
    [Parameter]
    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// The content types accepted (<c>text/csv</c>), compared without regard to case; a selection
    /// holding another type is refused whole. A family is accepted with a wildcard subtype
    /// (<c>image/*</c>), as in the <c>accept</c> attribute of a file input. Empty (the default) accepts any type.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string> AllowedContentTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// The <c>accept</c> attribute of the file input, as the browser reads it: extensions and types,
    /// comma separated (<c>.csv,text/csv</c>). It only filters the file picker; the files are still
    /// checked against <see cref="AllowedContentTypes"/>. Null or blank derives it from that list, as before.
    /// </summary>
    [Parameter]
    public string? Accept { get; set; }

    /// <summary>
    /// Stores an accepted selection: it reads the files through the request, may report progress,
    /// and refuses them with <see cref="OmniUploadRequest.Reject"/> when a check of the application
    /// fails. An exception shows <see cref="UploadErrorMessage"/> and offers a retry. Null, the default,
    /// only lists the selection.
    /// </summary>
    [Parameter]
    public Func<OmniUploadRequest, Task>? Upload { get; set; }

    /// <summary>Raised with the files of a selection that passed the checks, before <see cref="Upload"/> runs.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> OnFilesSelect { get; set; }

    /// <summary>The message shown when <see cref="Upload"/> throws; null uses the localized default.</summary>
    [Parameter]
    public string? UploadErrorMessage { get; set; }

    /// <summary>
    /// The files the field holds, listed under the drop zone with their size and a remove button:
    /// files the application already stores, then those the user adds. Bind it
    /// (<c>@bind-Files</c>) to get that list: an accepted selection is appended to it, or replaces it
    /// when <see cref="Multiple"/> is off, once <see cref="Upload"/>, if any, has succeeded; the remove
    /// button takes an entry out. Unbound, the field lists the files of the last selection, without
    /// a remove button. <see cref="MaximumFiles"/> then counts the whole list.
    /// </summary>
    [Parameter]
    public IReadOnlyList<OmniUploadFile> Files { get; set; } = Array.Empty<OmniUploadFile>();

    /// <summary>
    /// Raised with the new list when an accepted selection is added or a file removed. Handling it
    /// binds <see cref="Files"/>: without it, the field lists the last selection only.
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyList<OmniUploadFile>> FilesChanged { get; set; }

    /// <summary>
    /// Raised with the entry the user removed, before <see cref="FilesChanged"/>: what an application
    /// that stores the file needs in order to delete it, where comparing two lists could not tell
    /// two files of the same name and size apart.
    /// </summary>
    [Parameter]
    public EventCallback<OmniUploadFile> OnFileRemove { get; set; }

    private bool IsBound => FilesChanged.HasDelegate;

    // Read-only, a list handed in through Files is shown even with no FilesChanged to write it back to.
    private bool ReadOnlyShowsFiles => IsBound || Files.Count > 0;

    private string EffectiveUploadErrorMessage => string.IsNullOrWhiteSpace(UploadErrorMessage)
        ? Localize("UploadFailed")
        : UploadErrorMessage;

    private string? EffectiveAccept => !string.IsNullOrWhiteSpace(Accept)
        ? Accept
        : AllowedContentTypes.Count == 0 ? null : string.Join(',', AllowedContentTypes);
    private string MessageClass => CssClassBuilder.Combine(["omni-upload__message", _hasError ? "omni-upload__message--error" : null]);
    private string HintId => $"{BaseId}-hint";

    private string RootClass => Css(
        "omni-upload",
        Disabled ? "omni-upload--disabled" : null,
        _dragging && !Disabled ? "omni-upload--dragging" : null);

    /// <summary>What the zone accepts, in the words of the limits the component enforces.</summary>
    private string Hint
    {
        get
        {
            var parts = new List<string>(3);
            if (AllowedContentTypes.Count > 0)
            {
                parts.Add(string.Join(", ", AllowedContentTypes.Select(ShortType)));
            }

            parts.Add(Localize("UploadHintMaximumSize", FormatSize(MaximumFileSize)));
            if (Multiple)
            {
                parts.Add(Localize("UploadHintMaximumFiles", MaximumFiles));
            }

            return string.Join(" · ", parts);
        }
    }

    private static string ShortType(string contentType)
    {
        var slash = contentType.IndexOf('/', StringComparison.Ordinal);
        var subtype = slash >= 0 ? contentType[(slash + 1)..] : contentType;
        return subtype == "*" ? contentType : subtype.ToUpperInvariant();
    }

    // A type matches an entry equal to it, or an entry of its family written with a wildcard subtype (image/*).
    private bool IsAllowedType(string? contentType) => AllowedContentTypes.Any(allowed =>
        string.Equals(allowed, contentType, StringComparison.OrdinalIgnoreCase)
        || (allowed.EndsWith("/*", StringComparison.Ordinal) && contentType is not null
            && contentType.AsSpan().StartsWith(allowed.AsSpan(0, allowed.Length - 1), StringComparison.OrdinalIgnoreCase)));

    private void SetDragging(bool dragging) => _dragging = dragging && !Disabled && !_uploading;

    private async Task HandleFilesAsync(InputFileChangeEventArgs args)
    {
        // A picker closed without a choice reports no file: there is nothing to read, check or send.
        if (args.FileCount == 0)
        {
            return;
        }

        Cancel();
        _dragging = false;
        _canRetry = false;
        _progress = 0;
        _hasError = false;

        try
        {
            _files = Multiple ? args.GetMultipleFiles(MaximumFiles + 1) : [args.File];
        }
        catch (InvalidOperationException)
        {
            _files = Array.Empty<IBrowserFile>();
            SetError(Localize("UploadMaximumFiles", MaximumFiles));
            return;
        }

        var held = IsBound && Multiple ? Files.Count : 0;
        if (held + _files.Count > MaximumFiles)
        {
            _files = Array.Empty<IBrowserFile>();
            SetError(Localize("UploadMaximumFiles", MaximumFiles));
            return;
        }

        var oversized = _files.FirstOrDefault(file => file.Size > MaximumFileSize);
        if (oversized is not null)
        {
            _files = Array.Empty<IBrowserFile>();
            SetError(Localize("UploadFileTooLarge", oversized.Name));
            return;
        }

        var rejected = _files.FirstOrDefault(file => AllowedContentTypes.Count > 0 && !IsAllowedType(file.ContentType));
        if (rejected is not null)
        {
            _files = Array.Empty<IBrowserFile>();
            SetError(Localize("UploadTypeRejected", rejected.Name));
            return;
        }

        await OnFilesSelect.InvokeAsync(_files);
        if (_disposed)
        {
            return;
        }

        if (Upload is null)
        {
            _message = _files.Count == 1
                ? Localize("UploadOneSelected")
                : Localize("UploadManySelected", _files.Count);
            await AttachAsync();
            return;
        }

        if (await StartUploadAsync())
        {
            await AttachAsync();
        }
    }

    /// <summary>Adds the accepted selection to the bound list; unbound, the selection is the list.</summary>
    private async Task AttachAsync()
    {
        if (_disposed || !IsBound || _files.Count == 0)
        {
            return;
        }

        var added = _files.Select(file => new OmniUploadFile(file.Name, file.Size, file.ContentType));
        IReadOnlyList<OmniUploadFile> next = Multiple ? [.. Files, .. added] : [.. added.Take(1)];
        _files = Array.Empty<IBrowserFile>();
        await FilesChanged.InvokeAsync(next);
    }

    private async Task RemoveAsync(int index)
    {
        // The index comes from the rendered list, never negative; a host that replaced or unbound the list
        // since that render can still send a click already on its way.
        if (!IsBound || Disabled || ReadOnly || _uploading || index >= Files.Count)
        {
            return;
        }

        var removed = Files[index];
        IReadOnlyList<OmniUploadFile> next = [.. Files.Take(index), .. Files.Skip(index + 1)];
        _hasError = false;
        _progress = 0;
        _message = Localize("UploadRemoved", removed.Name);
        await OnFileRemove.InvokeAsync(removed);
        await FilesChanged.InvokeAsync(next);
    }

    private async Task<bool> StartUploadAsync()
    {
        if (_disposed || Upload is null || _files.Count == 0)
        {
            return false;
        }

        // Each attempt keeps its own source: a cancelled attempt that returns late is judged by its own
        // token, never by the one of the attempt that replaced it.
        _uploadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _uploadCancellation = cancellation;
        _uploading = true;
        _canRetry = false;
        _hasError = false;
        _message = Localize("UploadInProgress");

        try
        {
            var request = new OmniUploadRequest(_files, token, MaximumFileSize, percentage => ReportProgress(cancellation, percentage));
            await Upload(request);
            // Cancellation is cooperative: a callback that ignores its token and returns normally has
            // still been cancelled, and is neither announced as completed nor attached.
            if (token.IsCancellationRequested)
            {
                return Cancelled(cancellation);
            }

            if (request.Rejection is { } rejection)
            {
                SetError(rejection);
                return false;
            }

            _progress = 100;
            _message = Localize("UploadCompleted");
            return true;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return Cancelled(cancellation);
        }
        catch (Exception exception)
        {
            Microsoft.Extensions.Logging.LoggerExtensions.LogError(Logger, exception, "The upload callback failed.");
            if (IsCurrent(cancellation))
            {
                SetError(EffectiveUploadErrorMessage);
                _canRetry = true;
            }
        }
        finally
        {
            if (IsCurrent(cancellation))
            {
                _uploading = false;
            }
        }

        return false;
    }

    /// <summary>Whether this attempt still owns the field: neither replaced by another nor outlived by the field.</summary>
    private bool IsCurrent(CancellationTokenSource cancellation) => !_disposed && ReferenceEquals(_uploadCancellation, cancellation);

    /// <summary>A cancelled attempt offers a retry, unless a newer attempt or the disposal took the field over.</summary>
    private bool Cancelled(CancellationTokenSource cancellation)
    {
        if (IsCurrent(cancellation))
        {
            _message = Localize("UploadCancelled");
            _canRetry = true;
        }

        return false;
    }

    private void ReportProgress(CancellationTokenSource cancellation, double percentage)
    {
        if (!IsCurrent(cancellation) || cancellation.IsCancellationRequested)
        {
            return;
        }

        _progress = percentage;
        _ = InvokeAsync(StateHasChanged);
    }

    private void Cancel()
    {
        if (!_disposed)
        {
            _uploadCancellation?.Cancel();
        }
    }

    private async Task RetryAsync()
    {
        if (await StartUploadAsync())
        {
            await AttachAsync();
        }
    }

    private void SetError(string message)
    {
        _hasError = true;
        _message = message;
    }

    // The shared size writer, with the unit symbols read through this component's localizer so a host's text override still applies.
    private string FormatSize(long bytes) => OmniValueFormatter.FileSize(bytes, CultureInfo.CurrentUICulture, key => Localize(key));

    /// <summary>A picture for images, a page for anything else: enough to tell a list apart at a glance.</summary>
    private static OmniIconName IconFor(string name, string? contentType) =>
        contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
        || name.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".gif", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            ? OmniIconName.Image
            : OmniIconName.Document;

    /// <summary>
    /// Cancels an upload still running and releases its cancellation source. A callback that returns
    /// afterwards no longer moves the progress, changes the message or attaches its files.
    /// </summary>
    /// <returns>A completed task.</returns>
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        // Marked first: the cancellation below may run a callback's continuation inline.
        _disposed = true;
        _uploadCancellation?.Cancel();
        _uploadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// How the picker is offered: the drop zone (the default), or, for a single file, a read-only field
    /// welded to a Browse button, which a click on either or Enter opens. With <see cref="Multiple"/>
    /// the zone is kept. The field shows the chosen file and has no list under it.
    /// </summary>
    [Parameter]
    public OmniUploadDisplay Display { get; set; }

    /// <summary>
    /// Keeps a long list short: only the three most recent files are shown, and under the list, always,
    /// the count, "Show all (n)" or "Show less" when more files are held, and "Remove all". Off by
    /// default: every file is listed, as before.
    /// </summary>
    [Parameter]
    public bool ReducedList { get; set; }

    /// <summary>How many of the most recent files a reduced list shows.</summary>
    private const int ReducedCount = 3;

    private bool IsField => Display == OmniUploadDisplay.Field && !Multiple;
    private string BaseId => Id ?? _generatedId;
    private string ListId => $"{BaseId}-list";
    private string FieldValueId => $"{BaseId}-value";
    private int ListedCount => IsBound ? Files.Count : _files.Count;
    private bool Collapsible => ReducedList && ListedCount > ReducedCount;

    /// <summary>The index of the first file listed: the most recent ones are the last appended.</summary>
    private int FirstShown => Collapsible && !_showAll ? ListedCount - ReducedCount : 0;

    private string CountText => ListedCount switch
    {
        0 => Localize("UploadFileCountNone"),
        1 => Localize("UploadFileCountOne"),
        var count => Localize("UploadFileCountMany", count)
    };

    /// <summary>The chosen file, name and size, or null when the field holds none.</summary>
    private string? FieldText
    {
        get
        {
            if (IsBound)
            {
                return Files.Count == 0 ? null : Localize("UploadFieldValue", Files[^1].Name, FormatSize(Files[^1].Size));
            }

            return _files.Count == 0 ? null : Localize("UploadFieldValue", _files[0].Name, FormatSize(_files[0].Size));
        }
    }

    private string FieldValueClass => CssClassBuilder.Combine([
        "omni-input",
        "omni-upload__field-value",
        FieldText is null ? "omni-upload__field-value--empty" : null]);

    private void ToggleShowAll() => _showAll = !_showAll;

    /// <summary>
    /// Empties the list. Bound, each entry is reported through <see cref="OnFileRemove"/> before the
    /// empty list is sent, as a removal one by one would; unbound, the listed selection is cleared. Its
    /// button is disabled while locked, uploading or empty, and a disabled OmniButton raises no click.
    /// </summary>
    private async Task RemoveAllAsync()
    {
        _showAll = false;
        _hasError = false;
        _progress = 0;
        _message = Localize("UploadRemovedAll");
        if (!IsBound)
        {
            _files = Array.Empty<IBrowserFile>();
            return;
        }

        var removed = Files;
        foreach (var file in removed)
        {
            await OnFileRemove.InvokeAsync(file);
        }

        await FilesChanged.InvokeAsync(Array.Empty<OmniUploadFile>());
    }
}
