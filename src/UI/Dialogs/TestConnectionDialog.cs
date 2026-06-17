using System.Diagnostics;
using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class TestConnectionDialog : Dialog
{
    public TestConnectionDialog(string hostName)
    {
        Title = $"Test Connection: {hostName}";
        Width = 70;
        Height = 20;

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
        closeBtn.Accepting += (_, _) => RequestStop();
        Add(textView, closeBtn);

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
    }
}
