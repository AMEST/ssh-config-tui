using SshConfigTui.Application;
using SshConfigTui.Domain;
using Terminal.Gui;

namespace SshConfigTui.UI;

public class HostDetailView : View
{
    private readonly ConfigService _configService;
    private readonly Dictionary<string, TextField> _fields = new();
    private HostEntry? _currentEntry;

    public HostDetailView(ConfigService configService)
    {
        _configService = configService;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    public void ShowHost(HostEntry entry)
    {
        _currentEntry = entry;
        ClearFields();

        var y = 0;
        AddField("HostName", entry.HostName, ref y);
        AddField("User", entry.User, ref y);
        AddField("Port", entry.Port.ToString(), ref y);
        AddField("IdentityFile", entry.IdentityFile, ref y);
        AddField("ProxyJump", entry.ProxyJump, ref y);

        var saveBtn = new Button
        {
            X = 0,
            Y = y + 1,
            Text = "Save"
        };
        saveBtn.Accepting += OnSave;
        Add(saveBtn);
    }

    private void AddField(string label, string value, ref int y)
    {
        var lbl = new Label
        {
            X = 0,
            Y = y,
            Width = 15,
            Text = label + ":"
        };
        var field = new TextField
        {
            X = 16,
            Y = y,
            Width = (Dim.Fill() ?? 0) - 2,
            Text = value
        };
        _fields[label] = field;
        Add(lbl, field);
        y += 1;
    }

    private void ClearFields()
    {
        foreach (var f in _fields.Values)
            Remove(f);
        _fields.Clear();
    }

    private void OnSave(object? sender, CommandEventArgs args)
    {
        if (_currentEntry == null) return;

        _currentEntry.HostName = _fields.GetValueOrDefault("HostName")?.Text ?? string.Empty;
        _currentEntry.User = _fields.GetValueOrDefault("User")?.Text ?? string.Empty;
        int.TryParse(_fields.GetValueOrDefault("Port")?.Text ?? "22", out var port);
        _currentEntry.Port = port;
        _currentEntry.IdentityFile = _fields.GetValueOrDefault("IdentityFile")?.Text ?? string.Empty;
        _currentEntry.ProxyJump = _fields.GetValueOrDefault("ProxyJump")?.Text ?? string.Empty;

        _configService.UpdateHost(_currentEntry);
    }
}
