using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class TreeDemo
{
    [Inject]
    private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private IReadOnlyList<string> Selected { get; set; } = ["bru"];

    private List<string> Archives { get; } = [];

    private List<Account> Accounts { get; set; } = [];

    private OmniTree<string>? AccountTree { get; set; }

    private string? Moved { get; set; }

    // The open accounts, kept by the page so the account a drop lands on opens to show it.
    private HashSet<string> Open { get; } = [];

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

    protected override void OnInitialized() => Accounts =
    [
        new("6", null, Text["DemoGridTreeExpenses"]),
        new("60", "6", Text["DemoGridTreePurchases"]),
        new("607", "60", Text["DemoGridTreeGoods"]),
        new("64", "6", Text["DemoGridTreeStaff"]),
        new("7", null, Text["DemoGridTreeIncome"]),
        new("70", "7", Text["DemoGridTreeSales"]),
        new("706", "70", Text["DemoGridTreeServices"])
    ];

    private IEnumerable<Account> ChildrenOf(string? parent) => Accounts.Where(account => account.Parent == parent);

    // The tree reports the pair; the page moves the account in its own data, which renders the tree again.
    private void MoveAccount(OmniTreeDropEventArgs<string> drop)
    {
        var index = Accounts.FindIndex(account => account.Number == drop.Dragged);
        Accounts[index] = Accounts[index] with { Parent = drop.Target };
        Open.Add(drop.Target);
        Moved = Text["DemoTreeMoved", Accounts[index].Label, Accounts.Single(account => account.Number == drop.Target).Label];
    }

    private Task ExpandAccountsAsync() => AccountTree?.ExpandAllAsync() ?? Task.CompletedTask;

    private Task CollapseAccountsAsync() => AccountTree?.CollapseAllAsync() ?? Task.CompletedTask;

    /// <summary>An account of the chart: its number, the number of the account it is filed under, its name.</summary>
    private sealed record Account(string Number, string? Parent, string Name)
    {
        public string Label => $"{Number} - {Name}";
    }

    /// <summary>
    /// Stands in for a branch fetched on demand: the tree waits on the task before it draws the
    /// level, so a slow source cannot leave a half-open node behind.
    /// </summary>
    private async Task LoadArchivesAsync(CancellationToken cancellationToken)
    {
        if (Archives.Count > 0)
        {
            return;
        }

        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        Archives.AddRange(["2024", "2025", "2026"]);
    }
}
