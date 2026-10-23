using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// OmniUpload at its edges: too many or too large files, an upload cancelled or retried, removals refused
/// while locked or uploading, the reduced list unbound and with one file, the field display, the hint of
/// generic types, the error message of the host and the zone painted while a drag goes on.
/// </summary>
public sealed class UploadEdgeTests : OmniBunitContext
{
    private static InputFileContent Text(string name, string content = "hello") =>
        InputFileContent.CreateFromText(content, name, contentType: "text/plain");

    [Fact]
    public void MoreFilesThanTheBrowserListMayHold_AreRefusedTogether()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.MaximumFiles, 1));

        upload.FindComponent<InputFile>().UploadFiles(Text("a.txt"), Text("b.txt"), Text("c.txt"));

        Assert.Contains("1", upload.Find(".omni-upload__message--error").TextContent, StringComparison.Ordinal);
        Assert.Empty(upload.FindAll(".omni-upload__file"));
    }

    [Fact]
    public void FileOverTheSizeLimit_IsRefusedByName()
    {
        var upload = Render<OmniUpload>(parameters => parameters.Add(component => component.MaximumFileSize, 2L));

        upload.FindComponent<InputFile>().UploadFiles(Text("gros.txt"));

        Assert.Contains("gros.txt", upload.Find(".omni-upload__message--error").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadCancelled_OffersARetry_ThatAttachesTheFilesOnceItSucceeds()
    {
        IReadOnlyList<OmniUploadFile> files = [];
        var attempts = 0;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next)
            .Add(component => component.Upload, request => ++attempts == 1
                ? Task.Delay(Timeout.Infinite, request.CancellationToken)
                : Task.CompletedTask));

        // The selection is handed over only once the upload ends: it runs aside while the test cancels it.
        var input = upload.FindComponent<InputFile>();
        var selecting = Task.Run(() => input.UploadFiles(Text("note.txt")), Xunit.TestContext.Current.CancellationToken);
        upload.WaitForAssertion(() => Assert.Single(upload.FindAll(".omni-upload__cancel")));
        upload.Find(".omni-upload__cancel").Click();
        await selecting.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        upload.WaitForAssertion(() => Assert.Single(upload.FindAll(".omni-upload__retry")));
        Assert.Equal("Téléversement annulé.", upload.Find(".omni-upload__message").TextContent);

        upload.Find(".omni-upload__retry").Click();

        Assert.Equal(2, attempts);
        Assert.Equal("note.txt", Assert.Single(files).Name);
    }

    [Fact]
    public async Task Removal_IsRefusedWhileLockedOrUploading()
    {
        IReadOnlyList<OmniUploadFile> files = [new("a.txt", 2_000, "text/plain")];
        var removed = new List<OmniUploadFile>();
        var pending = new TaskCompletionSource();
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Multiple, true)
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next)
            .Add(component => component.OnFileRemove, file => removed.Add(file))
            .Add(component => component.Upload, _ => pending.Task));

        var input = upload.FindComponent<InputFile>();
        var selecting = Task.Run(() => input.UploadFiles(Text("b.txt")), Xunit.TestContext.Current.CancellationToken);
        upload.WaitForAssertion(() => Assert.Single(upload.FindAll(".omni-upload__cancel")));
        upload.Find(".omni-upload__remove").Click();
        pending.SetResult();
        await selecting.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Equal(2, files.Count);

        upload.Render(parameters => parameters.Add(component => component.Files, files).Add(component => component.Disabled, true));
        upload.Find(".omni-upload__remove").Click();

        Assert.Empty(removed);
        Assert.Equal("2 Ko", upload.Find(".omni-upload__file-size").TextContent);
    }

    [Fact]
    public void ReducedList_CountsOneFile_AndClearsAnUnboundSelection()
    {
        var upload = Render<OmniUpload>(parameters => parameters.Add(component => component.ReducedList, true));

        upload.FindComponent<InputFile>().UploadFiles(Text("seul.txt"));
        Assert.Equal("1 fichier", upload.Find(".omni-upload__count").TextContent);

        upload.Find(".omni-upload__clear").Click();
        Assert.Empty(upload.FindAll(".omni-upload__file"));
        Assert.Equal("Aucun fichier", upload.Find(".omni-upload__count").TextContent);

        // Nothing left: the cleared list refuses another clear.
        upload.Find(".omni-upload__clear").Click();
        Assert.Equal("Aucun fichier", upload.Find(".omni-upload__count").TextContent);
    }

    [Fact]
    public void FieldDisplay_ShowsTheChosenFile_AndItsHint()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Display, OmniUploadDisplay.Field)
            .Add(component => component.AllowedContentTypes, ["image/*", "pdf"]));

        Assert.StartsWith("image/*, PDF", upload.Find(".omni-upload__hint").TextContent, StringComparison.Ordinal);
        Assert.Contains("omni-upload__field-value--empty", upload.Find(".omni-upload__field-value").ClassList);

        upload.Render(parameters => parameters.Add(component => component.AllowedContentTypes, []));
        upload.FindComponent<InputFile>().UploadFiles(Text("plan.txt"));
        Assert.StartsWith("plan.txt", upload.Find(".omni-upload__field-value").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Retry_WithoutUploadOrFilesAnyMore_DoesNothing()
    {
        var attempts = 0;
        Func<OmniUploadRequest, Task> failing = _ =>
        {
            attempts++;
            throw new InvalidOperationException("down");
        };
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.ReducedList, true)
            .Add(component => component.Upload, failing));
        upload.FindComponent<InputFile>().UploadFiles(Text("note.txt"));

        upload.Render(parameters => parameters.Add(component => component.Upload, null));
        upload.Find(".omni-upload__retry").Click();
        upload.Render(parameters => parameters.Add(component => component.Upload, failing));
        upload.Find(".omni-upload__clear").Click();
        upload.Find(".omni-upload__retry").Click();

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void HostErrorMessage_ReplacesTheDefault()
    {
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.UploadErrorMessage, "Serveur indisponible.")
            .Add(component => component.Upload, _ => throw new InvalidOperationException("down")));

        upload.FindComponent<InputFile>().UploadFiles(Text("note.txt"));

        Assert.Equal("Serveur indisponible.", upload.Find(".omni-upload__message").TextContent);
    }

    [Fact]
    public void ZoneLockedDuringADrag_StopsPaintingIt()
    {
        var upload = Render<OmniUpload>();
        upload.Find(".omni-upload__zone").DragEnter();
        Assert.Contains("omni-upload--dragging", upload.Find(".omni-upload").ClassList);

        upload.Render(parameters => parameters.Add(component => component.Disabled, true));

        Assert.DoesNotContain("omni-upload--dragging", upload.Find(".omni-upload").ClassList);
    }

    [Fact]
    public async Task Cancel_IgnoredByACallbackThatReturnsNormally_IsNeitherCompletedNorAttached_ThenARetrySucceeds()
    {
        IReadOnlyList<OmniUploadFile> files = [];
        var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tokens = new List<CancellationToken>();
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => files = next)
            .Add(component => component.Upload, request =>
            {
                tokens.Add(request.CancellationToken);
                // The first attempt ignores its token, as a remote call that cannot be stopped would.
                return tokens.Count == 1 ? pending.Task : Task.CompletedTask;
            }));

        var input = upload.FindComponent<InputFile>();
        var selecting = Task.Run(() => input.UploadFiles(Text("a.txt")), Xunit.TestContext.Current.CancellationToken);
        upload.WaitForAssertion(() => Assert.Single(upload.FindAll(".omni-upload__cancel")));
        upload.Find(".omni-upload__cancel").Click();
        // The selection runs on another thread, so the click reaches the renderer's dispatcher asynchronously.
        upload.WaitForAssertion(() => Assert.True(tokens[0].IsCancellationRequested));
        pending.SetResult();
        await selecting.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        upload.WaitForAssertion(() => Assert.Single(upload.FindAll(".omni-upload__retry")));
        Assert.Equal("Téléversement annulé.", upload.Find(".omni-upload__message").TextContent);
        Assert.Empty(files);

        upload.Find(".omni-upload__retry").Click();

        // The retry reads the file again before it attaches it: awaited, not assumed synchronous.
        upload.WaitForAssertion(() =>
        {
            Assert.Equal(2, tokens.Count);
            Assert.Equal("a.txt", Assert.Single(files).Name);
        });
    }

    [Fact]
    public async Task Disposed_BeforeTheCallbackReturns_NeitherCompletesNorAttaches()
    {
        IReadOnlyList<OmniUploadFile> files = [];
        var changes = 0;
        var pending = new TaskCompletionSource();
        OmniUploadRequest? seen = null;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => { changes++; files = next; })
            .Add(component => component.Upload, request =>
            {
                seen = request;
                return pending.Task;
            }));

        var input = upload.FindComponent<InputFile>();
        var selecting = Task.Run(() => input.UploadFiles(Text("a.txt")), Xunit.TestContext.Current.CancellationToken);
        upload.WaitForAssertion(() => Assert.Single(upload.FindAll(".omni-upload__cancel")));
        await upload.InvokeAsync(() => upload.Instance.DisposeAsync().AsTask());
        Assert.True(seen!.CancellationToken.IsCancellationRequested);

        seen.ReportProgress(80);
        pending.SetResult();
        await selecting.WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);

        Assert.Equal(0, changes);
        Assert.Empty(files);
        Assert.DoesNotContain("Téléversement terminé", upload.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Disposed_WhileTheSelectionIsHandedOver_AttachesNothing()
    {
        IReadOnlyList<OmniUploadFile> files = [];
        var changes = 0;
        OmniUpload? field = null;
        var upload = Render<OmniUpload>(parameters => parameters
            .Add(component => component.Files, files)
            .Add(component => component.FilesChanged, next => { changes++; files = next; })
            .Add(component => component.OnFilesSelect, _ => field!.DisposeAsync().AsTask())
            .Add(component => component.Upload, _ => Task.CompletedTask));
        field = upload.Instance;

        // The first selection is handed over while the field goes away; a second one arrives after it.
        await upload.InvokeAsync(() => upload.FindComponent<InputFile>().UploadFiles(Text("a.txt")));
        await upload.InvokeAsync(() => upload.FindComponent<InputFile>().UploadFiles(Text("b.txt")));

        Assert.Equal(0, changes);
        Assert.Empty(files);
    }
}
