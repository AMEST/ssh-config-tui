using SshConfigTui.Application;
using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;
using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class AddHostDialog : Dialog
{
    public bool Saved { get; private set; }
    public string? AddedHostName { get; private set; }

    public AddHostDialog(ConfigService configService, TemplateService templateService, DebugLogger log, string currentGroup)
    {
        Title = "Add New Host";
        Width = 60;
        Height = 24;

        var templates = templateService.GetTemplates();
        var templateNames = new List<string> { "(none)" };
        templateNames.AddRange(templates.Select(t => t.Name));

        var y = 0;
        var templateLabel = new Label { X = 0, Y = y, Width = 15, Text = "Template:" };
        var templateRadio = new RadioGroup
        {
            X = 15, Y = y,
            RadioLabels = templateNames.ToArray(),
            SelectedItem = 0,
        };
        Add(templateLabel, templateRadio);
        y += templateNames.Count + 1;

        var nameField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        DialogHelper.AddField(this, "Host pattern:", nameField, ref y);
        y++;

        var hostNameField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        DialogHelper.AddField(this, "HostName:", hostNameField, ref y);
        y++;

        var userField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        DialogHelper.AddField(this, "User:", userField, ref y);
        y++;

        var portField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "22" };
        DialogHelper.AddField(this, "Port:", portField, ref y);
        y++;

        var idFileField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        DialogHelper.AddField(this, "IdentityFile:", idFileField, ref y);
        y++;

        var proxyField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = "" };
        DialogHelper.AddField(this, "ProxyJump:", proxyField, ref y);
        y++;

        var forwardAgentCheckbox = new CheckBox { X = 0, Y = y, Text = "ForwardAgent", CheckedState = CheckState.UnChecked };
        Add(forwardAgentCheckbox);
        y++;

        var defaultGroups = !Group.IsBuiltInGroup(currentGroup) ? currentGroup : "";
        var groupsField = new TextField { X = 15, Y = y, Width = Dim.Fill(), Text = defaultGroups };
        DialogHelper.AddField(this, "Groups:", groupsField, ref y);
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
                entry = templateService.ApplyTemplate(templates[selectedTemplate - 1].Name, name.Trim());
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

            configService.AddHost(entry);
            Saved = true;
            AddedHostName = entry.Name;
            log.Write($"AddHostDialog: added '{entry.Name}'");
            RequestStop();
        };

        var cancelBtn = new Button { X = 10, Y = y, Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => RequestStop();

        Add(saveBtn, cancelBtn);
    }
}
