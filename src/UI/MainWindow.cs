using System.Collections.ObjectModel;
using SshConfigTui.Application;
using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;
using SshConfigTui.UI.Dialogs;
using Terminal.Gui;

namespace SshConfigTui.UI;

public class MainWindow : Window
{
    private readonly ApplicationService _appService;
    private readonly ClipboardService _clipboardService;
    private readonly SessionService _sessionService;
    private readonly TemplateService _templateService;
    private readonly DebugLogger _log;
    private readonly MenuBar _menuBar;
    private readonly FrameView _groupsFrame;
    private readonly GroupTreeView _groupTreeView;
    private readonly FrameView _hostsFrame;
    private readonly ListView _hostListView;
    private readonly ObservableCollection<string> _hostListSource = new();
    private readonly Label _statusLabel;
    private List<HostEntry> _allHosts = new();
    private List<HostEntry> _hosts = new();
    private string _currentGroup = "All";
    private bool _hasUnsavedChanges;

    public MainWindow(ApplicationService appService, ClipboardService clipboardService, SessionService sessionService, TemplateService templateService, DebugLogger log)
    {
        _appService = appService;
        _clipboardService = clipboardService;
        _sessionService = sessionService;
        _templateService = templateService;
        _log = log;
        Title = "SSH Config Manager";

        _menuBar = new MenuBar
        {
            Menus = new[]
            {
                new MenuBarItem("_File", new[]
                {
                    new MenuItem("_Save", "Save config (Ctrl+S)", OnSave, null, null, Key.S.WithCtrl),
                    new MenuItem("_Backup", "Create backup", OnBackup),
                    new MenuItem("_Import", "Import hosts from file", OnImport),
                    new MenuItem("_Export", "Export selected host", OnExport),
                    new MenuItem("_Quit", "Exit (Ctrl+Q)", OnQuit, null, null, Key.Q.WithCtrl),
                }),
                new MenuBarItem("_Edit", new[]
                {
                    new MenuItem("_Add Host", "Add new host (Ctrl+N)", OnAddHost, null, null, Key.N.WithCtrl),
                    new MenuItem("_Delete Host", "Delete selected host", OnDeleteHost, null, null, Key.Delete.WithCtrl),
                    new MenuItem("_Edit Host", "Edit selected host (Enter)", OnEditHost),
                    new MenuItem("_Global Settings", "Edit Host *", () => OnEditGlobalSettings()),
                }),
                new MenuBarItem("_View", new[]
                {
                    new MenuItem("_Refresh", "Reload config (F5)", OnRefresh, null, null, Key.F5),
                    new MenuItem("_Test Connection", "Test ssh connection (Ctrl+T)", OnTestConnection, null, null, Key.T.WithCtrl),
                    new MenuItem("_Copy SSH String", "Copy ssh command", OnCopySshString),
                }),
                new MenuBarItem("_Tools", new[]
                {
                    new MenuItem("SSH _Keys", "Browse SSH keys in ~/.ssh", OnSshKeys),
                    new MenuItem("_Generate SSH Key", "Generate a new SSH key pair", OnGenerateSshKey),
                }),
                new MenuBarItem("_Help", new[]
                {
                    new MenuItem("_About", "About ssh-config-tui", OnAbout),
                    new MenuItem("_Keybindings", "Keyboard shortcuts", OnKeybindings),
                }),
            }
        };

        _groupTreeView = new GroupTreeView(appService);
        _groupTreeView.GroupSelected += OnGroupSelected;

        _groupsFrame = new FrameView
        {
            X = 0,
            Y = 1,
            Width = Dim.Percent(30),
            Height = Dim.Fill(1),
            Title = " Groups ",
            CanFocus = true,
        };
        _groupsFrame.Add(_groupTreeView);

        _hostListView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };
        _hostListView.OpenSelectedItem += OnHostSelected;
        _hostListView.SetSource(_hostListSource);

        _hostsFrame = new FrameView
        {
            X = Pos.Percent(30),
            Y = 1,
            Width = Dim.Percent(70),
            Height = Dim.Fill(1),
            Title = " Hosts ",
            CanFocus = true,
        };
        _hostsFrame.Add(_hostListView);

        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            Text = "Loading..."
        };

        Add(_menuBar, _groupsFrame, _hostsFrame, _statusLabel);

        Initialized += (_, _) =>
        {
            _log.Write("MainWindow initialized, setting focus");
            _groupTreeView.SetFocus();
            _log.Write($"  groupTreeView.HasFocus={_groupTreeView.HasFocus}, hostListView.HasFocus={_hostListView.HasFocus}");
        };
    }

    public void Initialize()
    {
        try
        {
            _appService.Initialize();
            _allHosts = _appService.ConfigService.GetAllHosts();
            _hosts = _allHosts;
            _groupTreeView.RefreshGroups();

            var lastGroup = _sessionService.LoadLastGroup();
            if (lastGroup != null && _groupTreeView.HasGroup(lastGroup))
            {
                _currentGroup = lastGroup;
                _groupTreeView.SelectGroup(lastGroup);
                LoadHostsForGroup(lastGroup);
            }
            else
            {
                RefreshHostList();
                _statusLabel.Text = $"Config loaded ({_hosts.Count} hosts) | {_appService.GetConfigPath()}";
            }

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
        {
            var groupsStr = h.Groups.Count > 0 ? $"[{string.Join(",", h.Groups)}]" : "";
            _hostListSource.Add($"{h.Name,-25} {h.HostName,-20} {groupsStr}");
        }
    }

    private void OnGroupSelected(string group)
    {
        _currentGroup = group;
        _sessionService.SaveLastGroup(group);
        LoadHostsForGroup(group);
    }

    private void LoadHostsForGroup(string group)
    {
        _allHosts = _appService.GroupService.GetHostsByGroup(group);
        _hosts = _allHosts;
        RefreshHostList();
        _statusLabel.Text = $"Group '{group}' ({_hosts.Count} hosts) | {_appService.GetConfigPath()}";
    }

    private void OnHostSelected(object? sender, ListViewItemEventArgs args)
    {
        if (args.Value is string item)
        {
            var hostName = item.TrimStart().Split(' ')[0];
            ShowHostDetail(hostName);
        }
    }

    private void ShowHostDetail(string hostName)
    {
        var dialog = new HostDetailDialog(hostName, _appService.ConfigService, _log);
        Terminal.Gui.Application.Run(dialog);
        if (dialog.Saved)
        {
            _hasUnsavedChanges = true;
            _statusLabel.Text = "Changes pending. Save (Ctrl+S) to persist.";
            _groupTreeView.RefreshGroups();
            LoadHostsForGroup(_currentGroup);
        }
    }

    private void OnEditHost()
    {
        if (_hostListView.SelectedItem < 0 || _hostListView.SelectedItem >= _hostListSource.Count) return;
        var item = _hostListSource[_hostListView.SelectedItem];
        var hostName = item.TrimStart().Split(' ')[0];
        ShowHostDetail(hostName);
    }

    private void OnAddHost()
    {
        var dialog = new AddHostDialog(_appService.ConfigService, _templateService, _log, _currentGroup);
        Terminal.Gui.Application.Run(dialog);
        if (dialog.Saved)
        {
            _hasUnsavedChanges = true;
            _statusLabel.Text = $"Host '{dialog.AddedHostName}' added. Save (Ctrl+S) to persist.";
            _groupTreeView.RefreshGroups();
            LoadHostsForGroup(_currentGroup);
        }
    }

    private void OnDeleteHost()
    {
        if (_hostListView.SelectedItem < 0 || _hostListView.SelectedItem >= _hostListSource.Count) return;
        var item = _hostListSource[_hostListView.SelectedItem];
        var hostName = item.TrimStart().Split(' ')[0];

        var result = MessageBox.Query(
            "Delete Host",
            $"Are you sure you want to delete '{hostName}'?",
            1,
            "Cancel", "Delete");
        if (result != 1) return;

        _appService.ConfigService.DeleteHost(hostName);
        _hasUnsavedChanges = true;
        _statusLabel.Text = $"Host '{hostName}' deleted. Save (Ctrl+S) to persist.";
        LoadHostsForGroup(_currentGroup);
    }

    private void OnEditGlobalSettings()
    {
        var global = _appService.ConfigService.GetGlobalConfig();
        if (global == null)
        {
            MessageBox.Query("Global Settings", "No global config (Host *) found.", 0, "OK");
            return;
        }

        ShowHostDetail("*");
    }

    private void OnBackup()
    {
        try
        {
            _appService.Repository.CreateBackup();
            _statusLabel.Text = $"Backup created: ~/.ssh/config.backup";
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Backup failed: {ex.Message}", "OK");
        }
    }

    private void OnSave()
    {
        try
        {
            _appService.SaveAll();
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
            _appService.Initialize();
            _groupTreeView.RefreshGroups();
            LoadHostsForGroup(_currentGroup);
            _statusLabel.Text = $"Config reloaded ({_hosts.Count} hosts)";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Error reloading: {ex.Message}";
        }
    }

    private static void OnSshKeys()
    {
        var dialog = new SshKeyPickerDialog();
        Terminal.Gui.Application.Run(dialog);
    }

    private static void OnGenerateSshKey()
    {
        var dialog = new GenerateSshKeyDialog();
        Terminal.Gui.Application.Run(dialog);
    }

    private static void OnAbout()
    {
        MessageBox.Query(
            "About",
            "SSH Config Manager v1.0 by AMEST\n\nA TUI editor for ~/.ssh/config\n\nBuilt with Terminal.Gui 2.0",
            0, "OK");
    }

    private static void OnKeybindings()
    {
        MessageBox.Query(
            "Keybindings",
            "Ctrl+S  Save\nCtrl+Q  Quit\nCtrl+N  Add host\nCtrl+T  Test connection\n" +
            "F5      Refresh\nAlt+F/E/V/H  Menus\nTab/Shift+Tab  Switch panel\n" +
            "Enter   Edit host\nDel     Delete host\n↑/↓     Navigate lists",
            0, "OK");
    }

    private void OnTestConnection()
    {
        if (_hostListView.SelectedItem < 0 || _hostListView.SelectedItem >= _hostListSource.Count) return;
        var item = _hostListSource[_hostListView.SelectedItem];
        var hostName = item.TrimStart().Split(' ')[0];
        var dialog = new TestConnectionDialog(hostName);
        Terminal.Gui.Application.Run(dialog);
    }

    private void OnCopySshString()
    {
        if (_hostListView.SelectedItem < 0 || _hostListView.SelectedItem >= _hostListSource.Count) return;
        var item = _hostListSource[_hostListView.SelectedItem];
        var hostName = item.TrimStart().Split(' ')[0];
        var sshStr = $"ssh {hostName}";
        _clipboardService.Copy(sshStr);
        _statusLabel.Text = $"Copied '{sshStr}' to clipboard.";
    }

    private void OnImport()
    {
        var dialog = new ImportDialog(_appService.ConfigService, _log);
        Terminal.Gui.Application.Run(dialog);
        if (dialog.Saved)
        {
            _hasUnsavedChanges = true;
            _groupTreeView.RefreshGroups();
            LoadHostsForGroup(_currentGroup);
            _statusLabel.Text = $"Imported {dialog.ImportedCount} host(s). Save (Ctrl+S) to persist.";
        }
    }

    private void OnExport()
    {
        if (_hostListView.SelectedItem < 0 || _hostListView.SelectedItem >= _hostListSource.Count) return;
        var item = _hostListSource[_hostListView.SelectedItem];
        var hostName = item.TrimStart().Split(' ')[0];

        var fragment = _appService.ExportHost(hostName);
        if (string.IsNullOrEmpty(fragment))
        {
            MessageBox.ErrorQuery("Error", $"Host '{hostName}' not found.", "OK");
            return;
        }

        var dialog = new ExportDialog(hostName, fragment);
        Terminal.Gui.Application.Run(dialog);
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
        _log.Write($"OnKeyDown: key={keyEvent}, groupFocus={_groupTreeView.HasFocus}, hostFocus={_hostListView.HasFocus}, groupsFrameFocus={_groupsFrame.HasFocus}, hostsFrameFocus={_hostsFrame.HasFocus}");

        if (keyEvent == Key.Q.WithCtrl || keyEvent == Key.C.WithCtrl)
        {
            OnQuit();
            return true;
        }
        if (keyEvent == Key.T.WithCtrl)
        {
            OnTestConnection();
            return true;
        }
        if (keyEvent == Key.Tab)
        {
            if (_hostListView.HasFocus || _hostsFrame.HasFocus)
                _groupTreeView.SetFocus();
            else
                _hostListView.SetFocus();
            _log.Write($"  after Tab: groupFocus={_groupTreeView.HasFocus}, hostFocus={_hostListView.HasFocus}");
            return true;
        }
        if (keyEvent == Key.Tab.WithShift)
        {
            if (_groupTreeView.HasFocus || _groupsFrame.HasFocus)
                _hostListView.SetFocus();
            else
                _groupTreeView.SetFocus();
            _log.Write($"  after Shift+Tab: groupFocus={_groupTreeView.HasFocus}, hostFocus={_hostListView.HasFocus}");
            return true;
        }
        var handled = base.OnKeyDown(keyEvent);
        _log.Write($"  unhandled, base returned {handled}");
        return handled;
    }
}
