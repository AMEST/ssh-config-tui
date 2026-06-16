using SshConfigTui.Application;
using Terminal.Gui;

namespace SshConfigTui.UI;

public class GroupTreeView : TreeView
{
    private readonly ApplicationService _appService;
    private bool _suppressChange;

    public string? SelectedGroup { get; private set; } = "All";

    public event Action<string>? GroupSelected;

    public GroupTreeView(ApplicationService appService)
    {
        _appService = appService;

        Height = Dim.Fill();
        Width = Dim.Fill();
        CanFocus = true;

        AspectGetter = (ITreeNode node) => node.Text ?? "";

        SelectionChanged += OnGroupSelected;
    }

    public void RefreshGroups()
    {
        _suppressChange = true;
        var groups = _appService.GroupService.GetGroups();
        ClearObjects();
        var root = new TreeNode("Groups");
        foreach (var g in groups)
            root.Children.Add(new TreeNode(g));
        AddObject(root);
        ExpandAll();
        _suppressChange = false;
    }

    public bool HasGroup(string group)
    {
        var root = Objects.FirstOrDefault();
        return root?.Children.Any(c => c.Text == group) ?? false;
    }

    public void SelectGroup(string group)
    {
        var root = Objects.FirstOrDefault();
        var node = root?.Children.FirstOrDefault(c => c.Text == group);
        if (node != null)
        {
            _suppressChange = true;
            SelectedObject = node;
            SelectedGroup = group;
            _suppressChange = false;
        }
    }

    private void OnGroupSelected(object? sender, SelectionChangedEventArgs<ITreeNode> e)
    {
        if (_suppressChange) return;
        var text = e.NewValue?.Text;
        if (string.IsNullOrEmpty(text) || text == "Groups") return;

        SelectedGroup = text;
        GroupSelected?.Invoke(text);
    }
}
