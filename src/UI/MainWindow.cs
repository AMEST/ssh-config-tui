using System.Collections.ObjectModel;
using System.Diagnostics;
using SshConfigTui.Application;
using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;
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
        var entry = _appService.ConfigService.GetEffectiveConfig(hostName);
        if (entry == null) return;

        var sourceEntry = _appService.ConfigService.GetHost(hostName);

        var dialog = new Dialog
        {
            Title = $"Edit {hostName}",
            Width = 60,
            Height = 22,
        };

        var y = 0;
        var nameLabel = new Label { X = 0, Y = y, Text = $"Host: {hostName}" };
        y += 2;

        var hostNameField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.HostName ?? entry.HostName
        };
        AddField(dialog, "HostName:", hostNameField, ref y);
        y++;

        var userField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.User ?? entry.User
        };
        AddField(dialog, "User:", userField, ref y);
        y++;

        var portField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = (sourceEntry?.Port ?? entry.Port).ToString()
        };
        AddField(dialog, "Port:", portField, ref y);
        y++;

        var idFileField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.IdentityFile ?? entry.IdentityFile
        };
        AddField(dialog, "IdentityFile:", idFileField, ref y);
        y++;

        var proxyField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.ProxyJump ?? entry.ProxyJump
        };
        AddField(dialog, "ProxyJump:", proxyField, ref y);
        y++;

        var forwardAgentCheckbox = new CheckBox
        {
            X = 0, Y = y,
            Text = "ForwardAgent",
            CheckedState = (sourceEntry?.ForwardAgent ?? entry.ForwardAgent) ? CheckState.Checked : CheckState.UnChecked
        };
        dialog.Add(forwardAgentCheckbox);
        y++;

        var groupsField = new TextField
        {
            X = 15, Y = y, Width = Dim.Fill(),
            Text = sourceEntry != null ? string.Join(", ", sourceEntry.Groups) : ""
        };
        AddField(dialog, "Groups:", groupsField, ref y);
        y += 2;

        var saveBtn = new Button { X = 0, Y = y, Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var groups = (groupsField.Text ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(g => g.Length > 0)
                .ToList();

            if (sourceEntry == null)
            {
                var newEntry = new HostEntry
                {
                    Name = hostName,
                    HostName = hostNameField.Text ?? "",
                    User = userField.Text ?? "",
                    Port = int.TryParse(portField.Text, out var p) ? p : 22,
                    IdentityFile = idFileField.Text ?? "",
                    ProxyJump = proxyField.Text ?? "",
                    ForwardAgent = forwardAgentCheckbox.CheckedState == CheckState.Checked,
                    Groups = groups,
                };
                _appService.ConfigService.AddHost(newEntry);
            }
            else
            {
                sourceEntry.HostName = hostNameField.Text ?? "";
                sourceEntry.User = userField.Text ?? "";
                sourceEntry.Port = int.TryParse(portField.Text, out var p) ? p : 22;
                sourceEntry.IdentityFile = idFileField.Text ?? "";
                sourceEntry.ProxyJump = proxyField.Text ?? "";
                sourceEntry.ForwardAgent = forwardAgentCheckbox.CheckedState == CheckState.Checked;
                sourceEntry.Groups = groups;
                _appService.ConfigService.UpdateHost(sourceEntry);
            }
            _hasUnsavedChanges = true;
            _statusLabel.Text = "Changes pending. Save (Ctrl+S) to persist.";
            _groupTreeView.RefreshGroups();
            LoadHostsForGroup(_currentGroup);
            dialog.RequestStop();
        };

        var cancelBtn = new Button { X = 10, Y = y, Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => dialog.RequestStop();

        dialog.Add(saveBtn, cancelBtn);
        Terminal.Gui.Application.Run(dialog);
    }

    private static void AddField(Dialog dialog, string label, TextField field, ref int y)
    {
        var lbl = new Label
        {
            X = 0, Y = y, Width = 15, Text = label
        };
        field.X = 15;
        field.Y = y;
        field.Width = Dim.Fill();
        dialog.Add(lbl, field);
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
        var dialog = new Dialog
        {
            Title = "Add New Host",
            Width = 60,
            Height = 24,
        };

        var y = 0;
        var templates = _templateService.GetTemplates();
        var templateNames = new List<string> { "(none)" };
        templateNames.AddRange(templates.Select(t => t.Name));

        var templateLabel = new Label { X = 0, Y = y, Width = 15, Text = "Template:" };
        var templateRadio = new RadioGroup
        {
            X = 15, Y = y,
            RadioLabels = templateNames.ToArray(),
            SelectedItem = 0,
        };
        dialog.Add(templateLabel, templateRadio);
        y += templateNames.Count + 1;

        var nameField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        AddField(dialog, "Host pattern:", nameField, ref y);
        y++;

        var hostNameField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        AddField(dialog, "HostName:", hostNameField, ref y);
        y++;

        var userField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        AddField(dialog, "User:", userField, ref y);
        y++;

        var portField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "22" };
        AddField(dialog, "Port:", portField, ref y);
        y++;

        var idFileField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        AddField(dialog, "IdentityFile:", idFileField, ref y);
        y++;

        var proxyField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        AddField(dialog, "ProxyJump:", proxyField, ref y);
        y++;

        var forwardAgentCheckbox = new CheckBox { X = 0, Y = y, Text = "ForwardAgent", CheckedState = CheckState.UnChecked };
        dialog.Add(forwardAgentCheckbox);
        y++;

        var defaultGroups = (!Group.IsBuiltInGroup(_currentGroup)) ? _currentGroup : "";
        var groupsField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = defaultGroups };
        AddField(dialog, "Groups:", groupsField, ref y);
        y += 2;

        var saveBtn = new Button { X = 0, Y = y, Text = "Add" };
        saveBtn.Accepting += (_, _) =>
        {
            var name = nameField.Text ?? "";
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.ErrorQuery("Error", "Host pattern cannot be empty.", "OK");
                return;
            }

            var groups = (groupsField.Text ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(g => g.Length > 0)
                .ToList();

            HostEntry entry;
            var selectedTemplate = templateRadio.SelectedItem;
            if (selectedTemplate > 0 && selectedTemplate <= templates.Count)
            {
                entry = _templateService.ApplyTemplate(templates[selectedTemplate - 1].Name, name.Trim());
                entry.HostName = hostNameField.Text ?? "";
                if (!string.IsNullOrEmpty(userField.Text)) entry.User = userField.Text ?? "";
                if (int.TryParse(portField.Text, out var p)) entry.Port = p;
                if (!string.IsNullOrEmpty(idFileField.Text)) entry.IdentityFile = idFileField.Text ?? "";
                if (!string.IsNullOrEmpty(proxyField.Text)) entry.ProxyJump = proxyField.Text ?? "";
                entry.ForwardAgent = forwardAgentCheckbox.CheckedState == CheckState.Checked;
                entry.Groups = groups;
            }
            else
            {
                entry = new HostEntry
                {
                    Name = name.Trim(),
                    HostName = hostNameField.Text ?? "",
                    User = userField.Text ?? "",
                    Port = int.TryParse(portField.Text, out var p) ? p : 22,
                    IdentityFile = idFileField.Text ?? "",
                    ProxyJump = proxyField.Text ?? "",
                    ForwardAgent = forwardAgentCheckbox.CheckedState == CheckState.Checked,
                    Groups = groups,
                };
            }

            _appService.ConfigService.AddHost(entry);
            _hasUnsavedChanges = true;
            _statusLabel.Text = $"Host '{entry.Name}' added. Save (Ctrl+S) to persist.";
            _groupTreeView.RefreshGroups();
            LoadHostsForGroup(_currentGroup);
            dialog.RequestStop();
        };

        var cancelBtn = new Button { X = 10, Y = y, Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => dialog.RequestStop();

        dialog.Add(saveBtn, cancelBtn);
        Terminal.Gui.Application.Run(dialog);
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

        var dialog = new Dialog
        {
            Title = $"Test Connection: {hostName}",
            Width = 70,
            Height = 20,
        };

        var textView = new TextView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ReadOnly = true,
            WordWrap = false,
            Text = $"Testing ssh connection for '{hostName}'...\n\n"
        };

        var closeBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Close" };
        closeBtn.Accepting += (_, _) => dialog.RequestStop();
        dialog.Add(textView, closeBtn);

        try
        {
            var psi = new ProcessStartInfo("ssh", $"-G \"{hostName}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit(10000);
                textView.Text = output + (error.Length > 0 ? $"\n--- stderr ---\n{error}" : "");
                if (textView.Text.Length == 0)
                    textView.Text = "(no output from ssh -G)";
            }
            else
            {
                textView.Text = "Error: could not start ssh process.";
            }
        }
        catch (Exception ex)
        {
            textView.Text = $"Error: {ex.Message}";
        }

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
        var dialog = new Dialog
        {
            Title = "Import Hosts",
            Width = 70,
            Height = 20,
        };

        var textView = new TextView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            Text = "# Paste SSH config fragment here:\n"
        };

        var importBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Import" };
        var cancelBtn = new Button { X = 10, Y = Pos.AnchorEnd(1), Text = "Cancel" };

        importBtn.Accepting += (_, _) =>
        {
            var fragment = textView.Text ?? "";
            if (string.IsNullOrWhiteSpace(fragment))
            {
                MessageBox.ErrorQuery("Error", "Nothing to import.", "OK");
                return;
            }

            try
            {
                var imported = _appService.ConfigService.ImportFragment(fragment);
                _hasUnsavedChanges = true;
                _groupTreeView.RefreshGroups();
                LoadHostsForGroup(_currentGroup);
                _statusLabel.Text = $"Imported {imported} host(s). Save (Ctrl+S) to persist.";
                dialog.RequestStop();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Import failed: {ex.Message}", "OK");
            }
        };
        cancelBtn.Accepting += (_, _) => dialog.RequestStop();

        dialog.Add(textView, importBtn, cancelBtn);
        Terminal.Gui.Application.Run(dialog);
    }

    private void OnExport()
    {
        if (_hostListView.SelectedItem < 0 || _hostListView.SelectedItem >= _hostListSource.Count) return;
        var item = _hostListSource[_hostListView.SelectedItem];
        var hostName = item.TrimStart().Split(' ')[0];

        try
        {
            var fragment = _appService.ExportHost(hostName);
            if (string.IsNullOrEmpty(fragment))
            {
                MessageBox.ErrorQuery("Error", $"Host '{hostName}' not found.", "OK");
                return;
            }

            var dialog = new Dialog
            {
                Title = $"Export: {hostName}",
                Width = 70,
                Height = 20,
            };

            var textView = new TextView
            {
                X = 0, Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(2),
                ReadOnly = true,
                WordWrap = false,
                Text = fragment
            };

            var closeBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Close" };
            closeBtn.Accepting += (_, _) => dialog.RequestStop();
            dialog.Add(textView, closeBtn);
            Terminal.Gui.Application.Run(dialog);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Export failed: {ex.Message}", "OK");
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
