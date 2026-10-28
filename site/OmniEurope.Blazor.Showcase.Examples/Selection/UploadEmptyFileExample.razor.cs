using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>A file field that refuses an empty file from its Upload callback, through <see cref="OmniUploadRequest.Reject"/>.</summary>
public partial class UploadEmptyFileExample
{
    /// <summary>Refuses the selection when one of its files has no content; nothing is read or sent otherwise.</summary>
    private Task RefuseEmptyAsync(OmniUploadRequest request)
    {
        var empty = request.Files.FirstOrDefault(file => file.Size == 0);
        if (empty is not null)
        {
            request.Reject($"The file {empty.Name} is empty.");
        }

        return Task.CompletedTask;
    }
}
