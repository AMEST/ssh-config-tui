using Terminal.Gui;

namespace SshConfigTui.UI.Dialogs;

public static class DialogHelper
{
    public static void AddField(Dialog dialog, string label, TextField field, ref int y)
    {
        var lbl = new Label
        {
            X = 0,
            Y = y,
            Width = 15,
            Text = label
        };
        field.X = 15;
        field.Y = y;
        field.Width = Dim.Fill();
        dialog.Add(lbl, field);
    }
}
