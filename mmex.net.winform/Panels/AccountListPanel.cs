using mmex.net.core.Entities;
using mmex.net.core.Enums;
using mmex.net.core.Services;

namespace mmex.net.winform.Panels;

/// <summary>Left navigation panel — shows accounts grouped by type in a TreeView.</summary>
public class AccountListPanel : UserControl
{
    private readonly IAccountService _accountService;
    private readonly TreeView _tree;

    public event EventHandler<Account>? AccountSelected;

    public AccountListPanel(IAccountService accountService)
    {
        _accountService = accountService;

        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            HideSelection = false,
            ShowLines = true,
            ShowPlusMinus = true
        };
        _tree.AfterSelect += OnAfterSelect;

        Controls.Add(_tree);
    }

    public async Task LoadAsync()
    {
        var accounts = await _accountService.GetAllOpenAsync();

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        var groups = accounts
            .GroupBy(a => a.Type)
            .OrderBy(g => g.Key.ToString());

        foreach (var group in groups)
        {
            var groupLabel = group.Key == AccountType.CreditCard ? "Credit Card" : group.Key.ToString();
            var groupNode = new TreeNode(groupLabel) { Tag = group.Key };

            foreach (var acct in group.OrderBy(a => a.Name))
            {
                var node = new TreeNode(acct.Name) { Tag = acct };
                groupNode.Nodes.Add(node);
            }

            _tree.Nodes.Add(groupNode);
            groupNode.Expand();
        }

        _tree.EndUpdate();
    }

    private void OnAfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is Account account)
            AccountSelected?.Invoke(this, account);
    }
}
