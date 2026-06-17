using CliWrap;
using CliWrap.Buffered;
using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class GenerateSshKeyDialog : Dialog
{
    private readonly TextField _nameField;
    private readonly RadioGroup _typeGroup;
    private readonly TextField _passwordField;
    private readonly Label _statusLabel;

    public GenerateSshKeyDialog()
    {
        Title = "Generate SSH Key";
        Width = 50;
        Height = 17;

        var y = 0;

        _nameField = new TextField { Text = "id_rsa" };
        DialogHelper.AddField(this, "Key name:", _nameField, ref y);
        y += 2;

        var typeLabel = new Label { X = 0, Y = y, Text = "Key type:" };
        _typeGroup = new RadioGroup
        {
            X = 15,
            Y = y,
            RadioLabels = ["RSA (4096 bit)", "ECDSA (256 bit)", "ED25519"],
            SelectedItem = 0,
        };
        Add(typeLabel, _typeGroup);
        y += 3;

        _passwordField = new TextField { Secret = true };
        DialogHelper.AddField(this, "Password:", _passwordField, ref y);
        y += 2;

        _statusLabel = new Label
        {
            X = 0,
            Y = y,
            Width = Dim.Fill(),
            Height = 1,
            Text = "",
        };
        Add(_statusLabel);
        y += 2;

        var generateBtn = new Button { X = 0, Y = y, Text = "Generate" };
        generateBtn.Accepting += OnGenerate;

        var closeBtn = new Button { X = 12, Y = y, Text = "Close" };
        closeBtn.Accepting += (_, _) => RequestStop();

        Add(generateBtn, closeBtn);
    }

    private async void OnGenerate(object? sender, EventArgs e)
    {
        var name = _nameField.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.ErrorQuery("Error", "Key name cannot be empty.", "OK");
            return;
        }

        if (name.Contains('/') || name.Contains('\\') || name.Contains('~'))
        {
            MessageBox.ErrorQuery("Error", "Key name must be a simple filename (no path).", "OK");
            return;
        }

        var sshDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ssh");

        var privPath = Path.Combine(sshDir, name);
        var pubPath = privPath + ".pub";

        if (File.Exists(privPath) || File.Exists(pubPath))
        {
            var result = MessageBox.Query(
                "Key Exists",
                $"A key named '{name}' already exists in ~/.ssh/.\nOverwrite?",
                1,
                "Cancel", "Overwrite");
            if (result != 1) return;
        }

        var password = _passwordField.Text ?? "";

        string keyType, args;
        switch (_typeGroup.SelectedItem)
        {
            case 0:
                keyType = "rsa";
                args = $"-t {keyType} -b 4096 -f \"{privPath}\" -N \"{password}\"";
                break;
            case 1:
                keyType = "ecdsa";
                args = $"-t {keyType} -b 256 -f \"{privPath}\" -N \"{password}\"";
                break;
            default:
                keyType = "ed25519";
                args = $"-t {keyType} -f \"{privPath}\" -N \"{password}\"";
                break;
        }

        _statusLabel.Text = "Generating key...";
        SetEnabled(false);

        try
        {

            var result = await Cli.Wrap("ssh-keygen")
                .WithArguments(args)
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            if (result.ExitCode == 0)
            {
                _statusLabel.Text = $"Key '{name}' generated successfully.";
                MessageBox.Query(
                    "Success",
                    $"SSH key '{name}' created.\n\nPrivate: ~/.ssh/{name}\nPublic:  ~/.ssh/{name}.pub",
                    0,
                    "OK");
                RequestStop();
            }
            else
            {
                var err = result.StandardError;
                _statusLabel.Text = "Generation failed.";
                MessageBox.ErrorQuery("Error", $"ssh-keygen failed:\n{err}", "OK");
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Generation failed.";
            MessageBox.ErrorQuery("Error", $"Failed to run ssh-keygen:\n{ex.Message}", "OK");
        }
        finally
        {
            SetEnabled(true);
        }
    }

    private void SetEnabled(bool enabled)
    {
        foreach (var v in new View[] { _nameField, _typeGroup, _passwordField })
        {
            v.Enabled = enabled;
        }
    }
}
