using SshConfigTui.Application;
using SshConfigTui.Domain;
using SshConfigTui.Infrastructure;
using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class HostDetailDialog : Dialog
{
    public bool Saved { get; private set; }

    public HostDetailDialog(string hostName, ConfigService configService, DebugLogger log) : this(hostName, configService, null, log)
    {
    }

    public HostDetailDialog(string hostName, ConfigService configService, HostEntry? prefillEntry, DebugLogger log)
    {
        Title = $"Edit {hostName}";
        Width = 60;
        Height = 22;

        var entry = configService.GetEffectiveConfig(hostName);
        var sourceEntry = configService.GetHost(hostName) ?? prefillEntry;

        var y = 0;
        var nameLabel = new Label { X = 0, Y = y, Text = $"Host: {hostName}" };
        y += 2;

        var hostNameField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.HostName ?? entry?.HostName ?? ""
        };
        DialogHelper.AddField(this, "HostName:", hostNameField, ref y);
        y++;

        var userField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.User ?? entry?.User ?? ""
        };
        DialogHelper.AddField(this, "User:", userField, ref y);
        y++;

        var portField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = (sourceEntry?.Port ?? entry?.Port ?? 22).ToString()
        };
        DialogHelper.AddField(this, "Port:", portField, ref y);
        y++;

        var idFileField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.IdentityFile ?? entry?.IdentityFile ?? ""
        };
        DialogHelper.AddField(this, "IdentityFile:", idFileField, ref y);
        y++;

        var proxyField = new TextField
        {
            X = 0, Y = y, Width = Dim.Fill(),
            Text = sourceEntry?.ProxyJump ?? entry?.ProxyJump ?? ""
        };
        DialogHelper.AddField(this, "ProxyJump:", proxyField, ref y);
        y++;

        var forwardAgentCheckbox = new CheckBox
        {
            X = 0, Y = y,
            Text = "ForwardAgent",
            CheckedState = (sourceEntry?.ForwardAgent ?? entry?.ForwardAgent ?? false) ? CheckState.Checked : CheckState.UnChecked
        };
        Add(forwardAgentCheckbox);
        y++;

        var groupsField = new TextField
        {
            X = 15, Y = y, Width = Dim.Fill(),
            Text = sourceEntry != null ? string.Join(", ", sourceEntry.Groups) : (prefillEntry != null ? string.Join(", ", prefillEntry.Groups) : "")
        };
        DialogHelper.AddField(this, "Groups:", groupsField, ref y);
        y += 2;

        var saveBtn = new Button { X = 0, Y = y, Text = "Save" };
        saveBtn.Accepting += (_, _) =>
        {
            var groups = (groupsField.Text ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(g => g.Length > 0)
                .ToList();

            var updatedEntry = new HostEntry
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

            if (configService.GetHost(hostName) == null)
                configService.AddHost(updatedEntry);
            else
                configService.UpdateHost(updatedEntry);

            Saved = true;
            log.Write($"HostDetailDialog: saved '{hostName}'");
            RequestStop();
        };

        var cancelBtn = new Button { X = 10, Y = y, Text = "Cancel" };
        cancelBtn.Accepting += (_, _) => RequestStop();

        Add(saveBtn, cancelBtn);
    }
}
