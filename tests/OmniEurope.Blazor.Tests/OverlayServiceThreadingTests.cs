using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A host may open or close a dialog from a background service while the renderer draws the stack
/// (L4-10 of the 2026-10-22 audit): the stack is guarded like the notifications are, and a reader never
/// sees it change under its enumeration.
/// </summary>
public sealed class OverlayServiceThreadingTests
{
    private const int Threads = 8;
    private const int Rounds = 1500;

    [Fact]
    public async Task Dialogs_OpenedAndClosedFromManyThreads_NeverBreakAReaderNorCorruptTheStack()
    {
        using var service = new OmniOverlayService();
        var request = new OmniDialogRequest("Titre", _ => { });
        var reading = true;
        var reader = Task.Run(() =>
        {
            while (Volatile.Read(ref reading))
            {
                foreach (var dialog in service.Dialogs)
                {
                    _ = dialog.Title;
                }

                _ = service.Dialog;
            }
        }, Xunit.TestContext.Current.CancellationToken);

        await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
        {
            for (var round = 0; round < Rounds; round++)
            {
                service.OpenDialog(request);
                service.CloseDialog();
            }
        }, Xunit.TestContext.Current.CancellationToken)));
        Volatile.Write(ref reading, false);
        await reader;

        Assert.Empty(service.Dialogs);
        Assert.Null(service.Dialog);
    }

    [Fact]
    public async Task DialogsAwaitedFromManyThreads_AreEachAnsweredOnce()
    {
        using var service = new OmniOverlayService();
        var waiting = await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
        {
            var tasks = new List<Task<object?>>();
            for (var round = 0; round < Rounds / 10; round++)
            {
                tasks.Add(service.OpenDialogAsync(new OmniDialogRequest("Titre", _ => { })));
            }

            return tasks;
        }, Xunit.TestContext.Current.CancellationToken)));
        var all = waiting.SelectMany(tasks => tasks).ToList();
        Assert.Equal(all.Count, service.Dialogs.Count);

        await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
        {
            for (var round = 0; round < Rounds / 10; round++)
            {
                service.CloseDialog("fait");
            }
        }, Xunit.TestContext.Current.CancellationToken)));

        await Task.WhenAll(all).WaitAsync(TimeSpan.FromSeconds(10), Xunit.TestContext.Current.CancellationToken);
        Assert.Empty(service.Dialogs);
    }
}
