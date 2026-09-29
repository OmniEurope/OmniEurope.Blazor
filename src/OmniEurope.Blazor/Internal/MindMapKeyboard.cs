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
    public async Task KeyDownAsync(KeyboardEventArgs args)
    {
        viewport.TakeFromCanvas();
        if (args.CtrlKey || args.MetaKey)
        {
            if (args.AltKey)
            {
                return;
            }

            if (Is(args, "z") && !args.ShiftKey)
            {
                await editor.UndoAsync();
            }
            else if (Is(args, "y") || (Is(args, "z") && args.ShiftKey))
            {
                await editor.RedoAsync();
            }

            return;
        }

        if (args.AltKey)
        {
            return;
        }

        switch (args.Key)
        {
            case "ArrowUp":
                await ArrowAsync(args, 0, -1);
                return;
            case "ArrowDown":
                await ArrowAsync(args, 0, 1);
                return;
            case "ArrowLeft":
                await ArrowAsync(args, -1, 0);
                return;
            case "ArrowRight":
                await ArrowAsync(args, 1, 0);
                return;
            case "Home":
                await editor.SelectNodeAsync(model.Root?.Id);
                return;
            case "Enter" or "F2":
                if (selection.IsLinking && selection.NodeId is not null)
                {
                    await editor.PickLinkEndAsync(selection.NodeId);
                }
                else
                {
                    await owner.RequestRenameAsync();
                }

                return;
            case "Escape":
                await EscapeAsync();
                return;
            case "Delete" or "Backspace":
                await editor.DeleteSelectionAsync();
                return;
            case "+" or "=":
                await viewport.ZoomByAsync(OmniMindMap.ZoomStep);
                return;
            case "-" or "_":
                await viewport.ZoomByAsync(1 / OmniMindMap.ZoomStep);
                return;
            case "0":
                await viewport.FitAsync();
                return;
            case "ContextMenu":
                await menu.OpenFromKeyboardAsync();
                return;
            case "F10" when args.ShiftKey:
                await menu.OpenFromKeyboardAsync();
                return;
        }

        if (Is(args, "n"))
        {
            await editor.AddNodeAsync();
        }
        else if (Is(args, "d"))
        {
            await editor.DuplicateSelectionAsync();
        }
        else if (Is(args, "c"))
        {
            await owner.CenterOnSelectionAsync();
        }
    }

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
            await editor.SelectNodeAsync((model.Root ?? model.Drawable.FirstOrDefault())?.Id);
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
