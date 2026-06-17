using SshConfigTui.Application;
using SshConfigTui.Infrastructure;
using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class ImportDialog : Dialog
{
    public bool Saved { get; private set; }
    public int ImportedCount { get; private set; }

    public ImportDialog(ConfigService configService, DebugLogger log)
    {
        Title = "Import Hosts";
        Width = 70;
        Height = 20;

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
                ImportedCount = configService.ImportFragment(fragment);
                Saved = true;
                log.Write($"ImportDialog: imported {ImportedCount} hosts");
                RequestStop();
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery("Error", $"Import failed: {ex.Message}", "OK");
            }
        };

        cancelBtn.Accepting += (_, _) => RequestStop();

        Add(textView, importBtn, cancelBtn);
    }
}
