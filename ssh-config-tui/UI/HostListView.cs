using System.Collections.ObjectModel;
using Terminal.Gui;

namespace SshConfigTui.UI;

public class HostListView : ListView
{
    private readonly ObservableCollection<string> _source = new();

    public HostListView()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
        SetSource(_source);
    }

    public void SetHosts(IEnumerable<string> items)
    {
        _source.Clear();
        foreach (var item in items)
            _source.Add(item);
    }
}
