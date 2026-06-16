using System.Collections.ObjectModel;
using SshConfigTui.Application;
using Terminal.Gui;

namespace SshConfigTui.UI;

public class GroupTreeView : TreeView
{
    private readonly ApplicationService _appService;
    private readonly ListView _hostListView;
    private readonly ObservableCollection<string> _hostListSource = new();

    public GroupTreeView(ApplicationService appService, ListView hostListView)
    {
        _appService = appService;
        _hostListView = hostListView;
        _hostListView.SetSource(_hostListSource);

        AspectGetter = (ITreeNode node) => node.Text ?? "";
        SelectionChanged += OnGroupSelected;
    }

    public void RefreshGroups()
    {
        var groups = _appService.GroupService.GetGroups();
        ClearObjects();
        var root = new TreeNode("Groups");
        foreach (var g in groups)
            root.Children.Add(new TreeNode(g));
        AddObject(root);
        ExpandAll();
    }

    private void OnGroupSelected(object? sender, SelectionChangedEventArgs<ITreeNode> e)
    {
        var text = e.NewValue?.Text;
        if (string.IsNullOrEmpty(text)) return;

        var hosts = _appService.GroupService.GetHostsByGroup(text);
        _hostListSource.Clear();
        foreach (var h in hosts)
            _hostListSource.Add($"{h.Name,-30} {h.HostName}");
    }
}
