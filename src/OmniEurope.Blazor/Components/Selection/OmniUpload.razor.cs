namespace OmniEurope.Blazor.Components;

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

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public string? InputId { get; set; }

    [Parameter]
    public int MaximumFiles { get; set; } = 10;

    [Parameter]
    public long MaximumFileSize { get; set; } = 10 * 1024 * 1024;

    [Parameter]
    public IReadOnlyList<string> AllowedContentTypes { get; set; } = Array.Empty<string>();

    [Parameter]
    public Func<OmniUploadRequest, Task>? Upload { get; set; }

    [Parameter]
    public Func<OmniUploadRequest, Task<string?>>? Validate { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<IBrowserFile>> FilesSelected { get; set; }

    [Parameter]
    public string UploadErrorMessage { get; set; } = string.Empty;

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

    [Parameter]
    public EventCallback<IReadOnlyList<OmniUploadFile>> FilesChanged { get; set; }

    /// <summary>
    /// Raised with the entry the user removed, before <see cref="FilesChanged"/>: what an application
    /// that stores the file needs in order to delete it, where comparing two lists could not tell
    /// two files of the same name and size apart.
    /// </summary>
    [Parameter]
    public EventCallback<OmniUploadFile> FileRemoved { get; set; }

    private bool IsBound => FilesChanged.HasDelegate;

    private string EffectiveUploadErrorMessage => string.IsNullOrWhiteSpace(UploadErrorMessage)
        ? Localize("UploadFailed")
        : UploadErrorMessage;

    private string? Accept => AllowedContentTypes.Count == 0 ? null : string.Join(',', AllowedContentTypes);
    private string MessageClass => CssClassBuilder.Combine(["omni-upload__message", _hasError ? "omni-upload__message--error" : null]);
    private string HintId => $"{InputId ?? Id ?? _generatedId}-hint";

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

    private void SetDragging(bool dragging) => _dragging = dragging && !Disabled && !_uploading;

    private async Task HandleFilesAsync(InputFileChangeEventArgs args)
    {
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

        var rejected = _files.FirstOrDefault(file => AllowedContentTypes.Count > 0
            && !AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase));
        if (rejected is not null)
        {
            _files = Array.Empty<IBrowserFile>();
            SetError(Localize("UploadTypeRejected", rejected.Name));
            return;
        }

        await FilesSelected.InvokeAsync(_files);
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
        if (!IsBound || _files.Count == 0)
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
        if (!IsBound || Disabled || _uploading || index < 0 || index >= Files.Count)
        {
            return;
        }

        var removed = Files[index];
        IReadOnlyList<OmniUploadFile> next = [.. Files.Take(index), .. Files.Skip(index + 1)];
        _hasError = false;
        _progress = 0;
        _message = Localize("UploadRemoved", removed.Name);
        await FileRemoved.InvokeAsync(removed);
        await FilesChanged.InvokeAsync(next);
    }

    private async Task<bool> StartUploadAsync()
    {
        if (Upload is null || _files.Count == 0)
        {
            return false;
        }

        _uploadCancellation?.Dispose();
        _uploadCancellation = new CancellationTokenSource();
        _uploading = true;
        _canRetry = false;
        _hasError = false;
        _message = Localize("UploadInProgress");

        try
        {
            var request = new OmniUploadRequest(_files, _uploadCancellation.Token, MaximumFileSize, ReportProgress);
            if (Validate is not null)
            {
                var validationMessage = await Validate(request);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    SetError(validationMessage);
                    return false;
                }
            }
            await Upload(request);
            _progress = 100;
            _message = Localize("UploadCompleted");
            return true;
        }
        catch (OperationCanceledException) when (_uploadCancellation.IsCancellationRequested)
        {
            _message = Localize("UploadCancelled");
            _canRetry = true;
        }
        catch (Exception exception)
        {
            Microsoft.Extensions.Logging.LoggerExtensions.LogError(Logger, exception, "The upload callback failed.");
            SetError(EffectiveUploadErrorMessage);
            _canRetry = true;
        }
        finally
        {
            _uploading = false;
        }

        return false;
    }

    private void ReportProgress(double percentage)
    {
        _progress = percentage;
        _ = InvokeAsync(StateHasChanged);
    }

    private void Cancel() => _uploadCancellation?.Cancel();

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

    private string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => Localize("UploadSizeMegabytes", bytes / 1024d / 1024d),
        >= 1024 => Localize("UploadSizeKilobytes", bytes / 1024d),
        _ => Localize("UploadSizeBytes", bytes)
    };

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

    public ValueTask DisposeAsync()
    {
        _uploadCancellation?.Cancel();
        _uploadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
