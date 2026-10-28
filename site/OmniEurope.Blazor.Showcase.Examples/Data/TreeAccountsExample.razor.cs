using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Data;

/// <summary>A chart of accounts whose accounts are dragged onto one another to be filed under it.</summary>
public partial class TreeAccountsExample
{
    private OmniTree<string>? Tree { get; set; }

    private string? Moved { get; set; }

    // The open accounts, kept by the host so that the account a drop lands on opens to show it.
    private HashSet<string> Open { get; } = [];

    private List<Account> Accounts { get; } =
    [
        new("6", null, "Expenses"),
        new("60", "6", "Purchases"),
        new("607", "60", "Goods"),
        new("64", "6", "Staff"),
        new("7", null, "Income"),
        new("70", "7", "Sales"),
        new("706", "70", "Services")
    ];

    private IEnumerable<Account> ChildrenOf(string? parent) => Accounts.Where(account => account.Parent == parent);

    private void SetOpen(string number, bool open)
    {
        if (open)
        {
            Open.Add(number);
        }
        else
        {
            Open.Remove(number);
        }
    }

    // The tree reports the pair; the host files the account in its own data, which renders the tree again.
    private void MoveAccount(OmniTreeDropEventArgs<string> drop)
    {
        var index = Accounts.FindIndex(account => account.Number == drop.Dragged);
        Accounts[index] = Accounts[index] with { Parent = drop.Target };
        Open.Add(drop.Target);
        Moved = $"{Accounts[index].Label} filed under {Accounts.Single(account => account.Number == drop.Target).Label}";
    }

    private Task ExpandAsync() => Tree?.ExpandAllAsync() ?? Task.CompletedTask;

    private Task CollapseAsync() => Tree?.CollapseAllAsync() ?? Task.CompletedTask;

    /// <summary>An account of the chart: its number, the number of the account it is filed under, its name.</summary>
    private sealed record Account(string Number, string? Parent, string Name)
    {
        public string Label => $"{Number} - {Name}";
    }
}
