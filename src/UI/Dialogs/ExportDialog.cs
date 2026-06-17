using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public class ExportDialog : Dialog
{
    public ExportDialog(string hostName, string fragment)
    {
        Title = $"Export: {hostName}";
        Width = 70;
        Height = 20;

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
        closeBtn.Accepting += (_, _) => RequestStop();
        Add(textView, closeBtn);
    }
}
