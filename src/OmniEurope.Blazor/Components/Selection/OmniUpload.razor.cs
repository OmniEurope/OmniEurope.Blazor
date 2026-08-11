namespace OmniEurope.Blazor.Components;

public partial class OmniUpload
{
    private CancellationTokenSource? _uploadCancellation;
    private IReadOnlyList<IBrowserFile> _files = Array.Empty<IBrowserFile>();
    private bool _uploading;
    private bool _canRetry;
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

    private string EffectiveUploadErrorMessage => string.IsNullOrWhiteSpace(UploadErrorMessage)
        ? Localize("UploadFailed")
        : UploadErrorMessage;

    private string? Accept => AllowedContentTypes.Count == 0 ? null : string.Join(',', AllowedContentTypes);
    private string MessageClass => Css("omni-upload__message", _hasError ? "omni-upload__message--error" : null);

    private async Task HandleFilesAsync(InputFileChangeEventArgs args)
    {
        Cancel();
        _canRetry = false;
        _progress = 0;
        _hasError = false;

        try
        {
            _files = Multiple ? args.GetMultipleFiles(MaximumFiles + 1) : [args.File];
        }
        catch (InvalidOperationException)
        {
            SetError(Localize("UploadMaximumFiles", MaximumFiles));
            return;
        }

        if (_files.Count > MaximumFiles)
        {
            SetError(Localize("UploadMaximumFiles", MaximumFiles));
            return;
        }

        var oversized = _files.FirstOrDefault(file => file.Size > MaximumFileSize);
        if (oversized is not null)
        {
            SetError(Localize("UploadFileTooLarge", oversized.Name));
            return;
        }

        var rejected = _files.FirstOrDefault(file => AllowedContentTypes.Count > 0
            && !AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase));
        if (rejected is not null)
        {
            SetError(Localize("UploadTypeRejected", rejected.Name));
            return;
        }

        await FilesSelected.InvokeAsync(_files);
        if (Upload is null)
        {
            _message = _files.Count == 1
                ? Localize("UploadOneSelected")
                : Localize("UploadManySelected", _files.Count);
            return;
        }

        await StartUploadAsync();
    }

    private async Task StartUploadAsync()
    {
        if (Upload is null || _files.Count == 0)
        {
            return;
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
                    return;
                }
            }
            await Upload(request);
            _progress = 100;
            _message = Localize("UploadCompleted");
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
    }

    private void ReportProgress(double percentage)
    {
        _progress = percentage;
        _ = InvokeAsync(StateHasChanged);
    }

    private void Cancel() => _uploadCancellation?.Cancel();
    private Task RetryAsync() => StartUploadAsync();

    private void SetError(string message)
    {
        _hasError = true;
        _message = message;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1024 * 1024 => Localize("UploadSizeMegabytes", bytes / 1024d / 1024d),
        >= 1024 => Localize("UploadSizeKilobytes", bytes / 1024d),
        _ => Localize("UploadSizeBytes", bytes)
    };

    public ValueTask DisposeAsync()
    {
        _uploadCancellation?.Cancel();
        _uploadCancellation?.Dispose();
        return ValueTask.CompletedTask;
    }
}
