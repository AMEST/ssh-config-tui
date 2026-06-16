using System.Collections.ObjectModel;
using SshConfigTui.Application;
using SshConfigTui.Domain;
using Terminal.Gui;

namespace SshConfigTui.UI;

public class MainWindow : Window
{
    private readonly ApplicationService _appService;
    private readonly MenuBar _menuBar;
    private readonly ListView _hostListView;
    private readonly ObservableCollection<string> _hostListSource = new();
    private readonly Label _statusLabel;
    private List<HostEntry> _hosts = new();
    private bool _hasUnsavedChanges;

    public MainWindow(ApplicationService appService)
    {
        _appService = appService;
        Title = "SSH Config Manager";

        _menuBar = new MenuBar
        {
            Menus = new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Save", "Save config (Ctrl+S)", OnSave, null, null, Key.S.WithCtrl),
                    new MenuItem("_Backup", "Create backup", () => { }),
                    new MenuItem("_Quit", "Exit (Ctrl+Q)", OnQuit, null, null, Key.Q.WithCtrl),
                }),
                new MenuBarItem("_Edit", new[]
                {
                    new MenuItem("_Add Host", "Add new host", () => { }, null, null, Key.N.WithCtrl),
                    new MenuItem("_Delete Host", "Delete selected host", () => { }, null, null, Key.Delete.WithCtrl),
                    new MenuItem("_Global Settings", "Edit Host *", () => { }),
                }),
                new MenuBarItem("_View", new[]
                {
                    new MenuItem("_Refresh", "Reload config (F5)", OnRefresh, null, null, Key.F5),
                }),
                new MenuBarItem("_Help", new[]
                {
                    new MenuItem("_About", "About ssh-config-tui", () => { }),
                    new MenuItem("_Keybindings", "Keyboard shortcuts", () => { }),
                }),
            }
        };

        _hostListView = new ListView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = (Dim.Fill() ?? 0) - 1,
            CanFocus = true,
        };
        _hostListView.OpenSelectedItem += OnHostSelected;
        _hostListView.SetSource(_hostListSource);

        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = "Loading..."
        };

        Add(_menuBar, _hostListView, _statusLabel);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _appService.InitializeAsync();
            _hosts = _appService.ConfigService.GetAllHosts();
            RefreshHostList();
            _statusLabel.Text = $"Config loaded ({_hosts.Count} hosts) | {_appService.GetConfigPath()}";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error loading config: {ex.Message}";
        }
    }

    private void RefreshHostList()
    {
        _hostListSource.Clear();
        foreach (var h in _hosts)
            _hostListSource.Add($"{h.Name,-30} {h.HostName}");
    }

    private void OnHostSelected(object? sender, ListViewItemEventArgs args)
    {
        if (args.Value is string item)
        {
            var hostName = item.Split(' ')[0];
            var entry = _appService.ConfigService.GetEffectiveConfig(hostName);
            if (entry != null)
            {
                MessageBox.Query(
                    "Host Details",
                    $"{entry.Name}\n" +
                    $"HostName: {entry.HostName}\n" +
                    $"User: {entry.User}\n" +
                    $"Port: {entry.Port}\n" +
                    $"IdentityFile: {entry.IdentityFile}\n" +
                    $"ProxyJump: {entry.ProxyJump}",
                    0,
                    "OK");
            }
        }
    }

    private void OnSave()
    {
        try
        {
            _appService.SaveAllAsync().GetAwaiter().GetResult();
            _hasUnsavedChanges = false;
            _statusLabel.Text = "Config saved.";
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to save: {ex.Message}", "OK");
        }
    }

    private void OnRefresh()
    {
        try
        {
            _appService.InitializeAsync().GetAwaiter().GetResult();
            _hosts = _appService.ConfigService.GetAllHosts();
            RefreshHostList();
            _statusLabel.Text = $"Config reloaded ({_hosts.Count} hosts)";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error reloading: {ex.Message}";
        }
    }

    private void OnQuit()
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Query(
                "Unsaved Changes",
                "You have unsaved changes. Exit anyway?",
                1,
                "Cancel", "Exit");
            if (result != 1) return;
        }
        Terminal.Gui.Application.RequestStop();
    }

    protected override bool OnKeyDown(Key keyEvent)
    {
        if (keyEvent == Key.Q.WithCtrl)
        {
            OnQuit();
            return true;
        }
        return base.OnKeyDown(keyEvent);
    }
}
