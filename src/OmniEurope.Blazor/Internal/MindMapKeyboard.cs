using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The keyboard commands of an <see cref="OmniMindMap"/> canvas: arrows select the nearest node in
/// their direction (Shift moves the selection), Home the root, Enter or F2 renames or ends a link,
/// Escape closes, cancels or clears, Delete removes, + - 0 zoom and fit, Ctrl+Z / Ctrl+Y undo and
/// redo, N D C add, duplicate and centre, and the menu key or Shift+F10 opens the context menu.
/// </summary>
internal sealed class MindMapKeyboard(
    OmniMindMap owner,
    MindMapModel model,
    MindMapSelection selection,
    MindMapViewport viewport,
    MindMapEditor editor,
    MindMapContextMenu menu)
{
    /// <summary>Runs the command of a key pressed on the canvas.</summary>
    public Task KeyDownAsync(KeyboardEventArgs args)
    {
        viewport.TakeFromCanvas();
        if (args.AltKey)
        {
            return Task.CompletedTask;
        }

        return args.CtrlKey || args.MetaKey ? HistoryAsync(args) : CommandOf(args);
    }

    /// <summary>Ctrl+Z undoes; Ctrl+Y and Ctrl+Shift+Z redo.</summary>
    private Task HistoryAsync(KeyboardEventArgs args) =>
        Is(args, "z") && !args.ShiftKey ? editor.UndoAsync()
        : Is(args, "y") || Is(args, "z") ? editor.RedoAsync()
        : Task.CompletedTask;

    private Task CommandOf(KeyboardEventArgs args) => args.Key switch
    {
        "ArrowUp" => ArrowAsync(args, 0, -1),
        "ArrowDown" => ArrowAsync(args, 0, 1),
        "ArrowLeft" => ArrowAsync(args, -1, 0),
        "ArrowRight" => ArrowAsync(args, 1, 0),
        "Home" => editor.SelectNodeAsync(model.Root?.Id),
        "Enter" or "F2" => EnterAsync(),
        "Escape" => EscapeAsync(),
        "Delete" or "Backspace" => editor.DeleteSelectionAsync(),
        "+" or "=" => viewport.ZoomByAsync(OmniMindMap.ZoomStep),
        "-" or "_" => viewport.ZoomByAsync(1 / OmniMindMap.ZoomStep),
        "0" => viewport.FitAsync(),
        "ContextMenu" => menu.OpenFromKeyboardAsync(),
        "F10" when args.ShiftKey => menu.OpenFromKeyboardAsync(),
        _ => LetterAsync(args)
    };

    /// <summary>Ends the link being drawn on the selected node, else renames it.</summary>
    private Task EnterAsync() => selection.IsLinking && selection.NodeId is not null
        ? editor.PickLinkEndAsync(selection.NodeId)
        : owner.RequestRenameAsync();

    /// <summary>N adds a node, D duplicates the selection, C centres on it.</summary>
    private Task LetterAsync(KeyboardEventArgs args) =>
        Is(args, "n") ? editor.AddNodeAsync()
        : Is(args, "d") ? editor.DuplicateSelectionAsync()
        : Is(args, "c") ? owner.CenterOnSelectionAsync()
        : Task.CompletedTask;

    private static bool Is(KeyboardEventArgs args, string letter) =>
        string.Equals(args.Key, letter, StringComparison.OrdinalIgnoreCase);

    /// <summary>Closes the menu, else cancels the link being drawn, else clears the selection.</summary>
    private Task EscapeAsync()
    {
        if (menu.State is not null)
        {
            menu.Dismiss();
        }
        else if (selection.IsLinking)
        {
            editor.CancelLinkMode();
        }
        else if (selection.HasAny)
        {
            return editor.ClearSelectionAsync();
        }

        return Task.CompletedTask;
    }

    private async Task ArrowAsync(KeyboardEventArgs args, int directionX, int directionY)
    {
        if (args.ShiftKey)
        {
            await editor.MoveSelectionAsync(directionX * OmniMindMap.KeyboardMoveStep, directionY * OmniMindMap.KeyboardMoveStep);
            return;
        }

        await NavigateAsync(directionX, directionY);
    }

    /// <summary>
    /// Moves the selection to the nearest node lying in the pressed direction: within about 63
    /// degrees of it, closest along it, sideways distance counting double.
    /// </summary>
    private async Task NavigateAsync(int directionX, int directionY)
    {
        if (editor.SelectedNode is not { } current)
        {
            // The root, or the first drawn node when there is none (MindMapModel.Root).
            await editor.SelectNodeAsync(model.Root?.Id);
            return;
        }

        OmniMindMapNode? best = null;
        var bestScore = double.MaxValue;
        foreach (var candidate in model.Drawable)
        {
            if (string.Equals(candidate.Id, current.Id, StringComparison.Ordinal))
            {
                continue;
            }

            var dx = candidate.X - current.X;
            var dy = candidate.Y - current.Y;
            var along = (dx * directionX) + (dy * directionY);
            var across = Math.Abs((dx * directionY) - (dy * directionX));
            if (along <= 0 || across > along * 2)
            {
                continue;
            }

            var score = along + (across * 2);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best is not null)
        {
            await editor.SelectNodeAsync(best.Id);
        }
    }
}
