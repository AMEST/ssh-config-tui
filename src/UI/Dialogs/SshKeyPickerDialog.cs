using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class SshKeyPickerDialog : Dialog
{
    public string? SelectedKey { get; private set; }

    public SshKeyPickerDialog(bool pickerMode = false)
    {
        Title = "SSH Keys";
        Width = 50;
        Height = 20;

        var sshDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh");

        var keys = new List<string>();
        try
        {
            if (Directory.Exists(sshDir))
            {
                foreach (var f in Directory.GetFiles(sshDir).OrderBy(f => Path.GetFileName(f)))
                {
                    try
                    {
                        var firstLine = File.ReadLines(f).FirstOrDefault() ?? "";
                        if (firstLine.Contains("PRIVATE KEY--", StringComparison.Ordinal))
                        {
                            keys.Add(Path.GetFileName(f));
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }

        var listView = new ListView
        {
            X = 0, Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(pickerMode ? 3 : 2),
        };

        if (keys.Count > 0)
        {
            listView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string>(keys));
        }
        else
        {
            listView.SetSource(new System.Collections.ObjectModel.ObservableCollection<string> { "(no SSH keys found)" });
        }

        Add(listView);

        if (pickerMode)
        {
            var selectBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Select" };
            selectBtn.Accepting += (_, _) =>
            {
                if (keys.Count > 0 && listView.SelectedItem >= 0 && listView.SelectedItem < keys.Count)
                {
                    SelectedKey = $"~/.ssh/{keys[listView.SelectedItem]}";
                    RequestStop();
                }
            };

            var cancelBtn = new Button { X = 10, Y = Pos.AnchorEnd(1), Text = "Cancel" };
            cancelBtn.Accepting += (_, _) => RequestStop();

            Add(selectBtn, cancelBtn);
        }
        else
        {
            var closeBtn = new Button { X = 0, Y = Pos.AnchorEnd(1), Text = "Close" };
            closeBtn.Accepting += (_, _) => RequestStop();
            Add(closeBtn);
        }
    }
}
